namespace MorphNGo.Mapping.Interfaces;

/// <summary>
/// Defines the contract for individual property mapping configuration.
/// </summary>
public interface IPropertyMapping
{
    /// <summary>
    /// Gets the destination property name.
    /// </summary>
    string DestinationPropertyName { get; }

    /// <summary>
    /// Gets the custom mapping function for this property.
    /// </summary>
    Delegate? MappingFunction { get; }

    /// <summary>
    /// Gets the source of data for this property (e.g., static data, external service).
    /// </summary>
    Delegate? DataSource { get; }

    /// <summary>
    /// Gets the condition that must be met for this property to be mapped.
    /// </summary>
    Delegate? Condition { get; }

    /// <summary>
    /// Gets whether this property is ignored.
    /// </summary>
    bool IsIgnored { get; }

    /// <summary>
    /// Gets the source property name if using standard property-to-property mapping.
    /// </summary>
    string? SourcePropertyName { get; }
}
