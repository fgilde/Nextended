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

- **Unified Cache Provider**: Consistent interface across different caching implementations
- **Cache Extensions**: Fluent API for `IMemoryCache` and `MemoryCache`
- **Automatic Invalidation**: Time-based cache expiration support
- **Cache Key Management**: Utilities for generating and managing cache keys

## Quick Start

```csharp
using Nextended.Cache;
using Nextended.Cache.Extensions;

var cacheProvider = new CacheProvider();

// Store and retrieve with expiration
cacheProvider.Set("key", myObject, TimeSpan.FromMinutes(30));
var item = cacheProvider.Get<MyType>("key");

// Get or create pattern
var user = cache.GetOrCreate("user:123", () => 
{
    return LoadUserFromDatabase(123);
}, TimeSpan.FromMinutes(10));
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
