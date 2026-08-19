---
layout: default
title: Nextended.Aspire.Hosting.AspireUI
parent: Projects
nav_order: 16
---

# Nextended.Aspire.Hosting.AspireUI
{: .no_toc }

AspireUI — the visual AppHost builder — as a resource inside your own Aspire stack, with an optional pre-seeded admin user and a starter stack built from your project paths.
{: .fs-5 .fw-300 }

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.AspireUI.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)

🇩🇪 [Diese Seite auf Deutsch](https://github.com/fgilde/Nextended/blob/main/docs/de/projects/aspire-aspireui.md)

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.AspireUI
```

## Runnable sample

A complete AppHost you can start is checked into the repository:

**[AspireUI.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/AspireUI.AppHost)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/AspireUI.AppHost
dotnet run
```

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

## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Dependencies

- Aspire.Hosting.AppHost

## Links

- 📦 [NuGet package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.AspireUI)
- 📄 [Package README](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.AspireUI/README.md)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)