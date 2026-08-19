---
title: Nextended.Aspire.Hosting.WebDataStudio
---
# Nextended.Aspire.Hosting.WebDataStudio

📚 **[Full API reference](/projects/aspire-webdatastudio-api)** — every public type and member, generated from the compiled assembly.

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-webdatastudio)

A [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) integration for
[WebDataStudio](https://fgilde.github.io/WebDataStudio/) — a browser-based database studio for
PostgreSQL, MySQL, SQL Server, SQLite, Oracle, DuckDB, ClickHouse, MongoDB and Redis. One call on a
database resource and the studio comes up with that database already configured.

## Overview

The package wraps the `ghcr.io/fgilde/webdatastudio` container into an Aspire resource and writes
the studio's `WDS_CONN_*` environment variables from the connection strings your stack already
produces. Nothing is typed twice, and no connection string ends up in source control.

Sharing is the interesting part: several databases normally want *one* studio, but a stack may
also want a second, differently configured one. Both are the same call with a different name.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.WebDataStudio
```

## Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var shop = builder.AddPostgres("pg").AddDatabase("shop").WithWebDataStudio();
var orders = builder.AddSqlServer("sql").AddDatabase("orders").WithWebDataStudio();
builder.AddRedis("cache").WithWebDataStudio();

builder.Build().Run();
```

One studio resource appears in the dashboard, with `SHOP`, `ORDERS` and `CACHE` in its explorer.

## Sharing one studio, or running several

`WithWebDataStudio()` creates the studio the first time and attaches to it every time after. The
studio's resource name is the key:

```csharp
// The default studio, shared by everything that does not ask for another
shop.WithWebDataStudio();
orders.WithWebDataStudio();

// A second studio, chosen by name
warehouse.WithWebDataStudio(studioName: "analytics-studio");
events.WithWebDataStudio(studioName: "analytics-studio");

// A third, built by hand and handed to the databases that belong in it
var admin = builder.AddWebDataStudio("admin-studio")
    .WithLogin("admin", builder.AddParameter("studio-password", secret: true))
    .WithReadOnly();

production.WithWebDataStudio(admin, group: "Production", color: "#e03131");
```

From the studio's side the same stack reads like this:

```csharp
builder.AddWebDataStudio("studio")
    .WithReference(shop)
    .WithReference(orders, connectionName: "ORDERS_PROD", readOnly: true)
    .WithConnection("LEGACY", "Host=old-box;Database=legacy;Username=ro;Password=pw",
        WebDataStudioEngine.PostgreSql, group: "Legacy");
```

`WithReference` on a studio is this package's own overload. Aspire's built-in one writes a
`ConnectionStrings__*` variable, which the studio does not read; this one writes the `WDS_CONN_*`
variables it does.

## Configuration

| Call | Effect |
|------|--------|
| `AddWebDataStudio(name = "webdatastudio", port?, image?, tag?)` | Add the studio container: HTTP endpoint, health check, per-instance data volume. |
| `.WithReference(resource, connectionName?, engine?, readOnly?, group?, color?)` | Attach any resource with a connection string. |
| `.WithConnection(name, connectionString, engine, …)` | Attach a database outside the stack. Also accepts a `ReferenceExpression`. |
| `.WithLogin(user, password)` | Guard the studio. Both halves also accept a `ParameterResource`. |
| `.WithTitle(name)` | Name in the studio's header and browser tab. Defaults to the resource name; `null` leaves it unnamed. |
| `.WithReadOnly(readOnly = true)` | Every connection read-only, enforced in the driver. |
| `.WithQueryTimeout(TimeSpan)` | Default statement timeout. |
| `.WithMaxRows(int)` | Default row cap per result. |
| `.WithSessionLimits(maxSessions?, idleTimeout?)` | Cap open sessions per connection and their idle life. |
| `.WithSecretKey(base64)` | Key for the secrets the studio stores; also takes a `ParameterResource`. |
| `.WithDataVolume(name?)` / `.WithDataBindMount(path)` | Move the studio's own data. |
| `resource.WithWebDataStudio(configure?, studioName?, connectionName?, engine?)` | Attach from the database's side. |
| `resource.WithWebDataStudio(studio, …)` | Attach to a studio you built yourself. |

## Engines

The engine is read from the resource type, so `AddPostgres`, `AddSqlServer`, `AddMySql`,
`AddOracle`, `AddMongoDB`, `AddRedis`, `AddValkey` and `AddGarnet` need no help. For anything else
pass `engine:` explicitly:

```csharp
studio.WithReference(clickhouse, engine: WebDataStudioEngine.ClickHouse);
```

Without one, the studio guesses from the connection string and skips the connection rather than
attaching it to the wrong driver.

## Accessing Resources

```csharp
var studio = builder.AddWebDataStudio();

studio.Resource.HttpEndpoint;       // the endpoint serving the studio
studio.Resource.ConnectionNames;    // labels of everything attached, in order
studio.Resource.Username;           // null while there is no login
studio.Resource.Title;              // the name shown in the studio, resource name by default
```

## Notes

- Connection names become environment variables: `shop-db` shows up as `SHOP_DB`. Pass
  `connectionName` for something nicer. A name ending in `_ENGINE`, `_READONLY`, `_GROUP` or
  `_COLOR` is rejected, because the studio reads those as settings for another connection.
- Without `WithLogin` the studio has no login screen at all — the right default while it listens
  on your machine only.
- Every studio gets its own named volume, so two of them never share saved connections.

## Default Ports

| Resource | Container port | Host port |
|---|---|---|
| WebDataStudio | 8080 | assigned by Aspire unless `port:` says otherwise |

## Supported frameworks

.NET 8, .NET 9, .NET 10.

## Related projects

- [Nextended.Aspire](aspire.md) — the shared Aspire helpers
- [Nextended.Aspire.Hosting.N8n](aspire-n8n.md), [Supabase](aspire-supabase.md) — other hosting integrations

## The sample AppHost

`Tests/TestProjects/WebDataStudio.AppHost` runs PostgreSQL, SQL Server, MongoDB and Redis behind
three studios — the shared one, a named one for analytics and a locked-down one with a login — and
seeds PostgreSQL with a small shop schema (customers, products, orders, order items and a view) so
the studio has real data in it on the first start.

## Links

- [Sample AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)
- [WebDataStudio documentation](https://fgilde.github.io/WebDataStudio/guide/)
- [WebDataStudio on GitHub](https://github.com/fgilde/WebDataStudio)
- [NuGet](https://www.nuget.org/packages/Nextended.Aspire.Hosting.WebDataStudio/)
