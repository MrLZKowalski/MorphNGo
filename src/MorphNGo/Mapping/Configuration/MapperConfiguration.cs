namespace MorphNGo.Mapping.Configuration;

using Microsoft.Extensions.Logging;
using MorphNGo.Mapping.Core;
using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Fluent configuration builder for creating mapper instances.
/// Provides a clean, chainable API for registering type-to-type mappings and creating mappers.
/// </summary>
public class MapperConfiguration : IMapperConfiguration
{
    private readonly List<ITypeMapping> _typeMappings = new();
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the collection of configured type mappings.
    /// </summary>
    public IReadOnlyList<ITypeMapping> TypeMappings => _typeMappings.AsReadOnly();

    /// <summary>
    /// Gets the logger instance for this configuration.
    /// </summary>
    public ILogger Logger => _logger;

    /// <summary>
    /// Initializes a new instance of the MapperConfiguration class with a logger and optional inline configuration.
    /// </summary>
    /// <param name="logger">The logger instance for mapping operations.</param>
    /// <param name="configAction">Optional action to configure type mappings inline.</param>
    public MapperConfiguration(ILogger logger, Action<MapperConfiguration>? configAction = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        configAction?.Invoke(this);
    }

    /// <summary>
    /// Creates a type mapping configuration for mapping from TSource to TDestination.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination type to map to.</typeparam>
    /// <param name="configAction">Optional action to configure the specific type mapping.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// cfg.CreateMap&lt;User, UserDto&gt;(builder =>
    /// {
    ///     builder.ForMember(dest => dest.FullName, 
    ///         opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    ///     builder.ReverseMap(); // Automatically creates UserDto -> User mapping
    /// });
    /// </code>
    /// </example>
    public MapperConfiguration CreateMap<TSource, TDestination>(
        Action<TypeMappingBuilder<TSource, TDestination>>? configAction = null)
    {
        var builder = new TypeMappingBuilder<TSource, TDestination>();
        configAction?.Invoke(builder);
        var mapping = builder.Build();
        _typeMappings.Add(mapping);

        // If reverse mapping is enabled, create and register the reverse mapping
        if (mapping is TypeMappingConfiguration<TSource, TDestination> typedMapping && typedMapping.IsReverseMapEnabled)
        {
            var reverseMapping = CreateReverseMapping<TSource, TDestination>(mapping);
            _typeMappings.Add(reverseMapping);
            typedMapping.ReverseMapping = reverseMapping;
        }

        return this;
    }

    /// <summary>
    /// Creates a reverse type mapping based on the original mapping configuration.
    /// Reverses simple property mappings and preserves ignored properties.
    /// </summary>
    /// <typeparam name="TSource">The original source type.</typeparam>
    /// <typeparam name="TDestination">The original destination type.</typeparam>
    /// <param name="originalMapping">The original mapping configuration.</param>
    /// <returns>The reverse type mapping.</returns>
    private static ITypeMapping CreateReverseMapping<TSource, TDestination>(
        ITypeMapping originalMapping)
    {
        var reversePropertyMappings = new Dictionary<string, PropertyMappingConfiguration>();

        // Reverse property mappings where possible (simple SourcePropertyName cases)
        foreach (var propertyMapping in originalMapping.PropertyMappings.Values)
        {
            if (propertyMapping is PropertyMappingConfiguration config && !config.IsIgnored &&
                    config.SourcePropertyName != null &&
                    config.MappingFunction == null &&
                    config.DataSource == null)
            {
                // Reverse: destination property becomes source, source property becomes destination
                var reversedConfig = new PropertyMappingConfiguration(
                    destinationPropertyName: config.SourcePropertyName,
                    mappingFunction: null,
                    dataSource: null,
                    condition: null,
                    isIgnored: false,
                    sourcePropertyName: propertyMapping.DestinationPropertyName);

                reversePropertyMappings[config.SourcePropertyName] = reversedConfig;
            }
        }

        var reverseMappingType = typeof(TypeMappingConfiguration<,>)
            .MakeGenericType(typeof(TDestination), typeof(TSource));

        var reverseMappingConstructor = reverseMappingType.GetConstructor(
            [
                typeof(IReadOnlyDictionary<string, PropertyMappingConfiguration>),
                typeof(IReadOnlyDictionary<string, Delegate>),
                typeof(IReadOnlySet<string>),
                typeof(Delegate),
                typeof(Delegate),
                typeof(bool)
            ]) ?? throw new InvalidOperationException("Could not find TypeMappingConfiguration constructor.");
        var reverseMapping = (ITypeMapping)reverseMappingConstructor.Invoke(
            [
                reversePropertyMappings,
                originalMapping.ValueTransformers,
                originalMapping.IgnoredProperties,
                null,
                null,
                false
            ]);

        return reverseMapping;
    }

    /// <summary>
    /// Creates an IMapper instance based on this configuration.
    /// The mapper can be reused across multiple mapping operations.
    /// </summary>
    /// <returns>A new mapper instance configured with the registered type mappings.</returns>
    public IMapper CreateMapper()
    {
        return new Mapper(_typeMappings, _logger);
    }
}
