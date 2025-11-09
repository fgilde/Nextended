# Nextended

[![NuGet](https://img.shields.io/nuget/v/Nextended.Core.svg)](https://www.nuget.org/packages/Nextended.Core/)
[![License](https://img.shields.io/github/license/fgilde/Nextended)](LICENSE)

A comprehensive suite of .NET libraries providing powerful extension methods, custom types, utilities, and code generation tools.

> **Note**: This library was previously known as "nExt". It has been renamed to Nextended with full support for modern .NET versions including .NET 8, .NET 9, and .NET 10 (when available).

## 📦 Package Ecosystem

| Package | Description | NuGet |
|---------|-------------|-------|
| **[Nextended.Core](Nextended.Core/README.md)** | Foundation library with extension methods and custom types | [![NuGet](https://img.shields.io/nuget/v/Nextended.Core.svg)](https://www.nuget.org/packages/Nextended.Core/) |
| **[Nextended.Blazor](Nextended.Blazor/README.md)** | Blazor-specific helpers and components | [![NuGet](https://img.shields.io/nuget/v/Nextended.Blazor.svg)](https://www.nuget.org/packages/Nextended.Blazor/) |
| **[Nextended.Cache](Nextended.Cache/README.md)** | Caching utilities and extensions | [![NuGet](https://img.shields.io/nuget/v/Nextended.Cache.svg)](https://www.nuget.org/packages/Nextended.Cache/) |
| **[Nextended.EF](Nextended.EF/README.md)** | Entity Framework Core extensions | [![NuGet](https://img.shields.io/nuget/v/Nextended.EF.svg)](https://www.nuget.org/packages/Nextended.EF/) |
| **[Nextended.Web](Nextended.Web/README.md)** | ASP.NET Core utilities | [![NuGet](https://img.shields.io/nuget/v/Nextended.Web.svg)](https://www.nuget.org/packages/Nextended.Web/) |
| **[Nextended.Imaging](Nextended.Imaging/README.md)** | Image processing utilities | [![NuGet](https://img.shields.io/nuget/v/Nextended.Imaging.svg)](https://www.nuget.org/packages/Nextended.Imaging/) |
| **[Nextended.UI](Nextended.UI/README.md)** | WPF/Windows UI helpers | [![NuGet](https://img.shields.io/nuget/v/Nextended.UI.svg)](https://www.nuget.org/packages/Nextended.UI/) |
| **[Nextended.CodeGen](Nextended.CodeGen/README.md)** | Source code generation | [![NuGet](https://img.shields.io/nuget/v/Nextended.CodeGen.svg)](https://www.nuget.org/packages/Nextended.CodeGen/) |
| **[Nextended.Aspire](Nextended.Aspire/README.md)** | .NET Aspire extensions | [![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.svg)](https://www.nuget.org/packages/Nextended.Aspire/) |

## 🚀 Quick Start

### Installation

```bash
dotnet add package Nextended.Core
```

### Basic Usage

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
var userDto = user.MapTo<UserDto>();

// Advanced mapping with settings
var settings = ClassMappingSettings.Default
    .IgnoreProperties<User>(u => u.Password);
var dto = user.MapTo<UserDto>(settings);
```

## 🎯 Key Features

### Extension Methods
- **String**: Case conversions, validation, manipulation
- **DateTime**: Business day calculations, date ranges, formatting
- **Collections**: Advanced LINQ, batch processing, safe operations
- **Type**: Reflection helpers, type inspection, attribute retrieval
- **Object**: Deep cloning, property manipulation, conversions
- **Task**: Async utilities, timeout operations, fire-and-forget

### Custom Types
- **Money** - Precise decimal type for financial calculations
- **Date** - Date-only type without time components
- **BaseId** - Generic strongly-typed ID wrapper
- **SuperType** - Advanced entity type with subtype relationships
- **Range** - Generic range type for intervals

### Class Mapping
Fast and flexible object mapping without external dependencies:
```csharp
var dto = sourceObject.MapTo<TargetDto>();
```

### Code Generation
Generate code at compile-time from:
- Auto-generate DTOs from domain models
- Create strongly-typed classes from JSON/XML
- Generate data classes from Excel spreadsheets

## 📚 Documentation

- 🏠 **[Main Documentation Portal](https://fgilde.github.io/Nextended/)** - Complete documentation site
- 📖 **[Getting Started Guide](docs/guides/installation.md)** - Installation and setup
- 🏗️ **[Architecture Overview](docs/guides/architecture.md)** - Solution structure and design
- 📦 **[Projects Documentation](docs/projects/README.md)** - Individual project guides
- 💡 **[Common Use Cases](docs/examples/common-use-cases.md)** - Real-world examples

## 🔗 Migration from nExt

If you're migrating from the old nExt package:
- The namespace has changed from `nExt.*` to `Nextended.*`
- All functionality has been preserved and enhanced
- See the [Migration Guide](docs/guides/migration.md) for details (coming soon)

**Legacy Package** (no longer maintained): [nExt.Core](https://www.nuget.org/packages/nExt.Core/)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- [Documentation](https://fgilde.github.io/Nextended/)
- [Source Repository](https://github.com/fgilde/Nextended)
- [NuGet Packages](https://www.nuget.org/packages?q=Nextended)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
