namespace MorphNGo.Mapping.Interfaces;

/// <summary>
/// Defines the contract for configuring type mappings.
/// </summary>
public interface IMapperConfiguration
{
    /// <summary>
    /// Gets the collection of mapping configurations.
    /// </summary>
    IReadOnlyList<ITypeMapping> TypeMappings { get; }

    /// <summary>
    /// Creates an IMapper instance from this configuration.
    /// </summary>
    /// <returns>A configured mapper instance.</returns>
    IMapper CreateMapper();
}
