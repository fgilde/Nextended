---
layout: default
title: Nextended.Aspire.Hosting.N8n
parent: Projects
nav_order: 13
---

# Nextended.Aspire.Hosting.N8n

A first-class [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) integration for
[n8n](https://n8n.io) — the fair-code workflow-automation platform. With one `AddN8n("n8n")` call
you get a fully wired n8n instance with a PostgreSQL backend, sensible self-hosting defaults,
optional queue mode, and 1:1 deployment to Azure Container Apps via `azd up`.

## Overview

The package wraps the official `n8nio/n8n` container into an Aspire-friendly fluent API. n8n is
modelled as a single Aspire resource that owns its database (and, in queue mode, a Redis broker and
worker containers) — all visible and grouped together in the Aspire dashboard.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.N8n
```

## Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var n8n = builder.AddN8n("n8n");

builder.Build().Run();
```

This starts n8n on port 5678 with an auto-created PostgreSQL backend.

## Database Backend

By default `AddN8n` creates a dedicated PostgreSQL container. Supply your own, or switch to SQLite:

```csharp
// Existing Aspire PostgreSQL resource (removes the auto-created one)
var pg = builder.AddPostgres("pg");
var db = pg.AddDatabase("n8ndb");
var n8n = builder.AddN8n("n8n").WithDatabase(db);

// ...or a server + database name
var n8n2 = builder.AddN8n("n8n2").WithDatabase(pg, "n8ndb");

// ...or the bundled SQLite database (single container)
var n8n3 = builder.AddN8n("n8n3").WithSqlite();
```

## Queue Mode (Redis + Workers)

For scalable, production-like execution:

```csharp
var n8n = builder.AddN8n("n8n")
    .WithQueueMode(workers: 3);   // Redis broker + 3 worker containers
```

Queue mode requires the PostgreSQL backend.

## Configuration

```csharp
var n8n = builder.AddN8n("n8n")
    .WithEncryptionKey("a-stable-32+-char-secret")   // keep stable across restarts!
    .WithBasicAuth("admin", "supersecret")
    .WithTimezone("Europe/Berlin")
    .WithWebhookUrl("https://n8n.example.com/")
    .WithEditorBaseUrl("https://n8n.example.com/")
    .WithImageTag("1.110.1")
    .WithEnvironmentVariable("N8N_LOG_LEVEL", "debug");
```

| Method | Purpose |
| --- | --- |
| `WithEncryptionKey(key)` / `WithEncryptionKey(parameter)` | Credential encryption key — string or Aspire parameter (keep stable!) |
| `WithRedisPassword(pw)` / `WithRedisPassword(parameter)` | Queue-mode Redis password — string or Aspire parameter |
| `WithTimezone(tz)` | Sets the scheduling/cron timezone |
| `WithWebhookUrl(url)` | Public webhook base URL (behind a proxy) |
| `WithEditorBaseUrl(url)` | Public editor base URL |
| `WithImage(image, tag)` / `WithImageTag(tag)` | Override the container image |
| `WithHostPort(port)` | Fixed editor host port (local development) |
| `WithEnvironmentVariable(name, value)` | Set any raw n8n env var |
| `WithBasicAuth(user, pw)` | Legacy basic auth — only n8n < 1.0 (see note below) |

Secrets (encryption key, Redis password) accept either a plain string (simple) or an
`IResourceBuilder<ParameterResource>` (recommended — user secrets locally, Key Vault on deploy).
Everything has a sensible default, so the zero-config `AddN8n("n8n")` just works.

> **`WithBasicAuth`**: sets the legacy `N8N_BASIC_AUTH_*` variables and only affects n8n < 1.0.
> The modern default image uses the built-in owner-account / user-management model (configured
> interactively on first launch); those variables are ignored there.

## Importing Workflows & Credentials

A one-shot init container imports JSON exports before the main instance starts (local development):

```csharp
var n8n = builder.AddN8n("n8n")
    .WithImportWorkflows(Path.Combine(builder.AppHostDirectory, "..", "n8n", "workflows"))
    .WithImportCredentials(Path.Combine(builder.AppHostDirectory, "..", "n8n", "credentials"));
```

(Use `n8n export:workflow --separate` / `n8n export:credentials --separate` to produce the files.)

## Accessing Resources

```csharp
var n8n = builder.AddN8n("n8n").WithQueueMode(2);

var database = n8n.GetDatabase();      // IResourceBuilder<PostgresDatabaseResource>?
var redis    = n8n.GetRedis();         // IResourceBuilder<RedisResource>?
var workers  = n8n.GetWorkers();
var endpoint = n8n.GetHttpEndpoint();  // EndpointReference

builder.AddProject<Projects.MyApi>("api")
    .WithReference(n8n)                // ConnectionStrings:n8n = n8n URL
    .WaitFor(n8n);
```

## Consuming the n8n API from a service

```csharp
builder.AddN8nClient("n8n", s => s.ApiKey = builder.Configuration["N8n:ApiKey"]);

public sealed class MyService(N8nApiClient n8n)
{
    public Task<HttpResponseMessage> ListWorkflows() => n8n.Http.GetAsync("/api/v1/workflows");
}
```

## Deployment

```bash
azd init
azd up
```

All containers and their configuration are translated 1:1 from the Aspire model into Bicep/ACA
resources. The n8n editor is exposed via an external HTTPS ingress; PostgreSQL, Redis and the
workers stay internal.

## Default Ports

| Service | Default port |
| --- | --- |
| n8n editor / REST API | 5678 |
| PostgreSQL backend | internal (auto-created) |
| Redis (queue mode) | internal |

## Supported frameworks

- .NET 8.0
- .NET 9.0
- .NET 10.0

## Related projects

- [Nextended.Aspire](aspire.md) — general-purpose Aspire extensions
- [Nextended.Aspire.Hosting.Supabase](aspire-supabase.md) — full Supabase stack as an Aspire resource

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.N8n/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.N8n)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
- [n8n Documentation](https://docs.n8n.io)
