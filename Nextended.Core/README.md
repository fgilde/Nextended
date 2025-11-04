# Nextended.Core

[![NuGet](https://img.shields.io/nuget/v/Nextended.Core.svg)](https://www.nuget.org/packages/Nextended.Core/)

The foundation library providing essential extension methods, custom types, and utilities for .NET development.

## Overview

Nextended.Core is the base library that all other Nextended packages depend on. It provides a comprehensive set of extension methods and custom types designed to enhance productivity and code quality in .NET applications.

## Installation

```bash
dotnet add package Nextended.Core
```

## Key Features

### Extension Methods
- **String Extensions**: Case conversions, validation, manipulation
- **DateTime Extensions**: Business day calculations, date ranges
- **Collection Extensions**: Advanced LINQ operations, batch processing
- **Type Extensions**: Reflection helpers, type inspection
- **Object Extensions**: Deep cloning, property manipulation
- **Task Extensions**: Async utilities, timeout operations

### Custom Types
- **Money**: Precise decimal type for financial calculations
- **Date**: Date-only type without time components
- **BaseId**: Generic strongly-typed ID wrapper
- **SuperType**: Advanced entity type with subtype relationships
- **Range**: Generic range type for intervals

### Class Mapping
Fast and flexible object mapping without external dependencies:

```csharp
// Simple mapping
var userDto = user.MapTo<UserDto>();

// Advanced mapping with settings
var settings = ClassMappingSettings.Default
    .IgnoreProperties<User>(u => u.Password);
var result = user.MapTo<UserDto>(settings);
```

## Quick Start

```csharp
using Nextended.Core.Extensions;
using Nextended.Core.Types;

// Extension methods
string text = "hello world";
string camelCase = text.ToCamelCase();     // "helloWorld"
string pascalCase = text.ToPascalCase();   // "HelloWorld"

// Custom types
var price = new Money(99.99m, Currency.USD);
var today = Date.Today;

// Object mapping
var dto = sourceObject.MapTo<TargetDto>();
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/core.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET Standard 2.0
- .NET Standard 2.1
- .NET 8.0
- .NET 9.0

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Core/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/core.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Core)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
