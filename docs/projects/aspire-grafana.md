---
title: Nextended.Aspire.Hosting.Grafana
---
# Nextended.Aspire.Hosting.Grafana

📚 **[Full API reference](/projects/aspire-grafana-api)** — every public type and member, generated from the compiled assembly.

Grafana, Prometheus, Loki, Tempo, Promtail, cAdvisor, postgres_exporter and the OpenTelemetry Collector as composable resources with auto-provisioned datasources.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.Grafana.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Grafana/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-grafana)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.Grafana
```

## Runnable sample

A complete AppHost you can start is checked into the repository:

**[Grafana.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/Grafana.AppHost)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/Grafana.AppHost
dotnet run
```

Grafana observability stack for .NET Aspire — Grafana, Prometheus, Loki, Tempo, Promtail, cAdvisor, postgres_exporter and OpenTelemetry Collector as composable container resources. Datasources are auto-provisioned, all YAML configs are generated at application start from the actual resource names — no hardcoded config files, and the fluent calls work in any order.

## Fluent API

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("pg");

builder.AddGrafana("grafana")
    .WithAnonymousAdmin()                    // or .WithAdminUser("admin", password)
    .WithPrometheus(configure: p => p
        .WithRetention("30d")
        .WithScrapeTarget("api", "my-api:8080")
        .WithDataVolume())
    .WithLoki(configure: l => l.WithPromtail())  // Promtail ships all Docker container logs
    .WithTempo()
    .WithOtelCollector()                     // OTLP receiver, fans out to Tempo/Loki/Prometheus
    .WithCAdvisor()                          // per-container CPU/Memory/Network metrics
    .WithPostgresDatasource(pg)              // browse your DB from Grafana
    .WithPostgresExporter(pg)                // DB internals as Prometheus metrics
    .WithDashboards("./dashboards", "MyApp") // auto-loaded dashboard JSONs
    .WithDataVolume();                       // Grafana state survives container recreation

builder.Build().Run();
```

Every component call also provisions the matching Grafana datasource, wires start
ordering (`WaitFor`) and nests the container under Grafana in the Aspire dashboard.
Anything the typed methods don't cover goes through the escape hatch:

```csharp
grafana.WithDatasource(new GrafanaDatasource
{
    Name = "MySQL",
    Type = "mysql",
    Url = "my-mysql:3306",
    User = "app",
});
```

## One-call stack

```csharp
using Nextended.Aspire.Hosting.Observability;

builder.AddObservabilityStack(new ObservabilityStackOptions
{
    ConfigRootPath = Path.Combine(builder.AppHostDirectory, "observability"),
    IncludeTempo = true,
    IncludeOtelCollector = true,
    GrafanaDashboardsFolder = "MyApp",
});
```

`Nextended.Aspire.Hosting.Supabase` builds on this package and adds an overload
that derives the Postgres connection from a Supabase stack:
`builder.AddObservabilityStack(supabase, opts => …)`.

## Deploying (Azure Container Apps)

In publish mode (`aspire publish`, `azd up`) the stack deploys without anything a container app cannot do:

- **No bind mounts.** Generated configs, provisioning and dashboards are baked into small images under `{configRoot}/.generated/publish/<resource>/` — `FROM` the component's own image (`WithImage`/`WithImageTag` still apply), one `COPY` per file.
- **Internal components speak TCP** on their own ports (Prometheus 9090, Loki 3100, Tempo 3200, collector 4318, postgres_exporter 9187), so every generated `name:port` address keeps working; Grafana stays HTTP. Deployed, the collector receives OTLP/HTTP only — 4317 belongs to Tempo.
- The collector drops the mirror to the local Aspire dashboard (unless you set your own endpoint) and logs one line per batch instead of every span.
- Promtail and cAdvisor are left out (they need the host's Docker socket).

What a deployed stack should add — all opt-in, credentials always as env vars (`${VAR}` in the files, no secret in an image):

```csharp
builder.AddObservabilityStack(new ObservabilityStackOptions
{
    ConfigRootPath = Path.Combine(builder.AppHostDirectory, "observability"),
    LokiStorage = new S3StorageOptions
    {
        Endpoint = ReferenceExpression.Create($"{s3.Property(EndpointProperty.HostAndPort)}"), // host:port
        Bucket = "loki",                                                                      // must exist
        AccessKey = ReferenceExpression.Create($"{accessKey}"),
        SecretKey = ReferenceExpression.Create($"{secretKey}"),
    },
    TempoStorage = new S3StorageOptions { /* same, Bucket = "tempo" */ },
    GrafanaDatabase = new GrafanaDatabaseOptions
    {
        HostAndPort = ReferenceExpression.Create($"{db.Property(EndpointProperty.HostAndPort)}"),
        Password = ReferenceExpression.Create($"{grafanaDbPassword}"),
    },
    GrafanaEntraId = new GrafanaEntraIdOptions
    {
        TenantId = "<tenant id>",
        ClientId = "<app registration client id>",
        ClientSecret = ReferenceExpression.Create($"{builder.AddParameter("grafana-client-secret", secret: true)}"),
    },
});
```

Fluent equivalents: `l.WithS3Storage(…)` on Loki, `t.WithS3Storage(…)` on Tempo, `grafana.WithDatabase(…)`, `grafana.WithEntraIdLogin(…)`.

**Entra ID sign-in** turns off the login form, basic auth, anonymous access and Grafana's initial `admin` user — the only way in is an account of the tenant holding one of the app roles `Viewer`, `Editor`, `Admin` or `GrafanaAdmin` (server admin); `AllowedGroups` narrows it further. The app registration needs the redirect URI `{grafana-url}/login/azuread`; `GF_SERVER_ROOT_URL` comes from Grafana's own endpoint.

**Any other OpenID Connect provider** — Keycloak, Authentik, Auth0, Okta … — goes through `GrafanaOAuth` (`GrafanaOAuthOptions`, fluent `grafana.WithOAuthLogin(…)`), locked down the same way; its redirect URI is `{grafana-url}/login/generic_oauth`. Grafana does no discovery there, so the auth, token and userinfo URLs are named. For Keycloak a realm is enough:

```csharp
opts.GrafanaOAuth = GrafanaOAuthOptions.Keycloak(
    "https://sso.example.com/realms/company", "grafana",
    ReferenceExpression.Create($"{builder.AddParameter("grafana-client-secret", secret: true)}"));
```

It reads the client roles `grafana-admin`, `admin`, `editor` and `viewer` from a `roles` claim — in Keycloak a "User Client Role" mapper with token claim name `roles`, added to the ID token and userinfo. `GrafanaOAuthOptions.Roles("groups", admins: ["ops"], viewers: ["staff"])` maps names in any claim array instead; someone matching none is turned away.

With `Nextended.Aspire.Hosting.Supabase`, `AddObservabilityStack(supabase, …)` sets storage and database up from the Supabase stack itself.

## Notes

- Promtail and cAdvisor need the host's Docker socket — they are automatically skipped in publish mode (`azd up`).
- Generated configs land under `{configRoot}/.generated/` so you can inspect what the containers actually loaded.
- Secrets (datasource passwords) flow through container env vars; the generated YAML only contains `${VAR}` references.

## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Dependencies

- Aspire.Hosting.AppHost
- Aspire.Hosting.PostgreSQL

## Links

- 📦 [NuGet package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Grafana/)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.Grafana)
- 📄 [Package README](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.Grafana/README.md)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)