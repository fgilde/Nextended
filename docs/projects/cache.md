# Nextended.Cache

Caching utilities and extensions for simplified caching operations in .NET applications.

## Overview

Nextended.Cache provides a unified caching interface and extensions for working with various caching providers including `IMemoryCache` and `System.Runtime.Caching.MemoryCache`.

## Installation

```bash
dotnet add package Nextended.Cache
```

## Key Features

### 1. Unified Cache Provider

The `CacheProvider` class offers a consistent interface across different caching implementations.

```csharp
using Nextended.Cache;

var cacheProvider = new CacheProvider();

// Store item with default expiration
cacheProvider.Set("key", myObject);

// Store item with custom expiration
cacheProvider.Set("key", myObject, TimeSpan.FromMinutes(30));

// Retrieve item
var item = cacheProvider.Get<MyType>("key");

// Remove item
cacheProvider.Remove("key");
```

### 2. Cache Extensions

Extensions for `IMemoryCache` and `MemoryCache` for fluent caching operations.

```csharp
using Nextended.Cache.Extensions;
using Microsoft.Extensions.Caching.Memory;

IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

// Get or create cached item
var user = cache.GetOrCreate("user:123", () => 
{
    // This expensive operation only runs if not cached
    return LoadUserFromDatabase(123);
}, TimeSpan.FromMinutes(10));

// Async version
var userData = await cache.GetOrCreateAsync("user:123", async () => 
{
    return await LoadUserFromDatabaseAsync(123);
}, TimeSpan.FromMinutes(10));
```

### 3. Automatic Cache Invalidation

Built-in support for time-based cache invalidation.

```csharp
// Sliding expiration - resets timer on access
cacheProvider.Set("key", value, new CacheItemPolicy
{
    SlidingExpiration = TimeSpan.FromMinutes(20)
});

// Absolute expiration - expires at specific time
cacheProvider.Set("key", value, new CacheItemPolicy
{
    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
});
```

### 4. Cache Key Management

Utilities for generating and managing cache keys.

```csharp
// Generate consistent cache keys
string key = CacheKeyHelper.GenerateKey("users", userId, "profile");
// Result: "users:123:profile"

// Clear all keys with prefix
cacheProvider.RemoveByPattern("users:*");
```

## Usage Examples

### Basic Caching Pattern

```csharp
using Nextended.Cache;
using Nextended.Cache.Extensions;

public class UserService
{
    private readonly CacheProvider _cache;
    
    public UserService()
    {
        _cache = new CacheProvider();
    }
    
    public User GetUser(int userId)
    {
        var cacheKey = $"user:{userId}";
        
        return _cache.GetOrCreate(cacheKey, () =>
        {
            // Expensive database operation
            return DatabaseContext.Users.Find(userId);
        }, TimeSpan.FromMinutes(10));
    }
}
```

### Caching with ASP.NET Core

```csharp
using Microsoft.Extensions.Caching.Memory;
using Nextended.Cache.Extensions;

public class ProductService
{
    private readonly IMemoryCache _cache;
    private readonly IProductRepository _repository;
    
    public ProductService(IMemoryCache cache, IProductRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }
    
    public async Task<Product> GetProductAsync(int productId)
    {
        var cacheKey = $"product:{productId}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _repository.GetByIdAsync(productId);
        }, TimeSpan.FromMinutes(15));
    }
    
    public void InvalidateProduct(int productId)
    {
        _cache.Remove($"product:{productId}");
    }
}
```

### Advanced Caching with Dependencies

```csharp
using System.Runtime.Caching;

public class ConfigurationCache
{
    private readonly CacheProvider _cache;
    
    public T GetConfiguration<T>(string fileName) where T : class
    {
        var cacheKey = $"config:{fileName}";
        
        var policy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddHours(1),
            ChangeMonitors =
            {
                // Invalidate cache when file changes
                new HostFileChangeMonitor(new[] { fileName })
            }
        };
        
        return _cache.Get<T>(cacheKey) ?? LoadAndCache(cacheKey, fileName, policy);
    }
    
    private T LoadAndCache<T>(string key, string fileName, CacheItemPolicy policy) 
        where T : class
    {
        var config = LoadConfigFromFile<T>(fileName);
        _cache.Set(key, config, policy);
        return config;
    }
}
```

### Bulk Cache Operations

```csharp
public class CacheManager
{
    private readonly CacheProvider _cache;
    
    public void CacheMultipleItems<T>(IDictionary<string, T> items, TimeSpan expiration)
    {
        foreach (var item in items)
        {
            _cache.Set(item.Key, item.Value, expiration);
        }
    }
    
    public IDictionary<string, T> GetMultipleItems<T>(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, T>();
        
        foreach (var key in keys)
        {
            var value = _cache.Get<T>(key);
            if (value != null)
            {
                result[key] = value;
            }
        }
        
        return result;
    }
    
    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

## Best Practices

### 1. Use Appropriate Expiration Times

```csharp
// Frequently changing data - short expiration
_cache.Set("active-users", users, TimeSpan.FromMinutes(1));

// Rarely changing data - longer expiration
_cache.Set("app-settings", settings, TimeSpan.FromHours(24));

// Static/reference data - very long expiration
_cache.Set("countries", countries, TimeSpan.FromDays(7));
```

### 2. Implement Cache-Aside Pattern

```csharp
public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
{
    // Try to get from cache
    var cached = _cache.Get<T>(key);
    if (cached != null)
        return cached;
    
    // Not in cache - load from source
    var value = await factory();
    
    // Store in cache for next time
    _cache.Set(key, value, TimeSpan.FromMinutes(10));
    
    return value;
}
```

### 3. Handle Cache Failures Gracefully

```csharp
public T GetWithFallback<T>(string key, Func<T> fallback)
{
    try
    {
        return _cache.Get<T>(key) ?? fallback();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Cache operation failed");
        return fallback();
    }
}
```

### 4. Use Meaningful Cache Keys

```csharp
// Good: descriptive and hierarchical
"user:123:profile"
"product:456:details"
"order:789:items"

// Bad: unclear or too generic
"data1"
"temp"
"x"
```

## Configuration

### ASP.NET Core Integration

```csharp
// Startup.cs or Program.cs
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
    options.CompactionPercentage = 0.25; // Compact when 75% full
});

// Register cache provider
builder.Services.AddSingleton<CacheProvider>();
```

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Microsoft.Extensions.Caching.Abstractions` - Caching abstractions
- `Microsoft.Extensions.Caching.Memory` - In-memory caching
- `System.Runtime.Caching` - Runtime caching support

## Performance Considerations

- **Memory Usage**: Monitor cache size to prevent memory issues
- **Expiration**: Use sliding expiration for frequently accessed items
- **Serialization**: Consider serialization cost for complex objects
- **Distributed Caching**: For multi-server scenarios, consider Redis or distributed cache

## Related Projects

- [Nextended.Core](core.md) - Foundation library
- [Nextended.Imaging](imaging.md) - Uses caching for processed images

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Cache/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Cache)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
