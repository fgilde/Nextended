---
title: Nextended.Aspire.Hosting.SigNoz
description: SigNoz — Traces, Metriken, Logs, Dashboards und Alerts auf ClickHouse — als Aspire-Ressourcen, mit bereitgestelltem Admin, Dashboards und Alert-Regeln für .NET-Dienste und OpenID-Connect-Anmeldung davor.
---

# Nextended.Aspire.Hosting.SigNoz

📚 **[Vollständige API-Referenz](/de/projects/aspire-signoz-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-signoz)

[SigNoz](https://signoz.io/) für .NET Aspire: Traces, Metriken, Logs, Dashboards und Alerts in einer Oberfläche, gespeichert in ClickHouse. Ein Aufruf fügt den ganzen Stack hinzu, legt seinen Admin an und erzeugt bei jedem Start Dashboards und Alert-Regeln für .NET-Dienste.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.SigNoz.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.SigNoz/)

**[▶ Beispielprojekt ansehen](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/SigNoz.AppHost)** — startet SigNoz und einen Beispieldienst (HTTP-Endpunkte, Postgres, Logs, Laufzeitmetriken), der seine Telemetrie dorthin schickt. Jedes Dashboard hat damit nach einer Minute Daten.

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/SigNoz.AppHost
dotnet run                               # Anmeldung bei SigNoz: admin@example.com / Sample-Passw0rd!
dotnet run --launch-profile anonymous    # ohne Anmeldung
dotnet run --launch-profile keycloak     # Anmeldung bei Keycloak über den Proxy: ada/ada kommt rein, nora/nora wird abgewiesen
```

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.SigNoz
```

Ins **AppHost**-Projekt.

## Schnellstart

```csharp
using Nextended.Aspire.Hosting.SigNoz;

var builder = DistributedApplication.CreateBuilder(args);

var signoz = builder.AddSigNoz("signoz")
    .WithAdminUser("admin@example.com", builder.AddParameter("signoz-admin-password", secret: true))
    .WithClickHouse(c => c.WithDataVolume())   // Telemetrie über Neustarts behalten
    .WithDataVolume();                         // Benutzer, Dashboards und Einstellungen behalten

builder.AddProject<Projects.Api>("api")
    .WithSigNozTelemetry(signoz);              // OTEL_EXPORTER_OTLP_ENDPOINT -> Collector von SigNoz

builder.Build().Run();
```

`AddSigNoz` fügt drei Container hinzu, im Aspire-Dashboard unter `signoz` eingeordnet:

| Ressource | Image | Aufgabe |
| --- | --- | --- |
| `signoz` | `signoz/signoz:v0.151.0` | Oberfläche und API (Port 8080). Legt den Admin an, danach die Dashboards und Alert-Regeln. |
| `signoz-collector` | `signoz/signoz-otel-collector:v0.144.18` | OTLP-Empfänger (gRPC 4317, HTTP 4318). Führt vor dem Start die Schema-Migrationen von ClickHouse aus. |
| `signoz-clickhouse` | `clickhouse/clickhouse-server:25.12.5` | Der Telemetriespeicher. ClickHouse Keeper läuft darin mit, es gibt also keinen ZooKeeper-Container. Die Funktion `histogramQuantile`, die SigNoz für Perzentile braucht, wird beim ersten Start heruntergeladen. |

Images und Versionen stammen aus dem Deployment-Werkzeug von SigNoz selbst ([Foundry](https://github.com/SigNoz/foundry)). `WithImageTag` auf einer Ressource ändert eine davon.

## Telemetrie senden

`WithSigNozTelemetry(signoz)` setzt `OTEL_EXPORTER_OTLP_ENDPOINT` und `OTEL_EXPORTER_OTLP_PROTOCOL` auf einem Projekt oder Container. Beim lokalen Lauf nutzt es gRPC. In einem Deployment nutzt es `http/protobuf`, weil der Collector dort OTLP nur über HTTP annimmt.

Die Standardnamen ersetzen das Aspire-Dashboard als Ziel. Wer beides will, übergibt eigene Namen und fügt einen zweiten Exporter dafür hinzu:

```csharp
api.WithSigNozTelemetry(signoz, "SIGNOZ_OTLP_ENDPOINT", "SIGNOZ_OTLP_PROTOCOL");
```

```csharp
// im Dienst (ServiceDefaults)
if (builder.Configuration["SIGNOZ_OTLP_ENDPOINT"] is { Length: > 0 } signoz)
{
    var http = builder.Configuration["SIGNOZ_OTLP_PROTOCOL"] == "http/protobuf";
    Action<OtlpExporterOptions> To(string signal) => o => (o.Endpoint, o.Protocol) = http
        ? (new Uri($"{signoz.TrimEnd('/')}/v1/{signal}"), OtlpExportProtocol.HttpProtobuf)
        : (new Uri(signoz), OtlpExportProtocol.Grpc);
    telemetry.WithLogging(l => l.AddOtlpExporter("signoz-logs", To("logs")))
        .WithMetrics(m => m.AddOtlpExporter("signoz-metrics", To("metrics")))
        .WithTracing(t => t.AddOtlpExporter("signoz-traces", To("traces")));
}
```

## Dashboards und Alert-Regeln

Bei jedem Start bekommt SigNoz diese Dashboards (Tag `stack:aspire`, jedes mit einer Variable `service`):

| Dashboard | Panels |
| --- | --- |
| **HTTP services** | Anfragen pro Sekunde, Fehlerquote, p95-Latenz, Fehler; Rate, Fehler und p95 je Dienst; p50/p99; Antworten nach Statuscode; p95 je Route (Histogramm); langsamste Endpunkte (p95, Aufrufe, Fehler); ausgehende HTTP-Aufrufe; aktive Anfragen |
| **.NET runtime** | CPU, Working Set; GC-Läufe je Generation, GC-Pausenzeit, Heap-Größe je Generation, Allokationsrate; Thread-Pool-Threads und -Warteschlange; Ausnahmen nach Typ; Lock-Konflikte; Kestrel-Verbindungen; JIT |
| **Database calls** | Aufrufe pro Sekunde, p95 und fehlgeschlagene Aufrufe (gesamt und je Dienst), p95 aus `db.client.operation.duration`, langsamste Statements, Verbindungspool |
| **Logs and traces** | Logs nach Level, Fehler und Warnungen je Dienst, die neuesten Fehler-Logs, die neuesten fehlgeschlagenen Spans |

Dazu bekommt es diese Alert-Regeln:

| Regel | Schlägt an, wenn |
| --- | --- |
| HTTP error rate above 5% | mehr als 5 % der eingehenden Anfragen eines Dienstes in 5 Minuten fehlschlagen |
| HTTP p95 latency above 1 s | die p95-Anfragedauer eines Dienstes über 5 Minuten über 1 s liegt |
| Database p95 latency above 500 ms | der p95-Datenbankaufruf eines Dienstes über 500 ms liegt |
| Error logs | ein Dienst in 5 Minuten mehr als 10 Einträge ab Level Error loggt |
| Thread pool queue growing | mehr als 50 Arbeitspakete auf einen Thread warten |

Die Regeln werden über die Benachrichtigungsrichtlinien von SigNoz geleitet statt über benannte Kanäle. Sie schlagen in der Oberfläche sofort an. Wer benachrichtigt werden will, legt unter *Alerts → Configuration* einen Kanal und eine Richtlinie an.

Die Panels lesen, was das OpenTelemetry-SDK für .NET mit seiner ASP.NET-Core-, HttpClient- und Runtime-Instrumentierung sendet: Server- und Client-Spans, `http.server.request.duration`, die `System.Runtime`-Metriken (`dotnet.*`) und die `db.client.*`-Metriken von Npgsql. Die Datenbank-Panels wählen Spans mit `db.system.name` aus, nach den aktuellen Semantic Conventions. Bibliotheken, die noch das ältere Attribut `db.system` senden, brauchen einen angepassten Filter.

Dashboards und Regeln werden angelegt, wenn sie fehlen, und sonst in Ruhe gelassen. Änderungen in der Oberfläche überstehen also Neustarts. Wer die mitgelieferte Fassung zurückhaben will, löscht sie in der Oberfläche. Der nächste Start legt sie dann neu an. Eigene kommen so dazu:

```csharp
signoz.WithDashboards("./signoz/dashboards")   // Dashboard-JSON von SigNoz, Schema v6 (Knopf "JSON" in der Oberfläche)
      .WithAlertRules("./signoz/alerts")       // Bodies von POST /api/v2/rules
      .WithoutBuiltInContent();                // nur die eigenen
```

Eine Datei mit dem Namen einer mitgelieferten ersetzt diese.

Die Bereitstellung ist ein Shell-Skript im SigNoz-Container (`/etc/signoz-provisioning/provision.sh`). Sie funktioniert in einem Deployment genauso, wo kein AppHost läuft.

## Anmeldung

| Aufruf | Wer reinkommt |
| --- | --- |
| *(Standard)* | `admin@signoz.local` mit erzeugtem Passwort (Parameter `signoz-password`) |
| `WithAdminUser(email, password, organization)` | Dieser Admin. SigNoz legt ihn bei jedem Start an, in der Oberfläche ist er nicht änderbar. SigNoz verlangt eine gültige E-Mail-Adresse und ein Passwort mit mindestens 12 Zeichen, Groß- und Kleinbuchstaben, einer Ziffer und einem Sonderzeichen. |
| `WithAnonymousAdmin()` | Jeder, als Admin (Impersonation von SigNoz; API-Keys und Login-Formular sind aus). Nur für die lokale Entwicklung. |
| `WithOidcLogin(options)` | Wen der OpenID-Connect-Anbieter reinlässt (siehe unten) |

`SigNozBuilderExtensions.IsValidAdminPassword(password)` prüft ein Passwort vorab gegen die Regel von SigNoz.

### OpenID Connect (Keycloak, Entra ID, der Auth-Server einer App …)

**Das eigene Single Sign-on von SigNoz gehört nicht zur freien Edition.** Anmeldung per OIDC und SAML braucht eine Enterprise-Lizenz; die Community-Edition kennt nur E-Mail/Passwort und Google. `WithOidcLogin` stellt deshalb einen [oauth2-proxy](https://oauth2-proxy.github.io/oauth2-proxy/) (`signoz-signin`) vor SigNoz. Der Proxy meldet den Benutzer beim Anbieter an, prüft Rollen oder Gruppen und leitet die Anfrage weiter. SigNoz dahinter handelt bei jeder Anfrage als sein Admin. SigNoz selbst ist dann nur noch über den Proxy erreichbar: kein externer Endpunkt, kein Link im Dashboard.

Das heißt:

- jeder, der durchkommt, ist Admin von SigNoz, also entscheidet der Anbieter, wer durchkommt;
- SigNoz sieht den einzelnen Benutzer nicht (Audit und "created by" zeigen den Admin);
- API-Keys sind aus. Automatisierung erreicht SigNoz stattdessen im Container-Netz.

```csharp
signoz.WithOidcLogin(SigNozOidcOptions.Keycloak(
    realmUrl: "https://sso.example.com/realms/company",
    clientId: "signoz",
    clientSecret: ReferenceExpression.Create($"{builder.AddParameter("signoz-client-secret", secret: true)}")));
```

Als Redirect-URI `{proxy-url}/oauth2/callback` registrieren. Das Keycloak-Preset lässt Benutzer mit der Client-Rolle `admin` des Clients ein (`signoz:admin`); `allowedRoles` ändert das. Erreicht der Proxy Keycloak unter einer anderen Adresse als der Browser (ein Keycloak-Container daneben), `backchannelRealmUrl: "http://keycloak:8080/realms/company"` übergeben; Keycloak braucht dann `KC_HOSTNAME` und `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`, wie im Beispiel.

Jeder andere Anbieter:

```csharp
signoz.WithOidcLogin(new SigNozOidcOptions
{
    IssuerUrl = "https://login.microsoftonline.com/<tenant>/v2.0",
    ClientId = "<client id>",
    ClientSecret = ReferenceExpression.Create($"{secret}"),
    AllowedGroups = ["<group object id>"],     // aus dem Claim groups
});
```

Mit `IssuerUrl` allein liest der Proxy das Discovery-Dokument des Anbieters. `LoginUrl`, `RedeemUrl` und `JwksUrl` zusammen gesetzt überspringen die Discovery.

## Deployment (Azure Container Apps, Docker Compose)

Im Publish-Modus kommt der Stack ohne Bind-Mounts aus:

- **Konfiguration im Image.** Die erzeugten Konfigurationen, Skripte, Dashboards und Regeln werden in kleine Images unter `{configRoot}/.generated/signoz/<name>/publish/<resource>/` gebacken. Jedes entsteht `FROM` dem Image der Komponente mit einem `COPY` je Ordner.
- **Eigene TCP-Ports.** Ein TCP-Port ist je Container-Apps-Umgebung eindeutig, und der Collector von Grafana belegt womöglich schon 4317/4318. ClickHouse ist deshalb als `signoz-clickhouse:29000` veröffentlicht, und der Collector nimmt OTLP über HTTP auf `signoz-collector:14318` an. `WithSigNozTelemetry` löst beides auf. Azure Container Apps reserviert einige Ports als Zielports (darunter 19000), deshalb sind es Ports, die es annimmt.
- **Kein OpAMP.** Der Collector läuft mit seiner statischen Konfiguration; die Log-Pipelines von SigNoz (per OpAMP verteilt) gibt es also nur lokal.

Für ein Deployment ClickHouse und SigNoz Volumes geben (`WithDataVolume`). ClickHouse mit mindestens 1 CPU und 2 GiB ausstatten. Ein ClickHouse-Volume braucht ein POSIX-Dateisystem (lokale Platte, NFS): Auf Azure Files über SMB (das Standard-Volume von Azure Container Apps) kann ClickHouse weder Dateizeiten setzen noch Hardlinks anlegen, jeder Insert scheitert mit `Cannot set modification time`. Dort das ClickHouse-Volume weglassen oder eine NFS-Freigabe einhängen.

## Hinweise

- Erzeugte Dateien liegen unter `{configRoot}/.generated/signoz/<name>/`; dort lässt sich nachsehen, was die Container geladen haben.
- Die Nutzungsstatistik von SigNoz ist aus (`SIGNOZ_STATSREPORTER_ENABLED=false`).
- Der Stack braucht im Betrieb etwa 1–2 GB Speicher, vor allem für ClickHouse. Nur dort hinzufügen, wo er gebraucht wird.
