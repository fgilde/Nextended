---
title: Nextended.Aspire.Hosting.Grafana
description: Grafana, Prometheus, Loki, Tempo, Promtail, cAdvisor, postgres_exporter und den OpenTelemetry Collector als kombinierbare Aspire-Ressourcen mit automatisch bereitgestellten Datenquellen.
---

# Nextended.Aspire.Hosting.Grafana

📚 **[Vollständige API-Referenz](/de/projects/aspire-grafana-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-grafana)

Der Grafana-Observability-Stack für .NET Aspire: Grafana, Prometheus, Loki, Tempo, Promtail,
cAdvisor, postgres_exporter und der OpenTelemetry Collector als kombinierbare
Container-Ressourcen. Datenquellen werden automatisch bereitgestellt, sämtliche YAML-Konfigurationen
beim Anwendungsstart aus den tatsächlichen Ressourcennamen erzeugt — keine fest verdrahteten
Konfigurationsdateien, und die Fluent-Aufrufe funktionieren in beliebiger Reihenfolge.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.Grafana.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Grafana/)

**[▶ Beispielprojekt ansehen](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/Grafana.AppHost)** — lauffähiger AppHost für diese Integration.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.Grafana
```

Ins **AppHost**-Projekt.

## Fluent API

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var pg = builder.AddPostgres("pg");

builder.AddGrafana("grafana")
    .WithAnonymousAdmin()                    // oder .WithAdminUser("admin", password)
    .WithPrometheus(configure: p => p
        .WithRetention("30d")
        .WithScrapeTarget("api", "my-api:8080")
        .WithDataVolume())
    .WithLoki(configure: l => l.WithPromtail())  // Promtail liefert alle Docker-Container-Logs
    .WithTempo()
    .WithOtelCollector()                     // OTLP-Empfänger, verteilt an Tempo/Loki/Prometheus
    .WithCAdvisor()                          // CPU-, Speicher- und Netzwerkmetriken pro Container
    .WithPostgresDatasource(pg)              // die Datenbank aus Grafana heraus durchsuchen
    .WithPostgresExporter(pg)                // Datenbank-Interna als Prometheus-Metriken
    .WithDashboards("./dashboards", "MyApp") // Dashboard-JSONs werden automatisch geladen
    .WithDataVolume();                       // Grafana-Zustand übersteht ein Neuanlegen des Containers

builder.Build().Run();
```

Jeder Komponentenaufruf legt zusätzlich die passende Grafana-Datenquelle an, verdrahtet die
Startreihenfolge über `WaitFor` und hängt den Container im Aspire-Dashboard unter Grafana ein.

## Eigene Datenquellen

Was die typisierten Methoden nicht abdecken, geht über den Notausgang:

```csharp
grafana.WithDatasource(new GrafanaDatasource
{
    Name = "MySQL",
    Type = "mysql",
    Url = "my-mysql:3306",
    User = "app",
});
```

## Der ganze Stack in einem Aufruf

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

[Nextended.Aspire.Hosting.Supabase](/de/projects/aspire-supabase) baut auf diesem Paket auf und
ergänzt eine Überladung, die die Postgres-Verbindung aus einem Supabase-Stack ableitet:
`builder.AddObservabilityStack(supabase, opts => …)`.

## Hinweise

**Docker-Socket.** Promtail und cAdvisor brauchen den Docker-Socket des Hosts. Im Publish-Modus
(`azd up`) werden sie deshalb automatisch übersprungen.

**Erzeugte Konfigurationen einsehen.** Die generierten Dateien landen unter
`{configRoot}/.generated/`. Sie können also nachsehen, was die Container tatsächlich geladen
haben — nützlich, wenn eine Datenquelle nicht so reagiert wie erwartet.

**Geheimnisse.** Datenquellen-Passwörter laufen über Container-Umgebungsvariablen; das erzeugte
YAML enthält nur `${VAR}`-Referenzen. Im Konfigurationsverzeichnis steht damit kein Klartext.

## Ausführbares Beispiel

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/Grafana.AppHost
dotnet run
```

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Abhängigkeiten

- `Aspire.Hosting.AppHost`
- `Aspire.Hosting.PostgreSQL`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Grafana/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.Grafana)
- 🧪 [Beispiel-AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/Grafana.AppHost)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
