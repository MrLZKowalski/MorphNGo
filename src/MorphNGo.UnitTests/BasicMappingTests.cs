namespace MorphNGo.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

/// <summary>
/// Core unit tests for basic mapping functionality including property mapping,
/// nested objects, collections, and conditional mapping scenarios.
/// </summary>
public class BasicMappingTests
{
    private static ILogger GetLogger() => NullLogger.Instance;

    [Fact]
    public void Test_SimplePropertyMapping_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal(user.Id, userDto.Id);
        Assert.Equal(user.FirstName, userDto.FirstName);
        Assert.Equal(user.LastName, userDto.LastName);
    }

    [Fact]
    public void Test_CustomPropertyMapping_WithForMember()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal("John Doe", userDto.FullName);
    }

    [Fact]
    public void Test_IgnoreProperty_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.Ignore(dest => dest.FullName);
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Null(userDto.FullName);
        Assert.Equal(user.Id, userDto.Id);
    }

    [Fact]
    public void Test_ConditionalMapping_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.ForMember(dest => dest.FirstName, opt => opt.When(src => src.Id > 0));
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal("John", userDto.FirstName);
    }

    [Fact]
    public void Test_MapCollection_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe" },
            new User { Id = 2, FirstName = "Jane", LastName = "Smith" }
        };

        // Act
        var userDtos = mapper.MapCollection<UserDto>(users.Cast<object>().ToList()).ToList();

        // Assert
        Assert.NotNull(userDtos);
        Assert.Equal(2, userDtos.Count);
        Assert.Equal("John", userDtos[0].FirstName);
        Assert.Equal("Jane", userDtos[1].FirstName);
    }

    [Fact]
    public void Test_MapCollection_WithNullSourceItems_ProducesDefaultDtos()
    {
        var config = new MapperConfiguration(GetLogger(), cfg => cfg.CreateMap<User, UserDto>());
        var mapper = config.CreateMapper();
        var mixed = new List<object?>
        {
            new User { Id = 1, FirstName = "A", LastName = "a" },
            null,
            new User { Id = 2, FirstName = "B", LastName = "b" }
        };

        var dtos = mapper.MapCollection<UserDto>(mixed.Cast<object>().ToList()).ToList();

        Assert.Equal(3, dtos.Count);
        Assert.Equal("A", dtos[0].FirstName);
        Assert.NotNull(dtos[1]);
        Assert.Equal(0, dtos[1].Id);
        Assert.Equal("", dtos[1].FirstName);
        Assert.Equal("B", dtos[2].FirstName);
    }

    [Fact]
    public void Test_NestedObjectMapping_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Address, AddressDto>();
            cfg.CreateMap<UserWithAddress, UserWithAddressDto>();
        });

        var mapper = config.CreateMapper();
        var user = new UserWithAddress
        {
            Id = 1,
            FirstName = "John",
            Address = new Address { Street = "123 Main St", City = "New York" }
        };

        // Act
        var userDto = mapper.Map<UserWithAddressDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.NotNull(userDto.Address);
        Assert.Equal("123 Main St", userDto.Address.Street);
        Assert.Equal("New York", userDto.Address.City);
    }

    [Fact]
    public void Test_MapToExistingDestination_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var existingDto = new UserDto { Id = 99, FirstName = "Old" };

        // Act
        var result = mapper.MapTo(user, existingDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.FirstName, result.FirstName);
    }

    [Fact]
    public void Test_InvalidMapping_ThrowsException()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var order = new Order { Id = 1, Amount = 100 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mapper.Map<UserDto>(order));
    }

    [Fact]
    public void Test_PreMappingCondition_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.When(src => src.Id > 0);
            });
        });

        var mapper = config.CreateMapper();
        var validUser = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var invalidUser = new User { Id = -1, FirstName = "Jane", LastName = "Smith" };

        // Act & Assert
        var result = mapper.Map<UserDto>(validUser);
        Assert.NotNull(result);

        Assert.Throws<InvalidOperationException>(() => mapper.Map<UserDto>(invalidUser));
    }

    [Fact]
    public void Test_CollectionPropertyMapping_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<OrderItem, OrderItemDto>();
            cfg.CreateMap<Order, OrderDto>();
        });

        var mapper = config.CreateMapper();
        var order = new Order
        {
            Id = 1,
            Amount = 100,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductName = "Item1", Price = 50 },
                new OrderItem { Id = 2, ProductName = "Item2", Price = 50 }
            }
        };

        // Act
        var orderDto = mapper.Map<OrderDto>(order);

        // Assert
        Assert.NotNull(orderDto);
        Assert.Equal(2, orderDto.Items.Count);
        Assert.Equal("Item1", orderDto.Items[0].ProductName);
    }

    [Fact]
    public void Test_CustomMapping_WithDelegate()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName.ToUpper()} {src.LastName.ToUpper()}"));
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act
        var userDto = mapper.Map<UserDto>(user);

        // Assert
        Assert.Equal("JOHN DOE", userDto.FullName);
    }

    [Fact]
    public void Test_NullSourceObject_CreatesDefaultDestination()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        User? nullUser = null;

        // Act
        var userDto = mapper.Map<UserDto>(nullUser!);

        // Assert
        Assert.NotNull(userDto);
    }
}

// Test Models
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? FullName { get; set; }
}

public class UserWithAddress
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public Address? Address { get; set; }
}

public class UserWithAddressDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public AddressDto? Address { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
}
