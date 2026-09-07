---
title: Nextended.Aspire.Hosting.AspireUI
description: AspireUI — der visuelle AppHost-Builder — als Ressource im eigenen Aspire-Stack, mit optional vorangelegtem Admin-Benutzer und Starter-Stack.
---

# Nextended.Aspire.Hosting.AspireUI

📚 **[Vollständige API-Referenz](/de/projects/aspire-aspireui-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-aspireui)

[AspireUI](https://github.com/fgilde/AspireUI) — der visuelle AppHost-Builder für .NET Aspire —
als Ressource im eigenen Aspire-Stack.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.AspireUI.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)

**[▶ Beispielprojekt ansehen](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/AspireUI.AppHost)** — lauffähiger AppHost für diese Integration.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.AspireUI
```

Ins **AppHost**-Projekt.

## Schnellstart

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAspireUI()
    .WithAdminUser("admin", builder.AddParameter("aspireui-password", secret: true))
    .WithSeedStack("Mein Stack", "../MyApi", "../MyWorker");

builder.Build().Run();
```

Der Container `ghcr.io/fgilde/aspireui` bekommt einen HTTP-Endpunkt und erscheint im
Aspire-Dashboard wie jede andere Ressource.

## API

### Der Container

| Aufruf | Wirkung |
| --- | --- |
| `AddAspireUI(name = "aspireui", port?, image?, tag?)` | Fügt den AspireUI-Container hinzu. |
| `.WithDataBindMount(hostPath)` | Legt AspireUIs Daten in einen Host-Ordner statt in ein benanntes Volume. |
| `.WithoutDockerSocket()` | Läuft ohne den Docker-Socket des Hosts: Bauen und Deploy auf ein entferntes Ziel gehen weiter, Hosting auf diesem Rechner nicht. |
| `.WithDockerHost(dockerHost)` | Richtet AspireUIs Docker-Client woanders hin (`DOCKER_HOST`), impliziert `WithoutDockerSocket()`. |
| `.WithSourceMount(hostPath, containerPath?)` | Bindet Quellcode in den Container ein, damit ein angelegter Stack dort auch laufen kann. |

### Benutzer und Tokens

| Aufruf | Wirkung |
| --- | --- |
| `.WithAdminUser(username, password)` | Legt den Administrator beim ersten Start an (idempotent, Passwort gehasht). Nimmt auch eine Aspire-`ParameterResource`. |
| `.WithUser(username, password, permissions?, viewModes?, mustChangePassword?)` | Legt ein Konto an. `permissions` ist ein Preset aus `AspireUIPermissions` oder eine kommagetrennte Liste von Ids. Passwort auch als `ParameterResource`. |
| `.WithUsers(params AspireUIUser[])` | Legt mehrere Konten auf einmal an, Administratoren eingeschlossen. |
| `.WithAppUser(username, password)` | Ein Konto, das Apps installiert, konfiguriert und Dateien ansieht — ohne Builder. |
| `.WithViewer(username, password)` | Ein Konto, das nur schauen darf. |
| `.WithApiToken(name, username, token)` | Ein Bearer-Token für Automatisierung, mit einem Wert, den Sie schon kennen. Auch als `ParameterResource`. |

### Deploy-Ziele

| Aufruf | Wirkung |
| --- | --- |
| `.WithSshTarget(name, host, user, port, keyFile?, key?, passphrase?, publicHost?, isDefault?)` | Der Docker-Daemon eines anderen Rechners über SSH. `keyFile` wird nur lesbar eingebunden und landet in keiner Umgebungsvariable. |
| `.WithDockerTcpTarget(name, host, port, caFile?, certFile?, keyFile?, publicHost?, isDefault?)` | Ein Docker-Daemon über TCP mit mTLS; die drei Zertifikate werden nur lesbar eingebunden. |
| `.WithKubernetesTarget(name, context?, kubeconfigFile?, namespace?, expose?, ingressHost?, storageClass?, isDefault?)` | Ein Kubernetes-Cluster, per Helm bespielt. |

### Apps und Stacks

| Aufruf | Wirkung |
| --- | --- |
| `.WithApps(params catalogIds)` | Installiert Apps aus AspireUIs eigenem Katalog per Id (`vaultwarden`, `gitea`, …). |
| `.WithApp(catalogId, name?, deploy?)` | Installiert eine Katalog-App unter einem eigenen Namen. |
| `.WithAppSource(name, url)` | Registriert eine App-Manifest-URL als Store-Quelle. |
| `.WithSeedStack(name, params projectPaths)` | Legt einen Stack mit je einem `AddProject`-Knoten pro Pfad an. |
| `.WithProjectStack(name, params projects)` | Dasselbe aus den `ProjectResource`s dieses AppHosts — deren Ordner werden eingebunden, damit der Stack auch laufen kann. |
| `.WithSeedFromDirectory(hostPath, name?, mode?, deploy?)` | Importiert einen Ordner als Stack: Manifest, Compose-Datei oder AppHost, je was drin liegt. Nur lesbar eingebunden, als Kopie importiert. |
| `.WithSeedFromCompose(hostPath, name?, deploy?)` | Importiert eine einzelne docker-compose-Datei als Stack. |
| `.WithSeedFromGit(url, branch?, subdir?, name?, mode?, deploy?)` | Klont ein Repository im Container und importiert es. |
| `.WithSeedFile(hostPath)` | Bindet ein Seed-Dokument (oder einen Ordner mit `aspireui.seed.json`) ein und zeigt AspireUI darauf. |
| `.WithAutoDeploy(deploy = true)` | Deployt die angelegten Stacks und Apps, sobald das Hosting läuft. |

### Einstellungen

| Aufruf | Wirkung |
| --- | --- |
| `.WithAi(baseUrl, model, apiKey?)` / `.WithAi(backend, model, …)` | Konfiguriert den eingebauten Assistenten: ein OpenAI-kompatibler Endpunkt oder eine Backend-Ressource im selben Stack. |
| `.WithPublicHost(host)` | Der Hostname, aus dem App-URLs gebaut werden. |
| `.WithNginxProxyManager(baseUrl, email, password, forwardHost?)` | Damit eine gehostete App aus ihrem eigenen Menü eine Domain und ein Zertifikat bekommen kann. Nimmt auch eine NPM-Ressource aus demselben Stack. |
| `.WithNotifications(webhookUrl?, telegramToken?, telegramChat?)` | Wohin gemeldet wird, dass eine App hochkam, wegging oder anfing zu scheitern. |
| `.WithBackupSchedule(intervalHours = 24, retain = 7)` | Sichert die Volumes jeder gehosteten App nach Plan. |
| `.WithHostedDashboards(browserToken?)` | Hostet neben jeder deployten App ein Aspire-Dashboard. |
| `.WithSetting(key, value)` | Jede AspireUI-Einstellung per Schlüssel. |
| `.WithForcedSettings(force = true)` | Wendet diese Einstellungen bei jedem Start an, statt nur zu füllen, was leer ist. |

Alles ist **idempotent über den Namen**: ein Neustart mit demselben AppHost ändert nichts, ein
hinzugefügter Eintrag fügt genau diesen hinzu, und was in der UI geändert wurde, bleibt geändert.
`WithAdminUser` ist die Ausnahme — wie AspireUIs eigenes Erst-Seeding wird es übersprungen, sobald
irgendein Konto existiert.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opsPassword = builder.AddParameter("ops-password", secret: true);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithUser("ops", opsPassword, AspireUIPermissions.Operator)
    .WithViewer("guest", "guest-password-1")
    .WithApiToken("pipeline", "ops", builder.AddParameter("ci-token", secret: true))
    .WithSshTarget("nas", "nas.local", "deploy", keyFile: "./keys/id_ed25519")
    .WithApps("vaultwarden", "gitea")
    .WithSeedFromDirectory("./seed/edge", "Edge")
    .WithAutoDeploy();

builder.Build().Run();
```

## Sicherheit

::: warning Docker-Socket
Der eingebundene Docker-Socket gibt dem Container Kontrolle über den Docker-Daemon des Hosts.
Betreiben Sie das nur auf einem vertrauenswürdigen Rechner — in der Praxis heißt das: lokale
Entwicklung, nicht ein geteilter oder öffentlich erreichbarer Host.
:::

Passwörter und Tokens gehören in Aspire-Parameter, Schlüsselmaterial in die Dateien, die die
Ziel-Methoden einbinden — dann landet beides nicht im Manifest.

```csharp
var password = builder.AddParameter("aspireui-password", secret: true);
builder.AddAspireUI().WithAdminUser("admin", password);
```

## Ausführbares Beispiel

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/AspireUI.AppHost
dotnet run
```

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Abhängigkeiten

- `Aspire.Hosting.AppHost`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.AspireUI)
- 🧪 [Beispiel-AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/AspireUI.AppHost)
- 🔗 [AspireUI-Projekt](https://github.com/fgilde/AspireUI)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
