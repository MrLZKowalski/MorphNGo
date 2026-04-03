using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;
using MorphNGo.Mapping.Extensions;
using MorphNGo.Mapping.Interfaces;

namespace MorphNGo.Examples;

/// <summary>
/// Comprehensive examples demonstrating MorphNGo mapper usage patterns.
/// </summary>
public static class ExamplesAndBestPractices
{
    // Get a logger instance - in real scenarios this comes from DI
    private static ILogger GetLogger() => NullLogger.Instance;
    // ============= BASIC MAPPING EXAMPLES =============

    /// <summary>
    /// Example 1: Simple property-to-property mapping
    /// </summary>
    public static void Example_BasicMapping()
    {
        // Create configuration
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        // Create mapper
        var mapper = config.CreateMapper();

        // Use mapper
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = mapper.Map<UserDto>(user);

        Console.WriteLine($"Mapped: {userDto.FirstName} {userDto.LastName}");
    }

    /// <summary>
    /// Example 2: Custom property mapping with transformations
    /// </summary>
    public static void Example_CustomMapping()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                // Custom mapping function
                builder.ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

                // Property renaming
                builder.ForMember(dest => dest.UserId,
                    opt => opt.From("Id"));
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = mapper.Map<UserDto>(user);

        Console.WriteLine($"Full Name: {userDto.FullName}");
    }

    /// <summary>
    /// Example 3: Conditional mapping
    /// </summary>
    public static void Example_ConditionalMapping()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                // Only map premium features if user is premium
                builder.ForMember(dest => dest.PremiumFeatures,
                    opt => opt.When(src => src.IsPremium));

                // Only map if condition is true (pre-mapping)
                builder.When(src => src.IsActive);
            });
        });

        var mapper = config.CreateMapper();
        var activeUser = new User { Id = 1, FirstName = "John", IsActive = true, IsPremium = true };
        var inactiveUser = new User { Id = 2, FirstName = "Jane", IsActive = false };

        var activeDto = mapper.Map<UserDto>(activeUser);
        // inactiveDto would throw because of the When condition
    }

    /// <summary>
    /// Example 4: Collection mapping
    /// </summary>
    public static void Example_CollectionMapping()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<OrderItem, OrderItemDto>();
            cfg.CreateMap<Order, OrderDto>();
        });

        var mapper = config.CreateMapper();
        var orders = new List<Order>
        {
            new Order
            {
                Id = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 1, ProductName = "Item1", Price = 50 }
                }
            }
        };

        var orderDtos = mapper.MapCollection<OrderDto>(orders.Cast<object>().ToList()).ToList();
        Console.WriteLine($"Mapped {orderDtos.Count} orders");
    }

    /// <summary>
    /// Example 5: Nested object mapping
    /// </summary>
    public static void Example_NestedMapping()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Address, AddressDto>();
            cfg.CreateMap<Customer, CustomerDto>();
        });

        var mapper = config.CreateMapper();
        var customer = new Customer
        {
            Id = 1,
            Name = "John",
            Address = new Address { Street = "123 Main", City = "NY" }
        };

        var customerDto = mapper.Map<CustomerDto>(customer);
        Console.WriteLine($"Customer at {customerDto.Address?.City}");
    }

    /// <summary>
    /// Example 6: Mapping from static data sources
    /// </summary>
    public static void Example_StaticDataMapping()
    {
        var statusLookup = new Dictionary<int, string>
        {
            { 1, "Active" },
            { 2, "Inactive" }
        };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<UserWithStatus, UserStatusDto>(builder =>
            {
                builder.ForMember(dest => dest.StatusName,
                    opt => opt.MapFromStaticData(src =>
                        statusLookup.TryGetValue(src.StatusId, out var status) ? status : "Unknown"));
            });
        });

        var mapper = config.CreateMapper();
        var user = new UserWithStatus { Id = 1, FirstName = "John", StatusId = 1 };
        var userDto = mapper.Map<UserStatusDto>(user);

        Console.WriteLine($"Status: {userDto.StatusName}");
    }

    /// <summary>
    /// Example 7: Computed properties
    /// </summary>
    public static void Example_ComputedProperties()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Order, OrderSummary>(builder =>
            {
                builder.ForMember(dest => dest.TotalAmount,
                    opt => opt.MapFrom(src => src.Items.Sum(i => i.Price * i.Quantity)));
                builder.ForMember(dest => dest.ItemCount,
                    opt => opt.MapFrom(src => src.Items.Count));
            });
        });

        var mapper = config.CreateMapper();
        var order = new Order
        {
            Id = 1,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductName = "A", Price = 50, Quantity = 2 },
                new OrderItem { Id = 2, ProductName = "B", Price = 30, Quantity = 1 }
            }
        };

        var summary = mapper.Map<OrderSummary>(order);
        Console.WriteLine($"Total: ${summary.TotalAmount}, Items: {summary.ItemCount}");
    }

    /// <summary>
    /// Example 8: Ignoring properties
    /// </summary>
    public static void Example_IgnoringProperties()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>(builder =>
            {
                builder.Ignore(dest => dest.FullName);
                builder.Ignore(dest => dest.InternalNotes);
            });
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = mapper.Map<UserDto>(user);

        Console.WriteLine($"FullName is null/default: {userDto.FullName}");
    }

    // ============= DEPENDENCY INJECTION EXAMPLES =============

    /// <summary>
    /// Example 9: Register mapper with dependency injection
    /// </summary>
    public static void Example_DependencyInjection()
    {
        var services = new ServiceCollection();

        // Register mapper with DI
        var logger = GetLogger();
        services.AddMorphNGoMapper(logger, cfg =>
        {
            cfg.CreateMap<User, UserDto>();
            cfg.CreateMap<Order, OrderDto>();
        });

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = mapper.Map<UserDto>(user);

        Console.WriteLine($"DI Mapper: {userDto.FirstName}");
    }

    /// <summary>
    /// Example 10: Inject mapper into a service
    /// </summary>
    public static void Example_ServiceWithInjectedMapper()
    {
        var services = new ServiceCollection();
        var logger = GetLogger();
        services.AddMorphNGoMapper(logger, cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });
        services.AddScoped<UserService>();

        var provider = services.BuildServiceProvider();
        var userService = provider.GetRequiredService<UserService>();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var userDto = userService.ConvertUserToDto(user);

        Console.WriteLine($"Service Mapper: {userDto.FirstName}");
    }

    /// <summary>
    /// Example 11: Different service lifetimes
    /// </summary>
    public static void Example_ServiceLifetimes()
    {
        var services = new ServiceCollection();
        var logger = GetLogger();

        // Scoped (default)
        services.AddMorphNGoMapper(logger, cfg => cfg.CreateMap<User, UserDto>(), ServiceLifetime.Scoped);

        // Transient
        services.AddMorphNGoMapper(logger, cfg => cfg.CreateMap<User, UserDto>(), ServiceLifetime.Transient);

        // Singleton
        services.AddMorphNGoMapper(logger, cfg => cfg.CreateMap<User, UserDto>(), ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();
        Console.WriteLine("Service lifetimes configured");
    }

    // ============= ADVANCED PATTERNS =============

    /// <summary>
    /// Example 12: Multiple mappings for same source/destination
    /// </summary>
    public static void Example_ProfileBasedConfiguration()
    {
        // Create separate configurations for different contexts
        var apiConfig = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserApiDto>(builder =>
            {
                builder.ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
            });
        });

        var databaseConfig = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDbDto>(builder =>
            {
                builder.ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.LastName}, {src.FirstName}"));
            });
        });

        var apiMapper = apiConfig.CreateMapper();
        var dbMapper = databaseConfig.CreateMapper();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var apiDto = apiMapper.Map<UserApiDto>(user);
        var dbDto = dbMapper.Map<UserDbDto>(user);

        Console.WriteLine($"API: {apiDto.FullName}");
        Console.WriteLine($"DB: {dbDto.FullName}");
    }

    /// <summary>
    /// Example 13: Mapping to existing object
    /// </summary>
    public static void Example_MapToExistingObject()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        });

        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var existingDto = new UserDto { FirstName = "Old" };

        // Populate existing object
        mapper.MapTo(user, existingDto);

        Console.WriteLine($"Updated object: {existingDto.FirstName}");
    }

    /// <summary>
    /// Example 14: Type-level custom mapping
    /// </summary>
    public static void Example_TypeLevelCustomMapping()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<LegacyUser, User>(builder =>
            {
                builder.WithCustomMapping((src, dest) =>
                {
                    dest.Id = src.LegacyId;
                    var nameParts = src.FullName.Split(' ');
                    dest.FirstName = nameParts[0];
                    dest.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                    return dest;
                });
            });
        });

        var mapper = config.CreateMapper();
        var legacyUser = new LegacyUser { LegacyId = 1, FullName = "John Doe" };
        var user = mapper.Map<User>(legacyUser);

        Console.WriteLine($"Converted: {user.FirstName} {user.LastName}");
    }

    /// <summary>
    /// Example 15: Runtime parameters (variadic) for mapping with lookup data
    /// </summary>
    public static void Example_ContextBasedMapping()
    {
        // Define lookup data that will be passed at mapping time
        var roleLookup = new Dictionary<int, string>
        {
            { 1, "Admin" },
            { 2, "Manager" },
            { 3, "User" }
        };

        var permissionLookup = new Dictionary<int, string>
        {
            { 1, "CanRead,CanWrite" },
            { 2, "CanRead,CanWrite,CanDelete" },
            { 3, "CanRead" }
        };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<UserWithRoleId, UserWithRoleDto>(builder =>
            {
                // Map role name using closure-based static data
                builder.ForMember(dest => dest.RoleName,
                    opt => opt.MapFromStaticData(src =>
                    {
                        return roleLookup.TryGetValue(src.RoleId, out var role) ? role : "Unknown";
                    }));

                // Pass runtime data via Map(params): use MapFrom with (source, parameters) when the delegate has two parameters
                builder.ForMember(dest => dest.Permissions,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length > 0 && parameters[0] is Dictionary<int, string> perms)
                        {
                            return perms.TryGetValue(src.RoleId, out var permission) ? permission : "NoPermissions";
                        }
                        return "NoPermissions";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var user = new UserWithRoleId { Id = 1, FirstName = "John", RoleId = 2 };

        // Pass lookup and optional metadata as separate arguments (variadic params)
        var userDto = mapper.Map<UserWithRoleDto>(
            user,
            permissionLookup,
            DateTime.UtcNow,
            Guid.NewGuid().ToString());

        Console.WriteLine($"User: {userDto.FirstName}, Role: {userDto.RoleName}, Permissions: {userDto.Permissions}");
    }

    /// <summary>
    /// Example 16: Collection mapping with runtime parameters
    /// </summary>
    public static void Example_CollectionMappingWithContext()
    {
        var departmentLookup = new Dictionary<int, string>
        {
            { 1, "Engineering" },
            { 2, "Sales" },
            { 3, "Support" }
        };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Employee, EmployeeDto>(builder =>
            {
                builder.ForMember(dest => dest.DepartmentName,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length > 0 && parameters[0] is Dictionary<int, string> depts)
                        {
                            return depts.TryGetValue(src.DepartmentId, out var dept) ? dept : "Unknown";
                        }
                        return "Unknown";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var employees = new[]
        {
            new Employee { Id = 1, Name = "Alice", DepartmentId = 1 },
            new Employee { Id = 2, Name = "Bob", DepartmentId = 2 },
            new Employee { Id = 3, Name = "Charlie", DepartmentId = 3 }
        }.Cast<object>().ToList();

        // Same parameters are passed for every item in the collection
        var employeeDtos = mapper.MapCollection<EmployeeDto>(employees, departmentLookup).ToList();

        foreach (var emp in employeeDtos)
        {
            Console.WriteLine($"Employee: {emp.Name}, Department: {emp.DepartmentName}");
        }
    }
}

// Example Models
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsPremium { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? FullName { get; set; }
    public string? PremiumFeatures { get; set; }
    public string? InternalNotes { get; set; }
}

public class UserApiDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? FullName { get; set; }
}

public class UserDbDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? FullName { get; set; }
}

public class UserWithStatus
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public int StatusId { get; set; }
}

public class UserStatusDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? StatusName { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderSummary
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
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

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address? Address { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressDto? Address { get; set; }
}

public class LegacyUser
{
    public int LegacyId { get; set; }
    public string FullName { get; set; } = "";
}

public class UserWithRoleId
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public int RoleId { get; set; }
}

public class UserWithRoleDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? RoleName { get; set; }
    public string? Permissions { get; set; }
}

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DepartmentId { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Example service showing mapper injection
/// </summary>
public class UserService
{
    private readonly IMapper _mapper;

    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto ConvertUserToDto(User user)
    {
        return _mapper.Map<UserDto>(user);
    }
}
