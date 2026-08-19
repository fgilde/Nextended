---
title: Nextended.Aspire.Hosting.Php
description: PHP-Endpunkte im .NET-Aspire-Stack betreiben — ein docroot-Ordner oder ein einzelnes Router-Skript, mit php.ini-Einstellungen als Fluent-Optionen.
---

# Nextended.Aspire.Hosting.Php

📚 **[Vollständige API-Referenz](/de/projects/aspire-php-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-php)

PHP-Endpunkte im .NET-Aspire-Stack betreiben: ein Ordner oder eine einzelne `.php`-Datei, bedient
vom eingebauten Webserver von PHP im offiziellen `php:cli`-Container — und aus Ihren
.NET-Diensten aufrufbar wie jede andere referenzierte Ressource.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.Php.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Php/)

**[▶ Beispielprojekt ansehen](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/Php.AppHost)** — lauffähiger AppHost mit PHP, MySQL, phpMyAdmin, Mailpit und einem .NET-Webformular.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.Php
```

Ins **AppHost**-Projekt.

## Schnellstart

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Ordner-Modus: ./php ist das docroot, jede .php-Datei darin wird ein Endpunkt.
var php = builder.AddPhp("php", "./php")
    .WithPhpIni("memory_limit", "256M")
    .WithPhpIni("display_errors", "1");

builder.AddProject<Projects.Api>("api")
    .WithReference(php);   // Service Discovery: http://php löst innerhalb von "api" auf

builder.Build().Run();
```

Aufruf aus .NET:

```csharp
// mit Microsoft.Extensions.ServiceDiscovery (den üblichen Aspire-Service-Defaults):
var client = new HttpClient { BaseAddress = new Uri("http://php") };

var json = await client.GetStringAsync("/index.php?who=aspire");
await client.PostAsJsonAsync("/send-mail.php", new { to = "x@y.z", subject = "hallo" });
```

### Einzeldatei-Modus

Übergeben Sie statt eines Ordners eine `.php`-Datei, wird sie zum Router-Skript: Jede Anfrage,
unabhängig vom Pfad, landet in dieser einen Datei.

```csharp
builder.AddPhp("mailer", "./php/send-mail.php");
```

## API

| Methode | Wirkung |
| --- | --- |
| `AddPhp(name, path, port?, image?, tag?)` | Fügt den Container hinzu. `path` ist ein Ordner (docroot) oder eine `.php`-Datei (Router-Skript), relativ zum AppHost-Verzeichnis. |
| `WithPhpIni(key, value)` | Eine php.ini-Direktive, übergeben als `php -d key=value`. |
| `WithPhpIni(dictionary)` | Mehrere Direktiven auf einmal. |
| `WithPhpIniConfiguration(a => …)` | Typisierte Direktiven: `a.DisplayErrors = true`, `a.MemoryLimit = "256M"`, `a.DateTimezone = "Europe/Berlin"`, … Für Fehlendes leiten Sie von `PhpIniConfiguration` ab (mit `[PhpIniKey("…")]`). |
| `WithPhpIniFile(path)` | Bindet eine vollständige ini-Datei in PHPs `conf.d`-Verzeichnis ein und überschreibt damit die Basis-php.ini. |
| `WithPhpExtensions("mysqli", …)` | Installiert PHP-Erweiterungen beim Containerstart (`docker-php-ext-install`). |
| `WithComposer(image?, tag?)` | Führt vor dem Start `composer install` aus (offizielles `composer`-Image, gleicher Mount); `vendor/` landet auf dem Host. Nur im Ordner-Modus. |
| `WithWorkers(n)` | Anzahl paralleler Request-Worker des eingebauten Servers (`PHP_CLI_SERVER_WORKERS`, Standard 8). |

## Hinweise

**Nebenläufigkeit.** Der eingebaute Server läuft mit standardmäßig 8 Request-Workern; über
`WithWorkers(n)` justieren Sie das.

**Entwicklungsserver.** PHPs eingebauter Server ist ein Entwicklungsserver — für Aspires lokale
Orchestrierung genau richtig. Für Produktions-Deployments nehmen Sie ein richtiges PHP-Image
(fpm + nginx oder Apache).

**Erweiterungen.** Das Standard-Image `php:cli` bringt Dinge wie `mysqli` oder `pdo_mysql` nicht
mit. Entweder `WithPhpExtensions(...)` verwenden (wird beim Containerstart kompiliert, etwa 20–40
Sekunden) oder ein eigenes Image bauen und über `AddPhp(..., image: "my/php", tag: "dev")`
übergeben. Schwergewichte wie `gd` oder `intl` brauchen ohnehin ein eigenes Image.

**MySQL.** Kombinierbar mit `Aspire.Hosting.MySql`: `AddMySql("mysql").WithPhpMyAdmin()` liefert
Server und phpMyAdmin. Den Endpunkt geben Sie PHP über
`.WithEnvironment("MYSQL_URL", mysql.GetEndpoint("tcp"))` mit und ergänzen
`WithPhpExtensions("mysqli")`.

**`mail()` funktioniert nicht.** Der Container hat keinen MTA, PHPs `mail()` läuft still ins
Leere. Sprechen Sie stattdessen SMTP — etwa mit einem
[Mailpit](https://mailpit.axllent.org/)-Container im Stack, dessen Endpunkt Sie PHP über
`.WithEnvironment("SMTP_URL", mailpit.GetEndpoint("smtp"))` mitgeben. Das Beispielprojekt
`Php.AppHost` zeigt das vollständige Muster inklusive eines abhängigkeitsfreien SMTP-Senders in
PHP.

## Ausführbares Beispiel

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/Php.AppHost
dotnet run
```

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Abhängigkeiten

- `Aspire.Hosting.AppHost`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Php/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.Php)
- 🧪 [Beispiel-AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/Php.AppHost)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
