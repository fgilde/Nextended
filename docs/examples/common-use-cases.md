---
layout: default
title: Common Use Cases
parent: Examples
nav_order: 1
---

# Common Use Cases

This guide demonstrates common scenarios and use cases for Nextended libraries.

## String Manipulation

### Case Conversions

```csharp
using Nextended.Core.Extensions;

string text = "hello world";

// Convert to different cases
string camelCase = text.ToCamelCase();     // "helloWorld"
string pascalCase = text.ToPascalCase();   // "HelloWorld"
string snakeCase = text.ToSnakeCase();     // "hello_world"
string kebabCase = text.ToKebabCase();     // "hello-world"

// Convert from various formats
string original = "UserFirstName";
string snake = original.ToSnakeCase();     // "user_first_name"
string camel = snake.ToCamelCase();        // "userFirstName"
```

### String Validation

```csharp
using Nextended.Core.Extensions;

string email = "user@example.com";
string url = "https://example.com";

// Check if string matches pattern
bool isEmail = email.IsValidEmail();
bool isUrl = url.IsValidUrl();

// Safe null checks
string value = null;
bool isEmpty = value.IsNullOrEmpty();      // true
bool isWhitespace = value.IsNullOrWhiteSpace(); // true
```

## Working with Collections

### Batch Processing

```csharp
using Nextended.Core.Extensions;

var items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Process in batches
foreach (var batch in items.Batch(3))
{
    // batch 1: [1, 2, 3]
    // batch 2: [4, 5, 6]
    // batch 3: [7, 8, 9]
    // batch 4: [10]
    Console.WriteLine($"Processing batch of {batch.Count()} items");
}
```

### Safe Operations

```csharp
using Nextended.Core.Extensions;

List<string> items = null;

// Safe operations that won't throw on null
bool hasItems = items.SafeAny();           // false
var first = items.SafeFirstOrDefault();    // null
var count = items.SafeCount();             // 0
```

### ForEach Extensions

```csharp
using Nextended.Core.Extensions;

var users = GetUsers();

// ForEach with side effects
users.ForEach(u => Console.WriteLine(u.Name));

// ForEach with index
users.ForEach((u, index) => 
    Console.WriteLine($"{index + 1}. {u.Name}")
);
```

## Date and Time Operations

### Business Day Calculations

```csharp
using Nextended.Core.Extensions;

DateTime today = DateTime.Today;

// Add business days (skips weekends)
DateTime fiveDaysLater = today.AddBusinessDays(5);

// Check if weekend
bool isWeekend = today.IsWeekend();

// Get next business day
DateTime nextBusinessDay = today.NextBusinessDay();
```

### Date-Only Type

```csharp
using Nextended.Core.Types;

var today = Date.Today;
var tomorrow = today.AddDays(1);
var lastWeek = today.AddDays(-7);

// Date comparisons
if (today > lastWeek)
{
    Console.WriteLine("Today is after last week");
}

// Date ranges
var startDate = new Date(2024, 1, 1);
var endDate = new Date(2024, 12, 31);
bool isInRange = today >= startDate && today <= endDate;
```

## Object Mapping

### Simple Mapping

```csharp
using Nextended.Core.Extensions;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

var user = new User 
{ 
    Id = 1, 
    FirstName = "John", 
    LastName = "Doe",
    Email = "john.doe@example.com"
};

// Map to DTO
var dto = user.MapTo<UserDto>();

// Map collection
var users = GetUsers();
var dtos = users.MapTo<UserDto[]>();
```

### Advanced Mapping with Settings

```csharp
using Nextended.Core.Extensions;

var settings = ClassMappingSettings.Default
    .IgnoreProperties<User>(u => u.Password)
    .IgnoreProperties<UserDto>(dto => dto.CalculatedField)
    .WithConverter<DateTime, string>(dt => dt.ToString("yyyy-MM-dd"));

var dto = user.MapTo<UserDto>(settings);
```

## Financial Calculations

### Money Type

```csharp
using Nextended.Core.Types;

var price = new Money(99.99m, Currency.USD);
var tax = price * 0.20m;              // Tax calculation
var total = price + tax;              // $119.99

// Currency conversions
var priceInEuros = price.ConvertTo(Currency.EUR, exchangeRate: 0.85m);

// Comparisons
if (total > new Money(100m, Currency.USD))
{
    Console.WriteLine("Total exceeds $100");
}
```

## Async Operations

### Timeout Operations

```csharp
using Nextended.Core.Extensions;

public async Task<Data> LoadDataWithTimeoutAsync()
{
    try
    {
        // Add timeout to any async operation
        var data = await LoadDataAsync()
            .WithTimeout(TimeSpan.FromSeconds(30));
        
        return data;
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Operation timed out");
        return null;
    }
}
```

### Fire and Forget

```csharp
using Nextended.Core.Extensions;

// Fire and forget with error handling
SendEmailAsync(email).FireAndForget(
    ex => Logger.LogError(ex, "Failed to send email")
);

// Continue with other work without waiting
```

## Validation

### Input Validation

```csharp
using Nextended.Core;

public void ProcessOrder(Order order)
{
    // Argument validation
    Check.NotNull(order, nameof(order));
    Check.NotEmpty(order.Items, nameof(order.Items));
    Check.Range(order.Total, 0.01m, decimal.MaxValue, nameof(order.Total));
    
    // Custom validation
    Check.That(order.CustomerId > 0, "Customer ID must be positive");
    
    // Process order...
}
```

## Serialization

### JSON Operations

```csharp
using Nextended.Core.Extensions;

var user = new User { Id = 1, Name = "John" };

// Serialize to JSON
string json = user.ToJson();

// Deserialize from JSON
var deserializedUser = json.FromJson<User>();

// Pretty print JSON
string prettyJson = user.ToJson(formatting: Formatting.Indented);
```

### XML Operations

```csharp
using Nextended.Core.Extensions;

var user = new User { Id = 1, Name = "John" };

// Serialize to XML
string xml = user.ToXml();

// Deserialize from XML
var deserializedUser = xml.FromXml<User>();
```

## Caching

### Memory Cache Pattern

```csharp
using Nextended.Cache;
using Nextended.Cache.Extensions;

public class UserService
{
    private readonly IMemoryCache _cache;
    private readonly IUserRepository _repository;
    
    public async Task<User> GetUserAsync(int userId)
    {
        var cacheKey = $"user:{userId}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            // This only runs if not in cache
            return await _repository.GetByIdAsync(userId);
        }, TimeSpan.FromMinutes(10));
    }
}
```

## Entity Framework Queries

### Search Operations

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

public async Task<List<Product>> SearchProductsAsync(string searchTerm)
{
    return await _context.Products
        .AlternateQueryMatch(searchTerm)  // Searches across all text properties
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .ToListAsync();
}
```

### Find or Create Pattern

```csharp
using Nextended.EF;

public async Task<User> GetOrCreateUserAsync(string email)
{
    return await _context.Users.FindOrCreateAsync(
        u => u.Email == email,
        () => new User 
        { 
            Email = email, 
            CreatedDate = DateTime.Now 
        }
    );
}
```

## Image Processing

### Resize and Optimize Images

```csharp
using Nextended.Imaging;

public byte[] ProcessUploadedImage(byte[] originalImage)
{
    // Resize maintaining aspect ratio
    var resized = ImageHelper.ResizeKeepAspect(originalImage, 800, 600);
    
    // Optimize for web
    var optimized = ImageHelper.CompressJpeg(resized, quality: 85);
    
    return optimized;
}

public ImageSet CreateImageSet(byte[] original)
{
    return new ImageSet
    {
        Original = original,
        Large = ImageHelper.ResizeKeepAspect(original, 1920, 1920),
        Medium = ImageHelper.ResizeKeepAspect(original, 800, 800),
        Thumbnail = ImageHelper.CreateThumbnail(original, 150, 150)
    };
}
```

## Web API Operations

### RESTful Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Nextended.Core.Extensions;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var products = await _service.SearchAsync(q);
        return Ok(products);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var product = dto.MapTo<Product>();
        await _service.CreateAsync(product);
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
```

## Next Steps

- Explore [Class Mapping Reference](../api/class-mapping.md)
- Learn about [Custom Types](custom-types.md)
- See [Code Generation Examples](code-generation.md)
- Review [Extension Methods API Reference](../api/extensions.md)
- Review [Custom Types API Reference](../api/types.md)
