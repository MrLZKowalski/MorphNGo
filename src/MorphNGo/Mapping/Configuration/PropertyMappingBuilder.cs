namespace MorphNGo.Mapping.Configuration;

/// <summary>
/// Builder for configuring individual property mappings in a fluent API style.
/// Provides methods to define custom mapping logic, conditions, and transformations for a single destination property.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
public class PropertyMappingBuilder<TSource, TDestination>
{
    private Delegate? _mappingFunction;
    private Delegate? _dataSource;
    private Delegate? _condition;
    private bool _isIgnored;
    private string? _sourcePropertyName;
    private readonly string _destinationPropertyName;

    /// <summary>
    /// Initializes a new instance of the PropertyMappingBuilder.
    /// </summary>
    /// <param name="destinationPropertyName">The name of the destination property.</param>
    public PropertyMappingBuilder(string destinationPropertyName)
    {
        if (string.IsNullOrWhiteSpace(destinationPropertyName))
        {
            throw new ArgumentException("Destination property name cannot be null or empty.", nameof(destinationPropertyName));
        }

        _destinationPropertyName = destinationPropertyName;
    }

    /// <summary>
    /// Specifies a custom mapping function for this property.
    /// </summary>
    /// <param name="mappingFunction">A function that takes the source object and returns the mapped value.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when mappingFunction is null.</exception>
    public PropertyMappingBuilder<TSource, TDestination> MapFrom(Func<TSource, object?> mappingFunction)
    {
        ArgumentNullException.ThrowIfNull(mappingFunction);
        _mappingFunction = mappingFunction;
        _dataSource = null;
        _sourcePropertyName = null;
        return this;
    }

    /// <summary>
    /// Specifies a custom mapping function for this property with access to runtime parameters passed to Map (params object[]).
    /// </summary>
    /// <param name="mappingFunction">A function that takes the source object and the parameters array from the map call, returning the mapped value.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when mappingFunction is null.</exception>
    public PropertyMappingBuilder<TSource, TDestination> MapFrom(Func<TSource, object?[], object?> mappingFunction)
    {
        ArgumentNullException.ThrowIfNull(mappingFunction);
        _mappingFunction = mappingFunction;
        _dataSource = null;
        _sourcePropertyName = null;
        return this;
    }

    /// <summary>
    /// Specifies a data source for this property mapping (e.g., static data or external service).
    /// </summary>
    /// <param name="dataSourceFunction">A function that provides the mapped value.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataSourceFunction is null.</exception>
    public PropertyMappingBuilder<TSource, TDestination> MapFromStaticData(Func<TSource, object?> dataSourceFunction)
    {
        ArgumentNullException.ThrowIfNull(dataSourceFunction);
        _dataSource = dataSourceFunction;
        _mappingFunction = null;
        _sourcePropertyName = null;
        return this;
    }

    /// <summary>
    /// Specifies a data source for this property mapping with access to runtime parameters passed to the map call.
    /// </summary>
    /// <param name="dataSourceFunction">A function that takes the source object and the parameters array from the map call, providing the mapped value.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataSourceFunction is null.</exception>
    public PropertyMappingBuilder<TSource, TDestination> MapFromStaticData(Func<TSource, object?[], object?> dataSourceFunction)
    {
        ArgumentNullException.ThrowIfNull(dataSourceFunction);
        _dataSource = dataSourceFunction;
        _mappingFunction = null;
        _sourcePropertyName = null;
        return this;
    }

    /// <summary>
    /// Specifies that this property should only be mapped when a condition is met.
    /// </summary>
    /// <param name="condition">A function that returns true if the property should be mapped.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when condition is null.</exception>
    public PropertyMappingBuilder<TSource, TDestination> When(Func<TSource, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _condition = condition;
        return this;
    }

    /// <summary>
    /// Specifies that this property should be ignored during mapping.
    /// </summary>
    /// <returns>This builder instance for chaining.</returns>
    public PropertyMappingBuilder<TSource, TDestination> Ignore()
    {
        _isIgnored = true;
        return this;
    }

    /// <summary>
    /// Maps this property from a specific source property name (property renaming).
    /// </summary>
    /// <param name="sourcePropertyName">The name of the source property.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when sourcePropertyName is null or empty.</exception>
    public PropertyMappingBuilder<TSource, TDestination> From(string sourcePropertyName)
    {
        if (string.IsNullOrWhiteSpace(sourcePropertyName))
        {
            throw new ArgumentException("Source property name cannot be null or empty.", nameof(sourcePropertyName));
        }

        _sourcePropertyName = sourcePropertyName;
        _mappingFunction = null;
        _dataSource = null;
        return this;
    }

    internal PropertyMappingConfiguration Build()
    {
        return new PropertyMappingConfiguration(
            _destinationPropertyName,
            _mappingFunction,
            _dataSource,
            _condition,
            _isIgnored,
            _sourcePropertyName
        );
    }
}
