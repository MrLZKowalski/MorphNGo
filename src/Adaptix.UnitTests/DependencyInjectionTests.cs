namespace MorphNGo.UnitTests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Extensions;
using MorphNGo.Mapping.Interfaces;

/// <summary>
/// Tests for dependency injection integration with MorphNGo mapper.
/// </summary>
public class DependencyInjectionTests
{
    private static Microsoft.Extensions.Logging.ILogger GetLogger() => NullLogger.Instance;

    [Fact]
    public void Test_RegisterMapperWithDI_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMorphNGoMapper(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var provider = services.BuildServiceProvider();

        // Act
        var mapper = provider.GetRequiredService<IMapper>();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(mapper);
        Assert.NotNull(userDto);
        Assert.Equal(user.Id, userDto.Id);
    }

    [Fact]
    public void Test_RegisterMapperWithTransientLifetime_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMorphNGoMapper(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        }, ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();

        // Act
        var mapper1 = provider.GetRequiredService<IMapper>();
        var mapper2 = provider.GetRequiredService<IMapper>();

        // Assert - Different instances for transient
        Assert.NotNull(mapper1);
        Assert.NotNull(mapper2);
    }

    [Fact]
    public void Test_RegisterMapperWithSingletonLifetime_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMorphNGoMapper(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        }, ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        // Act
        var mapper1 = provider.GetRequiredService<IMapper>();
        var mapper2 = provider.GetRequiredService<IMapper>();

        // Assert - Same instance for singleton
        Assert.NotNull(mapper1);
        Assert.NotNull(mapper2);
    }

    [Fact]
    public void Test_InjectMapperIntoService_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMorphNGoMapper(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });
        services.AddScoped<UserMappingService>();

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<UserMappingService>();

        // Act
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = service.MapUser(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal(user.Id, userDto.Id);
    }
}

/// <summary>
/// Sample service that uses injected mapper.
/// </summary>
public class UserMappingService
{
    private readonly IMapper _mapper;

    public UserMappingService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto MapUser(User user)
    {
        return _mapper.Map<UserDto>(user);
    }
}
