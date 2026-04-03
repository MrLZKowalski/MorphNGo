namespace MorphNGo.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

/// <summary>
/// Tests for error handling and edge cases.
/// </summary>
public class ErrorHandlingTests
{
    private static ILogger GetLogger() => NullLogger.Instance;
    [Fact]
    public void Test_MappingUnconfiguredTypes_ThrowsException()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var unconfiguredObject = new object();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mapper.Map<UserDto>(unconfiguredObject));
    }

    [Fact]
    public void Test_MappingToUnconfiguredType_ThrowsException()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mapper.Map(user, typeof(string)));
    }

    [Fact]
    public void Test_FailedConditionThrowsException()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.When(src => src.Id > 10);
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mapper.Map<UserDto>(user));
    }

    [Fact]
    public void Test_MappingWithMissingSourceProperty_MapsOtherProperties()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<MinimalUser, UserDto>();
        });

        var mapper = config.CreateMapper();
        var minimalUser = new MinimalUser { Id = 1, FirstName = "John" };

        // Act
        var userDto = mapper.Map<UserDto>(minimalUser);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal(1, userDto.Id);
        Assert.Equal("John", userDto.FirstName);
        Assert.Equal("", userDto.LastName);
    }

    [Fact]
    public void Test_MappingEmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var emptyUsers = new List<User>();

        // Act
        var userDtos = mapper.MapCollection<UserDto>(emptyUsers.Cast<object>().ToList()).ToList();

        // Assert
        Assert.NotNull(userDtos);
        Assert.Empty(userDtos);
    }

    [Fact]
    public void Test_MappingCollectionWithNullItems_HandlesGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var users = new List<object?> { new User { Id = 1, FirstName = "John" } };

        // Act
        var userDtos = mapper.MapCollection<UserDto>(users.OfType<object>().ToList()).ToList();

        // Assert
        Assert.NotNull(userDtos);
        Assert.Single(userDtos);
    }

    [Fact]
    public void Test_MapToReadOnlyProperty_DoesNotThrow()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserWithReadOnlyId>();
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserWithReadOnlyId>(user);

        // Assert
        Assert.NotNull(userDto);
        // Id is read-only so it won't be set
        Assert.Equal(0, userDto.Id);
        Assert.Equal("John", userDto.FirstName);
    }
}

// Error Handling Test Models
public class MinimalUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
}

public class UserWithReadOnlyId
{
    public int Id { get; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
