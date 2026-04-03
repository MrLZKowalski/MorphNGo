namespace MorphNGo.Mapping.Configuration;

using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Represents a configured property mapping.
/// </summary>
internal class PropertyMappingConfiguration : IPropertyMapping
{
    public string DestinationPropertyName { get; }
    public Delegate? MappingFunction { get; }
    public Delegate? DataSource { get; }
    public Delegate? Condition { get; }
    public bool IsIgnored { get; }
    public string? SourcePropertyName { get; }

    public PropertyMappingConfiguration(
        string destinationPropertyName,
        Delegate? mappingFunction = null,
        Delegate? dataSource = null,
        Delegate? condition = null,
        bool isIgnored = false,
        string? sourcePropertyName = null)
    {
        DestinationPropertyName = destinationPropertyName;
        MappingFunction = mappingFunction;
        DataSource = dataSource;
        Condition = condition;
        IsIgnored = isIgnored;
        SourcePropertyName = sourcePropertyName;
    }
}
