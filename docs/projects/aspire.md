---
layout: default
title: Nextended.Aspire
parent: Projects
nav_order: 9
---

# Nextended.Aspire

Extensions for .NET Aspire distributed application framework.

## Overview

Nextended.Aspire provides convenient extension methods that simplify the configuration of distributed applications built with the .NET Aspire framework. These extensions enable conditional dependency setup, environment variable management, and Docker checks.

## Installation

```bash
dotnet add package Nextended.Aspire
```

## Key Features

### 1. Conditional Dependency Configuration

Wait for dependencies only when they implement required interfaces.

```csharp
using Aspire.Hosting;
using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres")
    .WaitForIf(cache); // Only waits if cache supports waiting

builder.Build().Run();
```

### 2. Environment Variable Helpers

Configure environment variables using CallerArgumentExpression for type-safe keys.

```csharp
using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment<DatabaseConfig, ConnectionStrings>(
        c => c.DefaultConnection, 
        connectionString
    );
```

### 3. Docker Management

Ensure Docker is running before application starts. If Docker is not running, these methods will automatically attempt to start Docker Desktop.

```csharp
using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Configure resources...

builder.Build()
    .EnsureDockerRunning() // Starts Docker Desktop if not running
    .Run();

// Or conditionally check
builder.Build()
    .EnsureDockerRunningIf(condition) // Only checks and starts if condition is true
    .Run();

// Or check only in local debug
builder.Build()
    .EnsureDockerRunningIfLocalDebug() // Checks and starts only when debugging locally
    .Run();
```

### 4. Endpoint Configuration

Set environment variables based on endpoints.

```csharp
var api = builder.AddProject<Projects.MyApi>("api");
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WithEndpointAsEnvironmentIf(api, "ApiUrl", "http");
```

## Usage Examples

### Complete Aspire App Configuration

```csharp
using Aspire.Hosting;
using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var db = builder.AddPostgres("postgres")
    .PublishAsContainer()
    .AddDatabase("maindb", "ApplicationDb");

// Add Redis cache
var cache = builder.AddRedis("cache")
    .PublishAsContainer();

// Add API with dependencies
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(db)
    .WithReference(cache)
    .WithEnvironment("ConnectionStrings__DefaultConnection", db)
    .WithEnvironment("Redis__Configuration", cache);

// Add frontend with API reference
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WithReferenceIf(api) // Only reference if api is provided
    .WithEndpointAsEnvironmentIf(api, "ApiUrl", "https");

// Build and run with Docker check
builder.Build()
    .EnsureDockerRunningIfLocalDebug()
    .Run();
```

### Conditional Resource Configuration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Get configuration values
var useRedis = builder.Configuration.GetValue<bool>("UseRedis");
var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");

// Conditionally add resources
IResourceBuilder<RedisResource> cache = null;
if (useRedis)
{
    cache = builder.AddRedis("cache");
}

IResourceBuilder<PostgresServerResource> postgres = null;
IResourceBuilder<PostgresDatabaseResource> db = null;
if (usePostgres)
{
    postgres = builder.AddPostgres("postgres");
    db = postgres.AddDatabase("maindb");
}

// Add API with conditional dependencies
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReferenceIf(cache) // Only adds if cache is not null
    .WithReferenceIf(db);   // Only adds if db is not null

builder.Build().Run();
```

### Multi-Environment Configuration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;
var isDevelopment = environment == "Development";
var isProduction = environment == "Production";

// Different configurations per environment
var db = isDevelopment
    ? builder.AddPostgres("postgres").PublishAsContainer()
    : builder.AddPostgres("postgres").PublishAsConnectionString();

var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
    .WithReference(db);

// Only check Docker in development
builder.Build()
    .EnsureDockerRunningIf(isDevelopment)
    .Run();
```

### Frontend Integration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Backend services
var db = builder.AddPostgres("postgres").AddDatabase("maindb");
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(db);

// Frontend with backend endpoint
var frontend = builder.AddNpmApp("frontend", "../Frontend")
    .WithHttpEndpoint(port: 3000)
    .WithEndpointAsEnvironmentIf(api, "VITE_API_URL", "https");

builder.Build().Run();
```

### Service Bus Integration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Service Bus (or RabbitMQ)
var messaging = builder.AddAzureServiceBus("messaging");

// Add worker service
var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(messaging)
    .WithEnvironment("ServiceBus__ConnectionString", messaging);

// Add API that publishes messages
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(messaging);

builder.Build().Run();
```

## Best Practices

### 1. Use Conditional References

```csharp
// Only add reference if resource exists
var api = builder.AddProject<Projects.Api>("api")
    .WithReferenceIf(optionalDependency);
```

### 2. Check Docker in Development Only

```csharp
// Avoid Docker check in production
builder.Build()
    .EnsureDockerRunningIfLocalDebug()
    .Run();
```

### 3. Use Type-Safe Environment Variables

```csharp
// Better than magic strings
api.WithEnvironment<Config, Settings>(
    s => s.ConnectionString, 
    value
);
```

### 4. Handle Optional Dependencies

```csharp
// Gracefully handle missing dependencies
var api = builder.AddProject<Projects.Api>("api")
    .WaitForIf(cache)           // Wait only if exists
    .WithReferenceIf(cache);    // Reference only if exists
```

## Extension Methods Reference

### Resource Extensions
- `WaitForIf<T>()` - Conditionally wait for resource
- `WaitForCompletionIf<T>()` - Conditionally wait for completion
- `WithReferenceIf<T>()` - Conditionally add reference

### Environment Extensions
- `WithEnvironment<TSource, TTarget>()` - Type-safe environment variable
- `WithEndpointAsEnvironmentIf()` - Endpoint as environment variable

### Docker Extensions
- `EnsureDockerRunning()` - Ensure Docker is running, starts Docker Desktop if not running
- `EnsureDockerRunningIf()` - Conditionally check and start Docker
- `EnsureDockerRunningIfLocalDebug()` - Check and start Docker in debug mode only

### Dev Certificate Extensions
- `UseDevelopmentCertificate()` - Configure dev certificates

## Configuration

### appsettings.json

```json
{
  "Aspire": {
    "UseRedis": true,
    "UsePostgres": true,
    "CheckDocker": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;..."
  }
}
```

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- .NET Aspire hosting packages
- Aspire framework

## Related Projects

- [Nextended.Core](core.md) - Foundation library

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Aspire/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire)
- [Official README](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire/README.md)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire GitHub](https://github.com/dotnet/aspire)
