---
title: Nextended.Aspire.Hosting.DbTools
---
# Nextended.Aspire.Hosting.DbTools

📚 **[Full API reference](/projects/aspire-dbtools-api)** — every public type and member, generated from the compiled assembly.

Database tools for an Aspire app host. The first one is cloning: filling a database resource from a database that already exists.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.DbTools.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-dbtools)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.DbTools
```

## What it does

One call fills a database resource from an existing database — schema and data:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var dev = builder.AddPostgres("pg");

// From a server this stack does not model
dev.AddDatabase("shop")
   .WithCloneFrom("Host=staging.internal;Database=shop;Username=reader;Password=…");

// Or from another resource in it
var prod = builder.AddPostgres("prod").AddDatabase("shop");
dev.AddDatabase("shop-copy").WithCloneFrom(prod);
```

What arrives is the whole database: tables, rows, indexes, constraints, views, routines, and the
sequences with the numbers they were counted to.

## When it earns its place

Aspire creates empty databases, and `WithCreationScript` / `WithInitFiles` fill them from SQL you
wrote. That covers most stacks. This is for the cases it does not:

* a development stack that needs the shapes and the volumes of a real database, not a fixture,
* a new environment built out of an existing one,
* a copy of staging to run a migration against before running it for real.

If a seed script is enough for you, use the seed script.

## The engines, and what each one is cloned with

Each engine is cloned with its own dump and restore tools, because they know things about their
format that a hand-written copy does not.

| Engine | How | Notes |
|---|---|---|
| PostgreSQL | `pg_dump \| psql` | Tools are in the image. `pg_dump` refuses a server newer than itself |
| MySQL / MariaDB | `mysqldump \| mysql` | Tools are in the image. Routines and events are asked for explicitly, because mysqldump leaves them out. The target database is created if it is missing |
| SQL Server | BACPAC via `sqlpackage` | The one engine whose tools are not in its own image — see below |
| MongoDB | `mongodump \| mongorestore` | One archive stream, no temporary file. Collections, documents, indexes |
| Redis | replication | The target is pointed at the source and cut loose again. Every key with its TTL, in Redis's own binary form |

## Where the source can come from

All four overloads exist for every engine, because a stack rarely has everything modelled.

```csharp
// A resource in this stack: typed, so two engines cannot be mixed up by accident
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
taken apart there. Both forms are understood in both places: `Host=` / `Server=` / `Data Source=`,
`Username=` / `User Id=` / `Uid=`, SQL Server's `Server=host,1433`, and the
`scheme://user:pw@host:port/db` form with its escapes.

## What it will and will not overwrite

```csharp
target.WithCloneFrom(source, new DbCloneOptions
{
    OnlyWhenEmpty  = true,     // the default
    Overwrite      = false,    // say this to replace what is there
    SchemaOnly     = false,    // the shape without the rows
    DataOnly       = false,    // the rows into a schema that exists
    Image          = null,     // the image the dump runs in
    Name           = null,     // what the clone is called in the dashboard
    TimeoutSeconds = 3600,
});
```

**A clone leaves a target that already has something in it alone.** That is the default because the
alternative is a stack restart that throws away the morning's work. `Overwrite = true` replaces it,
and it is deliberate: nothing here does that for you.

`SchemaOnly` is refused for MongoDB and Redis when the app host is built, rather than three minutes
later by a container: a collection *is* its documents and a key *is* its value, so there would be
nothing to copy.

## It runs where the stack runs

A clone is a **container resource**, not code in the app host. That matters more than it sounds:

* `dotnet run` starts it like everything else, and its log is in the dashboard.
* `aspire publish` puts it in the manifest, so a deployed stack clones too. If the point is to build
  a new system out of an old one, this is the only way that works — the app host itself does not run
  when a stack is published.
* Nothing is scoped to run mode. For a clone only while developing, put your own `if` around it:

```csharp
if (builder.ExecutionContext.IsRunMode)
    dev.AddDatabase("shop").WithCloneFrom(staging);
```

The clone waits for the target, and for the source when the source is a resource here. A source
outside the stack cannot be waited for, so the script waits for it itself — and keeps waiting while a
database that is still starting refuses connections.

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
objects is refused by sqlpackage itself, with `SQL71659` — which is the "only when empty" rule,
enforced without anybody asking for it. Replacing it means dropping it first, and dropping needs a
client that lives in the server's image rather than the clone's, so a `…-clone-prepare` resource
appears next to the clone and the clone waits for it to finish.

**Versions.** A dump tool generally reads its own version and older, not newer. The image defaults to
a current one per engine; a source that is newer needs `Image` set.

**Redis is replaced whole.** Redis has no database to scope a clone to, and a replica is not a merge.
The target is left as a master whether the sync finished or not: one left as somebody's replica would
be read-only for ever, which is worse than a clone that did not complete.

**A clone copies the data as it is.** Real customers, real addresses, real e-mail addresses. Nothing
here anonymises anything — if that is what you need, take a masked subset instead:
[WebDataStudio](/projects/aspire-webdatastudio)'s *development subset* writes one as a SQL script
that `WithSeedScript` can load into the next fresh stack.

## The sample, and what it proves

[`Tests/TestProjects/DbTools.AppHost`](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/DbTools.AppHost)
runs, and not one table in it is described by hand. It clones twice — once from each kind of source:

```csharp
// 1. A server this stack does not model. The stand-in for it is a plain container, because a server
//    somewhere else is not a database resource either.
builder.AddContainer("northwind-legacy", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", password)
    .WithEnvironment("POSTGRES_DB", "northwind")
    .WithBindMount("northwind", "/docker-entrypoint-initdb.d")
    .WithEndpoint(targetPort: 5432, name: "pg");

var externalNorthwind = builder.AddConnectionString("northwind-source",
    ReferenceExpression.Create(
        $"Host=northwind-legacy;Port=5432;Username=postgres;Password={password};Database=northwind"));

var postgres = builder.AddPostgres("pg", password: password);
var northwind = postgres.AddDatabase("northwind").WithCloneFrom(externalNorthwind);

// 2. Another resource in this stack — the typed overload, so two engines cannot be mixed up.
var mysql = builder.AddMySql("mysql", password: password).WithInitFiles("mysql-init");
var legacy = mysql.AddDatabase("legacy");
var parts = mysql.AddDatabase("parts").WithCloneFrom(legacy);

// And the fastest way to see that the rows really arrived.
northwind.WithWebDataStudio();
parts.WithWebDataStudio();
```

`dotnet run`, and the studio lists **NORTHWIND** with eight tables, their foreign keys, twelve
indexes and the `order_totals` view, and **PARTS** with the suppliers, the parts, the `stock_value`
view and the function beside them. Each clone is a resource in the dashboard: its log is the dump and
restore output, and a second start says the target was not empty and left it alone.
