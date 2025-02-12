﻿
# .NET Aspire Extensions

The .NET Aspire Extensions library provides a set of convenient extension methods that simplify the configuration of distributed applications built with the Aspire framework. These extensions enable conditional dependency setup, environment variable management, and Docker checks in a clear and expressive manner.

## Key Features

### Conditional Dependency Configuration
- **WaitForIf / WaitForCompletionIf**  
  Conditionally waits for a dependency resource to be available if it implements the required interface.

- **WithReferenceIf**  
  Automatically adds a reference to a dependency if it is provided.

### Environment Variable Helpers
- **WithEnvironment**  
  Configures environment variables by generating keys from lambda expressions using the `CallerArgumentExpression` attribute.

- **WithEndpointAsEnvironmentIf**  
  Sets an environment variable based on an external endpoint if available.

### Docker Management
- **EnsureDockerRunning / EnsureDockerRunningIf / EnsureDockerRunningIfLocalDebug**  
  Ensures that Docker is running before the application starts, which is especially useful during development and debugging.

## Example Usage

Below is an exemplary snippet demonstrating how to use some of these extensions to configure resources in a distributed application:

```c#
using Aspire.Hosting.Postgres;
using Coworkee.AppHost;
using Coworkee.Application.Configurations;

var builder = DistributedApplication.CreateBuilder(args);

// Example: Configure a Postgres resource.
var db = builder.AddPostgres("pg")
    .PublishAsContainer()
    .AddDatabase(nameof(ServerConfiguration.ConnectionStrings.DefaultConnection), "ExampleDb");

// Optionally, wait for a dependent resource if it is provided.
db = db.WaitForIf(someDependencyResource);

// Example: Set an environment variable with automatic key generation.
db = db.WithEnvironment<SomeResourceType, SomeTargetType>(
    s => s.SomeProperty, "SomeValue");

// Build the application and ensure Docker is running in local debug mode.
builder.Build()
    .EnsureDockerRunningIfLocalDebug()
    .Run();
```

## Contributing

Contributions are welcome! If you have suggestions or find any issues, please open an issue or submit a pull request.



