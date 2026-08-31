# Nextended.Aspire.Hosting.DbTools

Clone a database into an Aspire resource — the whole thing, schema and data, from another resource
in the stack or from a server that Aspire knows nothing about.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.DbTools.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

```bash
dotnet add package Nextended.Aspire.Hosting.DbTools
```

## What it is for

Aspire can create a database and run a script in it. What it cannot do is fill one from a database
that already exists — and that is the thing everybody wants first: a development stack with real
shapes and real rows in it, a new environment built out of an old one, a copy of staging to try a
migration against.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var dev = builder.AddPostgres("pg");

// From a server this stack does not model
dev.AddDatabase("shop")
   .WithCloneFrom("Host=staging.internal;Database=shop;Username=reader;Password=…");

// Or from another database in the stack
var prod = builder.AddPostgres("prod").AddDatabase("shop");
dev.AddDatabase("shop-copy").WithCloneFrom(prod);
```

That is the whole surface. What arrives is the whole database: tables, rows, indexes, constraints,
views, routines, and the sequences with the numbers they were counted to.

## The engines, and what each one is cloned with

Each engine is cloned with its own dump and restore tools, because they know things about their
format that a hand-written copy does not.

| Engine | How | Notes |
|---|---|---|
| PostgreSQL | `pg_dump \| psql` | Tools are in the image. `pg_dump` refuses a server newer than itself — see *versions* below |
| MySQL / MariaDB | `mysqldump \| mysql` | Tools are in the image. Routines and events are asked for explicitly, because mysqldump leaves them out |
| SQL Server | BACPAC via `sqlpackage` | The one engine whose tools are not in its image — see *SQL Server* below |
| MongoDB | `mongodump \| mongorestore` | One archive stream, no temporary file. Collections, documents, indexes |
| Redis | replication | The target is pointed at the source and cut loose again. Every key with its TTL, in Redis's own binary form |

## Where the source can come from

All four overloads exist for every engine, because a stack rarely has everything modelled.

```csharp
// A resource in this stack: typed, so the engines cannot be mixed up by accident
target.WithCloneFrom(otherDatabase);

// A connection string, in either form people write them
target.WithCloneFrom("Host=box;Database=shop;Username=u;Password=p");
target.WithCloneFrom("postgres://u:p@box:5432/shop");

// A parameter — which is where a connection string carrying a password belongs
var staging = builder.AddParameter("staging-connection", secret: true);
target.WithCloneFrom(staging);

// Or a connection string this stack was given rather than built
var external = builder.AddConnectionString("legacy");
target.WithCloneFrom(external);
```

The last two are not known until the stack runs, so the whole string travels to the container and is
taken apart there. Both forms are understood in both places: `Host=`/`Server=`/`Data Source=`,
`Username=`/`User Id=`/`Uid=`, SQL Server's `Server=host,1433`, and the `scheme://user:pw@host:port/db`
form with its escapes.

## What it will and will not overwrite

```csharp
target.WithCloneFrom(source, new DbCloneOptions
{
    OnlyWhenEmpty = true,      // the default
    Overwrite     = false,     // say this to replace what is there
    SchemaOnly    = false,     // the shape without the rows
    DataOnly      = false,     // the rows into a schema that exists
    Image         = null,      // the image the dump runs in
    Name          = null,      // what the clone is called in the dashboard
    TimeoutSeconds = 3600,
});
```

**A clone leaves a target that already has something in it alone.** That is the default because the
alternative is a stack restart that throws away the morning's work. `Overwrite = true` replaces it,
and it is deliberate: nothing here does that for you.

`SchemaOnly` is refused for MongoDB and Redis, when the app host is built rather than three minutes
later by a container: a collection *is* its documents and a key *is* its value, so there would be
nothing to copy.

## It runs where the stack runs

A clone is a **container resource**, not code in the app host. That matters more than it sounds:

* `dotnet run` starts it like everything else, and its log is in the dashboard.
* `aspire publish` puts it in the manifest, so a deployed stack clones too. If the point is to build
  a new system out of an old one, this is the only way that works — the app host itself does not run
  when a stack is published.
* Nothing is scoped to run mode. If you want a clone only while developing, put your own `if`
  around it:

```csharp
if (builder.ExecutionContext.IsRunMode)
    dev.AddDatabase("shop").WithCloneFrom(staging);
```

The clone waits for the target, and for the source when the source is a resource here. A source
outside the stack cannot be waited for, so the script waits for it itself.

## The things worth knowing before you rely on it

**SQL Server needs the internet the first time.** `sqlcmd` and `bcp` are in the server image and
neither carries a schema; `sqlpackage`, which does, is a .NET tool that is in no image at all. So the
clone runs in `mcr.microsoft.com/dotnet/sdk:8.0` and installs it — about half a minute, once per
container start. For a stack that cannot reach nuget.org, build an image with sqlpackage in it and
say so:

```csharp
orders.WithCloneFrom(staging, new DbCloneOptions { Image = "registry.internal/sqlpackage:1" });
```

It is the .NET **8** SDK on purpose: sqlpackage 170 runs on net8, and the 9 image has no 8 runtime.

**`Overwrite` on SQL Server adds a second container.** Importing into a database that already holds
objects is refused by sqlpackage itself — which is the "only when empty" rule, enforced without
anybody asking. Replacing it means dropping it first, and dropping needs a client that lives in the
server's image rather than the clone's, so a `…-clone-prepare` resource appears next to the clone.

**Versions.** A dump tool generally reads its own version and older, not newer. The image defaults
to a current one per engine; a source that is newer needs `Image` set.

**A clone copies the data as it is.** Real customers, real addresses, real e-mail addresses. Nothing
here anonymises anything — if that is what you need, take a masked subset instead: WebDataStudio's
*development subset* writes one as a script that
[`WithSeedScript`](https://www.nuget.org/packages/Nextended.Aspire.Hosting.WebDataStudio/) can load.

## A whole example: Northwind, cloned, opened in a studio

This is in the repository and it runs:
[WebDataStudio.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost).

```csharp
var demoPassword = builder.AddParameter("demo-password", "change-me-please", secret: true);
var postgres = builder.AddPostgres("pg");

// The server the data comes from. A plain container rather than a database resource, because it
// stands in for the one that lives somewhere else — and the clone reaches it the way it would reach
// that: by connection string.
builder.AddContainer("northwind-legacy", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", demoPassword)
    .WithEnvironment("POSTGRES_DB", "northwind")
    .WithBindMount("northwind", "/docker-entrypoint-initdb.d")
    .WithEndpoint(targetPort: 5432, name: "pg");

// What a stack has when the database is not one of its resources.
var source = builder.AddConnectionString("northwind-source",
    ReferenceExpression.Create(
        $"Host=northwind-legacy;Port=5432;Username=postgres;Password={demoPassword};Database=northwind"));

// The copy this stack owns, and the studio that opens it.
postgres.AddDatabase("northwind")
    .WithCloneFrom(source)
    .WithWebDataStudio();
```

`dotnet run`, and the studio comes up with a **NORTHWIND** connection holding eight tables, their
foreign keys, the indexes and the `order_totals` view — none of which this app host describes. It was
cloned.

<!-- NEXTENDED:FOOTER:START generated by tools/Update-PackageDocs.ps1 — do not edit by hand -->
## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Dependencies

- Aspire.Hosting.AppHost
- Aspire.Hosting.PostgreSQL
- Aspire.Hosting.SqlServer
- Aspire.Hosting.MySql
- Aspire.Hosting.MongoDB
- Aspire.Hosting.Redis

## The Nextended family

The other 18 packages in the suite:

**Core libraries**

- [Nextended.Core](https://github.com/fgilde/Nextended/blob/main/Nextended.Core/README.md) — Foundation library — extension methods, custom types (Money, Date, BaseId, SuperType), class mapping, deep clone, encryption, hashing and the code-generation attributes.
- [Nextended.Cache](https://github.com/fgilde/Nextended/blob/main/Nextended.Cache/README.md) — Expression-based caching — automatic cache keys from method expressions, CacheProvider with condition-based invalidation, thread-safe AddOrGetExisting.

**Data access**

- [Nextended.EF](https://github.com/fgilde/Nextended/blob/main/Nextended.EF/README.md) — Entity Framework Core extensions — graph loading (LoadGraphAsync, IncludeAll, MultiInclude), declarative include definitions, paging, dynamic sorting and bulk operations.

**ASP.NET Core & web**

- [Nextended.Web](https://github.com/fgilde/Nextended/blob/main/Nextended.Web/README.md) — ASP.NET Core utilities — zero-config OData (AddODataAuto), composable IQueryable OData appliers, strongly typed controller URLs, streaming download helpers and a background executor that can replay a captured request.
- [Nextended.ResponseFilters](https://github.com/fgilde/Nextended/blob/main/Nextended.ResponseFilters/README.md) — Fluent, provider-agnostic pipeline that redacts, masks, rounds, truncates, hashes, prunes and restructures response DTOs before serialization — per request, per user, per permission.
- [Nextended.ResponseFilters.AspNetCore](https://github.com/fgilde/Nextended/blob/main/Nextended.ResponseFilters.AspNetCore/README.md) — ASP.NET Core adapter for Nextended.ResponseFilters — registers the pipeline as a global IAsyncResultFilter and replays structural edits against the serialized JSON tree.

**UI libraries**

- [Nextended.Blazor](https://github.com/fgilde/Nextended/blob/main/Nextended.Blazor/README.md) — Blazor helpers — IBrowserFile extensions (bytes, data URLs, downloads), a hierarchical model for browsing inside uploaded zip/tar/rar archives, MIME-type detection and component-parameter reflection.
- [Nextended.UI](https://github.com/fgilde/Nextended/blob/main/Nextended.UI/README.md) — WPF and Windows desktop helpers — a global input-binding manager with hold/sequence matching, DirectInput and XInput gamepad readers, key-bind capture controls, converters, behaviours, markup extensions and runtime-defined PropertyGrid types.

**Code generation & tooling**

- [Nextended.Imaging](https://github.com/fgilde/Nextended/blob/main/Nextended.Imaging/README.md) — Image processing — aspect-preserving resize, crop, colour replacement, brightness-based foreground picking, thumbnail generation, byte/data-URL conversion and MIME detection from magic bytes.
- [Nextended.CodeGen](https://github.com/fgilde/Nextended/blob/main/Nextended.CodeGen/README.md) — Roslyn source generator — DTOs and interfaces from your entities, strongly typed classes from JSON/XML, lookup tables from Excel, and documentation from source files.

**.NET Aspire hosting**

- [Nextended.Aspire](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire/README.md) — Conditional AppHost builder extensions — WithReferenceIf / WaitForIf / WithExplicitStartIf, strongly typed environment variables from config objects, HTTPS dev-cert wiring, Docker guards, GitHub-source resources and npm app discovery.
- [Nextended.Aspire.Hosting.Supabase](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Supabase/README.md) — The complete Supabase stack — Postgres, Auth (GoTrue), REST, Realtime, Storage, Studio, Kong and Edge Functions — as one composable Aspire resource.
- [Nextended.Aspire.Hosting.N8n](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.N8n/README.md) — The n8n workflow-automation platform as an Aspire resource, with Postgres persistence, workflow import and a typed client for triggering workflows from .NET.
- [Nextended.Aspire.Hosting.Grafana](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Grafana/README.md) — Grafana, Prometheus, Loki, Tempo, Promtail, cAdvisor, postgres_exporter and the OpenTelemetry Collector as composable resources with auto-provisioned datasources.
- [Nextended.Aspire.Hosting.WebDataStudio](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.WebDataStudio/README.md) — WebDataStudio — a browser database studio for PostgreSQL, MySQL, SQL Server, SQLite, Oracle, DuckDB, ClickHouse, MongoDB and Redis — wired to the databases of your stack, with accounts and roles, an optional SQL assistant, and an MCP endpoint for AI agents.
- [Nextended.Aspire.Hosting.AspireUI](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.AspireUI/README.md) — AspireUI — the visual AppHost builder — as a resource inside your own Aspire stack, with an optional pre-seeded admin user and a starter stack built from your project paths.
- [Nextended.Aspire.Hosting.LocalAI](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.LocalAI/README.md) — Self-hosted, OpenAI-compatible multimodal AI — image generation, text-to-speech, speech-to-text and video — with gallery model management, GPU support and Open WebUI.
- [Nextended.Aspire.Hosting.Php](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Php/README.md) — Run PHP endpoints inside your Aspire stack — a docroot folder or a single router script served by PHP's built-in web server, with php.ini settings as fluent options.
- **Nextended.Aspire.Hosting.DbTools** — Clone a database into an Aspire resource — the whole thing, schema and data, from another resource in the stack or from a server that is not one. PostgreSQL, SQL Server, MySQL/MariaDB, MongoDB and Redis, each through its own engine's dump and restore tools, as a container resource so it works in run and in publish alike. _(this package)_

## Links

- 📦 [NuGet package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)
- 📖 [Documentation — English](https://fgilde.github.io/Nextended/projects/aspire-dbtools)
- 📖 [Dokumentation — Deutsch](https://fgilde.github.io/Nextended/de/projects/aspire-dbtools)
- 🏠 [Documentation portal](https://fgilde.github.io/Nextended/)
- 🧪 [Runnable sample](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.DbTools)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)

## License

GPL-3.0-or-later — see [LICENSE](https://github.com/fgilde/Nextended/blob/main/LICENSE).
<!-- NEXTENDED:FOOTER:END -->
