# Nextended Projects

This document provides an overview of all projects in the Nextended solution.

## Core Libraries

### [Nextended.Core](core.md)
**Description**: The foundation library providing essential extension methods and custom types.

**Key Features**:
- Extensive extension methods for built-in .NET types
- Custom types (Money, Date, BaseId, SuperType, Range)
- Object mapping and cloning
- Serialization helpers

**NuGet**: [Nextended.Core](https://www.nuget.org/packages/Nextended.Core/)

---

### [Nextended.Cache](cache.md)
**Description**: Caching utilities and extensions for simplified caching operations.

**Key Features**:
- Unified caching provider interface
- Extensions for IMemoryCache
- Automatic cache expiration management

**NuGet**: [Nextended.Cache](https://www.nuget.org/packages/Nextended.Cache/)

---

### [Nextended.EF](ef.md)
**Description**: Entity Framework Core extensions for enhanced database operations.

**Key Features**:
- Advanced query matching
- DbSet extensions
- Query optimization utilities

**NuGet**: [Nextended.EF](https://www.nuget.org/packages/Nextended.EF/)

---

## UI Libraries

### [Nextended.Blazor](blazor.md)
**Description**: Blazor-specific helpers, components, and extensions.

**Key Features**:
- Blazor component utilities
- JavaScript interop helpers
- Navigation and localization extensions

**NuGet**: [Nextended.Blazor](https://www.nuget.org/packages/Nextended.Blazor/)

---

### [Nextended.UI](ui.md)
**Description**: WPF and Windows Forms utilities for desktop applications.

**Key Features**:
- ViewUtility for UI operations
- WPF behaviors and templates
- ViewModel base classes
- Theming support

**NuGet**: [Nextended.UI](https://www.nuget.org/packages/Nextended.UI/)
**Platform**: Windows only

---

### [Nextended.Web](web.md)
**Description**: ASP.NET Core and web application helpers.

**Key Features**:
- Controller extensions
- HTTP utilities
- OData helpers
- Web-specific extensions

**NuGet**: [Nextended.Web](https://www.nuget.org/packages/Nextended.Web/)

---

## Specialized Libraries

### [Nextended.Imaging](imaging.md)
**Description**: Image processing and manipulation utilities.

**Key Features**:
- Comprehensive ImageHelper class
- Image format conversions
- Dimension handling
- Cached image processing

**NuGet**: [Nextended.Imaging](https://www.nuget.org/packages/Nextended.Imaging/)

---

### [Nextended.CodeGen](codegen.md)
**Description**: Compile-time source code generation from various sources.

**Key Features**:
- DTO generation from attributes
- Class generation from JSON/XML
- Excel-to-class generation
- Roslyn source generator

**NuGet**: [Nextended.CodeGen](https://www.nuget.org/packages/Nextended.CodeGen/)

---

### [Nextended.Aspire](aspire.md)
**Description**: Extensions for .NET Aspire distributed applications.

**Key Features**:
- Conditional dependency configuration
- Environment variable management
- Docker runtime checks
- Endpoint configuration helpers

**NuGet**: [Nextended.Aspire](https://www.nuget.org/packages/Nextended.Aspire/)

---

### [Nextended.AutoDto](autodto.md)
**Description**: Automatic DTO generation support library.

**Key Features**:
- DTO generation infrastructure
- Works with Nextended.CodeGen

**NuGet**: [Nextended.AutoDto](https://www.nuget.org/packages/Nextended.AutoDto/)

---

## Quick Reference Matrix

| Project | Target | Platform | Dependencies |
|---------|--------|----------|--------------|
| Nextended.Core | .NET Standard 2.0+, .NET 8/9 | Cross-platform | None |
| Nextended.Cache | .NET 8/9 | Cross-platform | Core |
| Nextended.EF | .NET 8/9 | Cross-platform | Core, EF Core |
| Nextended.Blazor | .NET 8/9 | Browser | Core, Blazor |
| Nextended.UI | .NET 8/9 | Windows | Core, WPF |
| Nextended.Web | .NET 8/9 | Cross-platform | Core, EF, ASP.NET |
| Nextended.Imaging | .NET 8/9 | Cross-platform | Core, Cache |
| Nextended.CodeGen | .NET Standard 2.0 | Build-time | Roslyn |
| Nextended.Aspire | .NET 8/9 | Cross-platform | Aspire |
| Nextended.AutoDto | .NET Standard 2.0 | Build-time | Roslyn |

## Installation

Install any package via NuGet:

```bash
dotnet add package [PackageName]
```

For example:
```bash
dotnet add package Nextended.Core
dotnet add package Nextended.Blazor
```

## Getting Help

- Check individual project documentation for detailed information
- Visit the [Examples](../examples/common-use-cases.md) section for code samples
- Review the [API Reference](../api/extensions.md) for detailed API documentation
- Submit issues on [GitHub](https://github.com/fgilde/Nextended/issues)
