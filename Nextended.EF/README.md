# Nextended.EF

[![NuGet](https://img.shields.io/nuget/v/Nextended.EF.svg)](https://www.nuget.org/packages/Nextended.EF/)

Entity Framework Core extensions for enhanced database operations and query capabilities.

## Overview

Nextended.EF provides powerful extensions for Entity Framework Core, including advanced query matching, DbSet operations, and query optimization utilities.

## Installation

```bash
dotnet add package Nextended.EF
dotnet add package Microsoft.EntityFrameworkCore
```

## Key Features

- **LoadGraphAsync**: Automatically load entity graphs with navigation properties to a specified depth
- **IncludeAll**: Automatically include all navigation properties without manual specification
- **MultiInclude**: Simplified syntax for chaining multiple ThenInclude operations
- **Alternate Query Match Extensions**: Flexible search across multiple properties

## Quick Start

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

// Load an entity with all its navigation properties
var user = await dbContext.Users.FindAsync(userId);
await dbContext.LoadGraphAsync(user, maxDepth: 2);

// Include all navigation properties automatically
var products = await dbContext.Products
    .IncludeAll(dbContext)
    .ToListAsync();

// Include all except specific paths
var users = await dbContext.Users
    .IncludeAll(dbContext, new[] { "Orders.OrderItems" })
    .ToListAsync();

// Simplified multi-level includes
var orders = await dbContext.Orders
    .MultiInclude(
        q => q.Include(o => o.OrderItems),
        oi => oi.Product,
        oi => oi.Product.Category
    )
    .ToListAsync();

// Search across multiple properties
var searchResults = await dbContext.Users
    .AlternateQueryMatch("john")
    .ToListAsync();
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/ef.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Nextended.Core
- Microsoft.EntityFrameworkCore (9.0+)

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.EF/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/ef.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.EF)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
