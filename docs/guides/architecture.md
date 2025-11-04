# Architecture Overview

## Solution Structure

The Nextended solution is organized into multiple specialized libraries, each with a specific focus area. This modular architecture allows you to include only the packages you need in your projects.

## Dependency Graph

```
Nextended.Core (Foundation)
    ├── Nextended.Cache
    ├── Nextended.EF
    ├── Nextended.Blazor
    ├── Nextended.Web
    │   └── Nextended.EF
    ├── Nextended.Imaging
    │   └── Nextended.Cache
    └── Nextended.UI

Nextended.CodeGen (Independent - Code Generator)
Nextended.AutoDto (Independent - DTO Generator)
Nextended.Aspire (Independent - Aspire Extensions)
```

## Core Architecture Principles

### 1. Extension-Based Design
Most functionality is provided through extension methods, making the API discoverable and easy to use:

```csharp
// Extensions naturally extend existing types
string result = myString.ToCamelCase();
var mapped = sourceObject.MapTo<TargetType>();
```

### 2. Minimal Dependencies
Each package maintains minimal external dependencies to reduce conflicts and keep package sizes small. Most packages only depend on:
- `Nextended.Core` (for shared utilities)
- Framework-specific packages (e.g., `Microsoft.EntityFrameworkCore` for `Nextended.EF`)

### 3. Multi-Targeting
Most libraries support multiple .NET versions for maximum compatibility:
- .NET Standard 2.0 and 2.1 (for broad compatibility)
- .NET 8.0 (for modern features)
- .NET 9.0 (for latest capabilities)

### 4. Compile-Time Code Generation
Code generation packages (CodeGen, AutoDto) use Roslyn source generators for:
- Zero runtime overhead
- Full IntelliSense support
- Type-safe generated code
- Build-time validation

## Package Responsibilities

### Nextended.Core
**Purpose**: Foundation library providing core functionality used across all other packages.

**Key Components**:
- Extension methods (String, DateTime, Collections, Type, Object, Task, etc.)
- Custom types (Money, Date, BaseId, SuperType, Range)
- Object mapping and cloning
- Serialization helpers
- Validation utilities
- Reflection helpers

**Target Audience**: All .NET developers needing enhanced base functionality.

### Nextended.Cache
**Purpose**: Simplified caching abstractions and utilities.

**Key Components**:
- `CacheProvider` - Unified caching interface
- Extension methods for `IMemoryCache` and `MemoryCache`
- Caching helpers with automatic expiration

**Dependencies**: `Nextended.Core`, Microsoft caching abstractions

### Nextended.EF
**Purpose**: Entity Framework Core extensions and utilities.

**Key Components**:
- `AlternateQueryMatchExtensions` - Advanced query matching
- `DbSetExtensions` - Enhanced DbSet operations
- Query optimization helpers

**Dependencies**: `Nextended.Core`, `Microsoft.EntityFrameworkCore`

### Nextended.Blazor
**Purpose**: Blazor-specific helpers and extensions.

**Key Components**:
- Blazor component helpers
- JavaScript interop utilities
- Localization helpers
- Navigation extensions

**Dependencies**: `Nextended.Core`, Blazor framework packages

### Nextended.Web
**Purpose**: ASP.NET Core and web application utilities.

**Key Components**:
- Controller extensions
- HTTP helpers
- OData utilities
- Web-specific extensions

**Dependencies**: `Nextended.Core`, `Nextended.EF`, ASP.NET Core packages

### Nextended.UI
**Purpose**: WPF and Windows Forms utilities.

**Key Components**:
- `ViewUtility` - UI helpers
- WPF behaviors
- Theming support
- ViewModel base classes

**Dependencies**: `Nextended.Core`, WPF/WinForms frameworks
**Platform**: Windows only (net8.0-windows, net9.0-windows)

### Nextended.Imaging
**Purpose**: Image processing and manipulation utilities.

**Key Components**:
- `ImageHelper` - Comprehensive image operations
- `ImageSize` - Image dimension handling
- Image format conversions
- Caching support for processed images

**Dependencies**: `Nextended.Core`, `Nextended.Cache`, `System.Drawing.Common`

### Nextended.CodeGen
**Purpose**: Compile-time source code generation from various sources.

**Key Components**:
- DTO generation from classes with attributes
- Class generation from JSON/XML structures
- Excel-to-class generation
- Configurable via JSON configuration file

**Type**: Roslyn Source Generator
**Dependencies**: Roslyn APIs

### Nextended.AutoDto
**Purpose**: Automatic DTO generation utilities.

**Key Components**:
- DTO generation infrastructure
- Attribute-based configuration

**Type**: Source Generator Support
**Dependencies**: Roslyn APIs

### Nextended.Aspire
**Purpose**: .NET Aspire distributed application extensions.

**Key Components**:
- Conditional dependency configuration
- Environment variable helpers
- Docker management utilities
- Endpoint configuration extensions

**Dependencies**: .NET Aspire hosting packages

## Design Patterns

### Extension Methods Pattern
Used throughout for discoverability and ease of use:
```csharp
public static class StringExtensions
{
    public static string ToCamelCase(this string value) 
    { 
        // Implementation
    }
}
```

### Builder Pattern
Used in mapping configuration:
```csharp
var settings = ClassMappingSettings.Default
    .IgnoreProperties<Source>(s => s.Field1)
    .WithConverter<DateTime, string>(dt => dt.ToString("yyyy-MM-dd"));
```

### Provider Pattern
Used in caching and other infrastructure:
```csharp
public class CacheProvider : ICacheProvider
{
    // Unified caching interface
}
```

### Source Generator Pattern
Used in code generation packages:
```csharp
[Generator]
public class DtoGenerator : ISourceGenerator
{
    // Compile-time code generation
}
```

## Threading and Async Support

- Extension methods for `Task` and `Task<T>` for async operations
- Thread-safe implementations where applicable
- `PausableCancellationTokenSource` for pausable async operations

## Serialization Support

- JSON serialization (Newtonsoft.Json)
- XML serialization
- YAML serialization (YamlDotNet)
- Custom serialization helpers

## Platform Considerations

### Cross-Platform Packages
Most packages target .NET Standard 2.0/2.1 for maximum compatibility across:
- .NET Core/.NET 5+
- .NET Framework 4.6.1+
- Xamarin
- Unity

### Windows-Only Packages
- **Nextended.UI**: Requires Windows for WPF/WinForms support

### Web-Specific Packages
- **Nextended.Blazor**: Browser platform support
- **Nextended.Web**: ASP.NET Core environment
- **Nextended.Aspire**: Distributed application hosting

## Performance Considerations

1. **Caching**: Extensive use of caching for expensive operations (reflection, type conversion)
2. **Lazy Evaluation**: Deferred execution where appropriate
3. **Memory Efficiency**: Careful memory management in imaging and large data operations
4. **Zero-Cost Abstractions**: Source generators produce optimal code without runtime overhead

## Versioning Strategy

All packages share a common version number for consistency. The version follows:
- Major.Minor.Patch format
- Aligned with .NET version support (e.g., 9.0.x for .NET 9 support)
- Backward compatibility maintained within major versions
