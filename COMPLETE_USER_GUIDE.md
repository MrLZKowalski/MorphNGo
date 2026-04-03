# Complete User Guide

Full guide for using Adaptix. Pick what you need.

## Table of Contents

- [Beginner Guide](#beginner-guide)
- [Intermediate Guide](#intermediate-guide)
- [Advanced Guide](#advanced-guide)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

# Beginner Guide

## The Basics

Adaptix maps properties from one object to another. Properties with matching names map automatically.

### Simple Example

```csharp
// These map automatically because names match
var user = new User { Id = 1, FirstName = "John" };
var dto = mapper.Map<UserDto>(user);
```

### When Names Don't Match

Use `ForMember` to handle different property names:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(dest => dest.Email, 
        opt => opt.MapFrom(src => src.EmailAddress));
});
```

### Ignore Properties

Skip properties you don't want to map:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.Ignore(dest => dest.Password);
    map.Ignore(dest => dest.InternalId);
});
```

## Working with Collections

Map lists and arrays automatically:

```csharp
// List<T>
var userDtos = users.Select(u => mapper.Map<UserDto>(u)).ToList();

// Arrays
UserDto[] dtoArray = users.Select(u => mapper.Map<UserDto>(u)).ToArray();

// Any IEnumerable
var dtoEnumerable = users.AsEnumerable()
    .Select(u => mapper.Map<UserDto>(u));
```

## Nested Objects

Adaptix maps nested objects automatically when both type mappings are configured:

```csharp
// Configure both mappings
cfg.CreateMap<User, UserDto>();
cfg.CreateMap<Address, AddressDto>();

// Now this works - Address -> AddressDto automatically
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public AddressDto Address { get; set; }  // Nested!
}

var user = new User { Id = 1, Address = new Address { City = "NYC" } };
var dto = mapper.Map<UserDto>(user);
// dto.Address is now an AddressDto with City = "NYC"
```

## ASP.NET Core Setup

In `Program.cs`:

```csharp
builder.Services.AddAdaptixMapper(cfg =>
{
    cfg.CreateMap<User, UserDto>();
    cfg.CreateMap<Order, OrderDto>();
    cfg.CreateMap<Product, ProductDto>();
});

var app = builder.Build();
```

Then inject and use:

```csharp
public class UsersController : ControllerBase
{
    private readonly IMapper _mapper;

    public UsersController(IMapper mapper) => _mapper = mapper;

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var user = _service.GetUser(id);
        return Ok(_mapper.Map<UserDto>(user));
    }
}
```

---

# Intermediate Guide

## Combining Properties

Merge multiple source properties into one destination property:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(dest => dest.FullName, opt =>
        opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
});

var user = new User { FirstName = "John", LastName = "Doe" };
var dto = mapper.Map<UserDto>(user);
// dto.FullName = "John Doe"
```

## Conditional Mapping

Only map a property if a condition is met:

```csharp
cfg.CreateMap<Order, OrderDto>(map =>
{
    map.ForMember(d => d.DiscountAmount, opt =>
        opt.When(s => s.IsVip)
           .MapFrom(s => s.Total * 0.2m));
});

var vipOrder = new Order { IsVip = true, Total = 100m };
var dto = mapper.Map<OrderDto>(vipOrder);
// dto.DiscountAmount = 20 (because IsVip = true)

var normalOrder = new Order { IsVip = false, Total = 100m };
var dto2 = mapper.Map<OrderDto>(normalOrder);
// dto2.DiscountAmount = null (condition not met, property skipped)
```

## Static Values

Map to constant or looked-up values:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    // Always use this value
    map.ForMember(d => d.Source, opt =>
        opt.MapFromStaticData("API"));
    
    // Or look it up
    map.ForMember(d => d.UserType, opt =>
        opt.MapFromStaticData(_userTypeService.GetType(src.RoleId)));
});
```

## Value Transformers

Apply transformations to all values of a type:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    // All strings: replace nulls with "(empty)"
    map.WithValueTransformer<string>(value => 
        string.IsNullOrEmpty(value) ? "(empty)" : value);
});

var user = new User { FirstName = "", LastName = "Doe" };
var dto = mapper.Map<UserDto>(user);
// dto.FirstName = "(empty)"
// dto.LastName = "Doe"
```

## Bidirectional Mapping

Configure reverse mapping manually:

```csharp
// User -> UserDto
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(d => d.Email, opt =>
        opt.MapFrom(s => s.EmailAddress));
});

// UserDto -> User (manually)
cfg.CreateMap<UserDto, User>(map =>
{
    map.ForMember(d => d.EmailAddress, opt =>
        opt.MapFrom(s => s.Email));
});

var userDto = new UserDto { Email = "john@example.com" };
var user = mapper.Map<User>(userDto);
// user.EmailAddress = "john@example.com"
```

## Multiple Type Mappings

Handle different scenarios with different configurations:

```csharp
// Full details
cfg.CreateMap<User, UserDetailedDto>();

// Summary only
cfg.CreateMap<User, UserSummaryDto>();

// Admin view
cfg.CreateMap<User, AdminUserDto>();

var user = new User { /* ... */ };
var detailed = mapper.Map<UserDetailedDto>(user);
var summary = mapper.Map<UserSummaryDto>(user);
var admin = mapper.Map<AdminUserDto>(user);
```

## Working with Entity Framework

Map EF entities to DTOs:

```csharp
public class UserService
{
    private readonly DbContext _db;
    private readonly IMapper _mapper;

    public async Task<List<UserDto>> GetAll()
    {
        var users = await _db.Users.ToListAsync();
        return users.Select(u => _mapper.Map<UserDto>(u)).ToList();
    }

    public async Task<UserDetailDto> GetWithOrders(int id)
    {
        var user = await _db.Users
            .Include(u => u.Orders)
            .FirstAsync(u => u.Id == id);
        
        return _mapper.Map<UserDetailDto>(user);
    }
}
```

---

# Advanced Guide

## Complex Business Logic

Implement sophisticated mapping logic:

```csharp
cfg.CreateMap<Order, OrderDto>(map =>
{
    // Calculate discount based on multiple factors
    map.ForMember(d => d.FinalPrice, opt =>
        opt.MapFrom(src =>
        {
            var basePrice = src.LineItems.Sum(li => li.Price * li.Quantity);
            var discount = 0m;
            
            if (src.IsVip) discount += 0.2m;
            if (src.OrderDate < DateTime.Now.AddMonths(-6)) discount += 0.1m;
            if (basePrice > 1000) discount += 0.05m;
            
            return basePrice * (1 - discount);
        }));
});
```

## Custom Mapping Logic

Override entire mapping with custom function:

```csharp
cfg.CreateMap<ComplexSource, ComplexDestination>(map =>
{
    map.WithCustomMapping((src, dest) =>
    {
        // Your custom logic here
        dest.Id = src.Id;
        dest.Name = src.Name.ToUpper();
        dest.Items = src.Items.Where(x => x.Active).ToList();
        
        return dest;
    });
});
```

## Null Handling

Handle nulls gracefully:

```csharp
cfg.CreateMap<Order, OrderDto>(map =>
{
    map.ForMember(d => d.CustomerName, opt =>
        opt.MapFrom(src => src.Customer?.Name ?? "Unknown"));
    
    map.ForMember(d => d.Status, opt =>
        opt.MapFrom(src => src.Status ?? "Pending"));
});

var orderWithoutCustomer = new Order { Customer = null };
var dto = mapper.Map<OrderDto>(orderWithoutCustomer);
// dto.CustomerName = "Unknown"
```

## Lifetime Options

Choose how long the mapper lives:

```csharp
// Singleton - one instance for entire app (recommended)
builder.Services.AddAdaptixMapper(cfg => { }, ServiceLifetime.Singleton);

// Scoped - new per HTTP request
builder.Services.AddAdaptixMapper(cfg => { }, ServiceLifetime.Scoped);

// Transient - new every time
builder.Services.AddAdaptixMapper(cfg => { }, ServiceLifetime.Transient);
```

For most applications, **Singleton is recommended** - the mapper is thread-safe and can be reused.

## Pre-Mapping Conditions

Skip mapping if a condition isn't met:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    // Don't map if user is inactive
    map.When(src => src.IsActive);
});

var inactiveUser = new User { IsActive = false };
try
{
    var dto = mapper.Map<UserDto>(inactiveUser);
    // This throws an exception
}
catch (InvalidOperationException ex)
{
    // "Pre-mapping condition failed"
}
```

## Type-Level Transformations

Apply logic at the type level:

```csharp
cfg.CreateMap<SourceClass, DestinationClass>(map =>
{
    map.WithValueTransformer<string>(v => v?.Trim());
    map.WithValueTransformer<int?>(v => v ?? 0);
});
```

---

# Best Practices

## Organization

Keep mappings organized by feature:

```csharp
// In a separate class
public static class MappingConfiguration
{
    public static void ConfigureUserMappings(MapperConfiguration config)
    {
        config.CreateMap<User, UserDto>();
        config.CreateMap<User, UserDetailDto>();
    }

    public static void ConfigureOrderMappings(MapperConfiguration config)
    {
        config.CreateMap<Order, OrderDto>();
        config.CreateMap<OrderItem, OrderItemDto>();
    }
}

// In Program.cs
builder.Services.AddAdaptixMapper(cfg =>
{
    MappingConfiguration.ConfigureUserMappings(cfg);
    MappingConfiguration.ConfigureOrderMappings(cfg);
});
```

## Configuration Upfront

Configure all mappings at startup, not during mapping:

```csharp
// ✅ Good
builder.Services.AddAdaptixMapper(cfg =>
{
    cfg.CreateMap<User, UserDto>();
    cfg.CreateMap<Order, OrderDto>();
    cfg.CreateMap<Product, ProductDto>();
});

// ❌ Avoid - configuring in your controller
public IActionResult Get()
{
    var config = new MapperConfiguration(cfg => 
        cfg.CreateMap<User, UserDto>()); // Don't do this!
    var mapper = config.CreateMapper();
}
```

## Reuse the Mapper

Use dependency injection to get the mapper:

```csharp
// ✅ Good
public class UserService
{
    private readonly IMapper _mapper;

    public UserService(IMapper mapper) => _mapper = mapper;

    public UserDto GetUserDto(int id)
    {
        var user = _repository.GetUser(id);
        return _mapper.Map<UserDto>(user);
    }
}

// ❌ Avoid - creating new mappers
public UserDto GetUserDto(int id)
{
    var config = new MapperConfiguration(cfg =>
        cfg.CreateMap<User, UserDto>());
    var mapper = config.CreateMapper();
    return mapper.Map<UserDto>(_repository.GetUser(id));
}
```

## Test Your Mappings

Write simple tests:

```csharp
[Fact]
public void UserMapping_ShouldMapAllProperties()
{
    var config = new MapperConfiguration(cfg =>
        cfg.CreateMap<User, UserDto>());
    var mapper = config.CreateMapper();

    var user = new User { Id = 1, FirstName = "John" };
    var dto = mapper.Map<UserDto>(user);

    Assert.Equal(1, dto.Id);
    Assert.Equal("John", dto.FirstName);
}
```

---

# Troubleshooting

## "No mapping configured from X to Y"

You forgot to register the mapping.

```csharp
// Add this
builder.Services.AddAdaptixMapper(cfg =>
{
    cfg.CreateMap<MySource, MyDest>();  // <-- Add this
});
```

## Property Not Being Mapped

Check:
1. Are the property names exactly the same? (case-insensitive match)
2. Is the mapping configured?
3. Are the types compatible?

```csharp
// If names don't match, use ForMember
cfg.CreateMap<Source, Dest>(map =>
{
    map.ForMember(d => d.PropertyName, 
        opt => opt.MapFrom(s => s.DifferentName));
});
```

## Null Reference Exception

Adaptix handles nulls, but check if your custom logic does:

```csharp
// Safe
map.ForMember(d => d.Name, opt =>
    opt.MapFrom(s => s?.Name ?? "Unknown"));

// Not safe
map.ForMember(d => d.Name, opt =>
    opt.MapFrom(s => s.Name.ToUpper())); // Will throw if Name is null
```

## Nested Objects Not Mapping

Make sure you configure both type mappings:

```csharp
cfg.CreateMap<User, UserDto>();
cfg.CreateMap<Address, AddressDto>();  // <-- Don't forget this
```

## Property Ignored Unexpectedly

Check if you explicitly ignored it:

```csharp
// Remove this if you don't want to ignore
map.Ignore(d => d.PropertyName);
```

## Performance Issues

Use Singleton lifetime and avoid recreating configurations:

```csharp
// Once at startup
builder.Services.AddAdaptixMapper(cfg => 
{
    // All configurations here
}, ServiceLifetime.Singleton);  // Singleton!

// Then reuse the injected IMapper
```

## Configuration Validation

If something seems wrong, check your mapping:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<User, UserDto>();
});

try
{
    var mapper = config.CreateMapper();
}
catch (Exception ex)
{
    Console.WriteLine($"Mapping error: {ex.Message}");
}
```

---

## Common Questions

**Q: Can I map between different namespaces?**  
A: Yes, namespaces don't matter. Only types matter.

**Q: Can I have multiple mappings for the same source?**  
A: Yes, create separate CreateMap calls for different destinations.

**Q: Is the mapper thread-safe?**  
A: Yes, once configured, it's safe for concurrent use.

**Q: Can I modify mappings after creating the mapper?**  
A: No, configure everything upfront.

**Q: What if I need more complex scenarios?**  
A: Use custom mapping functions or write the transformation in your service layer.

---

## See Also

- [Quick Start](QUICKSTART.md)
- [Code Examples](src/Adaptix/Examples.cs)
- [Tests](src/Adaptix.UnitTests/) - Real-world examples in test code
