---
title: Nextended.Aspire.Hosting.Supabase
---
# Nextended.Aspire.Hosting.Supabase

📚 **[Full API reference](/projects/aspire-supabase-api)** — every public type and member, generated from the compiled assembly.

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-supabase)

Complete Supabase stack integration for .NET Aspire — PostgreSQL, GoTrue auth, PostgREST, Storage, Kong gateway, Studio dashboard, and Edge Functions, all wired up with sensible defaults for local development and Azure Container Apps deployment.

## Overview

This package wraps the official Supabase open-source containers into an Aspire-friendly fluent API. With one `AddSupabase("supabase")` call you get a fully functional Supabase stack identical to what runs in production at supabase.com — but local, reproducible, and integrated into your Aspire AppHost.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.Supabase
```

## Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var supabase = builder.AddSupabase("supabase");

builder.Build().Run();
```

This starts a full stack with:

| Service | Default port |
| --- | --- |
| PostgreSQL database | 54322 |
| GoTrue (authentication) | (internal, exposed via Kong) |
| PostgREST API | (internal, exposed via Kong) |
| Storage API | (internal, exposed via Kong) |
| Kong API Gateway | 8000 |
| Studio Dashboard | 54323 |
| Postgres Meta | 8080 |
| Edge Runtime | 9000 |

## Configuration

Every sub-resource is configurable via its own fluent method:

```csharp
var supabase = builder.AddSupabase("supabase")
    .ConfigureDatabase(db => db
        .WithPassword("secure-password")
        .WithPort(54322))

    .ConfigureAuth(auth => auth
        .WithAutoConfirm(true)
        .WithJwtExpiration(3600)
        .WithSiteUrl("http://localhost:3000"))

    .ConfigureRest(rest => rest
        .WithSchemas("public", "storage", "graphql_public")
        .WithAnonRole("anon"))

    .ConfigureStorage(storage => storage
        .WithFileSizeLimit(52428800))   // 50 MB

    .ConfigureKong(kong => kong.WithPort(8000))
    .ConfigureMeta(meta => meta.WithPort(8080))
    .ConfigureStudio(studio => studio
        .WithPort(54323)
        .WithOrganizationName("My Org")
        .WithProjectName("My Project"))
    .ConfigureEdgeRuntime(edge => edge.WithPort(9000));
```

### Direct container access

Each `Configure*` method has an overload that exposes the underlying container builder:

```csharp
.ConfigureDatabase(
    db => db.WithPassword("password"),
    container => container
        .WithEnvironment("CUSTOM_VAR", "value")
        .WithVolume("my-volume", "/data"))
```

## Syncing from a remote Supabase project

Pull schema, data, storage, and more from an existing cloud project:

```csharp
const string projectRef = "your-project-ref";
const string serviceKey = "eyJhbGciOiJIUzI1NiIs...";   // service_role key

var supabase = builder.AddSupabase("supabase")
    .WithProjectSync(projectRef, serviceKey);
```

Fine-grained control via `SyncOptions`:

```csharp
.WithProjectSync(
    projectRef,
    serviceKey,
    SyncOptions.Schema | SyncOptions.Data | SyncOptions.StorageBuckets);
```

| Option | What gets synced |
| --- | --- |
| `Schema` | Table structures (columns, types, constraints) |
| `Data` | Table data |
| `Policies` | Row-Level Security policies (requires DB password) |
| `Functions` | Stored procedures and functions (requires DB password) |
| `Triggers` | Database triggers (requires DB password) |
| `Types` | Custom types and enums (requires DB password) |
| `Views` | Database views (requires DB password) |
| `Indexes` | Table indexes (requires DB password) |
| `StorageBuckets` | Bucket definitions |
| `StorageFiles` | Bucket files (downloads from remote) |
| `EdgeFunctions` | Edge Functions (requires Management API token) |
| `AllSchema` | Everything schema-related |
| `AllStorage` | `StorageBuckets` + `StorageFiles` |
| `All` | The whole project |

Full sync with all options and tokens:

```csharp
.WithProjectSync(
    projectRef,
    serviceKey,
    SyncOptions.All,
    dbPassword,
    managementApiToken);
```

### Where the keys come from

| Key | Location in Supabase Dashboard |
| --- | --- |
| Project Ref | URL: `https://supabase.com/dashboard/project/{project-ref}` |
| Service Role Key | Project Settings → API → `service_role` (secret) |
| Database Password | Project Settings → Database → Database password |
| Management API Token | Account (top right) → Access Tokens |

## Local migrations

Apply SQL migrations from a directory using the `YYYYMMDDHHMMSS_description.sql` naming convention:

```csharp
var migrationsPath = Path.Combine(builder.AppHostDirectory, "..", "supabase", "migrations");

var supabase = builder.AddSupabase("supabase")
    .WithMigrations(migrationsPath);
```

## Edge Functions

```csharp
var edgeFunctionsPath = Path.Combine(builder.AppHostDirectory, "..", "supabase", "functions");

var supabase = builder.AddSupabase("supabase")
    .WithEdgeFunctions(edgeFunctionsPath);
```

Directory layout:

```
supabase/
  functions/
    hello-world/
      index.ts
    another-function/
      index.ts
```

Each function is invokable through Kong:

```bash
curl -X POST http://localhost:8000/functions/v1/hello-world \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ANON_KEY" \
  -d '{"name": "World"}'
```

## Pre-registered users

For development and integration tests:

```csharp
var supabase = builder.AddSupabase("supabase")
    .WithRegisteredUser("admin@example.com", "password123", "Admin User")
    .WithRegisteredUser("test@example.com",  "test1234",    "Test User");
```

Users are created with confirmed email status, get a profile in `public.profiles` if that table exists, and get an admin role in `public.user_roles` if that table exists.

## Sign-in: OAuth provider, external providers, Studio login

**Supabase Auth as OAuth 2.1 provider** — other tools sign in with the app's accounts
("Sign in with MyApp"):

```csharp
var supabase = builder.AddSupabase("supabase")
    .WithOAuthServer()                 // consent page of the app: /oauth/consent (default)
    .WithCustomAccessTokenHook();      // public.custom_access_token_hook adds claims, e.g. "permissions"

// Any client: public, PKCE, a stable client id per stack and name, upserted on every start.
var tool = supabase.AddOAuthClient("My Tool", ReferenceExpression.Create($"https://tool.example.com/callback"));
// tool.ClientId → hand it to the tool
```

The app serves the consent page: it reads `authorization_id`, shows the request with
`supabase.auth.oauth.getAuthorizationDetails(id)` and answers with `approveAuthorization(id)` /
`denyAuthorization(id)`; after a first approval Auth forwards at once (`redirect_url`). Kong lets
`/auth/v1/oauth/*` through without an apikey. With the usual HS256 JWT secret Auth signs no ID
tokens — request no `openid` scope and read the claims from the access token (both integrations
below do). Locally the site URL is the frontend's address on the host, so the consent redirect
reaches the browser.

**Studio behind a sign-in instead of basic auth** — the login proxy in front of Studio does
OpenID Connect with PKCE and a signed session cookie (8 h); only a token whose claim holds the
value gets in:

```csharp
// The stack's own accounts (registers the client "Supabase Studio"):
supabase.WithSupabaseLogin(claim: "permissions", value: "studio.manage", publicUrl: "https://studio.example.com");

// Or any OpenID Connect provider, e.g. a Keycloak realm (client role "studio-admin" in a "roles" claim):
supabase.WithOAuthLogin(StudioOAuthOptions.Keycloak("https://sso.example.com/realms/company", "studio", secret));
```

The redirect URI is `{studio-url}/__oauth/callback`. Grafana signs in the same way with
`grafana.WithSupabaseLogin(supabase, GrafanaOAuthOptions.Roles("permissions", grafanaAdmins: ["grafana.manage"]))`
— in `AddObservabilityStack(supabase, o => o.ConfigureGrafana = g => …)`.

**External providers for the app** — "Sign in with Keycloak / Azure / Google …" on the app's
own login page (`supabase.auth.signInWithOAuth({ provider })`):

```csharp
supabase.WithKeycloak("https://sso.example.com/realms/company", "my-app", keycloakSecret);
supabase.WithExternalProvider("github", "client-id", githubSecret);
supabase.WithPublicUrl("https://api.example.com");  // custom domain of the API: links and redirects
```

Register `{api-url}/auth/v1/callback` at the provider.

## Dashboard commands

Add a "Clear All Data" button to the Aspire dashboard:

```csharp
var supabase = builder.AddSupabase("supabase")
    .WithClearCommand();
```

Truncates every table in the `public` schema on click — useful during integration test runs.

## Accessing resources

### Sub-resource builders

```csharp
var supabase = builder.AddSupabase("supabase");

var kong     = supabase.GetKong();
var studio   = supabase.GetStudio();
var database = supabase.GetDatabase();
var auth     = supabase.GetAuth();
var rest     = supabase.GetRest();
var storage  = supabase.GetStorage();
var meta     = supabase.GetMeta();
var edge     = supabase.GetEdgeRuntime();
```

### Keys and endpoints

```csharp
var anonKey        = supabase.Resource.AnonKey;
var serviceRoleKey = supabase.Resource.ServiceRoleKey;
var kongEndpoint   = supabase.Resource.Kong!.GetEndpoint("http");
```

## Frontend integration

Wire your frontend app to the local stack:

```csharp
var supabase = builder.AddSupabase("supabase");

builder.AddNpmApp("frontend", "../frontend")
    .WithEnvironment("VITE_SUPABASE_URL",       supabase.Resource.Kong!.GetEndpoint("http"))
    .WithEnvironment("VITE_SUPABASE_ANON_KEY",  supabase.Resource.AnonKey);
```

## Database backups

`WithDatabaseBackups(...)` adds a backup service (`<stack>-backup`): the stack's own `supabase/postgres`
image (so `pg_dump` matches the server) running an embedded Python script — no image of its own, it works
locally, on Azure Container Apps and in a compose environment alike.

```csharp
var supabase = builder.AddSupabase("supabase")
    .WithDatabaseBackups(o =>
    {
        o.RunAt = new TimeOnly(2, 0);      // daily, local time
        o.TimeZone = "Europe/Berlin";
        o.KeepDaily = 14;                  // newest 14 days + newest of each of 8 weeks
        o.KeepWeekly = 8;
        o.AddAzureBlobTarget("azure", builder.AddParameter("backup-sas-url", secret: true), prefix: "prod");
        // or: o.AddS3Target("s3", endpoint, bucket, accessKey, secretKeyParameter);
    });
```

- **Backup**: database (`pg_dump -Fc` of the app's schemas — `auth`, `storage`, `public`, your own; none of
  Supabase's internal ones) plus every stored file, streamed as parts into the stack's storage (bucket
  `db-backups`) and every external target. A target address that is no URL at runtime (empty, `none`) switches that target off.
- **Restore**: safety backup first, then the backup's *data* into the *current* schema in one transaction —
  all or nothing; migration bookkeeping and the backup bucket's own rows stay. Then the files.
- **After losing the deployment**: the service lists the backups it finds on its targets, so an external
  target brings yesterday's state back into a fresh database.
- **State and API** in the schema `nx_backup` (never backed up or restored): `overview()`, `request_backup(…)`,
  `request_restore(…)`, `request_delete(…)`, `save_settings(…)` — `SECURITY DEFINER`, granted to `service_role`
  and `postgres`; the app wraps them in its own functions that check its admin rule.

Details, options and a wrapper example: [package README → Database Backups](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Supabase/README.md#database-backups).

## Deployment

The stack is designed to deploy to Azure Container Apps via `azd`:

```bash
azd init
azd up
```

All containers and their configuration are translated 1:1 from the Aspire model into Bicep/ACA resources.

With `AddMinioS3OnNfs` and `AddSupabaseNfsStorage` the database and uploaded files survive redeploys. `AddObservabilityStack(supabase, …)` deploys too: Grafana keeps its state in a `grafana` database of the Supabase Postgres and reads through the read-only login `grafana_reader`, Loki and Tempo store into the MinIO buckets `loki` and `tempo`. A deployed Grafana needs a sign-in (its own admin login with `GrafanaAdminPasswordParameter`, or `GrafanaEntraIdOptions`). Secrets passed as Aspire parameters (JWT secret, keys, database, MinIO and admin passwords) stay references when deployed: every container gets them as container app secrets, never as plain text in the manifest or bicep. Details: [package README → Deployment](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Supabase/README.md#deployment).

## Complete example

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var migrationsPath    = Path.Combine(builder.AppHostDirectory, "..", "supabase", "migrations");
var edgeFunctionsPath = Path.Combine(builder.AppHostDirectory, "..", "supabase", "functions");

var supabase = builder.AddSupabase("supabase")
    .ConfigureDatabase(db => db.WithPassword("secure-pw"))
    .ConfigureAuth(auth => auth.WithAutoConfirm(true))
    .ConfigureStudio(s => s.WithProjectName("My Project"))
    .WithMigrations(migrationsPath)
    .WithEdgeFunctions(edgeFunctionsPath)
    .WithRegisteredUser("dev@example.com", "dev1234", "Developer")
    .WithClearCommand();

builder.AddNpmApp("frontend", "../frontend")
    .WithEnvironment("VITE_SUPABASE_URL",      supabase.Resource.Kong!.GetEndpoint("http"))
    .WithEnvironment("VITE_SUPABASE_ANON_KEY", supabase.Resource.AnonKey);

builder.Build().Run();
```

## Supported frameworks

- .NET 8.0
- .NET 9.0
- .NET 10.0

## Related projects

- [Nextended.Aspire](aspire.md) — General-purpose Aspire extensions (Docker checks, conditional waits, environment helpers)

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Supabase/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.Supabase)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
- [Supabase Documentation](https://supabase.com/docs)
