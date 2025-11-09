# GitHub Copilot Instructions for Nextended

## Repository Overview

Nextended (formerly nExt) is a comprehensive suite of .NET libraries providing powerful extension methods, custom types, utilities, and code generation tools. The library targets multiple .NET versions including netstandard2.0, netstandard2.1, net8.0, net9.0, and net10.0 (when available).

### Main Packages

- **Nextended.Core**: Foundation library with extension methods and custom types
- **Nextended.Blazor**: Blazor-specific helpers and components
- **Nextended.Cache**: Caching utilities and extensions
- **Nextended.EF**: Entity Framework Core extensions
- **Nextended.Web**: ASP.NET Core utilities
- **Nextended.Imaging**: Image processing utilities
- **Nextended.UI**: WPF/Windows UI helpers
- **Nextended.CodeGen**: Source code generation tools
- **Nextended.Aspire**: .NET Aspire extensions

## Development Environment Setup

### Prerequisites

- .NET 9 SDK (9.0.306 or later)
- .NET 8 SDK for multi-targeting support
- Visual Studio 2022 (17.14 or later) or JetBrains Rider
- Git for version control

### Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/fgilde/Nextended.git
   cd Nextended
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build Nextended.sln
   ```

## Building and Testing

### Building

Build the entire solution:
```bash
dotnet build Nextended.sln
```

**Note**: On non-Windows platforms, the `Nextended.UI` project (which targets `net8.0-windows`, `net9.0-windows`, and `net10.0-windows` when available) cannot be built. You can either:
- Build individual projects: `dotnet build Nextended.Core/Nextended.Core.csproj`
- Use the `--filter` option to exclude Windows-specific projects
- Build on Windows to include all projects

Build a specific project:
```bash
dotnet build Nextended.Core/Nextended.Core.csproj
```

Build for a specific target framework:
```bash
dotnet build Nextended.Core/Nextended.Core.csproj -f net9.0
```

### Testing

The test suite uses MSTest and xUnit frameworks.

Run all tests:
```bash
dotnet test Nextended.sln
```

Run tests for a specific project:
```bash
dotnet test Nextended.Core.Tests/Nextended.Core.Tests.csproj
```

Run tests with detailed output:
```bash
dotnet test --verbosity detailed
```

### Packaging

Generate NuGet packages:
```bash
dotnet pack Nextended.sln --configuration Release
```

## Project Structure

```
Nextended/
├── .github/              # GitHub configuration and workflows
├── docs/                 # Documentation files
├── Nextended.Core/       # Core library with extensions and types
├── Nextended.Core.Tests/ # Tests for core library
├── Nextended.Blazor/     # Blazor-specific utilities
├── Nextended.Cache/      # Caching utilities
├── Nextended.EF/         # Entity Framework extensions
├── Nextended.Web/        # ASP.NET Core utilities
├── Nextended.Imaging/    # Image processing
├── Nextended.UI/         # WPF/Windows UI helpers
├── Nextended.CodeGen/    # Code generation tools
├── Nextended.Aspire/     # .NET Aspire extensions
├── Nextended.AutoDto/    # DTO auto-generation
├── CodeGenSample/        # Sample project demonstrating code generation
├── Shared.props          # Shared MSBuild properties
├── Package.props         # NuGet package properties
├── Version.props         # Version configuration
└── Nextended.sln         # Main solution file
```

## Code Structure and Organization

### Namespaces

- `Nextended.Core.Extensions`: Extension methods for various types
- `Nextended.Core.Types`: Custom types (Money, Date, BaseId, etc.)
- `Nextended.Core.Helper`: Helper classes and utilities
- `Nextended.Core.Attributes`: Custom attributes
- `Nextended.Core.Contracts`: Interfaces and contracts

### Key Components

1. **Extension Methods**: Located in `Nextended.Core/Extensions/`
   - String extensions (case conversion, validation, manipulation)
   - DateTime extensions (business days, date ranges)
   - Collection extensions (LINQ operations, batch processing)
   - Type extensions (reflection, type inspection)
   - Object extensions (cloning, property manipulation)
   - Task extensions (async utilities, timeout operations)

2. **Custom Types**: Located in `Nextended.Core/Types/`
   - Money: Precise decimal type for financial calculations
   - Date: Date-only type without time components
   - BaseId: Generic strongly-typed ID wrapper
   - SuperType: Advanced entity type with subtype relationships
   - Range: Generic range type for intervals

3. **Class Mapping**: Object-to-object mapping functionality without external dependencies

## Coding Standards and Conventions

### General Guidelines

- Use C# latest language version (specified in project files)
- Follow .NET naming conventions (PascalCase for types/methods, camelCase for parameters/variables)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Target multiple frameworks when possible (netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0)
- Use conditional compilation symbols when framework-specific code is needed

### Code Style

- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs
- Use extension methods for utility functionality
- Prefer async/await for I/O operations
- Follow SOLID principles

### Testing

- Write unit tests for new functionality
- Use MSTest attributes (`[TestClass]`, `[TestMethod]`) or xUnit (`[Fact]`, `[Theory]`)
- Use descriptive test method names that explain the scenario
- Use Shouldly or standard assertions for test validations
- Organize tests in the same namespace structure as the code being tested

### Project Configuration

All projects inherit from:
- `Shared.props`: Common build settings (target frameworks, nullable support)
- `Package.props`: NuGet package metadata
- `Version.props`: Version information
- `Output.props`: Output path configuration

### Multi-Targeting

When adding framework-specific code, use conditional compilation:

```csharp
#if NET10_0
    // .NET 10 specific code
#elif NET9_0
    // .NET 9 specific code
#elif NET8_0
    // .NET 8 specific code
#elif NETSTANDARD2_1
    // .NET Standard 2.1 specific code
#elif NETSTANDARD2_0
    // .NET Standard 2.0 specific code
#endif
```

## Dependencies and Tools

### Key Dependencies

- **Newtonsoft.Json**: JSON serialization (version 13.0.1)
- **YamlDotNet**: YAML processing (version 13.3.1)
- **System.Linq.Dynamic.Core**: Dynamic LINQ (version 1.6.6)
- **StringToExpression**: Expression parsing (version 2.2.0)
- **Microsoft.Extensions.DependencyInjection.Abstractions**: DI abstractions

### Test Dependencies

- **Microsoft.NET.Test.Sdk**: Test platform (version 17.12.0)
- **MSTest.TestAdapter** and **MSTest.TestFramework**: MSTest support
- **xunit** and **xunit.runner.visualstudio**: xUnit support
- **Shouldly**: Fluent assertions (version 4.3.0)

## Common Tasks and Workflows

### Adding a New Extension Method

1. Navigate to the appropriate file in `Nextended.Core/Extensions/`
2. Add the extension method as a public static method
3. Add XML documentation
4. Create corresponding unit tests in `Nextended.Core.Tests/`
5. Build and test to ensure compatibility across all target frameworks

### Adding a New Custom Type

1. Create the type in `Nextended.Core/Types/`
2. Implement necessary interfaces (IEquatable, IComparable, etc.)
3. Add type converters if needed in `Nextended.Core/TypeConverters/`
4. Create comprehensive unit tests
5. Update documentation

### Releasing a New Version

1. Update version in `Version.props`
2. Update changelogs and documentation
3. Build and test across all target frameworks
4. Generate NuGet packages: `dotnet pack --configuration Release`
5. Publish to NuGet.org

## Documentation

- Main documentation portal: https://fgilde.github.io/Nextended/
- Project-specific READMEs in each project directory
- API documentation generated from XML comments
- Examples and use cases in `/docs/examples/`

## Repository-Specific Guidelines

### Namespacing

All types should be in the `Nextended.*` namespace hierarchy. Legacy `nExt.*` namespaces are deprecated.

### Backward Compatibility

When making changes:
- Maintain backward compatibility when possible
- Mark deprecated members with `[Obsolete]` attribute
- Provide migration paths for breaking changes

### Package Generation

All main projects have `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` enabled. Each package includes:
- README.md
- icon.png
- License information (GPL-3.0-or-later)

### Source Generators

The project uses T4 templates and source generators:
- T4 templates in `Nextended.Core/Extensions/` for generated code
- Roslyn source generators in `Nextended.CodeGen/` and `Nextended.AutoDto/`

## Performance Considerations

- Extension methods should be efficient and avoid unnecessary allocations
- Use `Span<T>` and `Memory<T>` where appropriate for .NET 8+ targets
- Cache reflection results when possible
- Consider using `MemoryCache` for expensive operations

## License

This project is licensed under GPL-3.0-or-later. All contributions must be compatible with this license.
