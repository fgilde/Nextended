# Nextended.Blazor

[![NuGet](https://img.shields.io/nuget/v/Nextended.Blazor.svg)](https://www.nuget.org/packages/Nextended.Blazor/)

Blazor-specific helpers, extensions, and utilities for building modern web applications.

## Overview

Nextended.Blazor provides a collection of utilities and extensions specifically designed for Blazor applications, including component helpers, JavaScript interop utilities, and navigation extensions.

## Installation

```bash
dotnet add package Nextended.Blazor
```

## Key Features

- **JavaScript Interop Helpers**: Simplified JavaScript interop
- **Component Helpers**: Utilities for Blazor components
- **Navigation Extensions**: Enhanced navigation utilities
- **Localization Support**: Simplified localization helpers

## Quick Start

```razor
@using Nextended.Blazor.Extensions
@using Nextended.Core.Extensions
@inject IJSRuntime JSRuntime

<h3>@User.Name.ToPascalCase()</h3>
<button @onclick="CopyEmail">Copy Email</button>

@code {
    [Parameter]
    public User User { get; set; }
    
    private async Task CopyEmail()
    {
        await JSRuntime.InvokeVoidAsync(
            "navigator.clipboard.writeText", 
            User.Email
        );
    }
}
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/blazor.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Nextended.Core
- Microsoft.AspNetCore.Components.Web

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Blazor/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/blazor.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Blazor)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
