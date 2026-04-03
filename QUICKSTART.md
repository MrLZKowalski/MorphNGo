# Quick Start Guide

Get MorphNGo up and running in 5 minutes.

## 1. Install

```bash
dotnet add package MorphNGo
```

## 2. Create Your Classes

```csharp
// Your domain model
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
}

// Your API DTO
public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }  // Different name!
}
```

## 3. Configure in Program.cs

```csharp
builder.Services.AddMorphNGoMapper(cfg =>
{
    cfg.CreateMap<User, UserDto>(map =>
    {
        // Map EmailAddress -> Email
        map.ForMember(d => d.Email, opt => 
            opt.MapFrom(s => s.EmailAddress));
    });
});
```

## 4. Use It

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

That's it! You're mapping objects.

## Common Tasks

### Map a Collection

```csharp
var users = userList.Select(u => _mapper.Map<UserDto>(u)).ToList();
```

### Ignore a Property

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.Ignore(d => d.Password);
});
```

### Rename a Property

```csharp
cfg.CreateMap<User, UserDto>(map =>
{
    map.ForMember(d => d.Email, opt => 
        opt.MapFrom(s => s.EmailAddress));
});
```

### Conditional Mapping

```csharp
cfg.CreateMap<Order, OrderDto>(map =>
{
    map.ForMember(d => d.Discount, opt =>
        opt.When(s => s.Total > 100)
           .MapFrom(s => s.Total * 0.1m));
});
```

## Next Steps

- **Need more?** See [COMPLETE_USER_GUIDE.md](COMPLETE_USER_GUIDE.md)
- **Code examples?** Check [Examples.cs](src/MorphNGo/Examples.cs)
- **Issues?** Check the troubleshooting section in the complete guide
