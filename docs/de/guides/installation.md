---
title: Installation
---

<script setup>
import data from '@data/packages.json'

const cats = data.categories
const pkgs = data.packages
const base = '/Nextended/'
const byCat = (id) => pkgs.filter((p) => p.category === id)
</script>

# Installation

🇬🇧 [This page in English](/guides/installation.md)

Wie Sie die Nextended-Pakete einbinden, welche Zielframeworks unterstützt werden und worauf Sie bei
den plattformgebundenen Paketen achten müssen.


## Voraussetzungen

| | |
| --- | --- |
| **SDK** | .NET 8, 9 oder 10 |
| **Sprachversion** | `latest` (die Pakete nutzen aktuelle C#-Sprachfeatures) |
| **Nullable** | aktiviert; die Bibliotheken sind durchgängig nullable-annotiert |

`Nextended.Core` unterstützt zusätzlich `netstandard2.0` und `netstandard2.1`, ist also auch aus
älteren Zielframeworks nutzbar.

## Ein Paket hinzufügen

```bash
dotnet add package Nextended.Core
```

Oder in der `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Nextended.Core" Version="10.1.22" />
</ItemGroup>
```

Alle Pakete werden gemeinsam versioniert und veröffentlicht. Mischen Sie keine Hauptversionen
zwischen Nextended-Paketen innerhalb einer Anwendung.

## Welches Paket wofür

<div v-for="cat in cats" :key="cat.id">
  <h3>{{ cat.de }}</h3>
  <ul>
    <li v-for="p in byCat(cat.id)" :key="p.id">
      <code>dotnet add package {{ p.name }}</code><br>
      {{ p.summary.de }} — <a :href="base + 'de/projects/' + p.slug">Dokumentation</a>
    </li>
  </ul>
</div>

## Plattformhinweise

> **`Nextended.Imaging`** baut auf `System.Drawing.Common` auf. Microsoft unterstützt das ab .NET 7
> nur noch unter **Windows**; unter Linux und macOS werfen die `System.Drawing`-Typen
> `PlatformNotSupportedException`. Für plattformübergreifende Bildverarbeitung greifen Sie zu
> ImageSharp oder SkiaSharp.

> **`Nextended.UI`** hat Windows-Zielframeworks (`net8.0-windows` und höher) und referenziert WPF.
> Es lässt sich außerhalb von Windows nicht kompilieren.

Alle übrigen Pakete sind plattformübergreifend.

## Besonderheit: Nextended.CodeGen

`Nextended.CodeGen` ist ein Roslyn-Source-Generator und läuft nur zur Buildzeit. Es braucht zwei
Einträge in der `.csproj`:

```xml
<ItemGroup>
  <!-- die Attribute -->
  <PackageReference Include="Nextended.Core" Version="10.1.22"
                    PrivateAssets="all" GeneratePathProperty="true" />
  <!-- der Generator -->
  <PackageReference Include="Nextended.CodeGen" Version="10.1.22" />
</ItemGroup>

<ItemGroup>
  <!-- die Konfiguration; darf leer sein ({}), muss aber vorhanden sein -->
  <AdditionalFiles Include="CodeGen.config.json" />
</ItemGroup>
```

Ohne den `AdditionalFiles`-Eintrag erhält der Generator seine Konfiguration nicht und erzeugt nichts.
Details unter [Nextended.CodeGen](../projects/codegen.md).

## Registrierung im DI-Container

Die meisten Pakete sind reine Erweiterungsbibliotheken und brauchen keine Registrierung. Zwei Pakete
schon:

```csharp
// Response-Filter (ASP.NET Core)
builder.Services.AddNextendedResponseFilters(
    assemblies: [typeof(MeinFilter).Assembly]);

// OData ohne eigenes EDM-Modell
builder.Services.AddODataAuto();
builder.Services.AddControllers().AddODataAuto(ProvidedAsEdm.GetEdmModel());
```

## Prüfen, ob alles läuft

```csharp
using Nextended.Core.Extensions;

Console.WriteLine("hello world".ToPascalCase());   // HelloWorld
```

Kompiliert und läuft das, ist die Einbindung korrekt.

## Von nExt migrieren

Die Sammlung erschien früher als `nExt.*`. Die Namespaces heißen jetzt `Nextended.*`, die API ist
ansonsten quellkompatibel. Siehe den [Migrationsleitfaden](/guides/migration.md) *(englisch)*.
Das Altpaket [nExt.Core](https://www.nuget.org/packages/nExt.Core/) wird nicht mehr gepflegt.

## Pakete selbst bauen

`publish.ps1` im Repository-Wurzelverzeichnis baut alle Pakete mit **einer** Versionsnummer. Ohne
`-Push` ist es ein vollständiger Probelauf und legt die `.nupkg`-Dateien nur unter `.\artifacts` ab:

```bash
pwsh publish.ps1 -Version 10.1.23
```

Das Skript löst dabei ein Reihenfolgeproblem: `Nextended.CodeGen` referenziert `Nextended.Core` als
NuGet-*Paket* (festgelegt über `<UsedCorePackageVersion>`), alle anderen Projekte per
`ProjectReference`. Früher musste man Core erst veröffentlichen, auf die Indizierung durch nuget.org
warten und dann CodeGen nachziehen. Das Skript packt Core stattdessen lokal und stellt es CodeGen über
eine temporäre lokale Quelle bereit — eine Version, ein Durchlauf, kein Warten.

## Links

- [Alle Pakete](/projects/)
- [Typische Anwendungsfälle](../examples/common-use-cases.md)
- [GitHub Issues](https://github.com/fgilde/Nextended/issues)
