---
layout: default
title: Home
nav_order: 1
description: "Nextended - A comprehensive .NET library suite providing powerful extension methods, types, and utilities"
permalink: /
---

# Nextended

[![NuGet](https://img.shields.io/nuget/v/Nextended.Core.svg)](https://www.nuget.org/packages/Nextended.Core/)
[![License](https://img.shields.io/github/license/fgilde/Nextended)](LICENSE)

Welcome to Nextended - a comprehensive suite of .NET libraries providing powerful extension methods, custom types, utilities, and code generation tools to enhance your .NET development experience.

## Overview

Nextended is a collection of libraries designed to simplify and accelerate .NET development across various platforms and frameworks. Originally known as "nExt", the library was updated and renamed to Nextended with full support for modern .NET versions.

## 📦 Package Ecosystem

The Nextended suite consists of multiple specialized packages, each serving a specific purpose:

### Core Libraries

- **[Nextended.Core](projects/core.md)** - The foundation library with essential extension methods and custom types
- **[Nextended.Cache](projects/cache.md)** - Caching utilities and extensions
- **[Nextended.EF](projects/ef.md)** - Entity Framework Core extensions

### UI Libraries

- **[Nextended.Blazor](projects/blazor.md)** - Blazor-specific helpers and components
- **[Nextended.UI](projects/ui.md)** - WPF and Windows Forms utilities
- **[Nextended.Web](projects/web.md)** - ASP.NET Core and web application helpers

### Specialized Libraries

- **[Nextended.Imaging](projects/imaging.md)** - Image processing and manipulation utilities
- **[Nextended.CodeGen](projects/codegen.md)** - Source code generation tools (attributes, JSON/Excel to classes, DTO generation)
- **[Nextended.Aspire](projects/aspire.md)** - .NET Aspire framework extensions for distributed applications
- **[Nextended.AutoDto](projects/autodto.md)** - Automatic DTO generation utilities

## 🚀 Quick Start

### Installation

Install the core package via NuGet:

```bash
dotnet add package Nextended.Core
```

Or install specific packages based on your needs:

```bash
dotnet add package Nextended.Blazor
dotnet add package Nextended.EF
dotnet add package Nextended.CodeGen
```

### Basic Usage

```csharp
using Nextended.Core.Extensions;
using Nextended.Core.Types;

// Use powerful extension methods
var user = new User { FirstName = "John", LastName = "Doe" };
var userDto = user.MapTo<UserDto>();

// Work with custom types
var price = new Money(99.99m, Currency.USD);
var today = Date.Today;
```

## 📚 Documentation

### Getting Started
- [Installation Guide](guides/installation.md)
- [Architecture Overview](guides/architecture.md)
- [Migration from nExt](guides/migration.md)

### Projects
- [All Projects Overview](projects/README.md)
- Browse individual project documentation in the [projects](projects/) folder

### Examples
- [Common Use Cases](examples/common-use-cases.md)
- [Class Mapping Examples](examples/class-mapping.md)
- [Custom Types Examples](examples/custom-types.md)
- [Code Generation Examples](examples/code-generation.md)

### API Reference
- [Extension Methods Reference](api/extensions.md)
- [Custom Types Reference](api/types.md)
- [Class Mapping Reference](api/class-mapping.md)
- [Helper Utilities Reference](api/helpers.md)
- [Encryption & Security Reference](api/encryption.md)

## 🎯 Key Features

### Extension Methods
Nextended.Core provides extensive extension methods for:
- String manipulation
- DateTime operations
- Collection operations (LINQ enhancements)
- Type reflection and conversion
- Object mapping and cloning
- Task and async utilities
- And many more...

### Custom Types
Powerful custom types that solve common problems:
- **Money** - Precise decimal type for financial calculations
- **Date** - Date-only type without time components
- **BaseId** - Generic strongly-typed ID wrapper
- **SuperType** - Advanced entity type with subtype relationships
- **Range** - Generic range type for intervals

### Class Mapping
Fast and flexible object mapping without external dependencies:
```csharp
// Simple mapping
var dto = sourceObject.MapTo<TargetDto>();

// Advanced mapping with settings
var settings = ClassMappingSettings.Default
    .IgnoreProperties<Source>(s => s.InternalField)
    .AddConverter<string, DateTime>(DateTime.Parse);
var result = source.MapTo<Target>(settings);
```

See the [Class Mapping Reference](api/class-mapping.md) for complete documentation, examples, and usage scenarios.

### Code Generation
Generate code at compile-time from various sources:
- Auto-generate DTOs from your domain models
- Create strongly-typed classes from JSON/XML configuration files
- Generate data classes from Excel spreadsheets
- Full compile-time validation and IntelliSense support

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## 🔗 Links

- [Source Repository](https://github.com/fgilde/Nextended)
- [NuGet Package (Nextended.Core)](https://www.nuget.org/packages/Nextended.Core/)
- [Legacy Package (nExt.Core - no longer updated)](https://www.nuget.org/packages/nExt.Core/)

## 📬 Support

For issues, questions, or feature requests, please use the [GitHub Issues](https://github.com/fgilde/Nextended/issues) page.
