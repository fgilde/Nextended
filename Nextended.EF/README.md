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

- **Alternate Query Match Extensions**: Flexible search across multiple properties
- **DbSet Extensions**: Enhanced DbSet operations (Upsert, BulkInsert, FindOrCreate)
- **Query Optimization**: Utilities for optimizing EF queries

## Quick Start

```csharp
using Nextended.EF;
using Microsoft.EntityFrameworkCore;

// Search across multiple properties
var searchTerm = "john";
var users = await dbContext.Users
    .AlternateQueryMatch(searchTerm)
    .ToListAsync();

// Find or create
var user = await dbContext.Users
    .FindOrCreateAsync(
        u => u.Email == "test@example.com",
        () => new User { Email = "test@example.com" }
    );

// Bulk operations
await dbContext.Users.BulkInsertAsync(userList);
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
