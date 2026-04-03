namespace MorphNGo.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

/// <summary>
/// Tests for advanced mapping scenarios.
/// </summary>
public class AdvancedMappingTests
{
    private static ILogger GetLogger() => NullLogger.Instance;
    [Fact]
    public void Test_MapWithStaticDataLookup_Success()
    {
        // Arrange
        var statusMap = new Dictionary<int, string>
        {
            { 1, "Active" },
            { 2, "Inactive" },
            { 3, "Pending" }
        };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<UserWithStatus, UserWithStatusDto>(builder =>
            {
                builder.ForMember(dest => dest.StatusName,
                    opt => opt.MapFromStaticData(src => statusMap.TryGetValue(src.StatusId, out var status) ? status : "Unknown"));
            });
        });

        var mapper = config.CreateMapper();
        var user = new UserWithStatus { Id = 1, FirstName = "John", StatusId = 1 };

        // Act
        var userDto = mapper.Map<UserWithStatusDto>(user);

        // Assert
        Assert.NotNull(userDto);
        Assert.Equal("Active", userDto.StatusName);
    }

    [Fact]
    public void Test_MapWithMultipleLevelsOfNesting_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<PhoneNumber, PhoneNumberDto>();
            cfg.CreateMap<Contact, ContactDto>();
            cfg.CreateMap<EmployeeWithContact, EmployeeWithContactDto>();
        });

        var mapper = config.CreateMapper();
        var employee = new EmployeeWithContact
        {
            Id = 1,
            Name = "John",
            Contact = new Contact
            {
                Email = "john@example.com",
                Phone = new PhoneNumber { Number = "555-1234", Extension = "101" }
            }
        };

        // Act
        var employeeDto = mapper.Map<EmployeeWithContactDto>(employee);

        // Assert
        Assert.NotNull(employeeDto);
        Assert.NotNull(employeeDto.Contact);
        Assert.NotNull(employeeDto.Contact.Phone);
        Assert.Equal("john@example.com", employeeDto.Contact.Email);
        Assert.Equal("555-1234", employeeDto.Contact.Phone.Number);
        Assert.Equal("101", employeeDto.Contact.Phone.Extension);
    }

    [Fact]
    public void Test_MapWithComputedProperty_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<OrderDetailed, OrderSummary>(builder =>
            {
                builder.ForMember(dest => dest.TotalPrice,
                    opt => opt.MapFrom(src => src.Items.Sum(i => i.UnitPrice * i.Quantity)));
                builder.ForMember(dest => dest.ItemCount,
                    opt => opt.MapFrom(src => src.Items.Count));
            });
        });

        var mapper = config.CreateMapper();
        var order = new OrderDetailed
        {
            Id = 1,
            Amount = 0,
            Items = new List<OrderItemDetailed>
            {
                new OrderItemDetailed { Id = 1, ProductName = "Item1", UnitPrice = 50, Quantity = 2 },
                new OrderItemDetailed { Id = 2, ProductName = "Item2", UnitPrice = 30, Quantity = 1 }
            }
        };

        // Act
        var summary = mapper.Map<OrderSummary>(order);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(2, summary.ItemCount);
        Assert.Equal(130, summary.TotalPrice);
    }

    [Fact]
    public void Test_MapBothDirectionsWithConfiguration_Success()
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
    }

    [Fact]
    public void Test_MapListOfObjects_Success()
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
            new User { Id = 2, FirstName = "Jane", LastName = "Smith" },
            new User { Id = 3, FirstName = "Bob", LastName = "Johnson" }
        };

        // Act
        var userDtos = mapper.MapCollection<UserDto>(users.Cast<object>().ToList()).ToList();

        // Assert
        Assert.NotNull(userDtos);
        Assert.Equal(3, userDtos.Count);
        Assert.Equal("John", userDtos[0].FirstName);
        Assert.Equal("Jane", userDtos[1].FirstName);
        Assert.Equal("Bob", userDtos[2].FirstName);
    }

    [Fact]
    public void Test_MapWithPropertyRename_Success()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<LegacyUser, User>(builder =>
            {
                builder.ForMember(dest => dest.FirstName, opt => opt.From("GivenName"));
                builder.ForMember(dest => dest.LastName, opt => opt.From("FamilyName"));
            });
        });

        var mapper = config.CreateMapper();
        var legacyUser = new LegacyUser { Id = 1, GivenName = "John", FamilyName = "Doe" };

        // Act
        var user = mapper.Map<User>(legacyUser);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public void Test_MapWithParameters_LookupFromFirstArgument_Success()
    {
        var labels = new Dictionary<int, string> { { 10, "Alpha" }, { 20, "Beta" } };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<RuntimeParamSource, RuntimeParamDest>(builder =>
            {
                builder.ForMember(dest => dest.Label,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length > 0 && parameters[0] is Dictionary<int, string> map)
                        {
                            return map.TryGetValue(src.Code, out var label) ? label : "?";
                        }
                        return "?";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var source = new RuntimeParamSource { Code = 20 };

        var dest = mapper.Map<RuntimeParamDest>(source, labels);

        Assert.Equal("Beta", dest.Label);
    }

    [Fact]
    public void Test_MapWithParameters_MultipleArguments_Positional_Success()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<RuntimeParamSource, RuntimeParamDest>(builder =>
            {
                builder.ForMember(dest => dest.Label,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length >= 3
                            && parameters[0] is List<string> list
                            && parameters[1] is Dictionary<int, string> dict
                            && parameters[2] is string suffix)
                        {
                            var fromDict = dict.TryGetValue(src.Code, out var v) ? v : "";
                            return $"{list.Count}:{fromDict}:{suffix}";
                        }
                        return "";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var source = new RuntimeParamSource { Code = 1 };
        var list = new List<string>();
        var dict = new Dictionary<int, string> { { 1, "X" } };

        var dest = mapper.Map<RuntimeParamDest>(source, list, dict, "end");

        Assert.Equal("0:X:end", dest.Label);
    }

    [Fact]
    public void Test_MapCollectionWithParameters_Success()
    {
        var deptNames = new Dictionary<int, string> { { 1, "Eng" }, { 2, "Sales" } };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<RuntimeEmployee, RuntimeEmployeeDto>(builder =>
            {
                builder.ForMember(dest => dest.DepartmentLabel,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length > 0 && parameters[0] is Dictionary<int, string> map)
                        {
                            return map.TryGetValue(src.DeptId, out var n) ? n : "?";
                        }
                        return "?";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var items = new List<object>
        {
            new RuntimeEmployee { Id = 1, DeptId = 1 },
            new RuntimeEmployee { Id = 2, DeptId = 2 }
        };

        var dtos = mapper.MapCollection<RuntimeEmployeeDto>(items, deptNames).ToList();

        Assert.Equal("Eng", dtos[0].DepartmentLabel);
        Assert.Equal("Sales", dtos[1].DepartmentLabel);
    }

    [Fact]
    public void Test_MapToWithParameters_Success()
    {
        var multiplier = new Dictionary<int, int> { { 5, 100 } };

        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<RuntimeParamSource, RuntimeParamDest>(builder =>
            {
                builder.ForMember(dest => dest.Label,
                    opt => opt.MapFrom((src, parameters) =>
                    {
                        if (parameters.Length > 0 && parameters[0] is Dictionary<int, int> map)
                        {
                            var m = map.TryGetValue(src.Code, out var v) ? v : 0;
                            return m.ToString();
                        }
                        return "0";
                    }));
            });
        });

        var mapper = config.CreateMapper();
        var source = new RuntimeParamSource { Code = 5 };
        var existing = new RuntimeParamDest { Label = "old" };

        var result = mapper.MapTo(source, existing, multiplier);

        Assert.Same(existing, result);
        Assert.Equal("100", existing.Label);
    }

    [Fact]
    public void Test_MapFromSingleParameter_Delegate_WorksWhenMapPassesExtraParameters()
    {
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<RuntimeParamSource, RuntimeParamDest>(builder =>
            {
                builder.ForMember(dest => dest.Label,
                    opt => opt.MapFrom(src => $"code:{src.Code}"));
            });
        });

        var mapper = config.CreateMapper();
        var source = new RuntimeParamSource { Code = 42 };

        var dest = mapper.Map<RuntimeParamDest>(source, "ignored", 123);

        Assert.Equal("code:42", dest.Label);
    }
}

// Advanced Test Models
public class UserWithStatus
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public int StatusId { get; set; }
}

public class UserWithStatusDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? StatusName { get; set; }
}

public class PhoneNumber
{
    public string Number { get; set; } = "";
    public string? Extension { get; set; }
}

public class PhoneNumberDto
{
    public string Number { get; set; } = "";
    public string? Extension { get; set; }
}

public class Contact
{
    public string Email { get; set; } = "";
    public PhoneNumber? Phone { get; set; }
}

public class ContactDto
{
    public string Email { get; set; } = "";
    public PhoneNumberDto? Phone { get; set; }
}

public class EmployeeWithContact
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Contact? Contact { get; set; }
}

public class EmployeeWithContactDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ContactDto? Contact { get; set; }
}

public class OrderSummary
{
    public int Id { get; set; }
    public decimal TotalPrice { get; set; }
    public int ItemCount { get; set; }
}

public class LegacyUser
{
    public int Id { get; set; }
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
}

public class OrderDetailed
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItemDetailed> Items { get; set; } = new();
}

public class OrderItemDetailed
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class RuntimeParamSource
{
    public int Code { get; set; }
}

public class RuntimeParamDest
{
    public string? Label { get; set; }
}

public class RuntimeEmployee
{
    public int Id { get; set; }
    public int DeptId { get; set; }
}

public class RuntimeEmployeeDto
{
    public int Id { get; set; }
    public string? DepartmentLabel { get; set; }
}
