namespace MorphNGo.Mapping.Core;

using Microsoft.Extensions.Logging;
using MorphNGo.Mapping.Interfaces;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq.Expressions;

/// <summary>
/// The core mapper that performs type-to-type mapping operations.
/// Provides reflection-based property mapping with support for nested objects,
/// collections, conditional mapping, and custom transformation logic.
/// </summary>
internal class Mapper : IMapper
{
    private readonly ILogger _logger;
    private readonly Dictionary<(Type Source, Type Destination), ITypeMapping> _mappingByPair;
    private readonly Dictionary<(Type Source, Type Destination), Action<object, object>> _simpleCopiers = new();
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _destinationPropertiesByType = new();
    private readonly ConcurrentDictionary<(Type SourceType, string Name), PropertyInfo?> _sourcePropertyByTypeAndName = new();
    private readonly ConcurrentDictionary<(Type, string), Func<object, object?>> _compiledPropertyGetters = new();
    private readonly ConcurrentDictionary<(Type, string), Action<object, object?>> _compiledPropertySetters = new();
    private readonly ConcurrentDictionary<Type, MethodInfo> _invokeMethodByDelegateType = new();
    private readonly ConcurrentDictionary<Type, Func<object>> _destinationFactories = new();
    private static readonly ConcurrentDictionary<Type, bool> IsCollectionByType = new();
    private static readonly BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance;

    public Mapper(IReadOnlyList<ITypeMapping> typeMappings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(typeMappings);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        _mappingByPair = new Dictionary<(Type Source, Type Destination), ITypeMapping>(capacity: typeMappings.Count);
        foreach (var m in typeMappings)
        {
            var key = (m.SourceType, m.DestinationType);
            if (!_mappingByPair.ContainsKey(key))
            {
                _mappingByPair[key] = m;
            }
        }

        var compiledBuilder = new CompiledCopierBuilder(FindMapping);
        foreach (var m in _mappingByPair.Values)
        {
            var copier = compiledBuilder.Build(m);
            if (copier != null)
            {
                _simpleCopiers[(m.SourceType, m.DestinationType)] = copier;
            }
        }
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        return Map<TDestination>(source, []);
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var destinationType = typeof(TDestination);
        if (source == null)
        {
            return (TDestination)CreateDestinationInstance(destinationType);
        }

        var result = Map(source, destinationType, parameters);
        return (TDestination)result;
    }

    /// <inheritdoc />
    public object Map(object source, Type destinationType)
    {
        return Map(source, destinationType, []);
    }

    /// <inheritdoc />
    public object Map(object source, Type destinationType, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        ArgumentNullException.ThrowIfNull(parameters);

        if (source == null)
        {
            return CreateDestinationInstance(destinationType);
        }

        var sourceType = source.GetType();
        var mapping = FindMapping(sourceType, destinationType);

        if (mapping == null)
        {
            _logger.LogError("No mapping configured from {SourceType} to {DestinationType}", sourceType.Name, destinationType.Name);
            throw new InvalidOperationException(
                $"No mapping configured from {sourceType.Name} to {destinationType.Name}");
        }

        return MapUsingResolvedMapping(source, destinationType, parameters, mapping);
    }

    /// <inheritdoc />
    public TDestination MapTo<TDestination>(object source, TDestination destination)
    {
        return MapTo(source, destination, []);
    }

    /// <inheritdoc />
    public TDestination MapTo<TDestination>(object source, TDestination destination, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(parameters);

        var sourceType = source.GetType();
        var destinationType = typeof(TDestination);
        var mapping = FindMapping(sourceType, destinationType) ?? throw new InvalidOperationException(
            $"No mapping configured from {sourceType.Name} to {destinationType.Name}");

        if (mapping.PreMappingCondition != null && !InvokeCondition(mapping.PreMappingCondition, source))
        {
            _logger.LogWarning("Pre-mapping condition failed for {SourceType} to {DestinationType}", sourceType.Name, destinationType.Name);
            throw new InvalidOperationException(
                $"Pre-mapping condition failed for {sourceType.Name} to {destinationType.Name}");
        }

        if (mapping.CustomMapFunction != null)
        {
            _logger.LogDebug("Using custom map function for MapTo {SourceType} to {DestinationType}", sourceType.Name, destinationType.Name);
            return (TDestination)InvokeMapFunction(mapping.CustomMapFunction, source, destination!);
        }

        ApplyPropertyMappings(source, destination!, mapping, parameters);
        return destination;
    }

    /// <inheritdoc />
    public IEnumerable<TDestination> MapCollection<TDestination>(IEnumerable<object> source)
    {
        return MapCollection<TDestination>(source, Array.Empty<object>());
    }

    /// <inheritdoc />
    public IEnumerable<TDestination> MapCollection<TDestination>(IEnumerable<object> source, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(parameters);
        return source.Select(s => Map<TDestination>(s, parameters)).ToList();
    }

    /// <summary>
    /// Maps using an already-resolved <see cref="ITypeMapping"/> (avoids a second lookup for nested and collection item mapping).
    /// </summary>
    private object MapUsingResolvedMapping(object source, Type destinationType, object?[] parameters, ITypeMapping mapping)
    {
        var sourceRuntimeType = source.GetType();

        if (mapping.PreMappingCondition != null && !InvokeCondition(mapping.PreMappingCondition, source))
        {
            _logger.LogWarning("Pre-mapping condition failed for {SourceType} to {DestinationType}", sourceRuntimeType.Name, destinationType.Name);
            throw new InvalidOperationException(
                $"Pre-mapping condition failed for {sourceRuntimeType.Name} to {destinationType.Name}");
        }

        if (parameters.Length == 0 &&
            _simpleCopiers.TryGetValue((sourceRuntimeType, destinationType), out var simpleCopy))
        {
            var destinationFast = CreateDestinationInstance(destinationType);
            simpleCopy(source, destinationFast);
            return destinationFast;
        }

        var destination = CreateDestinationInstance(destinationType);

        if (mapping.CustomMapFunction != null)
        {
            return InvokeMapFunction(mapping.CustomMapFunction, source, destination);
        }

        ApplyPropertyMappings(source, destination, mapping, parameters);
        return destination;
    }

    private object CreateDestinationInstance(Type destinationType) =>
        _destinationFactories.GetOrAdd(destinationType, BuildDestinationFactory)();

    private static Func<object> BuildDestinationFactory(Type destinationType)
    {
        try
        {
            return Expression.Lambda<Func<object>>(Expression.New(destinationType)).Compile();
        }
        catch (Exception)
        {
            return () => Activator.CreateInstance(destinationType)
                ?? throw new InvalidOperationException($"Cannot create instance of {destinationType}");
        }
    }

    private void ApplyPropertyMappings(object source, object destination, ITypeMapping mapping, object?[] parameters)
    {
        var sourceType = source.GetType();
        var destinationType = destination.GetType();
        var destinationProperties = _destinationPropertiesByType.GetOrAdd(
            destinationType,
            static dt => dt.GetProperties(PropertyBindingFlags));

        foreach (var destProperty in destinationProperties)
        {
            if (mapping.IgnoredProperties.Contains(destProperty.Name))
            {
                continue;
            }

            if (mapping.PropertyMappings.TryGetValue(destProperty.Name, out var propertyMapping))
            {
                ApplyCustomPropertyMapping(source, destination, destProperty, propertyMapping, mapping, parameters);
            }
            else
            {
                ApplyAutomaticPropertyMapping(source, destination, sourceType, destProperty, mapping);
            }
        }
    }

    private void ApplyCustomPropertyMapping(
        object source,
        object destination,
        PropertyInfo destProperty,
        IPropertyMapping propertyMapping,
        ITypeMapping mapping,
        object?[] parameters)
    {
        if (propertyMapping.IsIgnored || !destProperty.CanWrite)
        {
            return;
        }

        if (propertyMapping.Condition != null && !InvokeCondition(propertyMapping.Condition, source))
        {
            return;
        }

        var value = GetMappingValue(source, propertyMapping, parameters);

        if (value != null)
        {
            TransformAndSetValue(destination, destProperty, value, mapping, parameters);
        }
        else if (propertyMapping.Condition != null && propertyMapping.MappingFunction == null &&
                 propertyMapping.DataSource == null && propertyMapping.SourcePropertyName == null)
        {
            ApplyAutomaticPropertyMapping(source, destination, source.GetType(), destProperty, mapping);
        }
    }

    private void ApplyAutomaticPropertyMapping(
        object source,
        object destination,
        Type sourceType,
        PropertyInfo destProperty,
        ITypeMapping mapping)
    {
        var sourceProperty = sourceType.GetProperty(destProperty.Name, PropertyBindingFlags);

        if (sourceProperty == null || !sourceProperty.CanRead || !destProperty.CanWrite)
        {
            return;
        }

        var getter = _compiledPropertyGetters.GetOrAdd(
            (sourceType, sourceProperty.Name),
            BuildPropertyGetter);

        var sourceValue = getter(source);
        if (sourceValue == null)
        {
            return;
        }

        var setter = _compiledPropertySetters.GetOrAdd(
            (destination.GetType(), destProperty.Name),
            BuildPropertySetter);

        TransformAndSetValue(destination, destProperty, sourceValue, mapping, Array.Empty<object>(), setter);
    }

    private static Func<object, object?> BuildPropertyGetter((Type Type, string Name) key)
    {
        var prop = key.Type.GetProperty(key.Name, PropertyBindingFlags);
        if (prop?.CanRead == false)
        {
            return _ => null;
        }

        try
        {
            var param = Expression.Parameter(typeof(object), "obj");
            var instance = Expression.Convert(param, key.Type);
            var propAccess = Expression.Property(instance, prop!);
            var boxed = Expression.Convert(propAccess, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxed, param).Compile();
        }
        catch
        {
            return obj => prop?.GetValue(obj);
        }
    }

    private static Action<object, object?> BuildPropertySetter((Type Type, string Name) key)
    {
        var prop = key.Type.GetProperty(key.Name, PropertyBindingFlags);
        if (prop?.CanWrite == false)
        {
            return (_, _) => { };
        }

        try
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var valParam = Expression.Parameter(typeof(object), "val");
            var instance = Expression.Convert(objParam, key.Type);
            var value = Expression.Convert(valParam, prop!.PropertyType);
            var assignment = Expression.Assign(Expression.Property(instance, prop), value);
            return Expression.Lambda<Action<object, object?>>(assignment, objParam, valParam).Compile();
        }
        catch
        {
            return (obj, val) => prop?.SetValue(obj, val);
        }
    }

    private object? GetMappingValue(object source, IPropertyMapping propertyMapping, object?[] parameters)
    {
        if (propertyMapping.MappingFunction != null)
        {
            return InvokeDelegate(propertyMapping.MappingFunction, source, parameters);
        }

        if (propertyMapping.DataSource != null)
        {
            return InvokeDelegate(propertyMapping.DataSource, source, parameters);
        }

        if (propertyMapping.SourcePropertyName != null)
        {
            var sourceProperty = GetCachedSourceProperty(source.GetType(), propertyMapping.SourcePropertyName);
            return sourceProperty?.GetValue(source);
        }

        return null;
    }

    private PropertyInfo? GetCachedSourceProperty(Type sourceType, string propertyName) =>
        _sourcePropertyByTypeAndName.GetOrAdd(
            (sourceType, propertyName),
            key => key.SourceType.GetProperty(key.Name, PropertyBindingFlags));

    private void TransformAndSetValue(
        object destination,
        PropertyInfo destProperty,
        object value,
        ITypeMapping mapping,
        object?[] parameters,
        Action<object, object?>? compiledSetter = null)
    {
        var mappedValue = value;

        if (mapping.ValueTransformers.TryGetValue(destProperty.PropertyType.Name, out var transformer))
        {
            mappedValue = InvokeDelegate(transformer, mappedValue);
        }

        if (IsCollection(destProperty.PropertyType) && mappedValue is IEnumerable sourceCollection && mappedValue is not string)
        {
            mappedValue = MapCollectionToDestinationType(sourceCollection, destProperty.PropertyType, parameters);
        }
        else if (mappedValue != null && IsComplexType(mappedValue.GetType()) && mappedValue is not string)
        {
            var nestedMapping = FindMapping(mappedValue.GetType(), destProperty.PropertyType);
            if (nestedMapping != null)
            {
                mappedValue = MapUsingResolvedMapping(mappedValue, destProperty.PropertyType, parameters, nestedMapping);
            }
        }

        if (compiledSetter != null)
        {
            compiledSetter(destination, mappedValue);
        }
        else
        {
            destProperty.SetValue(destination, mappedValue);
        }
    }

    private object MapCollectionToDestinationType(IEnumerable sourceCollection, Type destinationType, object?[] parameters)
    {
        var itemType = GetCollectionItemType(destinationType);

        if (itemType == null)
        {
            return sourceCollection;
        }

        var mappedItems = sourceCollection
            .Cast<object?>()
            .Select(item => item == null ? null : MapCollectionItem(item, itemType, parameters))
            .ToList();

        return CreateCollectionInstance(destinationType, itemType, mappedItems);
    }

    private object MapCollectionItem(object item, Type destinationType, object?[] parameters)
    {
        var itemMapping = FindMapping(item.GetType(), destinationType);
        return itemMapping != null
            ? MapUsingResolvedMapping(item, destinationType, parameters, itemMapping)
            : item;
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

    private static object CreateCollectionInstance(Type destinationType, Type itemType, List<object?> mappedItems)
    {
        if (destinationType.IsArray)
        {
            var array = Array.CreateInstance(itemType, mappedItems.Count);
            for (int i = 0; i < mappedItems.Count; i++)
            {
                array.SetValue(mappedItems[i], i);
            }
            return array;
        }

        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList?)Activator.CreateInstance(listType);
        if (list != null)
        {
            foreach (var item in mappedItems)
            {
                list.Add(item!);
            }
        }

        return list ?? new List<object>();
    }

    private static bool IsCollection(Type type) =>
        IsCollectionByType.GetOrAdd(type, static t =>
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return true;
            }

            if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return true;
            }

            return t.IsArray;
        });

    private static bool IsComplexType(Type type)
    {
        return !type.IsValueType && type != typeof(string);
    }

    private ITypeMapping? FindMapping(Type sourceType, Type destinationType) =>
        _mappingByPair.TryGetValue((sourceType, destinationType), out var mapping) ? mapping : null;

    private object? InvokeDelegate(Delegate del, object arg)
    {
        try
        {
            var invokeMethod = GetInvokeMethod(del.GetType());
            return invokeMethod.Invoke(del, [arg]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke delegate during mapping");
            throw new InvalidOperationException("Failed to invoke delegate during mapping.", ex);
        }
    }

    private object? InvokeDelegate(Delegate del, object source, object?[] parameters)
    {
        try
        {
            var invokeMethod = GetInvokeMethod(del.GetType());
            var invokeParameters = invokeMethod.GetParameters();

            if (invokeParameters.Length == 2)
            {
                return invokeMethod.Invoke(del, [source, parameters]);
            }

            return invokeMethod.Invoke(del, [source]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke delegate during mapping");
            throw new InvalidOperationException("Failed to invoke delegate during mapping.", ex);
        }
    }

    private object InvokeMapFunction(Delegate mapFunction, object source, object destination)
    {
        try
        {
            var invokeMethod = GetInvokeMethod(mapFunction.GetType());
            return invokeMethod.Invoke(mapFunction, [source, destination]) ?? destination;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke custom map function");
            throw new InvalidOperationException("Failed to invoke custom map function.", ex);
        }
    }

    private MethodInfo GetInvokeMethod(Type delegateType) =>
        _invokeMethodByDelegateType.GetOrAdd(delegateType, static dt =>
            dt.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.Name == "Invoke"));

    private bool InvokeCondition(Delegate condition, object source)
    {
        try
        {
            var result = InvokeDelegate(condition, source);
            return result is bool b && b;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Property or type mapping condition delegate threw; treating as false");
            return false;
        }
    }
}
