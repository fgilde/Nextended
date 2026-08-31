---
title: Nextended.Aspire.Hosting.DbTools
---
# Nextended.Aspire.Hosting.DbTools

📚 **[Vollständige API-Referenz](/de/projects/aspire-dbtools-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-dbtools)

Datenbank-Werkzeuge für einen Aspire-App-Host. Das erste: eine Datenbank-Ressource aus einer bestehenden Datenbank füllen.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.DbTools.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.DbTools
```

## Was das Paket macht

Ein Aufruf füllt eine Datenbank-Ressource aus einer bestehenden Datenbank — Schema und Daten:

```csharp
var dev = builder.AddPostgres("pg");

// Von einem Server, den dieser Stack nicht modelliert
dev.AddDatabase("shop")
   .WithCloneFrom("Host=staging.internal;Database=shop;Username=reader;Password=…");

// Oder von einer anderen Ressource darin
var prod = builder.AddPostgres("prod").AddDatabase("shop");
dev.AddDatabase("shop-copy").WithCloneFrom(prod);
```

Es kommt die ganze Datenbank an: Tabellen, Zeilen, Indizes, Constraints, Views, Routinen und die
Sequenzen mit dem Stand, auf dem sie stehen.

## Wann es sich lohnt

Aspire legt leere Datenbanken an, und `WithCreationScript` / `WithInitFiles` füllen sie aus SQL, das
man selbst geschrieben hat. Das reicht für die meisten Stacks. Dieses Paket ist für die Fälle, in
denen es nicht reicht:

* ein Entwicklungs-Stack, der die Strukturen und Größen einer echten Datenbank braucht, keine Fixture,
* eine neue Umgebung, aus einer bestehenden gebaut,
* eine Kopie von Staging, um eine Migration daran zu probieren, bevor sie echt läuft.

Wenn ein Seed-Skript genügt, nimm das Seed-Skript.

## Engines und Quellen

**Fünf Engines**, jede mit den Werkzeugen ihrer eigenen Engine: PostgreSQL (`pg_dump | psql`),
MySQL/MariaDB (`mysqldump | mysql`), SQL Server (BACPAC über `sqlpackage`), MongoDB
(`mongodump | mongorestore`) und Redis (über Replikation).

**Vier Wege zur Quelle**, für jede Engine: die typisierte Ressource, eine Verbindungszeichenfolge als
Text, ein `ParameterResource` — dort gehört eine mit Passwort hin — und `AddConnectionString`, wenn
der Stack die Zeichenfolge nur bekommen hat.

**Ein Klon ist eine Container-Ressource**, kein Code im App-Host. Deshalb läuft er auch beim
Veröffentlichen: Wer aus einem alten System ein neues baut, braucht genau das, denn der App-Host
selbst läuft dann nicht. Standardmäßig lässt ein Klon ein Ziel, in dem schon etwas steht, in Ruhe;
`Overwrite = true` ersetzt es, und das muss man sagen.

**Was ein Klon tut, während er läuft**, steht in seinem eigenen Log: jede Zeile der Dump- und
Restore-Werkzeuge, sobald sie kommt, plus ein Herzschlag, solange ein langer Schritt schweigt. Was
Aspire nicht kann: eine Datenbank-Ressource warten lassen — sie hat keinen eigenen Zustand. Wer die
Kopie erst benutzen will, wenn sie da ist, wartet deshalb auf den Klon-Container:
`.WaitForCompletion(builder.CloneOf("orders-copy"))`.

**`SchemaOnly` ist bei SQL Server ein anderes Werkzeugpaar** — DACPAC statt BACPAC, also Minuten statt
Stunden, und es lässt weg, womit ein Container nichts anfangen kann (Logins, Berechtigungen, External
Data Sources). Damit ist es die erste Frage an eine Datenbank, die noch niemand kopiert hat: kommt die
Struktur überhaupt an? Gelöscht wird dabei nichts.

Die **vollständige Referenz** — alle Optionen, die Eigenheiten jeder Engine und die Dinge, die man
vor dem Einsatz wissen sollte (SQL Server braucht beim ersten Klon Internet; ein Klon anonymisiert
nichts) — steht auf der englischen Seite:

[📖 Nextended.Aspire.Hosting.DbTools — vollständige Referenz (englisch)](/projects/aspire-dbtools)

Ebenso ausführlich und ebenfalls englisch ist das Paket-README:

[📄 Nextended.Aspire.Hosting.DbTools/README.md](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.DbTools/README.md)

## Ausführbares Beispiel

Im Repository liegt ein vollständiger, startbarer App-Host — er klont zweimal, einmal aus jeder Art
von Quelle, und öffnet beide Kopien in einem Browser-Studio:

**[DbTools.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/DbTools.AppHost)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/DbTools.AppHost
dotnet run
```

Danach hat das Studio zwei Verbindungen: **NORTHWIND** mit acht Tabellen, ihren Fremdschlüsseln,
zwölf Indizes und der View `order_totals`, geklont von einer Verbindungszeichenfolge; und **PARTS**
mit Lieferanten, Teilen, einer View und einer Funktion, geklont aus einer anderen Ressource desselben
Stacks. Keine dieser Tabellen ist im App-Host beschrieben — und jeder Klon ist im Dashboard eine
eigene Ressource, deren Log die Dump- und Restore-Ausgabe ist.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Links

- [Repository](https://github.com/fgilde/Nextended)
- [NuGet](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

## Lizenz

MIT
