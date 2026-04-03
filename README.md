# MorphNGo

A simple .NET 10 library for mapping data between objects. No complex configurations, just straightforward transformations.

## Install

Via NuGet Package Manager:

```bash
dotnet add package MorphNGo
```

Or via Package Manager Console:

```powershell
Install-Package MorphNGo
```

Or search for **MorphNGo** on [NuGet.org](https://www.nuget.org/packages/MorphNGo)

## Setup

In `Program.cs`:

```csharp
builder.Services.AddMorphNGoMapper(cfg =>
{
    cfg.CreateMap<User, UserDto>();
});
```

## Use

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMapper _mapper;

    public UsersController(IMapper mapper) => _mapper = mapper;

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = _userService.GetUser(id);
        return Ok(_mapper.Map<UserDto>(user));
    }
}
```

## What It Does

**Before:**
```csharp
var dto = new UserDto 
{
    Id = user.Id,
    FirstName = user.FirstName,
    LastName = user.LastName,
    Email = user.EmailAddress
};
```

**After:**
```csharp
var dto = mapper.Map<UserDto>(user);
```

## Features

- Automatic property mapping (same-name properties)
- Custom mapping with `ForMember()`
- Property renaming with `From()`
- Property ignoring with `Ignore()`
- Nested objects and collections
- Conditional mapping with `When()`
- Value transformers
- Full DI integration
- **Runtime parameters**: pass `params object[]` to `Map` / `MapTo` / `MapCollection`, and use `MapFrom((src, parameters) => …)` to read lookups or options per call (see [Runtime parameters guide](CONTEXT_BASED_MAPPING.md))

## Configuration Examples

### Custom Property Mapping

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(dest => dest.Email, 
        opt => opt.MapFrom(src => src.EmailAddress));
});
```

### Ignore Properties

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.Ignore(dest => dest.Password);
});
```

### Conditional Mapping

```csharp
cfg.CreateMap<Order, OrderDto>(map =>
{
    map.ForMember(d => d.Discount, opt =>
        opt.When(src => src.Total > 100)
           .MapFrom(src => src.Total * 0.1m));
});
```

### Bidirectional Mapping with ReverseMap

Define a mapping in one direction and automatically create the reverse mapping:

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(dest => dest.Email, 
        opt => opt.From("EmailAddress"));  // Simple property rename
    map.Ignore(dest => dest.Password);
    map.ReverseMap(); // Automatically creates UserDto -> User mapping
});

// Now you can map in both directions:
var dto = mapper.Map<UserDto>(user);      // User -> UserDto
var user = mapper.Map<User>(dto);         // UserDto -> User
```

The reverse mapping automatically:
- ✅ **Reverses simple property renames** (`From()` mappings)
- ✅ **Preserves ignored properties** (excluded in both directions)
- ✅ **Copies value transformers** (applied in both directions)
- ℹ️ **Skips complex mappings** (`MapFrom()` with custom functions only apply forward)

**Example with property rename reversal:**
```csharp
cfg.CreateMap<Person, PersonDto>(map =>
{
    // Forward: Person.FirstName -> PersonDto.FirstName
    // Reverse: PersonDto.FirstName -> Person.FirstName (automatic!)
    map.ForMember(dest => dest.FirstName, 
        opt => opt.From("FirstName"));

    // Forward: Person.LastName -> PersonDto.FullName (custom function - forward only)
    // Reverse: Uses automatic property matching
    map.ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

    map.ReverseMap();
});
```

### Lifetime Options

```csharp
// Singleton (recommended for most cases)
builder.Services.AddMorphNGoMapper(cfg => { }, ServiceLifetime.Singleton);

// Scoped (new per HTTP request)
builder.Services.AddMorphNGoMapper(cfg => { }, ServiceLifetime.Scoped);

// Transient (new every time)
builder.Services.AddMorphNGoMapper(cfg => { }, ServiceLifetime.Transient);
```

## Documentation

- [Quick Start Guide](QUICKSTART.md)
- [Complete User Guide](COMPLETE_USER_GUIDE.md)
- [Code Examples](src/MorphNGo/Examples/Examples.cs) — includes parameterized mapping (examples 15–16)

## Tests

53 tests. Run with:
```bash
dotnet test
```

## License

Apache 2.0. See [LICENSE](LICENSE) for details.
