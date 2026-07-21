# Nextended.Aspire.Hosting.AspireUI

Run [AspireUI](https://github.com/fgilde/AspireUI) — the visual .NET Aspire AppHost builder — as a
resource inside your own Aspire stack.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithSeedStack("My App", builder.AppHostDirectory);

builder.Build().Run();
```

This adds the `ghcr.io/fgilde/aspireui` container with:

- an **HTTP endpoint** for the web UI,
- the host **Docker socket** mounted, so stacks you build in AspireUI can run,
- a **named volume** for AspireUI's data (stacks, settings, users).

## API

| Call | Effect |
|------|--------|
| `AddAspireUI(name = "aspireui", port?, image?, tag?)` | Add the AspireUI container. |
| `.WithAdminUser(username, password)` | Seed the admin on first run (idempotent; password stored hashed). Also accepts an Aspire `ParameterResource` for the password. |
| `.WithSeedStack(name, params projectPaths)` | Seed a starter stack with one `AddProject` node per path. |
| `.WithSourceMount(hostPath, containerPath?)` | Bind-mount source into the container so a seeded stack can also run there. |

> The Docker-socket mount gives the container control over the host Docker daemon — run it only on a
> trusted host. Seeding is first-run only: once AspireUI has any user, the admin/stack seed is skipped.
