namespace MorphNGo.Mapping.Core;

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Builds compiled expression trees for efficient property-to-property mapping
/// for simple type mappings that don't require custom logic or transformations.
/// </summary>
internal sealed class CompiledCopierBuilder
{
    private readonly Func<Type, Type, ITypeMapping?> _findMapping;
    private readonly Dictionary<(Type Source, Type Destination), Action<object, object>> _cache = [];
    private readonly HashSet<(Type Source, Type Destination)> _building = [];
    private static readonly BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance;
    private static readonly MethodInfo CloneStringEnumerableToListMethodInfo =
        typeof(CompiledCopierBuilder).GetMethod(nameof(CloneStringEnumerableToList), BindingFlags.NonPublic | BindingFlags.Static)!;

    private class PropertyMappingContext
    {
        public required Type SourceType { get; init; }
        public required Type DestinationType { get; init; }
        public required Expression SourceRead { get; init; }
        public required PropertyInfo SourceProperty { get; init; }
        public required ParameterExpression DestinationVariable { get; init; }
        public required PropertyInfo DestinationProperty { get; init; }
        public required List<Expression> ExpressionBody { get; init; }
    }

    public CompiledCopierBuilder(Func<Type, Type, ITypeMapping?> findMapping)
    {
        _findMapping = findMapping;
    }

    public ITypeMapping? FindMapping(Type sourceType, Type destinationType) =>
        _findMapping(sourceType, destinationType);

    public Action<object, object>? Build(ITypeMapping mapping)
    {
        var key = (mapping.SourceType, mapping.DestinationType);
        if (_cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        if (!IsMappingEligibleForSimpleCopier(mapping))
        {
            return null;
        }

        if (!_building.Add(key))
        {
            return null;
        }

        try
        {
            var sourceType = mapping.SourceType;
            var destType = mapping.DestinationType;
            var destProps = destType.GetProperties(PropertyBindingFlags);
            var body = new List<Expression>();
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var destParam = Expression.Parameter(typeof(object), "dest");
            var sourceVar = Expression.Variable(sourceType, "src");
            var destVar = Expression.Variable(destType, "dst");

            body.Add(Expression.Assign(sourceVar, Expression.Convert(sourceParam, sourceType)));
            body.Add(Expression.Assign(destVar, Expression.Convert(destParam, destType)));

            if (!destProps.All(destProp => TryAppendSimpleCopierPropertyExpressions(
                    this, sourceType, sourceVar, destVar, destProp, body)))
            {
                return null;
            }

            body.Add(Expression.Empty());
            var block = Expression.Block([sourceVar, destVar], body);
            var copier = Expression.Lambda<Action<object, object>>(block, sourceParam, destParam).Compile();
            _cache[key] = copier;
            return copier;
        }
        finally
        {
            _building.Remove(key);
        }
    }

    private static bool IsMappingEligibleForSimpleCopier(ITypeMapping mapping) =>
        mapping.PropertyMappings.Count == 0 &&
        mapping.ValueTransformers.Count == 0 &&
        mapping.IgnoredProperties.Count == 0 &&
        mapping.PreMappingCondition == null &&
        mapping.CustomMapFunction == null;

    private static List<string>? CloneStringEnumerableToList(IEnumerable<string>? items) =>
        items == null ? null : [.. items];

    private static bool TryAppendSimpleCopierPropertyExpressions(
        CompiledCopierBuilder builder,
        Type sourceType,
        ParameterExpression sourceVar,
        ParameterExpression destVar,
        PropertyInfo destProp,
        List<Expression> body)
    {
        if (!destProp.CanWrite)
        {
            return true;
        }

        var srcProp = sourceType.GetProperty(destProp.Name, PropertyBindingFlags);
        if (srcProp == null || !srcProp.CanRead)
        {
            return true;
        }

        var srcT = srcProp.PropertyType;
        var dstT = destProp.PropertyType;
        var read = Expression.Property(sourceVar, srcProp);

        // Handle string collection cloning
        if (TryHandleStringCollectionMapping(dstT, srcT, read, destVar, destProp, body))
        {
            return true;
        }

        // Reject if either type is a non-string collection
        if (IsNonStringCollection(srcT) || IsNonStringCollection(dstT))
        {
            return false;
        }

        // Handle complex type mapping (nested objects)
        if (IsComplexType(srcT))
        {
            var context = new PropertyMappingContext
            {
                SourceType = srcT,
                DestinationType = dstT,
                SourceRead = read,
                SourceProperty = srcProp,
                DestinationVariable = destVar,
                DestinationProperty = destProp,
                ExpressionBody = body
            };
            return TryHandleComplexTypeMapping(builder, context);
        }

        // Handle simple type assignment
        if (dstT != srcT && !dstT.IsAssignableFrom(srcT))
        {
            return false;
        }

        return AppendSimpleAssign(destVar, destProp, read, srcT, dstT, body);
    }

    private static bool TryHandleStringCollectionMapping(
        Type dstT,
        Type srcT,
        Expression read,
        ParameterExpression destVar,
        PropertyInfo destProp,
        List<Expression> body)
    {
        if (!IsNonStringCollection(srcT) ||
            !IsNonStringCollection(dstT) ||
            GetCollectionItemType(srcT) != typeof(string) ||
            GetCollectionItemType(dstT) != typeof(string) ||
            !dstT.IsAssignableFrom(typeof(List<string>)))
        {
            return false;
        }

        var cloneCall = Expression.Call(
            CloneStringEnumerableToListMethodInfo,
            Expression.Convert(read, typeof(IEnumerable<string>)));
        Expression rhs = dstT == typeof(List<string>)
            ? (Expression)cloneCall
            : Expression.Convert(cloneCall, dstT);
        body.Add(Expression.IfThen(
            Expression.NotEqual(read, Expression.Constant(null, srcT)),
            Expression.Assign(Expression.Property(destVar, destProp), rhs)));
        return true;
    }

    private static bool TryHandleComplexTypeMapping(
        CompiledCopierBuilder builder,
        PropertyMappingContext context)
    {
        var nestedMap = builder.FindMapping(context.SourceType, context.DestinationType);
        if (nestedMap != null && IsMappingEligibleForSimpleCopier(nestedMap))
        {
            var nestedCopier = builder.Build(nestedMap);
            if (nestedCopier == null)
            {
                return false;
            }

            var childDest = Expression.Variable(context.DestinationType, "nested");
            var invokeNested = Expression.Invoke(
                Expression.Constant(nestedCopier),
                Expression.Convert(context.SourceRead, typeof(object)),
                Expression.Convert(childDest, typeof(object)));

            var innerBlock = Expression.Block(
                new[] { childDest },
                Expression.Assign(childDest, Expression.New(context.DestinationType)),
                invokeNested,
                Expression.Assign(Expression.Property(context.DestinationVariable, context.DestinationProperty), childDest));

            if (!context.SourceType.IsValueType || Nullable.GetUnderlyingType(context.SourceType) != null)
            {
                context.ExpressionBody.Add(Expression.IfThen(
                    Expression.NotEqual(context.SourceRead, Expression.Constant(null, context.SourceType)),
                    innerBlock));
            }
            else
            {
                context.ExpressionBody.Add(innerBlock);
            }

            return true;
        }

        if (!context.DestinationType.IsAssignableFrom(context.SourceType))
        {
            return false;
        }

        return AppendSimpleAssign(context.DestinationVariable, context.DestinationProperty, context.SourceRead, context.SourceType, context.DestinationType, context.ExpressionBody);
    }

    private static bool AppendSimpleAssign(
        ParameterExpression destVar,
        PropertyInfo destProp,
        Expression read,
        Type srcT,
        Type dstT,
        List<Expression> body)
    {
        var assign = BuildSimplePropertyAssign(destVar, destProp, read, srcT, dstT);
        if (!srcT.IsValueType || Nullable.GetUnderlyingType(srcT) != null)
        {
            body.Add(Expression.IfThen(
                Expression.NotEqual(read, Expression.Constant(null, srcT)),
                assign));
        }
        else
        {
            body.Add(assign);
        }

        return true;
    }

    private static BinaryExpression BuildSimplePropertyAssign(
        ParameterExpression destVar,
        PropertyInfo destProp,
        Expression read,
        Type sourcePropertyType,
        Type destPropertyType)
    {
        Expression rhs = read;
        if (sourcePropertyType != destPropertyType)
        {
            rhs = Expression.Convert(read, destPropertyType);
        }

        return Expression.Assign(Expression.Property(destVar, destProp), rhs);
    }

    private static bool IsNonStringCollection(Type type) =>
        type != typeof(string) && IsCollection(type);

    private static bool IsCollection(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return true;
        }

        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return true;
        }

        return type.IsArray;
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsValueType && type != typeof(string);
    }

    private static Type? GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            return collectionType.GetGenericArguments().FirstOrDefault();
        }

        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        return null;
    }
}
