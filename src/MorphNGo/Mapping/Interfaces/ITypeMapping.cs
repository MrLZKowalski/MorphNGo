namespace MorphNGo.Mapping.Interfaces;

/// <summary>
/// Defines the contract for a type-to-type mapping configuration.
/// </summary>
public interface ITypeMapping
{
    /// <summary>
    /// Gets the source type.
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Gets the destination type.
    /// </summary>
    Type DestinationType { get; }

    /// <summary>
    /// Gets the property mappings for this type mapping.
    /// </summary>
    IReadOnlyDictionary<string, IPropertyMapping> PropertyMappings { get; }

    /// <summary>
    /// Gets the value transformers for this type mapping.
    /// </summary>
    IReadOnlyDictionary<string, Delegate> ValueTransformers { get; }

    /// <summary>
    /// Gets whether to map only when a condition is met.
    /// </summary>
    Delegate? PreMappingCondition { get; }

    /// <summary>
    /// Gets the custom mapping function if defined.
    /// </summary>
    Delegate? CustomMapFunction { get; }

    /// <summary>
    /// Gets properties that should be ignored during mapping.
    /// </summary>
    IReadOnlySet<string> IgnoredProperties { get; }

    /// <summary>
    /// Gets the reverse mapping configuration if defined.
    /// </summary>
    ITypeMapping? ReverseMapping { get; }
}
