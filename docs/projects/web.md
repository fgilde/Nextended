# Nextended.Web

ASP.NET Core and web application helpers, extensions, and utilities.

## Overview

Nextended.Web provides utilities for ASP.NET Core applications, including controller extensions, HTTP helpers, and OData support.

## Installation

```bash
dotnet add package Nextended.Web
```

## Key Features

### 1. Controller Extensions

Enhanced controller functionality for ASP.NET Core MVC and API controllers.

```csharp
using Nextended.Web.Extensions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUsers()
    {
        // Use extension methods
        var users = GetUserData();
        return Ok(users);
    }
}
```

### 2. HTTP Helpers

Utilities for working with HTTP requests and responses.

```csharp
using Nextended.Web.Helper;

public class ApiService
{
    public async Task<T> GetDataAsync<T>(string url)
    {
        return await HttpHelper.GetAsync<T>(url);
    }
    
    public async Task<bool> PostDataAsync<T>(string url, T data)
    {
        var response = await HttpHelper.PostAsync(url, data);
        return response.IsSuccessStatusCode;
    }
}
```

### 3. OData Support

Extensions for working with OData in ASP.NET Core.

```csharp
using Nextended.Web.OData;
using Microsoft.AspNetCore.OData;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddOData(options => options
                .EnableQueryFeatures()
                .AddRouteComponents("odata", GetEdmModel())
            );
    }
}
```

## Usage Examples

### RESTful API Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Nextended.Web.Extensions;
using Nextended.Core.Extensions;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string search = null)
    {
        var products = await _productService.GetProductsAsync(search);
        return Ok(products);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        
        if (product == null)
            return NotFound();
        
        return Ok(product);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var product = dto.MapTo<Product>();
        await _productService.CreateProductAsync(product);
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var existing = await _productService.GetProductByIdAsync(id);
        if (existing == null)
            return NotFound();
        
        dto.MapTo(existing);
        await _productService.UpdateProductAsync(existing);
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        
        if (!result)
            return NotFound();
        
        return NoContent();
    }
}
```

### OData Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

public class OrdersController : ODataController
{
    private readonly ApplicationDbContext _context;
    
    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [EnableQuery]
    public IActionResult Get()
    {
        return Ok(_context.Orders);
    }
    
    [EnableQuery]
    public IActionResult Get(int key)
    {
        var order = _context.Orders.FirstOrDefault(o => o.Id == key);
        
        if (order == null)
            return NotFound();
        
        return Ok(order);
    }
}
```

### Middleware Example

```csharp
using Microsoft.AspNetCore.Http;
using Nextended.Core.Extensions;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(
        RequestDelegate next, 
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Request {Method} {Path} completed in {Duration}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                duration.TotalMilliseconds,
                context.Response.StatusCode
            );
        }
    }
}

// Extension method for middleware registration
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
```

### API Error Handling

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    
    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;
        
        switch (exception)
        {
            case ArgumentException _:
            case ValidationException _:
                code = HttpStatusCode.BadRequest;
                result = exception.Message;
                break;
            case NotFoundException _:
                code = HttpStatusCode.NotFound;
                result = exception.Message;
                break;
            case UnauthorizedException _:
                code = HttpStatusCode.Unauthorized;
                result = "Unauthorized";
                break;
            default:
                result = "An error occurred processing your request.";
                break;
        }
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return context.Response.WriteAsJsonAsync(new
        {
            error = result,
            statusCode = (int)code
        });
    }
}
```

### File Upload Handling

```csharp
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    
    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }
    
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)] // 10 MB limit
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");
        
        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Invalid file type");
        
        // Process file
        using var stream = file.OpenReadStream();
        var fileId = await _fileService.SaveFileAsync(stream, file.FileName);
        
        return Ok(new { fileId, fileName = file.FileName });
    }
    
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> Download(string fileId)
    {
        var fileData = await _fileService.GetFileAsync(fileId);
        
        if (fileData == null)
            return NotFound();
        
        return File(
            fileData.Content, 
            fileData.ContentType, 
            fileData.FileName
        );
    }
}
```

## Best Practices

### 1. Use DTOs for API Requests/Responses

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CreateUserDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

### 2. Implement Proper Validation

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(new ValidationProblemDetails(ModelState));
    }
    
    // Process request
    var user = await _userService.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
```

### 3. Use Async/Await Consistently

```csharp
[HttpGet]
public async Task<IActionResult> GetUsers()
{
    var users = await _userService.GetAllUsersAsync();
    return Ok(users);
}
```

### 4. Implement Proper Status Codes

```csharp
// 200 OK - Success
return Ok(data);

// 201 Created - Resource created
return CreatedAtAction(nameof(GetById), new { id }, resource);

// 204 No Content - Success with no body
return NoContent();

// 400 Bad Request - Client error
return BadRequest("Invalid input");

// 404 Not Found - Resource not found
return NotFound();

// 500 Internal Server Error - Server error
return StatusCode(500, "Internal server error");
```

## Configuration

### Program.cs Setup

```csharp
using Nextended.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Nextended.Web services
// builder.Services.AddNextendedWeb();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Nextended.EF` - Entity Framework extensions
- `Microsoft.AspNetCore.OData` - OData support

## Related Projects

- [Nextended.Core](core.md) - Foundation library
- [Nextended.EF](ef.md) - Database operations
- [Nextended.Blazor](blazor.md) - Blazor components

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Web/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Web)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
