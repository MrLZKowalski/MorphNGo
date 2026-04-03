namespace MorphNGo.Mapping.Interfaces;

/// <summary>
/// Defines the contract for mapping between source and destination types.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps the source object to the destination type.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <returns>The mapped destination object.</returns>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps the source object to the destination type with additional parameters accessible during mapping.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <param name="parameters">Additional parameters (e.g., lookup lists, reference data) accessible during mapping.</param>
    /// <returns>The mapped destination object.</returns>
    TDestination Map<TDestination>(object source, params object[] parameters);

    /// <summary>
    /// Maps the source object to the specified destination type.
    /// </summary>
    /// <param name="source">The source object to map.</param>
    /// <param name="destinationType">The destination type.</param>
    /// <returns>The mapped destination object.</returns>
    object Map(object source, Type destinationType);

    /// <summary>
    /// Maps the source object to the specified destination type with additional parameters accessible during mapping.
    /// </summary>
    /// <param name="source">The source object to map.</param>
    /// <param name="destinationType">The destination type.</param>
    /// <param name="parameters">Additional parameters (e.g., lookup lists, reference data) accessible during mapping.</param>
    /// <returns>The mapped destination object.</returns>
    object Map(object source, Type destinationType, params object[] parameters);

    /// <summary>
    /// Maps the source object to an existing destination object.
    /// Applies the same <see cref="ITypeMapping.PreMappingCondition"/> and <see cref="ITypeMapping.CustomMapFunction"/>
    /// as <see cref="Map{TDestination}(object, object[])"/>; only the creation of the destination instance is skipped.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <param name="destination">The existing destination object.</param>
    /// <returns>The mapped destination object.</returns>
    TDestination MapTo<TDestination>(object source, TDestination destination);

    /// <summary>
    /// Maps the source object to an existing destination object with additional parameters accessible during mapping.
    /// Applies the same <see cref="ITypeMapping.PreMappingCondition"/> and <see cref="ITypeMapping.CustomMapFunction"/>
    /// as <see cref="Map{TDestination}(object, object[])"/>; only the creation of the destination instance is skipped.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <param name="destination">The existing destination object.</param>
    /// <param name="parameters">Additional parameters (e.g., lookup lists, reference data) accessible during mapping.</param>
    /// <returns>The mapped destination object.</returns>
    TDestination MapTo<TDestination>(object source, TDestination destination, params object[] parameters);

    /// <summary>
    /// Maps a collection of source objects to the destination collection type.
    /// </summary>
    /// <typeparam name="TDestination">The destination item type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>A collection of mapped destination objects.</returns>
    IEnumerable<TDestination> MapCollection<TDestination>(IEnumerable<object> source);

    /// <summary>
    /// Maps a collection of source objects to the destination collection type with additional parameters accessible during mapping each item.
    /// The result is materialized (e.g. a list) so each item is mapped once.
    /// </summary>
    /// <typeparam name="TDestination">The destination item type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="parameters">Additional parameters (e.g., lookup lists, reference data) accessible during mapping each item.</param>
    /// <returns>A collection of mapped destination objects.</returns>
    IEnumerable<TDestination> MapCollection<TDestination>(IEnumerable<object> source, params object[] parameters);
}
