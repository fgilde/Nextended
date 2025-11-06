---
layout: default
title: Migration from nExt
parent: Guides
nav_order: 3
---

# Migration Guide from nExt to Nextended

This guide helps you migrate from the legacy nExt package to Nextended.

## Overview

Nextended is the successor to nExt, providing the same powerful functionality with enhanced features and support for modern .NET versions (.NET 8 and .NET 9).

## Key Changes

### Package Name
- **Old**: `nExt.Core`
- **New**: `Nextended.Core`

### Namespace
- **Old**: `nExt.*`
- **New**: `Nextended.*`

### NuGet Package
- **Old**: https://www.nuget.org/packages/nExt.Core/ (no longer maintained)
- **New**: https://www.nuget.org/packages/Nextended.Core/

## Migration Steps

### 1. Update Package References

Update your `.csproj` file:

**Before:**
```xml
<PackageReference Include="nExt.Core" Version="6.x.x" />
```

**After:**
```xml
<PackageReference Include="Nextended.Core" Version="9.x.x" />
```

### 2. Update Using Statements

Use Find & Replace to update namespaces across your solution:

**Before:**
```csharp
using nExt;
using nExt.Extensions;
using nExt.Types;
```

**After:**
```csharp
using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
```

### 3. Update Code References

Most functionality remains the same. The primary change is the namespace:

**Before:**
```csharp
using nExt.Extensions;

var result = myString.ToCamelCase();
var dto = source.MapTo<TargetDto>();
```

**After:**
```csharp
using Nextended.Core.Extensions;

var result = myString.ToCamelCase(); // Same API
var dto = source.MapTo<TargetDto>();  // Same API
```

## Compatibility Matrix

| nExt Version | Nextended Version | .NET Support |
|--------------|-------------------|--------------|
| 6.x          | 7.x               | .NET 6, .NET 7 |
| 7.x          | 8.x               | .NET 8 |
| -            | 9.x               | .NET 8, .NET 9 |

## Breaking Changes

### Minimal Breaking Changes

Nextended maintains backward compatibility with nExt's API. Most code will work without changes after updating namespaces.

### Known Changes

1. **Namespace**: All `nExt.*` namespaces are now `Nextended.*`
2. **Package Name**: Package must be updated in project files
3. **Target Frameworks**: Modern Nextended versions target newer .NET versions

## Automated Migration

### Using Find & Replace

1. **In Visual Studio:**
   - Press `Ctrl+Shift+H` (Edit → Find and Replace → Replace in Files)
   - Find: `using nExt`
   - Replace: `using Nextended.Core`
   - Click "Replace All"

2. **Using Command Line (PowerShell):**
   ```powershell
   Get-ChildItem -Recurse -Filter *.cs | ForEach-Object {
       (Get-Content $_.FullName) -replace 'using nExt', 'using Nextended.Core' | 
       Set-Content $_.FullName
   }
   ```

3. **Using dotnet CLI:**
   ```bash
   dotnet remove package nExt.Core
   dotnet add package Nextended.Core
   ```

## Step-by-Step Example

### Before Migration

**MyProject.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="nExt.Core" Version="6.0.10" />
  </ItemGroup>
</Project>
```

**UserService.cs:**
```csharp
using System;
using nExt.Extensions;
using nExt.Types;

public class UserService
{
    public UserDto GetUser(int id)
    {
        var user = LoadUser(id);
        return user.MapTo<UserDto>();
    }
    
    public Money CalculateTotal(decimal amount)
    {
        return new Money(amount, Currency.USD);
    }
}
```

### After Migration

**MyProject.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Nextended.Core" Version="9.0.15" />
  </ItemGroup>
</Project>
```

**UserService.cs:**
```csharp
using System;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

public class UserService
{
    public UserDto GetUser(int id)
    {
        var user = LoadUser(id);
        return user.MapTo<UserDto>(); // Same API!
    }
    
    public Money CalculateTotal(decimal amount)
    {
        return new Money(amount, Currency.USD); // Same API!
    }
}
```

## Testing After Migration

After migration, ensure all functionality works:

1. **Build the solution:**
   ```bash
   dotnet build
   ```

2. **Run tests:**
   ```bash
   dotnet test
   ```

3. **Verify functionality:**
   - Test extension methods
   - Test custom types (Money, Date, etc.)
   - Test object mapping
   - Test any other nExt features you use

## New Features in Nextended

While migrating, consider taking advantage of new features:

### Enhanced Extension Methods
```csharp
// New in Nextended
var batches = items.Batch(10);
var result = await operation.WithTimeout(TimeSpan.FromSeconds(30));
```

### Improved Type Support
```csharp
// Better .NET 8/9 support
var today = Date.Today; // Now aligns with DateOnly in .NET 6+
```

### Additional Packages
Consider adding specialized packages:
```bash
dotnet add package Nextended.EF        # Entity Framework extensions
dotnet add package Nextended.Blazor    # Blazor helpers
dotnet add package Nextended.CodeGen   # Code generation
```

## Troubleshooting

### Build Errors After Migration

**Problem**: Compilation errors after updating packages.

**Solution**:
1. Clean the solution: `dotnet clean`
2. Delete `bin` and `obj` folders
3. Restore packages: `dotnet restore`
4. Rebuild: `dotnet build`

### Namespace Not Found

**Problem**: Compiler can't find types after migration.

**Solution**:
- Ensure all `using nExt` statements are updated to `using Nextended.Core`
- Check that the Nextended.Core package is properly installed
- Verify project targets a supported framework (.NET Standard 2.0+ or .NET 8+)

### Version Conflicts

**Problem**: NuGet version conflicts.

**Solution**:
- Ensure all Nextended packages use the same version
- Update all packages: `dotnet add package Nextended.Core --version 9.0.15`

## Support

If you encounter issues during migration:

1. Check the [Documentation](https://fgilde.github.io/Nextended/)
2. Review [Common Use Cases](../examples/common-use-cases.md)
3. Open an [Issue on GitHub](https://github.com/fgilde/Nextended/issues)

## Summary

Migration from nExt to Nextended is straightforward:
1. Update package reference
2. Update namespaces (Find & Replace)
3. Test your application
4. Optionally upgrade to newer .NET versions
5. Explore new Nextended packages and features

The API remains largely the same, ensuring a smooth transition with minimal code changes.
