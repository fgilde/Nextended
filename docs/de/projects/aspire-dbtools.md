---
title: Nextended.Aspire.Hosting.DbTools
---
# Nextended.Aspire.Hosting.DbTools

📚 **[Vollständige API-Referenz](/de/projects/aspire-dbtools-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/aspire-dbtools)

Eine Datenbank in eine Aspire-Ressource klonen — ganz, Schema und Daten, aus einer anderen Ressource des Stacks oder von einem Server, den Aspire nicht kennt.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.DbTools.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.DbTools
```

## Was das Paket macht

Aspire kann eine Datenbank anlegen und ein Skript darin ausführen. Was es nicht kann: eine Datenbank
aus einer *bestehenden* füllen — und genau das will man zuerst. Ein Entwicklungs-Stack mit echten
Strukturen und echten Zeilen, eine neue Umgebung aus einer alten gebaut, eine Kopie von Staging, um
eine Migration daran zu probieren.

```csharp
var dev = builder.AddPostgres("pg");

// Von einem Server, den dieser Stack nicht modelliert
dev.AddDatabase("shop")
   .WithCloneFrom("Host=staging.internal;Database=shop;Username=reader;Password=…");

// Oder von einer anderen Datenbank im Stack
var prod = builder.AddPostgres("prod").AddDatabase("shop");
dev.AddDatabase("shop-copy").WithCloneFrom(prod);
```

Es kommt die ganze Datenbank an: Tabellen, Zeilen, Indizes, Constraints, Views, Routinen und die
Sequenzen mit dem Stand, auf dem sie stehen.

**Fünf Engines**, jede mit den Werkzeugen ihrer eigenen Engine: PostgreSQL (`pg_dump | psql`),
MySQL/MariaDB (`mysqldump | mysql`), SQL Server (BACPAC über `sqlpackage`), MongoDB
(`mongodump | mongorestore`) und Redis (über Replikation).

**Vier Wege zur Quelle**, für jede Engine: die typisierte Ressource, eine
Verbindungszeichenfolge als Text, ein `ParameterResource` — dort gehört eine mit Passwort hin — und
`AddConnectionString`, wenn der Stack die Zeichenfolge nur bekommen hat.

**Ein Klon ist eine Container-Ressource**, kein Code im AppHost. Deshalb läuft er auch beim
Veröffentlichen: Wer aus einem alten System ein neues baut, braucht genau das, denn der AppHost
selbst läuft dann nicht. Standardmäßig lässt ein Klon ein Ziel, in dem schon etwas steht, in Ruhe;
`Overwrite = true` ersetzt es, und das muss man sagen.

Die **vollständige Referenz** — alle Optionen, die Eigenheiten jeder Engine und die Dinge, die man
vor dem Einsatz wissen sollte (SQL Server braucht beim ersten Klon Internet; ein Klon anonymisiert
nichts) — steht auf der englischen Seite:

[📖 Nextended.Aspire.Hosting.DbTools — vollständige Referenz (englisch)](/projects/aspire-dbtools)

Ebenso ausführlich und ebenfalls englisch ist das Paket-README:

[📄 Nextended.Aspire.Hosting.DbTools/README.md](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.DbTools/README.md)

## Ausführbares Beispiel

Im Repository liegt ein vollständiger, startbarer AppHost — dort wird eine Northwind-Datenbank von
einer „fremden" Verbindungszeichenfolge geklont und anschließend im WebDataStudio geöffnet:

**[WebDataStudio.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/WebDataStudio.AppHost
dotnet run
```

Nach dem Start hat das Studio eine Verbindung **NORTHWIND** mit acht Tabellen, ihren Fremdschlüsseln,
zwölf Indizes und der View `order_totals` — von denen keine im AppHost beschrieben ist. Sie wurden
geklont, und die Ressource `northwind-clone` im Dashboard sagt das.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Links

- [Repository](https://github.com/fgilde/Nextended)
- [NuGet](https://www.nuget.org/packages/Nextended.Aspire.Hosting.DbTools/)

## Lizenz

MIT
