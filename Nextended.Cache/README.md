# Nextended.Cache

[![NuGet](https://img.shields.io/nuget/v/Nextended.Cache.svg)](https://www.nuget.org/packages/Nextended.Cache/)

Caching utilities and extensions for simplified caching operations in .NET applications.

## Overview

Nextended.Cache provides a unified caching interface and extensions for working with various caching providers including `IMemoryCache` and `System.Runtime.Caching.MemoryCache`.

## Installation

```bash
dotnet add package Nextended.Cache
```

## Key Features

- **Expression-Based Caching**: Automatic cache key generation from method expressions
- **CacheProvider**: Intelligent caching with conditional clearing and monitoring
- **Thread-Safe Lazy Initialization**: AddOrGetExisting for ObjectCache
- **IMemoryCache Extensions**: ExecuteWithCache for simplified caching
- **Automatic Invalidation**: Condition-based cache clearing with monitoring

## Quick Start

```csharp
using Nextended.Cache;
using Nextended.Cache.Extensions;

// Create cache provider
var cacheProvider = new CacheProvider();

// Cache method execution with automatic key generation
public User GetUser(int userId)
{
    return this.ExecuteWithCache(_cacheProvider, () => LoadUserFromDb(userId));
}

// Thread-safe caching with ObjectCache
var result = MemoryCache.Default.AddOrGetExisting(
    "key",
    () => ExpensiveOperation(),
    DateTimeOffset.Now.AddMinutes(10)
);

// Conditional cache clearing
cacheProvider.ClearWhen(cache => 
    (DateTime.Now - cache.LastWriteTime).TotalHours > 1);
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/cache.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Nextended.Core
- Microsoft.Extensions.Caching.Abstractions
- Microsoft.Extensions.Caching.Memory

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Cache/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/cache.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Cache)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
