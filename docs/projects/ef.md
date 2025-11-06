---
layout: default
title: Nextended.EF
parent: Projects
nav_order: 1
---

# Nextended.EF

Entity Framework Core extensions for enhanced database operations and query capabilities.

## Overview

Nextended.EF provides powerful extensions for Entity Framework Core, including graph loading, automatic inclusion of related entities, alternate query matching, and simplified multi-level includes.

## Installation

```bash
dotnet add package Nextended.EF
dotnet add package Microsoft.EntityFrameworkCore
```

## Key Features

### 1. Load Graph Async

Automatically load entity graphs with navigation properties to a specified depth, preventing circular references.

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

// Load an entity with all its navigation properties up to depth 2
var user = await dbContext.Users.FindAsync(userId);
await dbContext.LoadGraphAsync(user, maxDepth: 2);

// Now user.Orders, user.Address, and nested properties are loaded
```

### 2. Include All Extensions

Automatically include all navigation properties without manually specifying each one.

```csharp
using Nextended.EF;

// Include all navigation properties
var users = await dbContext.Users
    .IncludeAll(dbContext)
    .ToListAsync();

// Include all except specific paths
var users = await dbContext.Users
    .IncludeAll(dbContext, new[] { "Orders.OrderItems", "Profile.Avatar" })
    .ToListAsync();

// Include all except specific properties using expressions
var users = await dbContext.Users
    .IncludeAll(dbContext, u => u.Orders, u => u.Profile)
    .ToListAsync();

// For DbSet (context is automatically retrieved)
var products = await dbContext.Products
    .IncludeAll(excludePaths: new[] { "Category.Products" })
    .ToListAsync();
```

### 3. Multi-Level Include Helpers

Simplified syntax for chaining multiple ThenInclude operations.

```csharp
using Nextended.EF;

// Chain multiple includes at once
var orders = await dbContext.Orders
    .MultiInclude(
        q => q.Include(o => o.OrderItems),
        oi => oi.Product,
        oi => oi.Product.Category,
        oi => oi.Discounts
    )
    .ToListAsync();

// Simplified nested includes for collections
var users = await dbContext.Users
    .Include(
        u => u.Orders,
        o => o.OrderItems,
        oi => oi.Product
    )
    .ToListAsync();
```

### 4. Alternate Query Match Extensions

Advanced query matching capabilities for flexible search operations.

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

// Search across multiple properties (requires AlternateQueryMatchExtensions)
var searchTerm = "john";
var users = await dbContext.Users
    .AlternateQueryMatch(searchTerm)
    .ToListAsync();

// This will match users where any text property contains "john"
```

## Usage Examples

### Load Complete Entity Graphs

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

public class OrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order> GetOrderWithAllRelatedDataAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        
        // Load all navigation properties up to 3 levels deep
        // Prevents circular references and N+1 queries
        await _context.LoadGraphAsync(order, maxDepth: 3);
        
        return order;
        // Order now includes:
        // - Order.Customer
        // - Order.OrderItems
        // - Order.OrderItems[].Product
        // - Order.OrderItems[].Product.Category
        // And so on, up to depth 3
    }
    
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        return await _context.Users
            .AlternateQueryMatch(searchTerm)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
}
```

### Using IncludeAll for Automatic Loading

```csharp
public class ProductRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<List<Product>> GetAllProductsWithRelationsAsync()
    {
        // Automatically include all navigation properties
        return await _context.Products
            .IncludeAll(dbContext)
            .ToListAsync();
        // Loads Product.Category, Product.Supplier, Product.Reviews, etc.
    }
    
    public async Task<List<Product>> GetProductsExcludingReviewsAsync()
    {
        // Include all except Reviews to avoid loading large collections
        return await _context.Products
            .IncludeAll(_context, new[] { "Reviews", "Reviews.User" })
            .ToListAsync();
    }
    
    public async Task<List<Product>> SearchProductsAsync(
        string searchTerm, 
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        var query = _context.Products
            .IncludeAll(_context, p => p.Reviews) // Exclude reviews
            .AlternateQueryMatch(searchTerm);
        
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }
        
        return await query
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
}
```

### Multi-Level Includes Made Easy

```csharp
public class OrderService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<List<Order>> GetOrdersWithDetailsAsync()
    {
        // Traditional way - verbose
        var ordersOld = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Discounts)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
            .ToListAsync();
        
        // Nextended way - concise
        var orders = await _context.Orders
            .MultiInclude(
                q => q.Include(o => o.OrderItems),
                oi => oi.Product,
                oi => oi.Product.Category,
                oi => oi.Discounts
            )
            .ToListAsync();
        
        return orders;
    }
    
    public async Task<Order> GetOrderWithAllDataAsync(int orderId)
    {
        // Load single order with all related data
        var order = await _context.Orders
            .Include(o => o.Customer, c => c.Address, a => a.City)
            .Include(o => o.OrderItems, oi => oi.Product, p => p.Category)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        return order;
    }
}
```

### Complete Data Loading Example

```csharp
public class ReportService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<UserReport> GenerateUserReportAsync(int userId)
    {
        // Find user
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;
        
        // Load complete graph of related data
        await _context.LoadGraphAsync(user, maxDepth: 3);
        
        // Now we have access to:
        // - user.Orders
        // - user.Orders[].OrderItems
        // - user.Orders[].OrderItems[].Product
        // - user.Profile
        // - user.Address
        // All loaded in a single efficient operation
        
        return new UserReport
        {
            User = user,
            TotalOrders = user.Orders.Count,
            TotalSpent = user.Orders.Sum(o => o.Total),
            RecentOrders = user.Orders.OrderByDescending(o => o.Date).Take(5).ToList()
        };
    }
    
    public async Task<List<User>> GetFilteredUsersAsync(UserFilter filter)
    {
        var query = _context.Users
            .IncludeAll(_context, u => u.Orders); // Include all except Orders to control data size
        
        // Apply search term if provided
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            query = query.AlternateQueryMatch(filter.SearchTerm);
        }
        
        // Apply date filters
        if (filter.CreatedAfter.HasValue)
        {
            query = query.Where(u => u.CreatedDate >= filter.CreatedAfter.Value);
        }
        
        if (filter.CreatedBefore.HasValue)
        {
            query = query.Where(u => u.CreatedDate <= filter.CreatedBefore.Value);
        }
        
        // Apply sorting
        query = filter.SortBy switch
        {
            "name" => query.OrderBy(u => u.Name),
            "date" => query.OrderByDescending(u => u.CreatedDate),
            "email" => query.OrderBy(u => u.Email),
            _ => query.OrderBy(u => u.Id)
        };
        
        // Apply pagination
        if (filter.PageSize > 0)
        {
            query = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }
        
        return await query.ToListAsync();
    }
}

public class UserFilter
{
    public string SearchTerm { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

## Best Practices

### 1. Use AsNoTracking for Read-Only Queries

```csharp
// Better performance for read-only scenarios
var users = await _context.Users
    .AsNoTracking()
    .IncludeAll(_context)
    .AlternateQueryMatch(searchTerm)
    .ToListAsync();
```

### 2. Avoid N+1 Query Problems with LoadGraphAsync

```csharp
// Bad - N+1 queries
var users = await _context.Users.ToListAsync();
foreach (var user in users)
{
    var orders = await _context.Orders
        .Where(o => o.UserId == user.Id)
        .ToListAsync();
}

// Good - Use IncludeAll or LoadGraphAsync
var users = await _context.Users
    .IncludeAll(_context)
    .ToListAsync();

// Or for a single entity
var user = await _context.Users.FindAsync(userId);
await _context.LoadGraphAsync(user, maxDepth: 2);
```

### 3. Control Depth to Avoid Loading Too Much Data

```csharp
// Be careful with depth - higher values load more data
var order = await _context.Orders.FindAsync(orderId);

// Depth 1: Load direct navigation properties only
await _context.LoadGraphAsync(order, maxDepth: 1);

// Depth 3: Load navigation properties, their properties, and one more level
await _context.LoadGraphAsync(order, maxDepth: 3);
```

### 4. Exclude Large Collections with IncludeAll

```csharp
// Exclude collections that might be very large
var products = await _context.Products
    .IncludeAll(_context, new[] { "Reviews", "OrderItems" })
    .ToListAsync();

// Or use expressions
var products = await _context.Products
    .IncludeAll(_context, p => p.Reviews, p => p.OrderItems)
    .ToListAsync();
```

### 5. Use Pagination for Large Datasets

```csharp
public async Task<PagedResult<User>> GetPagedUsersAsync(int page, int pageSize)
{
    var query = _context.Users
        .IncludeAll(_context, u => u.Orders); // Exclude large collections
    
    var total = await query.CountAsync();
    var users = await query
        .OrderBy(u => u.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<User>
    {
        Items = users,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}
```

## Configuration

### DbContext Setup

```csharp
using Microsoft.EntityFrameworkCore;
using Nextended.EF;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entities
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasMany(e => e.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);
        });
    }
}
```

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Microsoft.EntityFrameworkCore` (9.0+) - EF Core framework

## Performance Tips

- Use `AsNoTracking()` for read-only queries to avoid change tracking overhead
- Use `LoadGraphAsync` with appropriate `maxDepth` to control how much data is loaded
- Use `IncludeAll` with exclusions to automatically load relations while avoiding large collections
- Implement pagination for large datasets
- Use `MultiInclude` to simplify complex include chains
- Use compiled queries for frequently executed queries
- Consider splitting very large queries into smaller batches
- Monitor the depth parameter in `LoadGraphAsync` - higher values can significantly impact performance

## Related Projects

- [Nextended.Core](core.md) - Foundation library
- [Nextended.Web](web.md) - Includes OData support for EF queries

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.EF/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.EF)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
