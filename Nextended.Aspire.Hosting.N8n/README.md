# Nextended.Aspire.Hosting.N8n provides n8n for .NET Aspire

A first-class [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) integration for
[n8n](https://n8n.io) — the fair-code workflow-automation platform. One `AddN8n("n8n")` call
gives you a fully wired n8n instance with a PostgreSQL backend, sensible self-hosting defaults,
optional queue mode (Redis + workers), and 1:1 deployment to Azure Container Apps via `azd up`.

## Table of Contents

- [Quick Start](#quick-start)
- [Database Backend](#database-backend)
- [Queue Mode (Redis + Workers)](#queue-mode-redis--workers)
- [Security](#security)
- [Public URLs & Timezone](#public-urls--timezone)
- [Importing Workflows & Credentials](#importing-workflows--credentials)
- [Accessing Resources](#accessing-resources)
- [Consuming the n8n API from a service](#consuming-the-n8n-api-from-a-service)
- [Deployment](#deployment)
- [Defaults](#defaults)

---

## Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var n8n = builder.AddN8n("n8n");

builder.Build().Run();
```

This starts:

| Service | Default port |
| --- | --- |
| n8n editor / REST API | 5678 |
| PostgreSQL backend | (internal, auto-created) |

All values use sensible self-hosting defaults and are ready for local development and
Azure Container Apps deployment.

---

## Database Backend

By default `AddN8n` creates a dedicated PostgreSQL container as the n8n backend.

### Use an existing Aspire PostgreSQL resource

```csharp
var pg = builder.AddPostgres("pg");
var db = pg.AddDatabase("n8ndb");

var n8n = builder.AddN8n("n8n")
    .WithDatabase(db);          // or: .WithDatabase(pg, "n8ndb")
```

The auto-created PostgreSQL container is removed automatically when you supply your own.

### Use the bundled SQLite database

```csharp
var n8n = builder.AddN8n("n8n")
    .WithSqlite();              // single container, data persisted in the n8n data dir
```

---

## Queue Mode (Redis + Workers)

For scalable, production-like execution, enable queue mode. A plain Redis container is added as the
broker and one or more worker containers process executions. Queue mode requires the PostgreSQL
backend.

```csharp
var n8n = builder.AddN8n("n8n")
    .WithQueueMode(workers: 3);  // Redis + 3 worker containers

// or add/scale workers explicitly:
var n8n2 = builder.AddN8n("n8n2")
    .WithWorkers(2);             // enables queue mode + 2 workers
```

> A deliberately plain (non-TLS) Redis is used: Aspire's `AddRedis` enables TLS with a self-signed
> certificate, which the n8n/ioredis client cannot consume out of the box.

The Redis password defaults to a stable development value. Override it — ideally via an Aspire
parameter, so the secret flows through user secrets locally and Key Vault on deployment:

```csharp
var redisPassword = builder.AddParameter("n8n-redis-password", secret: true);

var n8n = builder.AddN8n("n8n")
    .WithQueueMode(workers: 2, redisPassword: redisPassword);
    // order-independent alternatives: .WithRedisPassword(redisPassword) / .WithRedisPassword("plain")
```

---

## Security

Everything is overridable; secrets accept either a plain string (simple) or an Aspire parameter
(recommended — user secrets locally, Key Vault on deployment):

```csharp
var encryptionKey = builder.AddParameter("n8n-encryption-key", secret: true);

var n8n = builder.AddN8n("n8n")
    .WithEncryptionKey(encryptionKey);          // or .WithEncryptionKey("a-stable-32+-char-secret")
```

> The encryption key encrypts stored credentials. If it changes, existing credentials can no
> longer be decrypted. A stable development default is used when none is set — always set your own.

### A note on `WithBasicAuth`

`WithBasicAuth(user, password)` sets the legacy `N8N_BASIC_AUTH_*` variables and only takes effect
on n8n versions **< 1.0**. The modern default image uses the built-in **owner-account / user
management** model, which is configured interactively on first launch — those variables are ignored
there.

---

## Public URLs & Timezone

```csharp
var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    .WithWebhookUrl("https://n8n.example.com/")
    .WithEditorBaseUrl("https://n8n.example.com/");
```

In publish mode (`azd up`) the webhook and editor URLs default to the public n8n endpoint when
not set explicitly, and the instance is configured for running behind the Azure Container Apps
ingress (`https`, proxy hops, secure cookies).

---

## Seeding Workflows & Credentials

For local development and integration tests, seed n8n with workflows/credentials on startup.
A one-shot init container runs the n8n CLI import before the main instance starts (and the main
instance waits for it to finish).

Seed workflows from files, from raw JSON content, or from a whole directory:

```csharp
var n8n = builder.AddN8n("n8n")
    // individual workflow JSON files
    .WithWorkflows("workflows/order-sync.json", "workflows/cleanup.json")
    // raw JSON content (e.g. embedded resources / generated)
    .WithWorkflowContents(myWorkflowJsonString)
    // every *.json in a directory
    .WithWorkflowsFromDirectory(Path.Combine(builder.AppHostDirectory, "workflows"));
```

All variants are additive and collect into a single managed staging directory that is imported via
`n8n import:workflow --separate`. Each file/content must be a workflow export (a single workflow
JSON object, as produced by `n8n export:workflow --separate`).

Credentials work the same way from a directory:

```csharp
var n8n = builder.AddN8n("n8n")
    .WithImportCredentials(Path.Combine(builder.AppHostDirectory, "credentials"));
```

> Seeding uses local bind mounts and is skipped in publish mode. `WithImportWorkflows(dir)` remains
> available as an alias for `WithWorkflowsFromDirectory(dir)`.

---

## Accessing Resources

```csharp
var n8n = builder.AddN8n("n8n").WithQueueMode(2);

var database = n8n.GetDatabase();      // IResourceBuilder<PostgresDatabaseResource>?
var redis    = n8n.GetRedis();         // IResourceBuilder<RedisResource>?
var workers  = n8n.GetWorkers();       // IReadOnlyList<...>
var endpoint = n8n.GetHttpEndpoint();  // EndpointReference

// Wire a frontend / service to n8n:
builder.AddProject<Projects.MyApi>("api")
    .WithReference(n8n)                // ConnectionStrings:n8n = n8n URL
    .WaitFor(n8n);
```

---

## Consuming the n8n API from a service

In a referenced service project:

```csharp
builder.AddN8nClient("n8n", settings => settings.ApiKey = builder.Configuration["N8n:ApiKey"]);

// then inject:
public sealed class MyService(N8nApiClient n8n)
{
    public Task<HttpResponseMessage> ListWorkflows()
        => n8n.Http.GetAsync("/api/v1/workflows");
}
```

The base URL is resolved from `ConnectionStrings:n8n` (set automatically by `WithReference(n8n)`).
A health check probing `/healthz` is registered as well.

---

## Deployment

The whole topology deploys to Azure Container Apps via `azd`:

```bash
azd init
azd up
```

All containers and their configuration are translated 1:1 from the Aspire model into Bicep/ACA
resources. The n8n editor is exposed via an external HTTPS ingress; PostgreSQL, Redis and the
workers stay internal.

---

## Defaults

| Setting | Default |
| --- | --- |
| Image | `n8nio/n8n:1.110.1` |
| Editor / REST port | 5678 |
| Database | dedicated PostgreSQL container |
| Encryption key | insecure dev default (override with `WithEncryptionKey`) |
| Timezone | UTC |
| Queue mode | disabled |
| Diagnostics / telemetry | disabled |

Override the image with `WithImage("n8nio/n8n", "<tag>")` or `WithImageTag("<tag>")`.

## Supported frameworks

- .NET 8.0
- .NET 9.0
- .NET 10.0
