# Nextended.EF

Entity Framework Core extensions for enhanced database operations and query capabilities.

## Overview

Nextended.EF provides powerful extensions for Entity Framework Core, including advanced query matching, DbSet operations, and query optimization utilities.

## Installation

```bash
dotnet add package Nextended.EF
dotnet add package Microsoft.EntityFrameworkCore
```

## Key Features

### 1. Alternate Query Match Extensions

Advanced query matching capabilities for flexible search operations.

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

// Search across multiple properties
var searchTerm = "john";
var users = await dbContext.Users
    .AlternateQueryMatch(searchTerm)
    .ToListAsync();

// This will match users where any text property contains "john"
```

### 2. DbSet Extensions

Enhanced DbSet operations for common database tasks.

```csharp
using Nextended.EF;

// Upsert (Insert or Update)
await dbContext.Users.UpsertAsync(user);

// Bulk operations
await dbContext.Users.BulkInsertAsync(userList);
await dbContext.Users.BulkUpdateAsync(userList);

// Find or create
var user = await dbContext.Users
    .FindOrCreateAsync(u => u.Email == "test@example.com", 
        () => new User { Email = "test@example.com" });
```

### 3. Query Optimization

Utilities for optimizing Entity Framework queries.

```csharp
// Include related entities conditionally
var query = dbContext.Users.AsQueryable();

if (includeOrders)
{
    query = query.Include(u => u.Orders);
}

if (includeAddress)
{
    query = query.Include(u => u.Address);
}

var users = await query.ToListAsync();
```

## Usage Examples

### Basic CRUD with Extensions

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

public class UserRepository
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<User> GetOrCreateAsync(string email)
    {
        return await _context.Users
            .FindOrCreateAsync(
                u => u.Email == email,
                () => new User { Email = email, CreatedDate = DateTime.Now }
            );
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

### Advanced Search Operations

```csharp
public class ProductRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<List<Product>> SearchProductsAsync(
        string searchTerm, 
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        var query = _context.Products
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
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
}
```

### Bulk Operations

```csharp
public class OrderService
{
    private readonly ApplicationDbContext _context;
    
    public async Task ProcessBulkOrdersAsync(List<Order> orders)
    {
        // Bulk insert for better performance
        await _context.Orders.BulkInsertAsync(orders);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateOrderStatusesAsync(
        List<int> orderIds, 
        OrderStatus newStatus)
    {
        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync();
        
        foreach (var order in orders)
        {
            order.Status = newStatus;
            order.UpdatedDate = DateTime.Now;
        }
        
        await _context.Orders.BulkUpdateAsync(orders);
        await _context.SaveChangesAsync();
    }
}
```

### Query Building with Extensions

```csharp
public class ReportService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<List<User>> GetFilteredUsersAsync(UserFilter filter)
    {
        var query = _context.Users.AsQueryable();
        
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
        
        // Apply status filter
        if (filter.Status.HasValue)
        {
            query = query.Where(u => u.Status == filter.Status.Value);
        }
        
        // Include related data if requested
        if (filter.IncludeOrders)
        {
            query = query.Include(u => u.Orders);
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
    public UserStatus? Status { get; set; }
    public bool IncludeOrders { get; set; }
    public string SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

### Upsert Operations

```csharp
public class ConfigurationService
{
    private readonly ApplicationDbContext _context;
    
    public async Task SaveSettingAsync(string key, string value)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting != null)
        {
            // Update existing
            setting.Value = value;
            setting.UpdatedDate = DateTime.Now;
        }
        else
        {
            // Insert new
            setting = new Setting
            {
                Key = key,
                Value = value,
                CreatedDate = DateTime.Now
            };
            _context.Settings.Add(setting);
        }
        
        await _context.SaveChangesAsync();
        
        // Or use UpsertAsync extension
        await _context.Settings.UpsertAsync(setting);
    }
}
```

## Best Practices

### 1. Use AsNoTracking for Read-Only Queries

```csharp
// Better performance for read-only scenarios
var users = await _context.Users
    .AsNoTracking()
    .AlternateQueryMatch(searchTerm)
    .ToListAsync();
```

### 2. Avoid N+1 Query Problems

```csharp
// Bad - N+1 queries
foreach (var user in users)
{
    var orders = await _context.Orders
        .Where(o => o.UserId == user.Id)
        .ToListAsync();
}

// Good - Single query with Include
var users = await _context.Users
    .Include(u => u.Orders)
    .ToListAsync();
```

### 3. Use Pagination for Large Datasets

```csharp
public async Task<PagedResult<User>> GetPagedUsersAsync(int page, int pageSize)
{
    var query = _context.Users.AsQueryable();
    
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

### 4. Use Compiled Queries for Repeated Queries

```csharp
private static readonly Func<ApplicationDbContext, int, Task<User>> GetUserByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
        context.Users.FirstOrDefault(u => u.Id == id));

public async Task<User> GetUserAsync(int id)
{
    return await GetUserByIdCompiled(_context, id);
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

- Use `AsNoTracking()` for read-only queries
- Implement pagination for large datasets
- Use compiled queries for frequently executed queries
- Leverage bulk operations for multiple inserts/updates
- Use `Include()` judiciously to avoid loading unnecessary data
- Consider splitting large queries into smaller ones

## Related Projects

- [Nextended.Core](core.md) - Foundation library
- [Nextended.Web](web.md) - Includes OData support for EF queries

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.EF/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.EF)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
