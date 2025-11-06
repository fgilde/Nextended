---
layout: default
title: Nextended.Cache
parent: Projects
nav_order: 10
---

# Nextended.Cache

Caching utilities and extensions for simplified caching operations in .NET applications.

## Overview

Nextended.Cache provides caching extensions for `IMemoryCache` and `System.Runtime.Caching.MemoryCache`, along with a `CacheProvider` class for expression-based caching with automatic cache invalidation.

## Installation

```bash
dotnet add package Nextended.Cache
```

## Key Features

### 1. CacheProvider with Expression-Based Caching

The `CacheProvider` class provides intelligent caching based on method expressions with automatic key generation and conditional cache clearing.

```csharp
using Nextended.Cache;
using Microsoft.Extensions.Caching.Memory;

// Create cache provider
var cacheProvider = new CacheProvider();

// Or with custom options
var cacheProvider = new CacheProvider(
    cache: myMemoryCache,
    cacheEntryOptions: new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
);

// Cache method execution results automatically
public class UserService
{
    private readonly CacheProvider _cache;
    
    public User GetUser(int userId)
    {
        // ExecuteWithCache generates cache key from method expression
        return this.ExecuteWithCache(_cache, () => LoadUserFromDb(userId));
    }
    
    private User LoadUserFromDb(int userId)
    {
        // Expensive database operation
        return database.Users.Find(userId);
    }
}
```

### 2. AddOrGetExisting for ObjectCache

Thread-safe lazy initialization for System.Runtime.Caching.MemoryCache.

```csharp
using Nextended.Cache.Extensions;
using System.Runtime.Caching;

var cache = MemoryCache.Default;

// Thread-safe get or create
var user = cache.AddOrGetExisting(
    key: "user:123",
    valueFactory: () => LoadUserFromDatabase(123),
    absoluteExpiration: DateTimeOffset.Now.AddMinutes(10)
);

// The valueFactory only executes if the item isn't in cache
// Multiple concurrent calls with the same key won't result in multiple executions
```

### 3. ExecuteWithCache Extensions for IMemoryCache

Execute and cache method results with automatic key generation from expressions.

```csharp
using Nextended.Cache.Extensions;
using Microsoft.Extensions.Caching.Memory;

public class ProductService
{
    private readonly IMemoryCache _cache;
    
    public Product GetProduct(int productId)
    {
        // Cache key is automatically generated from the expression
        var info = this.ExecuteWithCache(
            _cache,
            () => LoadProductFromDb(productId),
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
        );
        
        // info.Result contains the product
        // info.IsNewEntry tells you if it was cached or newly loaded
        // info.Key contains the generated cache key
        return info.Result;
    }
    
    private Product LoadProductFromDb(int productId)
    {
        return database.Products.Find(productId);
    }
}
```

### 4. Conditional Cache Clearing

Clear cache based on conditions with automatic monitoring.

```csharp
var cacheProvider = new CacheProvider();

// Clear cache automatically when a condition is met
cacheProvider.ClearWhen(cache => 
{
    // Clear if more than 1 hour has passed since last write
    return (DateTime.Now - cache.LastWriteTime).TotalHours > 1;
});

// Clear manually
cacheProvider.Clear();

// Check cache size
int itemCount = cacheProvider.Count();

// Subscribe to clear event
cacheProvider.Cleared += (sender, args) =>
{
    Console.WriteLine("Cache was cleared!");
};
```

## Usage Examples

### Expression-Based Caching

```csharp
using Nextended.Cache;
using Nextended.Cache.Extensions;

public class DataService
{
    private readonly CacheProvider _cacheProvider;
    private readonly IMemoryCache _memoryCache;
    
    public DataService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _cacheProvider = new CacheProvider(memoryCache);
    }
    
    public List<Product> GetProducts(string category)
    {
        // Cache key is automatically generated from method signature and parameters
        return this.ExecuteWithCache(_cacheProvider, () => 
            LoadProductsFromDb(category));
    }
    
    public User GetUserProfile(int userId)
    {
        // Using IMemoryCache directly
        var cacheInfo = this.ExecuteWithCache(
            _memoryCache,
            () => LoadUserProfile(userId),
            new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(20))
        );
        
        if (cacheInfo.IsNewEntry)
        {
            Console.WriteLine($"Loaded user {userId} from database");
        }
        
        return cacheInfo.Result;
    }
    
    private List<Product> LoadProductsFromDb(string category)
    {
        return database.Products.Where(p => p.Category == category).ToList();
    }
    
    private User LoadUserProfile(int userId)
    {
        return database.Users
            .Include(u => u.Profile)
            .FirstOrDefault(u => u.Id == userId);
    }
}
```

### Thread-Safe Caching with ObjectCache

```csharp
using System.Runtime.Caching;
using Nextended.Cache.Extensions;

public class ConfigurationService
{
    private readonly ObjectCache _cache = MemoryCache.Default;
    
    public AppSettings GetSettings()
    {
        // Multiple threads calling this will only execute LoadSettings once
        return _cache.AddOrGetExisting(
            "app:settings",
            () => LoadSettings(),
            DateTimeOffset.Now.AddHours(1)
        );
    }
    
    public string GetConnectionString(string name)
    {
        return _cache.AddOrGetExisting(
            $"connection:{name}",
            () => LoadConnectionString(name),
            ObjectCache.InfiniteAbsoluteExpiration // Never expires
        );
    }
    
    private AppSettings LoadSettings()
    {
        // Expensive operation - read from file, database, etc.
        return Configuration.LoadFromFile("appsettings.json");
    }
    
    private string LoadConnectionString(string name)
    {
        return Configuration.GetConnectionString(name);
    }
}
```

### Automatic Cache Invalidation

```csharp
using Nextended.Cache;

public class CachedDataService
{
    private readonly CacheProvider _cache;
    
    public CachedDataService()
    {
        _cache = new CacheProvider();
        
        // Configure automatic cache clearing
        ConfigureCacheInvalidation();
    }
    
    private void ConfigureCacheInvalidation()
    {
        // Clear cache after 2 hours of inactivity
        _cache.ClearWhen(cache => 
            (DateTime.Now - cache.LastWriteTime).TotalHours > 2);
        
        // Clear cache if it grows too large
        _cache.ClearWhen(cache => cache.Count() > 1000);
        
        // Set custom check interval
        _cache.ClearCheckInterval = TimeSpan.FromMinutes(5);
        
        // Log when cache is cleared
        _cache.Cleared += (sender, args) => 
            Logger.Info("Cache was automatically cleared");
    }
    
    public Product GetProduct(int id)
    {
        return this.ExecuteWithCache(_cache, () => LoadProduct(id));
    }
    
    private Product LoadProduct(int id)
    {
        return database.Products.Find(id);
    }
}
```

### Custom Cache Entry Options

```csharp
using Microsoft.Extensions.Caching.Memory;
using Nextended.Cache;

public class AdvancedCachingService
{
    private readonly CacheProvider _cache;
    
    public AdvancedCachingService()
    {
        // Configure default cache options
        var options = new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High)
            .SetAbsoluteExpiration(TimeSpan.FromHours(1))
            .SetSlidingExpiration(TimeSpan.FromMinutes(20))
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                Console.WriteLine($"Cache key {key} was evicted: {reason}");
            });
        
        _cache = new CacheProvider(
            cache: null, // Will create default MemoryCache
            cacheEntryOptions: options
        );
    }
    
    public Data GetData(string key)
    {
        return this.ExecuteWithCache(_cache, () => LoadData(key));
    }
    
    private Data LoadData(string key)
    {
        return dataSource.Get(key);
    }
}
```

## Best Practices

### 1. Use Expression-Based Caching for Method Results

```csharp
// Good: Automatic key generation based on method and parameters
return this.ExecuteWithCache(_cache, () => ExpensiveOperation(param1, param2));

// The cache key is automatically generated from the expression
// Different parameters result in different cache keys
```

### 2. Monitor Cache Performance

```csharp
public class MonitoredCacheService
{
    private readonly CacheProvider _cache;
    
    public MonitoredCacheService()
    {
        _cache = new CacheProvider();
        
        // Subscribe to events
        _cache.Cleared += OnCacheCleared;
        
        // Set up periodic monitoring
        _cache.ClearWhen(cache => 
        {
            var count = cache.Count();
            if (count > 0)
                Logger.Debug($"Cache size: {count} items");
            return false; // Don't clear
        });
    }
    
    private void OnCacheCleared(object sender, EventArgs e)
    {
        Logger.Info("Cache was cleared");
    }
}
```

### 3. Use Appropriate Expiration Strategies

```csharp
var cacheProvider = new CacheProvider(
    cacheEntryOptions: new MemoryCacheEntryOptions()
        // Use sliding expiration for frequently accessed data
        .SetSlidingExpiration(TimeSpan.FromMinutes(30))
        // Use absolute expiration for time-sensitive data
        .SetAbsoluteExpiration(TimeSpan.FromHours(2))
);
```

### 4. Handle Cache Invalidation Properly

```csharp
public class UserService
{
    private readonly CacheProvider _cache;
    
    public void UpdateUser(User user)
    {
        database.Users.Update(user);
        database.SaveChanges();
        
        // Clear cache to ensure fresh data on next request
        _cache.Clear();
    }
}
```

## API Reference

### CacheProvider

- **Constructor**: `CacheProvider(IMemoryCache cache = null, MemoryCacheEntryOptions cacheEntryOptions = null)`
- **ExecuteWithCache**: `T ExecuteWithCache<TInstance, T>(TInstance owner, Expression<Func<TInstance, T>> expression)`
- **Clear**: `void Clear()` - Clears all cached items
- **Count**: `int Count()` - Returns the number of items in cache
- **ClearWhen**: `CacheProvider ClearWhen(Func<CacheProvider, bool> predicate)` - Registers a condition for automatic cache clearing
- **Properties**:
  - `MemoryCacheEntryOptions CacheEntryOptions` - Default cache entry options
  - `TimeSpan ClearCheckInterval` - Interval for checking clear conditions (default: 10 minutes)
  - `DateTime LastWriteTime` - Time of last cache write
- **Events**:
  - `EventHandler<EventArgs> Cleared` - Fired when cache is cleared

### CacheExtensions

- **AddOrGetExisting**: `T AddOrGetExisting<T>(this ObjectCache cache, string key, Func<T> valueFactory, DateTimeOffset absoluteExpiration, string regionName = null)`
- **ExecuteWithCache**: `CacheExecutionInfo<TResult> ExecuteWithCache<TParam, TResult>(this TParam param, IMemoryCache cache, Expression<Func<TParam, TResult>> expression, MemoryCacheEntryOptions entryOptions = null)`

## Configuration

### ASP.NET Core Integration

```csharp
// Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheProvider>();

// Or with custom configuration
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.CompactionPercentage = 0.25;
});

builder.Services.AddSingleton(sp =>
{
    var memoryCache = sp.GetRequiredService<IMemoryCache>();
    var cacheOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
    
    return new CacheProvider(memoryCache, cacheOptions);
});
```

## Supported Frameworks

- .NET Standard 2.0
- .NET Standard 2.1
- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Microsoft.Extensions.Caching.Abstractions` - Caching abstractions
- `Microsoft.Extensions.Caching.Memory` - In-memory caching
- `System.Runtime.Caching` - Runtime caching support

## Performance Considerations

- **Automatic Key Generation**: Cache keys are generated from method expressions, including parameters
- **Thread Safety**: `AddOrGetExisting` uses lazy initialization for thread-safe caching
- **Memory Usage**: Monitor cache size using `Count()` method
- **Automatic Invalidation**: Use `ClearWhen` for condition-based cache clearing
- **Check Interval**: Adjust `ClearCheckInterval` based on your needs

## Related Projects

- [Nextended.Core](core.md) - Foundation library with core extensions

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Cache/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Cache)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
