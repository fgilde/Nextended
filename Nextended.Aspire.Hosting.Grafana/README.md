# Nextended.Aspire.Hosting.Grafana

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

## Notes

- Promtail and cAdvisor need the host's Docker socket — they are automatically skipped in publish mode (`azd up`).
- Generated configs land under `{configRoot}/.generated/` so you can inspect what the containers actually loaded.
- Secrets (datasource passwords) flow through container env vars; the generated YAML only contains `${VAR}` references.
