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

| Aufruf | Wirkung |
| --- | --- |
| `AddAspireUI(name = "aspireui", port?, image?, tag?)` | Fügt den AspireUI-Container hinzu. |
| `.WithAdminUser(username, password)` | Legt den Administrator beim ersten Start an (idempotent, Passwort wird gehasht gespeichert). Nimmt auch eine Aspire-`ParameterResource` als Passwort. |
| `.WithSeedStack(name, params projectPaths)` | Legt einen Starter-Stack mit je einem `AddProject`-Knoten pro Pfad an. |
| `.WithSourceMount(hostPath, containerPath?)` | Bindet Quellcode in den Container ein, damit ein angelegter Stack dort auch laufen kann. |

## Sicherheit

::: warning Docker-Socket
Der eingebundene Docker-Socket gibt dem Container Kontrolle über den Docker-Daemon des Hosts.
Betreiben Sie das nur auf einem vertrauenswürdigen Rechner — in der Praxis heißt das: lokale
Entwicklung, nicht ein geteilter oder öffentlich erreichbarer Host.
:::

Das Anlegen von Benutzer und Stack passiert **nur beim ersten Start**. Sobald AspireUI irgendeinen
Benutzer kennt, wird das Seeding übersprungen; ein vorhandenes Passwort wird also nicht
überschrieben.

Das Passwort sollte über eine Aspire-Parameter-Ressource kommen, nicht als Zeichenkette im Code:

```csharp
var password = builder.AddParameter("aspireui-password", secret: true);
builder.AddAspireUI().WithAdminUser("admin", password);
```

So landet es nicht im Manifest.

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
