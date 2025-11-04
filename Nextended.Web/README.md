# Nextended.Web

[![NuGet](https://img.shields.io/nuget/v/Nextended.Web.svg)](https://www.nuget.org/packages/Nextended.Web/)

ASP.NET Core and web application helpers, extensions, and utilities.

## Overview

Nextended.Web provides utilities for ASP.NET Core applications, including controller extensions, HTTP helpers, and OData support.

## Installation

```bash
dotnet add package Nextended.Web
```

## Key Features

- **Controller Extensions**: Enhanced controller functionality
- **HTTP Helpers**: Utilities for HTTP requests and responses
- **OData Support**: Extensions for working with OData
- **Middleware Helpers**: Utilities for building middleware

## Quick Start

```csharp
using Microsoft.AspNetCore.Mvc;
using Nextended.Web.Extensions;
using Nextended.Core.Extensions;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string search = null)
    {
        var users = await _userService.GetUsersAsync(search);
        return Ok(users);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = dto.MapTo<User>();
        await _userService.CreateUserAsync(user);
        
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
}
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/web.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Nextended.Core
- Nextended.EF
- Microsoft.AspNetCore.OData

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Web/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/web.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Web)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
