namespace MorphNGo.Mapping.Configuration;

using System.Linq.Expressions;

/// <summary>
/// Builder for configuring type-to-type mappings in a fluent API style.
/// Provides comprehensive methods to configure how properties are mapped, including custom logic,
/// conditions, transformations, and property ignoring.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
public class TypeMappingBuilder<TSource, TDestination>
{
    private readonly Dictionary<string, PropertyMappingConfiguration> _propertyMappings = new();
    private readonly Dictionary<string, Delegate> _valueTransformers = new();
    private readonly HashSet<string> _ignoredProperties = new(StringComparer.Ordinal);
    private Delegate? _preMappingCondition;
    private Delegate? _customMapFunction;
    private bool _reverseMapping;

    /// <summary>
    /// Configures mapping for a specific destination property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="destinationMember">An expression selecting the destination property.</param>
    /// <param name="configAction">An action to configure the property mapping.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when destinationMember or configAction is null.</exception>
    public TypeMappingBuilder<TSource, TDestination> ForMember<TProperty>(
        Expression<Func<TDestination, TProperty>> destinationMember,
        Action<PropertyMappingBuilder<TSource, TDestination>> configAction)
    {
        ArgumentNullException.ThrowIfNull(destinationMember);
        ArgumentNullException.ThrowIfNull(configAction);

        var memberName = GetMemberName(destinationMember);
        var builder = new PropertyMappingBuilder<TSource, TDestination>(memberName);
        configAction(builder);
        _propertyMappings[memberName] = builder.Build();
        return this;
    }

    /// <summary>
    /// Specifies a condition that must be met before mapping occurs (pre-mapping validation).
    /// </summary>
    /// <param name="condition">A function that returns true if mapping should proceed.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when condition is null.</exception>
    public TypeMappingBuilder<TSource, TDestination> When(Func<TSource, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _preMappingCondition = condition;
        return this;
    }

    /// <summary>
    /// Specifies a custom mapping function for the entire type mapping.
    /// This function takes precedence over property-level mappings.
    /// </summary>
    /// <param name="mappingFunction">A function that takes source and destination and performs custom mapping.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when mappingFunction is null.</exception>
    public TypeMappingBuilder<TSource, TDestination> WithCustomMapping(
        Func<TSource, TDestination, TDestination> mappingFunction)
    {
        ArgumentNullException.ThrowIfNull(mappingFunction);
        _customMapFunction = mappingFunction;
        return this;
    }

    /// <summary>
    /// Registers a value transformer for a specific property type.
    /// The transformer is applied to all properties of this type during mapping.</summary>
    /// <typeparam name="TProperty">The property type to transform.</typeparam>
    /// <param name="transformer">A function that transforms the value.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transformer is null.</exception>
    public TypeMappingBuilder<TSource, TDestination> WithValueTransformer<TProperty>(
        Func<TProperty, TProperty> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);
        var transformerName = typeof(TProperty).Name;
        _valueTransformers[transformerName] = transformer;
        return this;
    }

    /// <summary>
    /// Ignores a property during mapping (property will not be mapped).
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="destinationMember">An expression selecting the property to ignore.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when destinationMember is null.</exception>
    public TypeMappingBuilder<TSource, TDestination> Ignore<TProperty>(
        Expression<Func<TDestination, TProperty>> destinationMember)
    {
        ArgumentNullException.ThrowIfNull(destinationMember);
        var memberName = GetMemberName(destinationMember);
        _ignoredProperties.Add(memberName);
        return this;
    }

    /// <summary>
    /// Enables reverse mapping (TDestination -> TSource).
    /// </summary>
    /// <returns>This builder instance for chaining.</returns>
    public TypeMappingBuilder<TSource, TDestination> ReverseMap()
    {
        _reverseMapping = true;
        return this;
    }

    internal TypeMappingConfiguration<TSource, TDestination> Build()
    {
        return new TypeMappingConfiguration<TSource, TDestination>(
            _propertyMappings,
            _valueTransformers,
            _ignoredProperties,
            _preMappingCondition,
            _customMapFunction,
            _reverseMapping);
    }

    /// <summary>
    /// Extracts the member name from a lambda expression.
    /// </summary>
    /// <typeparam name="T">The member type.</typeparam>
    /// <param name="expression">The lambda expression.</param>
    /// <returns>The member name.</returns>
    /// <exception cref="ArgumentException">Thrown when expression is not a member expression.</exception>
    private static string GetMemberName<T>(Expression<Func<TDestination, T>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a member expression. Use expressions like 'x => x.PropertyName'.",
            nameof(expression));
    }
}
