---
title: Nextended.ResponseFilters.AspNetCore
description: ASP.NET-Core-Adapter für Nextended.ResponseFilters — registriert die Pipeline als globalen IAsyncResultFilter und spielt strukturelle Änderungen auf den JSON-Baum zurück.
---

# Nextended.ResponseFilters.AspNetCore

📚 **[Vollständige API-Referenz](/de/projects/responsefilters-aspnetcore-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/responsefilters-aspnetcore)

Der ASP.NET-Core-Adapter für [Nextended.ResponseFilters](/de/projects/responsefilters). Er hängt
die Filter-Pipeline als globalen `IAsyncResultFilter` in MVC ein: Jeder `ObjectResult.Value`
durchläuft vor der Serialisierung die konfigurierten Filter.

[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.AspNetCore.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)

## Installation

```bash
dotnet add package Nextended.ResponseFilters.AspNetCore
```

Der providerunabhängige Kern kommt transitiv mit — ein eigener Verweis auf
`Nextended.ResponseFilters` ist nicht nötig.

## Schnellstart

```csharp
using Nextended.ResponseFilters;
using Nextended.ResponseFilters.AspNetCore;

builder.Services.AddNextendedResponseFilters(
    assemblies: [typeof(OrderResponseFilter).Assembly]);
```

Das ist alles. Der Aufruf

1. registriert Pipeline, Registry und Optionen,
2. durchsucht die angegebenen Assemblies nach `ResponseFilter<T>`-Implementierungen,
3. registriert `ResponseFilterResultFilter` und trägt ihn in `MvcOptions.Filters` ein.

Ohne Assembly-Angabe wird die aufrufende Assembly verwendet.

## Konfiguration

```csharp
builder.Services.AddNextendedResponseFilters(
    assemblies: [typeof(OrderResponseFilter).Assembly],
    lifetime: ServiceLifetime.Scoped,
    configure: opts =>
    {
        opts.ExceptionBehavior = FilterExceptionBehavior.LogAndContinue;
        opts.SkipResponseType = t => t.Namespace?.StartsWith("Volo.Abp") == true;

        // Schranke pro Request — nur /api/app/* durchläuft die Pipeline
        opts.ShouldHandle = (request, type) =>
            Task.FromResult(request.Path.StartsWithSegments("/api/app"));
    });
```

Die vollständige Optionsreferenz steht auf der
[Seite zum Kernpaket](/de/projects/responsefilters#konfiguration).

## Was der Adapter tut

```
Controller gibt ObjectResult(value) zurück
        │
        ▼
ResponseFilterResultFilter.OnResultExecutionAsync
        │
        ├─ kein ObjectResult oder Value ist null?  ── durchlassen
        ├─ ShouldHandle(request, valueType) false? ── durchlassen
        ▼
IResponseFilterPipeline.ProcessAsync(value, context)
        │   Wertregeln verändern das DTO direkt
        │   Strukturregeln landen im StructuralEditBook
        ▼
StructuralEdits.HasAny?
        │   ja ─▶ JsonStructuralTransformer.Transform(value, edits, jsonOptions)
        │         ObjectResult.Value = JsonNode
        │         ObjectResult.DeclaredType = null
        ▼
weiter an den MVC-Formatter
```

Zwei Details, die leicht übersehen werden:

**Die JSON-Optionen kommen aus Ihrer Anwendung.** Der Transformer holt sich
`IOptions<JsonOptions>` aus dem Request-Scope. Ihre `JsonNamingPolicy` und Ihre
`[JsonPropertyName]`-Attribute gelten also auch für umbenannte und hinzugefügte Schlüssel.

**`DeclaredType` wird zurückgesetzt.** Sobald der Wert ein `JsonNode` ist, muss die deklarierte
Typinformation weg — sonst versucht der Formatter, den Baum in die Form des ursprünglichen
DTO-Typs zu pressen. Der Adapter erledigt das.

## Nur wenn es etwas zu tun gibt

Die Pipeline hat mehrere Kurzschlüsse, bevor überhaupt ein Objektgraph durchlaufen wird:

1. Kein `ObjectResult` oder `Value` ist `null` → nichts passiert.
2. `ShouldHandle` liefert `false` → nichts passiert.
3. `SkipResponseType` trifft zu → nichts passiert.
4. Die Erreichbarkeitsanalyse findet im Typgraphen keinen registrierten Zieltyp → nichts passiert.

Erst danach beginnt die Tiefensuche. Eine Anwendung, in der nur zwei DTOs Filter haben, zahlt für
alle übrigen Endpunkte praktisch nichts.

## Ohne MVC

Wenn Sie Minimal APIs, gRPC oder einen eigenen Transport verwenden, nehmen Sie nur den Kern und
steuern die Pipeline selbst:

```csharp
using Nextended.ResponseFilters.Extensions;

services.AddResponseFilters([typeof(OrderResponseFilter).Assembly]);
```

```csharp
await pipeline.ProcessAsync(dto, context);

if (context.StructuralEdits.HasAny)
{
    var node = JsonStructuralTransformer.Transform(dto, context.StructuralEdits, jsonOptions);
    return Results.Json(node);
}

return Results.Ok(dto);
```

Den `IResponseFilterContext` implementieren Sie dabei selbst — er braucht nur `Services`,
`CancellationToken`, `Items` und `StructuralEdits`.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Abhängigkeiten

- [Nextended.ResponseFilters](/de/projects/responsefilters)

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)
- 📖 [Kernpaket und vollständige Regelreferenz](/de/projects/responsefilters)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters.AspNetCore)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
