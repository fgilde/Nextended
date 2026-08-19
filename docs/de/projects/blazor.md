---
title: Nextended.Blazor
description: Blazor-Helfer — IBrowserFile-Erweiterungen, ein hierarchisches Modell für hochgeladene Archive, MIME-Erkennung und Reflection über Komponentenparameter.
---

# Nextended.Blazor

📚 **[Vollständige API-Referenz](/de/projects/blazor-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/blazor)

Helfer für Blazor: Uploads als Bytes, Data-URLs oder Download, ein Ordnerbaum über hochgeladene
ZIP-, TAR- und RAR-Archive, MIME-Erkennung und Reflection über Komponentenparameter.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Blazor.svg)](https://www.nuget.org/packages/Nextended.Blazor/)

## Installation

```bash
dotnet add package Nextended.Blazor
```

## Übersicht

| Bereich | API |
| --- | --- |
| **`IBrowserFile`-Erweiterungen** | `GetBytesAsync`, `GetBytes`, `GetDataUrlAsync`, `GetContentType`, `DownloadFileAsync`, `GetReadableFileSize` |
| **Archiv-Erkennung** | `IsZipFile`, `IsRarFile`, `IsTarFile`, `IsArchive` |
| **Archiv-Navigation** | `ArchiveStructure` — ein `Hierarchical<T>`-Ordnerbaum: `CreateStructure`, `ToArchiveBytesAsync`, `IsDirectory`, `Size`, `ContainingFiles` |
| **Einträge als Dateien** | `IArchivedBrowserFile` / `ZipBrowserFile` implementieren `IBrowserFile` |
| **Data-URLs** | `DataUrl` — `data:`-URLs aus Bytes und Content-Type bauen und wieder zerlegen |
| **Komponenten-Reflection** | `ComponentRenderHelper.GetCompatibleParameters<T>`, `GetCompatibleProperties<T>`, `IsValidParameter`, `IsRenderFragment`, `IsEventCallback` |
| **MIME-Typen** | `MimeTypeHelper.GetContentType`, `Matches` (Muster wie `image/*`) |

## Einen Upload lesen

```razor
@using Nextended.Blazor.Extensions

<InputFile OnChange="OnChange" />

@code {
    private async Task OnChange(InputFileChangeEventArgs e)
    {
        var file = e.File;

        var bytes   = await file.GetBytesAsync();
        var dataUrl = await file.GetDataUrlAsync();   // direkt für <img src="...">
        var mime    = file.GetContentType();
        var size    = BrowserFileExtensions.GetReadableFileSize(file.Size);  // "1,4 MB"
    }
}
```

`GetReadableFileSize` nimmt optional einen `IStringLocalizer`, sodass die Einheiten übersetzt
werden, und mit `fullName: true` die ausgeschriebene Form.

## In einem hochgeladenen Archiv navigieren

```csharp
using Nextended.Blazor.Models;

if (file.IsArchive())
{
    // Die flache Eintragsliste liefert Ihr Archiv-Reader;
    // CreateStructure macht daraus einen Ordnerbaum.
    IList<IArchivedBrowserFile> entries = ReadEntries(file);

    ArchiveStructure root = ArchiveStructure.CreateStructure(entries, rootFolderName: file.Name);

    foreach (var node in root.Children)
    {
        node.IsDirectory;   // Ordner oder Eintrag
        node.Size;          // Ordner summieren ihren Inhalt
        node.BrowserFile;   // bei Einträgen ein IBrowserFile
    }

    // Nach dem Entfernen oder Ergänzen von Knoten wieder packen:
    byte[] repacked = await root.ToArchiveBytesAsync();
}
```

Weil ein Eintrag selbst ein `IBrowserFile` ist, funktioniert jede Komponente, die einen Upload
annimmt, ohne Anpassung auch mit einer Datei **aus** dem Archiv — Vorschau und Download laufen
über denselben Code.

`ArchiveStructureBase<T>` leitet von `Nextended.Core.Types.Hierarchical<T>` ab; Eltern-/Kind-
Navigation, Flatten und Pfadsuche kommen von dort. `ContainingFiles` liefert alle Dateien unterhalb
eines Knotens.

::: warning ZipStructure ist veraltet
`ZipStructure` trägt `[Obsolete]` und ist der Vorgänger von `ArchiveStructure`. Neuer Code nimmt
`ArchiveStructure`.
:::

## Download auslösen

```csharp
@inject IJSRuntime Js

await file.DownloadFileAsync(Js);
```

## Data-URLs

```csharp
using Nextended.Core.Types;

var url = new DataUrl(bytes, new ContentType("image/png"));
string asString = url.ToString();      // "data:image/png;base64,…"

var parsed = new DataUrl("data:image/png;base64,…");
byte[] raw = parsed.Bytes;
```

## Parameter aus einem Wörterbuch an eine Komponente geben

```csharp
using Nextended.Blazor.Helper;

// Nur die Einträge, die es auf TComponent wirklich als [Parameter] gibt, korrekt typisiert.
var parameters = ComponentRenderHelper.GetCompatibleParameters<MyComponent>(incoming);
```

Nützlich, wenn Parameter aus Konfiguration oder JSON kommen und Sie eine Laufzeitausnahme wegen
eines unbekannten Parameters vermeiden wollen. `IsValidParameter`, `IsRenderFragment` und
`IsEventCallback` beantworten Einzelfragen; `IsValidPropertyWithAttribute<TAttribute>` filtert
nach einem eigenen Attribut.

## MIME-Typen abgleichen

```csharp
using Nextended.Blazor.Helper;

MimeTypeHelper.GetContentType("bericht.pdf");        // "application/pdf"
MimeTypeHelper.Matches("image/png", "image/*");      // true
```

Praktisch für eine `accept`-Prüfung, die Platzhalter versteht.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Plattform

Plattformübergreifend (Blazor Server und WebAssembly).

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- `Microsoft.AspNetCore.Components.Web`
- `Microsoft.Extensions.Localization.Abstractions`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Blazor/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Blazor)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.Blazor/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
