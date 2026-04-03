using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

namespace MorphNGo.UnitTests;

/// <summary>
/// Real-world scenario tests demonstrating practical use cases
/// of the MorphNGo mapping library in production scenarios.
/// </summary>
public class RealWorldScenarioTests
{
    private static ILogger GetLogger() => NullLogger.Instance;
    #region E-Commerce Domain Models

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int InventoryCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public bool InStock { get; set; }
        public string Status { get; set; }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Product Product { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }

    #endregion

    #region User & Profile Domain Models

    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string UserRole { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public string AccountStatus { get; set; }
    }

    public class AdminUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion

    #region Scenario 1: E-Commerce Product Catalog API

    [Fact]
    public void Scenario_ProductCatalogAPI_MapProductToDto()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Product, ProductDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.ProductId));
                map.ForMember(d => d.Name, opt =>
                    opt.MapFrom(s => s.ProductName));
                map.ForMember(d => d.UnitPrice, opt =>
                    opt.MapFrom(s => s.Price));
                map.ForMember(d => d.InStock, opt =>
                    opt.MapFrom(s => s.InventoryCount > 0));
                map.ForMember(d => d.Status, opt =>
                    opt.MapFrom(s => s.IsActive ? "Available" : "Discontinued"));
            });
        });

        var mapper = config.CreateMapper();

        var product = new Product
        {
            ProductId = 101,
            ProductName = "Wireless Headphones",
            Description = "High-quality wireless headphones",
            Price = 79.99m,
            InventoryCount = 150,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = mapper.Map<ProductDto>(product);

        // Assert
        Assert.Equal(101, dto.Id);
        Assert.Equal("Wireless Headphones", dto.Name);
        Assert.Equal(79.99m, dto.UnitPrice);
        Assert.True(dto.InStock);
        Assert.Equal("Available", dto.Status);
    }

    #endregion

    #region Scenario 2: E-Commerce Order Processing with Nested Objects

    [Fact]
    public void Scenario_OrderProcessing_MapComplexOrderToDto()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Order, OrderDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.OrderId));
                map.ForMember(d => d.SubTotal, opt =>
                    opt.MapFrom(s => s.Items.Sum(i => i.UnitPrice * i.Quantity)));
                map.ForMember(d => d.TaxAmount, opt =>
                    opt.MapFrom(s =>
                    {
                        var subtotal = s.Items.Sum(i => i.UnitPrice * i.Quantity);
                        return subtotal * 0.1m; // 10% tax
                    }));
                map.ForMember(d => d.Total, opt =>
                    opt.MapFrom(s =>
                    {
                        var subtotal = s.Items.Sum(i => i.UnitPrice * i.Quantity);
                        var tax = subtotal * 0.1m;
                        return subtotal + tax;
                    }));
            });

            cfg.CreateMap<OrderItem, OrderItemDto>(map =>
            {
                map.ForMember(d => d.ProductName, opt =>
                    opt.MapFrom(s => s.Product.ProductName));
                map.ForMember(d => d.LineTotal, opt =>
                    opt.MapFrom(s => s.UnitPrice * s.Quantity));
            });
        });

        var mapper = config.CreateMapper();

        var order = new Order
        {
            OrderId = 1001,
            OrderDate = new DateTime(2024, 1, 15),
            Status = "Shipped",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = 101,
                    Quantity = 2,
                    UnitPrice = 79.99m,
                    Product = new Product { ProductName = "Wireless Headphones" }
                },
                new OrderItem
                {
                    ProductId = 102,
                    Quantity = 1,
                    UnitPrice = 199.99m,
                    Product = new Product { ProductName = "Bluetooth Speaker" }
                }
            }
        };

        // Act
        var dto = mapper.Map<OrderDto>(order);

        // Assert
        Assert.Equal(1001, dto.Id);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(359.97m, dto.SubTotal); // (79.99*2) + 199.99
        Assert.Equal(35.997m, dto.TaxAmount); // SubTotal * 0.1
        Assert.Equal(395.967m, dto.Total); // SubTotal + Tax
        Assert.Equal("Wireless Headphones", dto.Items[0].ProductName);
        Assert.Equal(159.98m, dto.Items[0].LineTotal); // 79.99 * 2
    }

    #endregion

    #region Scenario 3: User Profile Management with Conditional Mapping

    [Fact]
    public void Scenario_UserProfile_ConditionalMapping()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, UserProfileDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.UserId));
                map.ForMember(d => d.FullName, opt =>
                    opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
                map.ForMember(d => d.Email, opt =>
                    opt.MapFrom(s => s.EmailAddress));
                map.ForMember(d => d.Phone, opt =>
                    opt.MapFrom(s => s.PhoneNumber));
                map.ForMember(d => d.JoinDate, opt =>
                    opt.MapFrom(s => s.RegistrationDate));
                map.ForMember(d => d.AccountStatus, opt =>
                    opt.MapFrom(s => s.IsActive ? "Active" : "Inactive"));
            });
        });

        var mapper = config.CreateMapper();

        var user = new User
        {
            UserId = 5001,
            FirstName = "Sarah",
            LastName = "Johnson",
            EmailAddress = "sarah.johnson@example.com",
            PhoneNumber = "+1-555-0123",
            IsActive = true,
            RegistrationDate = new DateTime(2022, 6, 15),
            UserRole = "Customer"
        };

        // Act
        var dto = mapper.Map<UserProfileDto>(user);

        // Assert
        Assert.Equal(5001, dto.Id);
        Assert.Equal("Sarah Johnson", dto.FullName);
        Assert.Equal("sarah.johnson@example.com", dto.Email);
        Assert.Equal("Active", dto.AccountStatus);
    }

    #endregion

    #region Scenario 4: Admin View with Role-Based Field Selection

    [Fact]
    public void Scenario_AdminView_RoleBasedMapping()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<User, AdminUserDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.UserId));
                map.ForMember(d => d.FullName, opt =>
                    opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
                map.ForMember(d => d.Email, opt =>
                    opt.MapFrom(s => s.EmailAddress));
                map.ForMember(d => d.Role, opt =>
                    opt.MapFrom(s => s.UserRole));
            });
        });

        var mapper = config.CreateMapper();

        var adminUser = new User
        {
            UserId = 5002,
            FirstName = "John",
            LastName = "Admin",
            EmailAddress = "admin@example.com",
            PhoneNumber = "+1-555-0199",
            IsActive = true,
            RegistrationDate = new DateTime(2020, 1, 1),
            UserRole = "Administrator"
        };

        // Act
        var dto = mapper.Map<AdminUserDto>(adminUser);

        // Assert
        Assert.Equal(5002, dto.Id);
        Assert.Equal("John Admin", dto.FullName);
        Assert.Equal("Administrator", dto.Role);
        Assert.True(dto.IsActive);
    }

    #endregion

    #region Scenario 5: Batch Processing - Mapping Product Collections

    [Fact]
    public void Scenario_BatchProcessing_MapProductCollections()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Product, ProductDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.ProductId));
                map.ForMember(d => d.Name, opt =>
                    opt.MapFrom(s => s.ProductName));
                map.ForMember(d => d.UnitPrice, opt =>
                    opt.MapFrom(s => s.Price));
                map.ForMember(d => d.InStock, opt =>
                    opt.MapFrom(s => s.InventoryCount > 0));
                map.ForMember(d => d.Status, opt =>
                    opt.MapFrom(s => s.IsActive ? "Available" : "Discontinued"));
            });
        });

        var mapper = config.CreateMapper();

        var products = new List<Product>
        {
            new Product { ProductId = 1, ProductName = "Product A", Price = 10m, InventoryCount = 100, IsActive = true },
            new Product { ProductId = 2, ProductName = "Product B", Price = 20m, InventoryCount = 0, IsActive = true },
            new Product { ProductId = 3, ProductName = "Product C", Price = 30m, InventoryCount = 50, IsActive = false }
        };

        // Act
        var dtos = products.Select(p => mapper.Map<ProductDto>(p)).ToList();

        // Assert
        Assert.Equal(3, dtos.Count);
        Assert.Equal("Product A", dtos[0].Name);
        Assert.True(dtos[0].InStock);
        Assert.False(dtos[1].InStock);
        Assert.Equal("Discontinued", dtos[2].Status);
    }

    #endregion

    #region Scenario 6: API Response with Null Handling

    [Fact]
    public void Scenario_APIResponse_NullHandling()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Product, ProductDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.ProductId));
                map.ForMember(d => d.Name, opt =>
                    opt.MapFrom(s => s.ProductName ?? "Unknown Product"));
                map.ForMember(d => d.UnitPrice, opt =>
                    opt.MapFrom(s => s.Price > 0 ? s.Price : 0m));
                map.ForMember(d => d.Status, opt =>
                    opt.MapFrom(s => s.IsActive ? "Available" : "Unavailable"));
            });
        });

        var mapper = config.CreateMapper();

        var productWithNulls = new Product
        {
            ProductId = 99,
            ProductName = null, // Null name
            Price = 0, // Zero price
            InventoryCount = 0,
            IsActive = false
        };

        // Act
        var dto = mapper.Map<ProductDto>(productWithNulls);

        // Assert
        Assert.Equal(99, dto.Id);
        Assert.Equal("Unknown Product", dto.Name); // Null handled
        Assert.Equal(0m, dto.UnitPrice); // Zero handled
        Assert.Equal("Unavailable", dto.Status);
    }

    #endregion

    #region Scenario 7: Bidirectional Mapping (DTO to Domain Model)

    [Fact]
    public void Scenario_Bidirectional_DTOtoDomain()
    {
        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Product, ProductDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.ProductId));
                map.ForMember(d => d.Name, opt =>
                    opt.MapFrom(s => s.ProductName));
                map.ForMember(d => d.UnitPrice, opt =>
                    opt.MapFrom(s => s.Price));
                map.ForMember(d => d.InStock, opt =>
                    opt.MapFrom(s => s.InventoryCount > 0));
                map.ForMember(d => d.Status, opt =>
                    opt.MapFrom(s => s.IsActive ? "Available" : "Discontinued"));
            });

            // Register reverse mapping (DTO -> Product)
            cfg.CreateMap<ProductDto, Product>(map =>
            {
                map.ForMember(d => d.ProductId, opt =>
                    opt.MapFrom(s => s.Id));
                map.ForMember(d => d.ProductName, opt =>
                    opt.MapFrom(s => s.Name));
                map.ForMember(d => d.Price, opt =>
                    opt.MapFrom(s => s.UnitPrice));
                map.ForMember(d => d.IsActive, opt =>
                    opt.MapFrom(s => s.Status == "Available"));
            });
        });

        var mapper = config.CreateMapper();

        var productDto = new ProductDto
        {
            Id = 1,
            Name = "Test Product",
            UnitPrice = 49.99m,
            InStock = true,
            Status = "Available"
        };

        // Act - Map both directions
        var dto = new ProductDto { Id = 1, Name = "Test Product", UnitPrice = 49.99m, InStock = true, Status = "Available" };
        var product = mapper.Map<Product>(dto);

        // Assert
        Assert.Equal(1, product.ProductId);
        Assert.Equal("Test Product", product.ProductName);
        Assert.Equal(49.99m, product.Price);
        Assert.True(product.IsActive); // Derived from Status = "Available"
    }

    #endregion

    #region Scenario 8: Complex Business Logic - Discount Calculation

    public class OrderWithDiscount
    {
        public int OrderId { get; set; }
        public decimal SubTotal { get; set; }
        public int DaysAsCustomer { get; set; }
        public bool IsPremiumMember { get; set; }
    }

    public class OrderDiscountDto
    {
        public int Id { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
    }

    [Fact]
    public void Scenario_BusinessLogic_DiscountCalculation()
    {

        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<OrderWithDiscount, OrderDiscountDto>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => s.OrderId));

                // Calculate discount percentage based on loyalty and membership
                map.ForMember(d => d.DiscountPercentage, opt =>
                    opt.MapFrom(s =>
                    {
                        decimal discount = 0;
                        if (s.IsPremiumMember) discount += 10;
                        if (s.DaysAsCustomer > 365) discount += 5;
                        if (s.DaysAsCustomer > 730) discount += 5;
                        return discount;
                    }));

                // Calculate discount amount
                map.ForMember(d => d.DiscountAmount, opt =>
                    opt.MapFrom(s =>
                    {
                        decimal discount = 0;
                        if (s.IsPremiumMember) discount += 10;
                        if (s.DaysAsCustomer > 365) discount += 5;
                        if (s.DaysAsCustomer > 730) discount += 5;
                        return s.SubTotal * (discount / 100m);
                    }));

                // Calculate final total
                map.ForMember(d => d.FinalTotal, opt =>
                    opt.MapFrom(s =>
                    {
                        decimal discount = 0;
                        if (s.IsPremiumMember) discount += 10;
                        if (s.DaysAsCustomer > 365) discount += 5;
                        if (s.DaysAsCustomer > 730) discount += 5;
                        var discountAmount = s.SubTotal * (discount / 100m);
                        return s.SubTotal - discountAmount;
                    }));
            });
        });

        var mapper = config.CreateMapper();

        var order = new OrderWithDiscount
        {
            OrderId = 1,
            SubTotal = 1000m,
            DaysAsCustomer = 800, // 2+ years
            IsPremiumMember = true
        };

        // Act
        var dto = mapper.Map<OrderDiscountDto>(order);

        // Assert
        Assert.Equal(1000m, dto.SubTotal);
        Assert.Equal(20m, dto.DiscountPercentage); // 10 (premium) + 5 (1 year) + 5 (2 years)
        Assert.Equal(200m, dto.DiscountAmount); // 1000 * 0.20
        Assert.Equal(800m, dto.FinalTotal); // 1000 - 200
    }

    #endregion

    #region Scenario 9: Data Transformation with Type Safety

    public class ImportRecord
    {
        public string Id { get; set; }
        public string Amount { get; set; }
        public string Date { get; set; }
    }

    public class TransactionRecord
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    [Fact]
    public void Scenario_DataTransformation_TypeSafety()
    {

        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<ImportRecord, TransactionRecord>(map =>
            {
                map.ForMember(d => d.Id, opt =>
                    opt.MapFrom(s => int.Parse(s.Id)));
                map.ForMember(d => d.Amount, opt =>
                    opt.MapFrom(s => decimal.Parse(s.Amount)));
                map.ForMember(d => d.Date, opt =>
                    opt.MapFrom(s => DateTime.Parse(s.Date)));
            });
        });

        var mapper = config.CreateMapper();

        var import = new ImportRecord
        {
            Id = "123",
            Amount = "99.99",
            Date = "2024-01-15"
        };

        // Act
        var transaction = mapper.Map<TransactionRecord>(import);

        // Assert
        Assert.Equal(123, transaction.Id);
        Assert.Equal(99.99m, transaction.Amount);
        Assert.Equal(new DateTime(2024, 1, 15), transaction.Date);
    }

    #endregion

    #region Scenario 10: Multi-Level Nested Mapping

    // Domain models with multiple nesting levels
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Department> Departments { get; set; }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Employee> Employees { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // DTOs
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<DepartmentDto> Departments { get; set; }
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<EmployeeDto> Employees { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void Scenario_DeepNesting_MultiLevelMapping()
    {

        // Arrange
        var config = new MapperConfiguration(GetLogger(), cfg =>
        {
            cfg.CreateMap<Company, CompanyDto>();
            cfg.CreateMap<Department, DepartmentDto>();
            cfg.CreateMap<Employee, EmployeeDto>();
        });

        var mapper = config.CreateMapper();

        var company = new Company
        {
            Id = 1,
            Name = "TechCorp",
            Departments = new List<Department>
            {
                new Department
                {
                    Id = 101,
                    Name = "Engineering",
                    Employees = new List<Employee>
                    {
                        new Employee { Id = 1001, Name = "Alice" },
                        new Employee { Id = 1002, Name = "Bob" }
                    }
                },
                new Department
                {
                    Id = 102,
                    Name = "Sales",
                    Employees = new List<Employee>
                    {
                        new Employee { Id = 2001, Name = "Charlie" }
                    }
                }
            }
        };

        // Act
        var dto = mapper.Map<CompanyDto>(company);

        // Assert
        Assert.Equal("TechCorp", dto.Name);
        Assert.Equal(2, dto.Departments.Count);
        Assert.Equal("Engineering", dto.Departments[0].Name);
        Assert.Equal(2, dto.Departments[0].Employees.Count);
        Assert.Equal("Alice", dto.Departments[0].Employees[0].Name);
    }

    #endregion
}
