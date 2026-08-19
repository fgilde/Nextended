---
title: Nextended.Aspire
description: Konditionale AppHost-Builder-Erweiterungen — WithReferenceIf, WaitForIf, typisierte Umgebungsvariablen, HTTPS-Dev-Cert, Docker-Guards und npm-App-Erkennung.
---

# Nextended.Aspire

📚 **[Vollständige API-Referenz](/de/projects/aspire-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire)

Konditionale Builder-Erweiterungen für den .NET-Aspire-AppHost, dazu typisierte
Umgebungsvariablen aus Konfigurationsobjekten, HTTPS-Dev-Cert-Anbindung, Docker-Guards,
Ressourcen aus GitHub-Repositories und npm-App-Erkennung.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.svg)](https://www.nuget.org/packages/Nextended.Aspire/)

## Installation

```bash
dotnet add package Nextended.Aspire
```

Ins **AppHost**-Projekt. Die übrigen `Nextended.Aspire.Hosting.*`-Pakete bauen darauf auf.

## Der Gedanke

Ein AppHost sammelt Bedingungen an. Eine Ressource existiert nur in der lokalen Entwicklung, eine
Referenz wird nur bei aktivem Feature-Flag verdrahtet, auf eine Warteschlange wird nur in der CI
gewartet. Geradeheraus geschrieben wird `Program.cs` daraus eine Treppe aus `if`-Blöcken, in der
in jedem Zweig dieselbe Builder-Kette noch einmal steht.

Jede Erweiterung hier ist die **konditionale Form** eines bestehenden Aspire-Aufrufs: Sie führt
den Schritt aus, wenn die Bedingung greift, und gibt den Builder sonst unverändert zurück. Die
Kette bleibt eine Kette.

## Übersicht

| Bereich | API |
| --- | --- |
| **Konditionale Referenzen** | `WithReferenceIf`, `WithReferencesIf` — Überladungen für Ressourcen mit Verbindungszeichenfolge, mit Service Discovery, für `EndpointReference` und für `(name, Uri?)` |
| **Konditionales Warten** | `WaitForIf`, `WaitForCompletionIf`, `WaitForIfResourceWithParent`, `WaitForCompletionIfResourceWithParent` |
| **Konditionaler Lebenszyklus** | `WithExplicitStartIf`, `WithActionIf` |
| **Typisierte Umgebung** | `WithEnvironments<T, TObject>(options[, prefix])`, `WithEnvironmentsIf`, `WithEnvironment(keyExpression, value)` |
| **Endpunkte** | `WithEndpointAsEnvironment`, `WithEndpointAsEnvironmentIf`, `WithEndpointsAsEnvironmentIf`, `WithEndpointList`, `GetFirstExistingEndpoint` |
| **Projektbenennung** | `AddWithAutoNaming<TProject>()` |
| **HTTPS** | `RunWithHttpsDevCertificate` |
| **Docker-Guards** | `IsDockerInstalled`, `IsDockerRunning`, `StartDocker`, `EnsureDockerIsRunning`, `EnsureDockerRunning`, `EnsureDockerRunningIf`, `EnsureDockerRunningIfLocalDebug` |
| **GitHub-Quellen** | `AddGithubRepository`, `WithGithubSource`, `EnsureGitCheckout` |
| **npm** | `AddAllNpmAppsInPath` |
| **Deployment-Domains** | `BuildDomainName`, `BuildDomainNames`, `BuildDeploymentDomain`, `BuildDeploymentDomains`, `BuildDeploymentDomainList` |

## Eine Kette statt einer Treppe

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var isLocal = builder.Environment.IsDevelopment();
var cache   = isLocal ? builder.AddRedis("cache") : null;

var api = builder.AddProject<Projects.Api>("api")
    // Abhängigkeit null? Der Aufruf ist wirkungslos — kein if, keine doppelte Kette.
    .WithReferenceIf(cache)
    .WithReferenceIf("legacy", legacyUri)          // übersprungen, wenn legacyUri null ist
    .WaitForIf(isLocal, database)
    .WithExplicitStartIf(!isLocal);                // in Produktion von Hand starten
```

Die `WithReferenceIf`-Überladungen nehmen einen **nullbaren** Builder. „Die Ressource existiert
vielleicht gar nicht" ist damit an der Aufrufstelle ausdrückbar, ohne Null-Prüfung.

## Konfigurationsobjekte statt Umgebungsvariablen-Zeichenketten

```csharp
record SmtpOptions(string Host, int Port, bool UseTls);

api.WithEnvironments(new SmtpOptions("localhost", 1025, false));
```

Das Objekt wird mit `__` als Trenner flachgeklopft und mit dem Typnamen präfigiert. Es entstehen
`SmtpOptions__Host`, `SmtpOptions__Port` und `SmtpOptions__UseTls` — genau die Form, die die
`IConfiguration`-Bindung erwartet. Der konsumierende Dienst liest sie mit
`Configuration.GetSection("SmtpOptions").Get<SmtpOptions>()` wieder ein. Verschachtelte Objekte
werden weiter aufgefaltet, ein ganzer Optionsbaum reist also in einem Aufruf.

Eigenes Präfix, wenn der Abschnitt anders heißt; `WithEnvironmentsIf` überspringt den ganzen Block,
wenn das Optionsobjekt null ist:

```csharp
api.WithEnvironments(smtpOptions, "Mail")     // Mail__Host, Mail__Port, …
   .WithEnvironmentsIf(featureOptions);       // wirkungslos, wenn featureOptions null ist
```

Für einen einzelnen Wert gibt es eine refaktorierungssichere Form, die den Variablennamen aus dem
Ausdruck ableitet:

```csharp
api.WithEnvironment<Projects.Api, SmtpOptions>(o => o.Host, "smtp.internal");
```

## Projektnamen, die man nicht wiederholt

```csharp
// Ressourcenname und Startprofil werden aus dem Projekttyp abgeleitet:
// PascalCase wird zu einem bindestrichgetrennten, Aspire-tauglichen Ressourcennamen.
var api = builder.AddWithAutoNaming<Projects.My_Api_Service>();
```

## Nicht an einem Container-Fehler scheitern, wenn Docker schlicht nicht läuft

```csharp
builder.Build()
    .EnsureDockerRunningIfLocalDebug()
    .Run();
```

`IsDockerInstalled()` und `IsDockerRunning()` stehen für eigene Verzweigungen bereit,
`StartDocker()` startet den Daemon unter Windows.

## HTTPS-Entwicklungszertifikat im Container

```csharp
var app = builder.AddContainer("frontend", "my/frontend")
    .RunWithHttpsDevCertificate();
```

Das ASP.NET-Core-Entwicklungszertifikat wird exportiert und eingebunden. Eine containerisierte
Ressource kann damit lokal HTTPS ausliefern, ohne Zertifikatsgefummel.

## Ressourcen direkt aus einem Git-Repository

```csharp
var tool = builder.AddGithubRepository("docs", "https://github.com/fgilde/Nextended");
```

`EnsureGitCheckout` klont oder aktualisiert die Arbeitskopie vorher, die Ressource hat ihre Quellen
also vor dem Build.

## Jede npm-Anwendung in einem Ordner

```csharp
// Eine NodeAppResource pro package.json unterhalb des Pfads.
var frontends = builder.AddAllNpmAppsInPath("../frontends");
```

## Die Hosting-Integrationen

Fertige Ressourcen auf Basis dieses Pakets, jeweils mit lauffähigem Beispiel-AppHost:

| Paket | Bringt mit |
| --- | --- |
| [Supabase](/de/projects/aspire-supabase) | Postgres, Auth, REST, Realtime, Storage, Studio, Kong, Edge Functions |
| [n8n](/de/projects/aspire-n8n) | Workflow-Automatisierung mit Postgres-Persistenz und typisiertem Trigger-Client |
| [Grafana](/de/projects/aspire-grafana) | Grafana, Prometheus, Loki, Tempo, Promtail, cAdvisor, OTel Collector |
| [WebDataStudio](/de/projects/aspire-webdatastudio) | Browser-Datenbankstudio, verdrahtet mit Ihren Datenbanken |
| [AspireUI](/de/projects/aspire-aspireui) | Der visuelle AppHost-Builder als Ressource |
| [LocalAI](/de/projects/aspire-localai) | Selbst gehostete multimodale KI: Bilder, Sprache, Video |
| [Php](/de/projects/aspire-php) | PHP-Endpunkte als vollwertige Aspire-Ressourcen |

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- `Aspire.Hosting.AppHost`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Aspire/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
