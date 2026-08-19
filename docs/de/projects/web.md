---
title: Nextended.Web
description: ASP.NET-Core-Werkzeuge — OData ohne EDM-Modell, kombinierbare OData-Applier, typisierte Controller-URLs, Streaming-Downloads und ein Background-Executor mit Request-Snapshot.
---

# Nextended.Web

📚 **[Vollständige API-Referenz](/de/projects/web-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/web)

ASP.NET-Core-Werkzeuge: OData ohne handgeschriebenes EDM-Modell, kombinierbare OData-Applier für
`IQueryable<T>`, typisierte Controller-URLs, Streaming-Downloads und ein Background-Executor, der
einen aufgezeichneten Request erneut abspielen kann.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Web.svg)](https://www.nuget.org/packages/Nextended.Web/)

## Installation

```bash
dotnet add package Nextended.Web
```

## Übersicht

| Bereich | API |
| --- | --- |
| **OData ohne Modellbauer** | `[ProvideAsEdm]`, `services.AddODataAuto()`, `mvcBuilder.AddODataAuto(model, routePrefix)` |
| **Kombinierbare Applier** | `ApplyOData`, `ApplyODataFilter`, `ApplyODataOrderBy`, `ApplyODataTop`, `ApplyODataSkip`, `ApplyODataSearch`, `ApplyODataExpandIncludes` |
| **Generischer Controller** | `GenericODataController<T>` — Abfrage, Schlüsselzugriff und Facettenaufbau bereits verdrahtet |
| **Facetten in der Antwort** | `FacetResourceSetSerializer` liefert Facetten-Metadaten neben dem OData-Ergebnis |
| **Typisierte URLs** | `RequestHelper.UrlFor<TController>(…)`, `Action<TController>(…)`, `ActionLink<TController>(…)`, `RedirectToAction<TController>(…)` |
| **Downloads** | `controller.DownloadDataAsync(...)` für Streams und Writer-Callbacks, inline oder als Anhang |
| **Abgekoppelte Arbeit** | `BackgroundExecutor`, `HttpRequestSnapshot`, `BackgroundExecutionScope` |
| **Uploads** | Erweiterungen für `IFormFile` und `IBrowserFile` |

## OData, das sein Modell selbst baut

Markieren Sie die Entitäten, die Sie veröffentlichen wollen, und sparen Sie sich den
`ODataConventionModelBuilder`:

```csharp
using Nextended.Core.Attributes;

[ProvideAsEdm("Products")]
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public Category? Category { get; set; }
}

// Eine Basisklasse kann die Registrierung an alle abgeleiteten Typen weitergeben:
[ProvideAsEdm(ProvideInherits = true)]
public abstract class EntityBase { public Guid Id { get; set; } }
```

```csharp
// Program.cs
builder.Services.AddODataAuto();                       // sucht nach [ProvideAsEdm]
builder.Services.AddControllers().AddODataAuto(
    ProvidedAsEdm.GetEdmModel(), routePrefix: "odata");
```

`ProvidedAsEdm` sammelt die markierten Typen, verdrahtet die Vererbung zwischen ihnen, erzeugt
Entity Sets für die markierten Typen **und** für die Navigationsziele, die sie erreichen, und legt
das Ergebnis im Cache ab. `EntitySetClrMap` gibt die Zuordnung von Set-Name zu CLR-Typ heraus.

Es gibt drei Überladungen: ohne Argument (Attribut-Scan), mit fertigem `IEdmModel` oder mit einer
Factory `Func<IServiceProvider, IEdmModel>`.

## Ein Controller, den Sie kaum schreiben

```csharp
using Nextended.Web.Controller;

public class ProductsController(AppDbContext db) : GenericODataController<Product>
{
    protected override IQueryable<Product> Queryable() => db.Products;
}
```

Damit haben Sie `GET /odata/Products` mit `$filter`, `$orderby`, `$top`, `$skip`, `$expand`,
`$search` und Facettenaufbau sowie `GET /odata/Products({key})`.

Zum Anpassen:

| Überschreiben | Zweck |
| --- | --- |
| `Queryable()` | Die Datenquelle — der einzige Pflichtteil |
| `IdPropertyName` | Wenn der Schlüssel nicht `Id` heißt |
| `GetFacetBuilderOptions()` | Facetten zuschneiden |
| `Get<TService>()` | Dienst aus dem Request-Scope holen |

## OData-Optionen auf beliebige Abfragen anwenden

Praktisch, wenn der Endpunkt keine OData-Route ist, Sie die Semantik aber trotzdem wollen:

```csharp
using Nextended.Web.Extensions;

[HttpGet]
public async Task<IActionResult> Search(ODataQueryOptions<Product> options)
{
    var query = db.Products
        .ApplyODataFilter(options.Filter)
        .ApplyODataSearch(options.Search)
        .ApplyODataOrderBy(options.OrderBy)
        .ApplyODataSkip(options.Skip)
        .ApplyODataTop(options.Top);

    return Ok(await query.ToListAsync());
}
```

`ApplyODataExpandIncludes` übersetzt `$expand` in `Include`-Ketten von EF Core — eine Expansion
wird damit nicht zu N+1 Abfragen. `ApplyOData` wendet alles in einem Aufruf an.

`Request.ODataQueryOptions<T>(edmModel)` erzeugt die Optionen auch dort, wo das Framework sie nicht
selbst bindet.

## Typisierte URLs

```csharp
using Nextended.Web.Helper;

var helper = new RequestHelper(urlHelper);

var url = helper.UrlFor<ProductsController>(c => c.Details(product.Id));
var name = helper.GetActionName<ProductsController>(c => c.Details);
var ctrl = helper.GetControllerName<ProductsController>();
```

Wird die Action umbenannt, wandert die URL mit — keine magischen Zeichenketten. In Views gibt es
`ActionLink<TController>(…)`, im Controller `RedirectToAction<TController>(…)`.

## Download streamen

```csharp
[HttpGet("export")]
public Task Export(CancellationToken ct)
    => this.DownloadDataAsync(
        writeResponseDataAction: stream => _exporter.WriteCsvAsync(stream, ct),
        mimeType: "text/csv",
        fileName: "products.csv",
        inlineFile: false,
        httpStatusCode: 200);
```

Nichts wird zwischengespeichert — der Callback schreibt direkt in den Response-Body. Es gibt
Überladungen für einen fertigen `Stream`, mit Cancellation-Token und mit zusätzlichen Headern.
`inlineFile: true` zeigt die Datei im Browser an statt sie zum Download anzubieten.

## Arbeit, die die Antwort überleben muss

```csharp
public async Task<IActionResult> Import(IFormFile file, [FromServices] BackgroundExecutor executor)
{
    // Zeichnet den aktuellen Request selbst auf, damit der abgekoppelte Scope
    // Header, Benutzer und Route noch sieht, nachdem die Antwort draußen ist.
    await executor.ExecuteDetachedWithCapturedRequestAsync(
        timeout: TimeSpan.FromMinutes(10),
        action: async (services, ct) =>
        {
            var importer = services.GetRequiredService<IImporter>();
            await importer.RunAsync(ct);
        });

    return Accepted();
}
```

Die Arbeit läuft in einem **neuen** DI-Scope. Dass der `DbContext` des Requests verworfen wird,
kann sie also nicht zerreißen. Zeitüberschreitungen, Ausnahmen und das Aufräumen von Disposables
werden protokolliert statt ins Leere zu laufen.

Wenn Sie den Snapshot selbst in der Hand haben wollen — etwa vor einem früheren `await`:

```csharp
var snapshot = await HttpContext.CaptureAsync(ct);
await executor.ExecuteDetachedAsync(snapshot, TimeSpan.FromMinutes(5), DoWorkAsync);
```

Weitere Überladungen nehmen eine Liste von `IDisposable` bzw. `IAsyncDisposable`, die nach der
Arbeit freigegeben wird, sowie `onSetup`- und `onTeardown`-Callbacks, die die Arbeit umklammern.

::: warning Kein Ersatz für eine Warteschlange
Der Executor läuft im Prozess. Ein Neustart bricht laufende Arbeit ab. Für Aufgaben, die garantiert
zu Ende laufen müssen, nehmen Sie eine echte Warteschlange — etwa über
[n8n](/de/projects/aspire-n8n) oder einen Hintergrunddienst mit persistenter Ablage.
:::

## Uploads

```csharp
byte[] bytes = await formFile.GetBytesAsync();
string size  = BrowserFileExtensions.GetReadableFileSize(formFile.Length);
```

Für Blazor-Uploads siehe [Nextended.Blazor](/de/projects/blazor).

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Plattform

Plattformübergreifend.

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- [Nextended.EF](/de/projects/ef)
- `Microsoft.AspNetCore.OData`

## Verwandt

Für berechtigungsabhängiges Zuschneiden der Antwort ist
[Nextended.ResponseFilters](/de/projects/responsefilters) zuständig, nicht dieses Paket.

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Web/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Web)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.Web/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
