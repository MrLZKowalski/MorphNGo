namespace MorphNGo.Mapping.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorphNGo.Mapping.Configuration;
using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Extension methods for registering the MorphNGo mapping library with dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the mapper configuration and mapper instance with the service collection, including a logger instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logger">The logger instance to use for mapping operations.</param>
    /// <param name="configAction">An action to configure the mapper.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMorphNGoMapper(
        this IServiceCollection services,
        ILogger logger,
        Action<MapperConfiguration> configAction,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var configuration = new MapperConfiguration(logger, configAction);
        services.Add(new ServiceDescriptor(typeof(IMapperConfiguration), _ => configuration, lifetime));
        services.Add(new ServiceDescriptor(typeof(IMapper), sp => configuration.CreateMapper(), lifetime));
        return services;
    }

    /// <summary>
    /// Registers a preconfigured mapper configuration with the service collection, including a logger instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logger">The logger instance to use for mapping operations.</param>
    /// <param name="configuration">The mapper configuration instance.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMorphNGoMapper(
        this IServiceCollection services,
        ILogger logger,
        MapperConfiguration configuration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(logger);
        services.Add(new ServiceDescriptor(typeof(IMapperConfiguration), _ => configuration, lifetime));
        services.Add(new ServiceDescriptor(typeof(IMapper), sp => configuration.CreateMapper(), lifetime));
        return services;
    }
}
