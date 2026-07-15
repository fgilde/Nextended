# Nextended.Aspire

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.svg)](https://www.nuget.org/packages/Nextended.Aspire/)

Extensions for .NET Aspire distributed application framework.

## Overview

The Nextended.Aspire library provides a set of convenient extension methods that simplify the configuration of distributed applications built with the Aspire framework. These extensions enable conditional dependency setup, environment variable management, and Docker checks in a clear and expressive manner.

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
  Ensures that Docker is running before the application starts. If Docker is not running, these methods will automatically attempt to start Docker Desktop. This is especially useful during development and debugging.

### Run any GitHub repository as a container
- **AddGithubRepository / WithGithubSource**  
  Clones (or refreshes) a git repository on the host and runs it as a container resource, built from
  the repo's own Dockerfile — or from a **generated** one for repos that ship none:

  ```c#
  // Repo has a Dockerfile:
  builder.AddGithubRepository("myui", "https://github.com/acme/my-ui",
          o => o.GitRef = "v1.2.0")                     // pin a tag for reproducible builds
      .WithHttpEndpoint(targetPort: 3000);

  // Repo without a Dockerfile — generate one:
  builder.AddGithubRepository("tool", "https://github.com/acme/tool", o =>
  {
      o.GitRef = "main";
      o.DockerfileContent = """
          FROM node:22-bookworm
          COPY . /app
          WORKDIR /app
          RUN npm ci
          CMD ["npm", "start"]
          """;
  });

  // Or turn an existing (custom) container resource into a source build:
  appBuilder.AddResource(myResource).WithGithubSource("https://github.com/acme/svc", o => o.GitRef = "v2");
  ```

  Checkouts live under `{AppHostDirectory}/obj/github/{name}` (shallow clone, refreshed per run,
  offline-safe once cloned). Used internally by `Nextended.Aspire.Hosting.LocalAI`'s `WithAceStepUi`.

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

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/aspire.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Aspire/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/aspire.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## Contributing

Contributions are welcome! If you have suggestions or find any issues, please open an issue or submit a pull request.

## License

This project is licensed under the GPL License.
