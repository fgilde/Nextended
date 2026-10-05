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

## Deployment (Azure Container Apps)

Im Publish-Modus (`aspire publish`, `azd up`) kommt der Stack ohne alles aus, was eine Container App nicht kann:

- **Keine Bind-Mounts.** Generierte Konfigurationen, Provisioning und Dashboards werden in kleine Images gebacken (`{configRoot}/.generated/publish/<ressource>/`) — `FROM` dem Image der Komponente (`WithImage`/`WithImageTag` gelten weiter), ein `COPY` pro Datei.
- **Interne Komponenten sprechen TCP** auf ihren eigenen Ports (Prometheus 9090, Loki 3100, Tempo 3200, Collector 4318, postgres_exporter 9187), damit jede generierte `name:port`-Adresse gültig bleibt; Grafana bleibt HTTP. Deployt nimmt der Collector nur OTLP/HTTP an — 4317 gehört Tempo.
- Der Collector lässt den Spiegel zum lokalen Aspire-Dashboard weg (außer bei eigenem Endpoint) und schreibt eine Logzeile pro Batch statt jedes Spans.
- Promtail und cAdvisor entfallen (sie brauchen den Docker-Socket des Hosts).

Für einen deployten Stack gedacht, alles optional, Zugangsdaten immer als Umgebungsvariablen (`${VAR}` in den Dateien, kein Geheimnis im Image): `LokiStorage`/`TempoStorage` (`S3StorageOptions`, Bucket muss existieren), `GrafanaDatabase` (`GrafanaDatabaseOptions`, Grafanas Zustand in Postgres statt SQLite) und `GrafanaEntraId` (`GrafanaEntraIdOptions`). Fluent: `WithS3Storage(…)` an Loki bzw. Tempo, `grafana.WithDatabase(…)`, `grafana.WithEntraIdLogin(…)`. Codebeispiel auf der [englischen Seite](/projects/aspire-grafana#deploying-azure-container-apps).

**Anmeldung per Entra ID** schaltet Login-Formular, Basic Auth, anonymen Zugriff und Grafanas initialen `admin` ab — hinein kommt nur ein Konto des Tenants mit einer der App-Rollen `Viewer`, `Editor`, `Admin` oder `GrafanaAdmin` (Server-Admin); `AllowedGroups` schränkt weiter ein. Die App-Registrierung braucht die Redirect-URI `{grafana-url}/login/azuread`.

**Jeder andere OpenID-Connect-Anbieter** — Keycloak, Authentik, Auth0, Okta … — geht über `GrafanaOAuth` (`GrafanaOAuthOptions`, fluent `grafana.WithOAuthLogin(…)`), genauso abgesichert; Redirect-URI `{grafana-url}/login/generic_oauth`. Grafana macht dort keine Discovery, deshalb werden Auth-, Token- und Userinfo-URL angegeben — für Keycloak reicht der Realm: `GrafanaOAuthOptions.Keycloak("https://sso.example.com/realms/company", "grafana", secret)`. Erreichen Browser und Grafana Keycloak unterschiedlich — ein Keycloak-Container neben Grafana ist für den Browser `http://localhost:…`, für Grafana `http://keycloak:8080` —, kommt die zweite Adresse als `backchannelRealmUrl` dazu; Keycloak selbst braucht dann `KC_HOSTNAME` (die Browser-Adresse) und `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`, wie im Beispielprojekt. Lokal ist Grafanas Root-URL — wohin der Browser nach der Anmeldung zurückkehrt — die Adresse auf dem Host, nicht der Name im Container-Netz. Das Preset liest die Client-Rollen `grafana-admin`, `admin`, `editor` und `viewer` aus einem Claim `roles` (in Keycloak ein Mapper „User Client Role“ mit Token-Claim-Name `roles`, für ID-Token und Userinfo). `GrafanaOAuthOptions.Roles("groups", admins: ["ops"], viewers: ["staff"])` ordnet Namen aus einem beliebigen Claim zu; wer auf nichts passt, wird abgewiesen.

Mit `Nextended.Aspire.Hosting.Supabase` richtet `AddObservabilityStack(supabase, …)` Speicher und Datenbank aus dem Supabase-Stack selbst ein.

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

`dotnet run --launch-profile keycloak` startet es mit Anmeldung über ein Keycloak (Realm `nextended` aus `keycloak/nextended-realm.json`, Keycloak auf `http://localhost:8180`, Admin-Konsole `admin`/`admin`). Demo-Benutzer, Passwort = Benutzername: `ada` (Grafana-Server-Admin), `eve` (Editor), `vic` (Viewer) und `nora` ohne Rolle, die abgewiesen wird.

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
