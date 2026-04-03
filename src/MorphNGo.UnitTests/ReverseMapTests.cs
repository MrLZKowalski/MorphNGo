namespace MorphNGo.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

/// <summary>
/// Unit tests for bidirectional mapping with ReverseMap functionality.
/// Tests cover automatic reverse mapping, property preservation, and complex scenarios.
/// </summary>
public class ReverseMapTests
{
    private static ILogger GetLogger() => NullLogger.Instance;
    [Fact]
    public void Test_ReverseMap_BasicBidirectionalMapping()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(map =>
            {
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act - Map forward
        var userDto = mapper.Map<UserDto>(user);

        // Act - Map reverse
        var userMapped = mapper.Map<User>(userDto);

        // Assert
        Assert.NotNull(userDto);
        Assert.NotNull(userMapped);
        Assert.Equal(user.Id, userDto.Id);
        Assert.Equal(user.FirstName, userDto.FirstName);
        Assert.Equal(user.LastName, userDto.LastName);
        Assert.Equal(userDto.Id, userMapped.Id);
        Assert.Equal(userDto.FirstName, userMapped.FirstName);
        Assert.Equal(userDto.LastName, userMapped.LastName);
    }

    [Fact]
    public void Test_ReverseMap_WithIgnoredProperties()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<UserWithSensitiveData, UserWithSensitiveDataDto>(map =>
            {
                map.Ignore(dest => dest.Password);
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var user = new UserWithSensitiveData
        {
            Id = 1,
            FirstName = "John",
            Password = "SecretPassword123"
        };

        // Act - Map forward (Password should be ignored)
        var userDto = mapper.Map<UserWithSensitiveDataDto>(user);

        // Act - Map reverse (Password should be ignored)
        var userMapped = mapper.Map<UserWithSensitiveData>(userDto);

        // Assert
        Assert.NotNull(userDto);
        Assert.Null(userDto.Password);
        Assert.NotNull(userMapped);
        Assert.Null(userMapped.Password);
        Assert.Equal(user.Id, userDto.Id);
        Assert.Equal(user.FirstName, userDto.FirstName);
    }

    [Fact]
    public void Test_ReverseMap_WithSimplePropertyRename_Reversed()
    {
        // Arrange - Simple property renames should be automatically reversed
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<PersonWithOldProps, PersonWithNewProps>(map =>
            {
                map.ForMember(dest => dest.FullName,
                    opt => opt.From("CompleteName"));
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var person = new PersonWithOldProps { Id = 1, CompleteName = "John Doe" };

        // Act - Map forward
        var personDto = mapper.Map<PersonWithNewProps>(person);

        // Assert forward mapping
        Assert.NotNull(personDto);
        Assert.Equal("John Doe", personDto.FullName);

        // Act - Map reverse (should use reversed mapping: CompleteName <- FullName)
        var personMapped = mapper.Map<PersonWithOldProps>(personDto);

        // Assert reverse mapping - property rename is reversed
        Assert.NotNull(personMapped);
        Assert.Equal(person.Id, personMapped.Id);
        Assert.Equal(person.CompleteName, personMapped.CompleteName);
    }

    [Fact]
    public void Test_ReverseMap_WithComplexCustomMapping_ForwardOnly()
    {
        // Arrange - Complex custom mappings (MapFrom with functions) only apply to the forward direction
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Employee, EmployeeDto>(map =>
            {
                map.ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var employee = new Employee { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act - Map forward
        var employeeDto = mapper.Map<EmployeeDto>(employee);

        // Assert forward mapping
        Assert.NotNull(employeeDto);
        Assert.Equal("John Doe", employeeDto.FullName);

        // Act - Map reverse (reverse mapping uses automatic property mapping)
        var employeeMapped = mapper.Map<Employee>(employeeDto);

        // Assert reverse mapping - complex mappings are not reversed, automatic matching is used
        Assert.NotNull(employeeMapped);
        Assert.Equal(employee.Id, employeeMapped.Id);
        // FullName doesn't match FirstName/LastName, so they won't be auto-populated from FullName
    }

    [Fact]
    public void Test_ReverseMap_WithNestedObjects()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Address, AddressDto>(map =>
            {
                map.ReverseMap();
            });
            cfg.CreateMap<UserWithAddress, UserWithAddressDto>(map =>
            {
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var user = new UserWithAddress
        {
            Id = 1,
            FirstName = "John",
            Address = new Address { Street = "123 Main St", City = "New York" }
        };

        // Act - Map forward
        var userDto = mapper.Map<UserWithAddressDto>(user);

        // Act - Map reverse
        var userMapped = mapper.Map<UserWithAddress>(userDto);

        // Assert
        Assert.NotNull(userDto);
        Assert.NotNull(userDto.Address);
        Assert.Equal(user.Address.Street, userDto.Address.Street);
        Assert.Equal(user.Address.City, userDto.Address.City);

        Assert.NotNull(userMapped);
        Assert.NotNull(userMapped.Address);
        Assert.Equal(user.FirstName, userMapped.FirstName);
        Assert.Equal(user.Address.Street, userMapped.Address.Street);
    }

    [Fact]
    public void Test_ReverseMap_WithCollections()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<OrderItem, OrderItemDto>(map =>
            {
                map.ReverseMap();
            });
            cfg.CreateMap<Order, OrderDto>(map =>
            {
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var order = new Order
        {
            Id = 1,
            Amount = 150,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductName = "Item1", Price = 75 },
                new OrderItem { Id = 2, ProductName = "Item2", Price = 75 }
            }
        };

        // Act - Map forward
        var orderDto = mapper.Map<OrderDto>(order);

        // Act - Map reverse
        var orderMapped = mapper.Map<Order>(orderDto);

        // Assert
        Assert.NotNull(orderDto);
        Assert.Equal(2, orderDto.Items.Count);
        Assert.Equal("Item1", orderDto.Items[0].ProductName);
        Assert.Equal(75, orderDto.Items[0].Price);

        Assert.NotNull(orderMapped);
        Assert.Equal(2, orderMapped.Items.Count);
        Assert.Equal(order.Amount, orderMapped.Amount);
        Assert.Equal(order.Items[0].ProductName, orderMapped.Items[0].ProductName);
    }

    [Fact]
    public void Test_ReverseMap_ComplexScenario_IgnoresAndAutomaticMapping()
    {
        // Arrange - Ignored properties are preserved in reverse mapping, automatic property matching applies
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Product, ProductDto>(map =>
            {
                map.Ignore(dest => dest.InternalId);
                map.Ignore(dest => dest.CreatedDate);
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            InternalId = "INT-001",
            CreatedDate = DateTime.Now
        };

        // Act - Map forward
        var productDto = mapper.Map<ProductDto>(product);

        // Act - Map reverse
        var productMapped = mapper.Map<Product>(productDto);

        // Assert forward mapping
        Assert.NotNull(productDto);
        Assert.Equal("Laptop", productDto.Name);
        Assert.Null(productDto.InternalId);
        Assert.Equal(DateTime.MinValue, productDto.CreatedDate);

        // Assert reverse mapping - ignored properties remain null/default
        Assert.NotNull(productMapped);
        Assert.Equal(product.Id, productMapped.Id);
        Assert.Equal(product.Name, productMapped.Name);
        // InternalId and CreatedDate are ignored in reverse mapping as well
        Assert.Null(productMapped.InternalId);
        Assert.Equal(DateTime.MinValue, productMapped.CreatedDate);
    }

    [Fact]
    public void Test_ReverseMap_BothDirectionsMapped()
    {
        // Arrange - Configure only forward mapping with ReverseMap
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Customer, CustomerDto>(map =>
            {
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();

        // Act & Assert - Both directions should work
        var customer = new Customer { Id = 1, Name = "John" };
        var customerDto = mapper.Map<CustomerDto>(customer);
        Assert.Equal(customer.Id, customerDto.Id);
        Assert.Equal(customer.Name, customerDto.Name);

        var customerMappedBack = mapper.Map<Customer>(customerDto);
        Assert.Equal(customerDto.Id, customerMappedBack.Id);
        Assert.Equal(customerDto.Name, customerMappedBack.Name);
    }

    [Fact]
    public void Test_ReverseMap_WithMultipleConfigurations()
    {
        // Arrange - Multiple ReverseMap configurations
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(map =>
            {
                map.ReverseMap();
            });
            cfg.CreateMap<Order, OrderDto>(map =>
            {
                map.ReverseMap();
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "Alice", LastName = "Johnson" };
        var order = new Order { Id = 1, Amount = 200 };

        // Act
        var userDto = mapper.Map<UserDto>(user);
        var orderDto = mapper.Map<OrderDto>(order);
        var userMappedBack = mapper.Map<User>(userDto);
        var orderMappedBack = mapper.Map<Order>(orderDto);

        // Assert
        Assert.Equal(user.FirstName, userDto.FirstName);
        Assert.Equal(order.Amount, orderDto.Amount);
        Assert.Equal(userDto.FirstName, userMappedBack.FirstName);
        Assert.Equal(orderDto.Amount, orderMappedBack.Amount);
    }
}

// Additional Test Models for ReverseMap Tests
public class UserWithSensitiveData
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? Password { get; set; }
}

public class UserWithSensitiveDataDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? Password { get; set; }
}

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? InternalId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? InternalId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class PersonWithOldProps
{
    public int Id { get; set; }
    public string CompleteName { get; set; } = "";
}

public class PersonWithNewProps
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
}
