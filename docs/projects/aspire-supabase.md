---
layout: default
title: Nextended.Aspire.Hosting.Supabase
parent: Projects
nav_order: 12
---

# Nextended.Aspire.Hosting.Supabase

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

## Deployment

The stack is designed to deploy to Azure Container Apps via `azd`:

```bash
azd init
azd up
```

All containers and their configuration are translated 1:1 from the Aspire model into Bicep/ACA resources.

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
