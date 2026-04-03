namespace MorphNGo.Mapping.Configuration;

using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Represents a complete type-to-type mapping configuration.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
internal class TypeMappingConfiguration<TSource, TDestination> : ITypeMapping
{
    public Type SourceType => typeof(TSource);
    public Type DestinationType => typeof(TDestination);
    public IReadOnlyDictionary<string, IPropertyMapping> PropertyMappings { get; }
    public IReadOnlyDictionary<string, Delegate> ValueTransformers { get; }
    public IReadOnlySet<string> IgnoredProperties { get; }
    public Delegate? PreMappingCondition { get; }
    public Delegate? CustomMapFunction { get; }
    public ITypeMapping? ReverseMapping { get; set; }
    internal bool IsReverseMapEnabled { get; }

    public TypeMappingConfiguration(
        IReadOnlyDictionary<string, PropertyMappingConfiguration> propertyMappings,
        IReadOnlyDictionary<string, Delegate> valueTransformers,
        IReadOnlySet<string> ignoredProperties,
        Delegate? preMappingCondition,
        Delegate? customMapFunction,
        bool reverseMapping)
    {
        PropertyMappings = propertyMappings.ToDictionary(x => x.Key, x => (IPropertyMapping)x.Value);
        ValueTransformers = valueTransformers;
        IgnoredProperties = ignoredProperties;
        PreMappingCondition = preMappingCondition;
        CustomMapFunction = customMapFunction;
        IsReverseMapEnabled = reverseMapping;
    }
}
