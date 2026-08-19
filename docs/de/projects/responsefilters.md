---
layout: default
title: Nextended.ResponseFilters
parent: Projekte
grand_parent: Deutsch
nav_order: 5
---

# Nextended.ResponseFilters
{: .no_toc }

🇬🇧 [This page in English](https://github.com/fgilde/Nextended/blob/main/docs/projects/responsefilters.md)

Eine providerunabhängige Fluent-Pipeline, die Response-DTOs vor der Serialisierung schwärzt,
maskiert, rundet, kürzt, hasht, ausdünnt und umstrukturiert — pro Request, pro Benutzer, pro
Berechtigung.
{: .fs-5 .fw-300 }

[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters/)
[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.AspNetCore.svg?label=AspNetCore)](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)

## Inhalt
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Das Problem

Ein DTO ist auf den Endpunkt zugeschnitten, nicht auf den Aufrufer. Dasselbe `OrderDto` soll der
Buchhaltung die Kostenaufstellung zeigen, dem Kunden nicht, die Kartennummer für alle außer
Administratoren maskieren und interne Positionen für anonyme Aufrufer ganz weglassen.

Die drei üblichen Antworten kosten jeweils etwas:

| Ansatz | Kosten |
| --- | --- |
| Ein DTO pro Zielgruppe | Kombinatorische Explosion; der Mapping-Code vervielfacht sich |
| Attribute wie `[JsonIgnore]` | Statisch — kann nicht vom aktuellen Benutzer abhängen und ist bei einem DTO aus einer Fremdbibliothek unmöglich |
| Manuelles Aufräumen im Controller | Verstreut, ungetestet und beim zweiten Endpunkt, der denselben Typ zurückgibt, vergessen |

`Nextended.ResponseFilters` verlagert die Entscheidung in genau eine deklarative Klasse pro DTO. Ein
`ResponseFilter<T>` liest sich wie ein `FluentValidator<T>` — nur dass er nicht validiert, sondern
den Objektgraphen auf dem Weg nach draußen **verändert**.

## Architektur

```
Controller gibt ObjectResult(value) zurück
        │
        ▼
ResponseFilterResultFilter                (Nextended.ResponseFilters.AspNetCore)
        │   ShouldHandle(request, type)?   ── nein ─▶ unverändert durchlassen
        ▼
IResponseFilterPipeline.ProcessAsync
        │   SkipResponseType(type)?        ── ja ───▶ zurück
        │   Erreichbarkeits-Cache: kann
        │   überhaupt ein Filter greifen?  ── nein ─▶ zurück
        ▼
Tiefensuche über den Objektgraphen (zyklensicher, ReferenceEqualityComparer)
        │
        ├─ pro Knoten: IResponseFilterRegistry löst die Filter für den Laufzeittyp auf
        ├─ Wertregeln verändern die Instanz direkt        (Nullify, Mask, Round, …)
        └─ Strukturregeln schreiben ins StructuralEditBook (Remove, Rename, AddProperty, …)
        │
        ▼
StructuralEdits.HasAny?
        │   ja ─▶ JsonStructuralTransformer.Transform(value, edits, jsonOptions)
        │         ObjectResult.Value = JsonNode, DeclaredType = null
        ▼
MVC-Formatter serialisiert
```

Es gibt zwei Arten von Regeln, weil sie technisch nicht gleich funktionieren können. Eine
**Wertänderung** (`Nullify`, `Mask`, `Round`) lässt sich direkt auf dem POCO ausführen. Eine
**Strukturänderung** (`Remove`, `Rename`, `AddProperty`) nicht — ein CLR-Objekt kann zur Laufzeit
keine Eigenschaft entfernen oder umbenennen. Solche Änderungen werden pro Instanz in einem
`StructuralEditBook` protokolliert und auf dem serialisierten JSON-Baum nachgespielt.

### Kerntypen

| Typ | Aufgabe |
| --- | --- |
| `ResponseFilter<T>` | Abstrakte Basisklasse. Ableiten und die Regeln im Konstruktor konfigurieren. |
| `InlineFilter<T>` | Konkreter Filter für `ForEach`-Unterfilter und für zur Laufzeit gebaute Filter (Tests). |
| `IResponseFilterContext` | Kontext pro Durchlauf: `Services`, `CancellationToken`, `Items` als Notizzettel, `StructuralEdits`. |
| `IResponseFilterPipeline` | Läuft den Graphen in Tiefensuche ab und ruft die passenden Filter auf. |
| `IResponseFilterRegistry` | Löst die für einen Typ registrierten Filter aus dem DI-Container auf. |
| `ResponseFilterOptions` | Konfiguration der gesamten Pipeline. |
| `StructuralEdit` / `StructuralEditBook` | Das Protokoll der Änderungen auf Schlüsselebene, nach Instanzidentität geführt. |
| `JsonStructuralTransformer` | Spielt dieses Protokoll auf einem `JsonNode`-Baum nach. |
| `SyncPredicate<T>` / `AsyncPredicate<T>` | `Func<T, ctx, bool>` bzw. `Func<T, ctx, ValueTask<bool>>`. |

## Installation

```bash
# providerunabhängiger Kern
dotnet add package Nextended.ResponseFilters

# ASP.NET-Core-Adapter (zieht den Kern transitiv mit)
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Registrierung

```csharp
using Nextended.ResponseFilters;
using Nextended.ResponseFilters.AspNetCore;

builder.Services.AddNextendedResponseFilters(
    assemblies: [typeof(OrderResponseFilter).Assembly],
    lifetime: ServiceLifetime.Scoped,
    configure: options =>
    {
        options.ExceptionBehavior = FilterExceptionBehavior.Rethrow;
        options.SkipUnaffectedResponses = true;
        options.SkipResponseType = t => typeof(Stream).IsAssignableFrom(t);
        options.ShouldHandle = (request, type) =>
            Task.FromResult(request.Path.StartsWithSegments("/api/app"));
    });
```

Die Filter werden gefunden, indem die angegebenen Assemblies nach `ResponseFilter<T>`-Implementierungen
durchsucht werden; ohne Angabe wird die aufrufende Assembly verwendet. Weil sie standardmäßig als
**Scoped** registriert werden, darf ein Filter Scoped-Abhängigkeiten im Konstruktor annehmen.

Ohne ASP.NET Core registrieren Sie nur den Kern und steuern die Pipeline selbst:

```csharp
using Nextended.ResponseFilters.Extensions;

services.AddResponseFilters([typeof(OrderResponseFilter).Assembly]);

// später
await pipeline.ProcessAsync(dto, context);
```

`AddResponseFilter<TFilter>()` registriert einen einzelnen Filter — nützlich in Tests und für zur
Laufzeit erzeugte Filter.

## Ein vollständiger Filter

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nextended.ResponseFilters;

public class OrderResponseFilter : ResponseFilter<OrderDto>
{
    public OrderResponseFilter()
    {
        // ---- Wertänderungen -------------------------------------------------
        Nullify(x => x.TotalCost, x => x.UnitCost).When(NotInRole("Finance"));

        Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).When(NotInRole("Admin"));
        Mask(x => x.CustomerEmail).WithPattern("***@***.***")
            .When(async ctx => !await ctx.IsAuthenticatedAsync());

        Truncate(x => x.Notes).After(200, "…").Always();
        Hash(x => x.AuditToken).AsSha256().Always();
        Round(x => x.Price).To(2).Always();
        Clear(x => x.DebugTrace).When(NotInRole("Internal"));
        SetToDefault(x => x.InternalScore, x => x.IsBookmarked).When(NotInRole("Internal"));
        SetValue(x => x.Status).To("redacted").When(NotInRole("Support"));

        // ---- Sammlungen ------------------------------------------------------
        RemoveItems<LineDto>(x => x.Lines).Where(l => l.Hidden).Always();
        KeepOnly<LineDto>(x => x.Attachments).Where(a => a.IsPublic).When(NotInRole("Internal"));
        Take<LineDto>(x => x.Lines).First(10).When(NotInRole("Premium"));

        ForEach(x => x.Lines, line =>
        {
            line.Nullify(l => l.UnitCost).When(NotInRole("Finance"));
            line.Truncate(l => l.Description).After(80).Always();
        });

        // ---- Struktur (Schlüsselebene) ---------------------------------------
        Remove(x => x.InternalRef, x => x.DebugInfo).When(NotInRole("Internal"));
        Rename(x => x.Id).To("orderId").Always();
        AddProperty("displayName").From(o => $"#{o.Number} — {o.CustomerName}").Always();

        // ---- über Metadaten --------------------------------------------------
        PropertiesWhere(p => p.GetCustomAttribute<SecretAttribute>() is not null)
            .Remove().When(NotInRole("Admin"));

        // ---- Notausgang ------------------------------------------------------
        Apply((order, _) =>
        {
            if (order.Status == "Cancelled") order.PaymentDetails = null;
        }).Always();
    }

    private static SyncPredicate<OrderDto> NotInRole(string role) =>
        (_, ctx) => !ctx.Services.GetRequiredService<ICurrentUser>().IsInRole(role);
}
```

## Regelreferenz

Jeder Builder wird durch eine Methode auf `ResponseFilter<T>` eröffnet, optional verfeinert und durch
ein Terminal aus dem [Prädikatswortschatz](#prädikatswortschatz) abgeschlossen. Eine Regel ohne
abschließendes Terminal wird **nie registriert** — das ist der häufigste Fehler.

### Eigenschaften verändern

| Builder | Wirkung | Beispiel |
| --- | --- | --- |
| `Nullify(...)` | Setzt eine oder mehrere nullbare Eigenschaften auf `null`. Nimmt mehrere Selektoren in einem Aufruf. | `Nullify(x => x.Cost, x => x.Notes).When(...)` |
| `SetValue(...).To(...)` | Setzt eine Eigenschaft auf eine Konstante oder einen aus Instanz und/oder Kontext berechneten Wert. | `SetValue(x => x.Status).To("hidden").When(...)` |
| `Replace(...).With(...)` | Wie `SetValue`, sprachlich für den Fall, dass schon ein Wert vorhanden ist. | `Replace(x => x.Email).With("***").When(...)` |
| `SetToDefault(...)` | Setzt Eigenschaften auf `default(TProperty)` zurück — nullbare, nicht nullbare Werttypen und Referenztypen gemischt in einem Aufruf. | `SetToDefault(x => x.Cost, x => x.IsActive).When(...)` |
| `Transform(...).Using(...)` | Bildet den aktuellen Wert über eine Funktion ab. Überladungen geben Zugriff auf Instanz und Kontext. | `Transform(x => x.Notes).Using(s => s?.ToUpper()).Always()` |
| `Clear(...)` | Leert eine Eigenschaft: `string` → `""`, veränderbare `IList` → `Clear()` an der Stelle, Array → leeres Array, alles andere → `null`. | `Clear(x => x.Lines).When(...)` |

### Zeichenketten

| Builder | Wirkung | Beispiel |
| --- | --- | --- |
| `Mask(...)` | Maskiert eine Zeichenkette. Verfeinerung über `KeepFirst(n)`, `KeepLast(n)`, `With(char)`, `WithPattern(string)`. Die Keep-Anzahlen werden auf die Länge begrenzt. | `Mask(x => x.Card).KeepFirst(4).KeepLast(4).When(...)` |
| `Truncate(...).After(n[, suffix])` | Schneidet nach `n` Zeichen ab und hängt das Suffix nur an, wenn wirklich geschnitten wurde. | `Truncate(x => x.Notes).After(200, "…").Always()` |
| `Hash(...)` | Ersetzt die Zeichenkette durch einen Hash. `AsSha256()` (Standard), `AsSha1()`, `AsSha512()`, `AsMd5()` oder `Using(fn)`. | `Hash(x => x.Token).AsSha512().When(...)` |

`WithPattern` ignoriert die `Keep*`-Einstellungen und ersetzt den gesamten Wert — richtig, wenn schon
die Form des Werts schützenswert ist (`***@***.***` statt `j***@e***.com`).

### Zahlen

| Builder | Wirkung | Beispiel |
| --- | --- | --- |
| `Round(...).To(n)` | Rundet auf `n` Dezimalstellen mit kaufmännisch-symmetrischer Rundung (`MidpointRounding.ToEven`). | `Round(x => x.Price).To(2).Always()` |
| `Round(...).To(n, mode)` | Rundet mit ausdrücklich gewählter Rundungsregel. | `Round(x => x.Price).To(2, MidpointRounding.AwayFromZero)` |
| `Round(...).ToInteger()` | Rundet auf ganze Zahlen. | `Round(x => x.Score).ToInteger().When(...)` |

Der Selektor ist auf `INumber<TSelf>` eingeschränkt — `Round` auf ein `string` zu richten ist damit
ein Kompilierfehler. Zur Laufzeit wird für `decimal`, `double` und `float` gerundet; ganzzahlige Typen
bleiben unverändert.

### Sammlungen

| Builder | Wirkung | Beispiel |
| --- | --- | --- |
| `ForEach(...)` | Steigt in eine Sammlungseigenschaft ab und wendet einen Inline-Unterfilter auf jedes Element an. | `ForEach(x => x.Lines, l => l.Nullify(i => i.Cost).When(...))` |
| `RemoveItems(...).Where(pred)` | Entfernt passende Elemente. Verändert `IList<T>` an der Stelle, Arrays werden neu aufgebaut. | `RemoveItems<Line>(x => x.Lines).Where(l => l.Hidden).Always()` |
| `KeepOnly(...).Where(pred)` | Umkehrung von `RemoveItems`. | `KeepOnly<Line>(x => x.Lines).Where(l => l.IsPublic).When(...)` |
| `Take(...).First(n)` / `.Last(n)` | Begrenzt eine Sammlung auf die ersten oder letzten `n` Elemente. | `Take<Line>(x => x.Lines).First(10).When(...)` |

Das Element-Prädikat von `RemoveItems` / `KeepOnly` existiert in synchroner, asynchroner und
instanzbezogener Form — eine Berechtigungsprüfung pro Element darf also awaiten.

### Strukturänderungen

Diese verändern die **serialisierten Schlüssel**, was ein POCO nicht ausdrücken kann. Sie werden in
`context.StructuralEdits` protokolliert und zur Serialisierungszeit nachgespielt. Mit dem
ASP.NET-Core-Adapter geschieht das automatisch. Wenn Sie die Pipeline selbst steuern, wenden Sie sie
ausdrücklich an:

```csharp
await pipeline.ProcessAsync(dto, context);

if (context.StructuralEdits.HasAny)
{
    JsonNode? node = JsonStructuralTransformer.Transform(dto, context.StructuralEdits, jsonOptions);
    return node;   // statt des DTO serialisieren
}
```

| Builder | Wirkung | Beispiel |
| --- | --- | --- |
| `Remove(...)` | Entfernt Eigenschaften vollständig aus der Ausgabe — der Schlüssel verschwindet, anders als bei `Nullify`, das ein `null` stehen lässt. | `Remove(x => x.Internal, x => x.Debug).When(...)` |
| `Rename(...).To(name)` | Benennt den serialisierten Schlüssel um. | `Rename(x => x.Id).To("orderId").Always()` |
| `TransformKey(...).Using(fn)` | Wandelt den serialisierten Schlüssel einer Eigenschaft um. | `TransformKey(x => x.Id).Using(k => "x_" + k).Always()` |
| `TransformKeys().Using(fn)` | Wandelt **alle** Schlüssel um — etwa um für eine einzelne Antwort eine Namenskonvention zu erzwingen. | `TransformKeys().Using(k => k.ToUpperInvariant()).When(...)` |
| `AddProperty(name).From(...)` / `.WithValue(...)` | Fügt einen Schlüssel ein, den es am CLR-Typ nicht gibt, konstant oder pro Instanz und Kontext berechnet. | `AddProperty("displayName").From(o => $"#{o.Id}").Always()` |

Hinweise und Grenzen:

- Die Schlüsseltransformation erhält den **serialisierten** Schlüssel — also nach `JsonNamingPolicy`
  und `[JsonPropertyName]` — während die Änderungen selbst gegen das CLR-Member auflösen. Beides
  funktioniert unter jeder Namenskonvention weiter.
- Kinder werden vor den Änderungen ihres Besitzers besucht. Eine Umbenennung auf einer Ebene
  versteckt daher niemals einen geänderten Teilbaum darunter.
- Strukturänderungen in `ForEach`-Unterfiltern gelten pro Element.
- **Einschränkung:** Werte, die *ausschließlich* über Dictionary-Einträge erreichbar sind, werden für
  verschachtelte Strukturänderungen nicht durchlaufen. Oberste Ebene, Sammlungen, Arrays und
  komplexe Eigenschaften schon. Wertänderungen sind nicht betroffen — sie laufen vorher, direkt am
  Objekt.
- Sobald der Wert ein `JsonNode` ist, muss `ObjectResult.DeclaredType = null` gesetzt werden, sonst
  versucht der Formatter, den Knoten in die Form des ursprünglichen DTO-Typs zu bringen. Der Adapter
  erledigt das.

### Auswahl über Metadaten

Eigenschaftsmetadaten sind statisch, deshalb lösen beide folgenden Mechanismen **zur Bauzeit** des
Filters auf — nicht passende Eigenschaften fallen aus der Regel heraus und kosten zur Laufzeit nichts.

`.WhenProperty(p => …)` steht auf **jedem** eigenschaftsbezogenen Builder zur Verfügung. Es verengt
die bereits ausgewählten Eigenschaften und lässt sich mit `When`/`Unless` kombinieren; mehrfaches
Verketten wirkt als logisches UND.

```csharp
Nullify(x => x.A, x => x.B, x => x.C)
    .WhenProperty(p => p.GetCustomAttribute<SecretAttribute>() is not null)
    .When(NotInRole("Admin"));

Remove(x => x.Token).WhenProperty(p => p.PropertyType == typeof(string)).Always();
```

`Properties(...)` und `PropertiesWhere(...)` sind die *umgedrehten* Einstiegspunkte: zuerst die
Eigenschaftsmenge wählen, dann die Operation (`.Nullify()`, `.Remove()`, `.SetToDefault()`,
`.TransformKey()`).

```csharp
// Jede [Secret]-Eigenschaft, ohne sie aufzuzählen
PropertiesWhere(p => p.GetCustomAttribute<SecretAttribute>() is not null).Remove().Always();

// Eine benannte Menge
Properties(x => x.Name, x => x.Id).Nullify().When(...);
```

Deshalb gibt es keine Familie aus `NullifyWhere` / `RemoveWhere` / `SetToDefaultWhere`: ein
`WhenProperty` deckt jede Operation ab, und `PropertiesWhere` bietet die typunabhängigen Operationen
aus einem Builder an. Auf `Apply` hat `WhenProperty` keine Wirkung, da dort keine Eigenschaft
angesprochen wird.

### Notausgang

| Builder | Wirkung |
| --- | --- |
| `Apply(Action<T, ctx>)` | Beliebige synchrone Änderung für Logik, die die strukturierten Builder nicht abdecken — typischerweise mehrere Eigenschaften, die zusammen geändert werden müssen. |
| `ApplyAsync(Func<T, ctx, Task>)` | Die asynchrone Form. |

## Prädikatswortschatz

Jeder Builder endet mit demselben Satz von Terminals, und jedes Terminal nimmt ein Prädikat in
**jeder** Form an. Die Bibliothek überführt alle Formen intern in das kanonische
`AsyncPredicate<T>`.

| Terminal | Greift, wenn |
| --- | --- |
| `.When(predicate)` | das Prädikat `true` liefert |
| `.Unless(predicate)` | das Prädikat `false` liefert |
| `.Always()` | immer |
| `.WhenAll(p1, p2, …)` | alle Prädikate `true` sind (bricht beim ersten `false` ab) |
| `.WhenAny(p1, p2, …)` | mindestens ein Prädikat `true` ist (bricht beim ersten `true` ab) |

`WhenAll` / `WhenAny` nehmen `AsyncPredicate<T>` direkt an.

### Unterstützte Prädikatsformen

Verfügbar auf `When` und `Unless`:

| Form | Typischer Einsatz |
| --- | --- |
| `Func<bool>` | Feature-Flag oder Konstante — `.When(() => Config.HideCost)` |
| `Func<Task<bool>>` | Asynchrones Signal ohne Argumente — `.When(async () => await CheckExternalAsync())` |
| `Func<IResponseFilterContext, bool>` | Synchrone Prüfung nur am Kontext — `.When(ctx => ctx.Items["env"] as string == "prod")` |
| `Func<IResponseFilterContext, Task<bool>>` | Asynchron nur am Kontext — die natürliche Form für eine per DI aufgelöste Berechtigungsprüfung |
| `Func<T, bool>` | Reine Instanzprüfung — `.When(o => o.IsPublic)` |
| `Func<T, Task<bool>>` | Instanzprüfung mit IO |
| `SyncPredicate<T>` | Kanonisch synchron `(instance, ctx)` |
| `AsyncPredicate<T>` | Kanonisch asynchron `(instance, ctx)` — was die Kombinatoren annehmen |

Drei geschützte Hilfsmethoden der Basisklasse machen kurze Prädikate lesbarer: `Always()`,
`WhenContext(ctx => …)` und `WhenInstance(instance => …)`.

### Den Wortschatz erweitern

Vorgesehen ist eine projekteigene Basisklasse, die die Bedingungen Ihrer Domäne einmal benennt:

```csharp
public abstract class AppResponseFilter<T> : ResponseFilter<T> where T : class
{
    protected static AsyncPredicate<T> HasPermission(string name) =>
        async (_, ctx) => await ctx.Services
            .GetRequiredService<IPermissionChecker>()
            .IsGrantedAsync(name);

    protected static AsyncPredicate<T> LacksPermission(string name) =>
        async (instance, ctx) => !await HasPermission(name)(instance, ctx);

    protected static SyncPredicate<T> InRole(string role) =>
        (_, ctx) => ctx.Services.GetRequiredService<ICurrentUser>().IsInRole(role);
}
```

```csharp
public class InvoiceFilter : AppResponseFilter<InvoiceDto>
{
    public InvoiceFilter()
    {
        Nullify(x => x.Margin).When(LacksPermission("Invoices.SeeMargin"));
        Remove(x => x.InternalNotes).WhenAll(LacksPermission("Invoices.Internal"),
                                            LacksPermission("Admin"));
    }
}
```

### Teure Prädikate zwischenspeichern

Eine Berechtigungsprüfung, die zehn Regeln derselben Antwort auswerten, soll einmal laufen.
`context.Items` ist genau dafür der Notizzettel:

```csharp
protected static AsyncPredicate<T> HasPermissionCached(string name) => async (_, ctx) =>
{
    var key = $"perm:{name}";
    if (ctx.Items.TryGetValue(key, out var cached) && cached is bool b) return b;

    var granted = await ctx.Services.GetRequiredService<IPermissionChecker>().IsGrantedAsync(name);
    ctx.Items[key] = granted;
    return granted;
};
```

`Items` gilt für genau einen Pipeline-Durchlauf und ist nicht thread-sicher — die Regeln werden pro
Objekt sequenziell angewendet, Sperren sind also nicht nötig.

## Konfiguration

`ResponseFilterOptions`:

| Option | Standard | Zweck |
| --- | --- | --- |
| `ExceptionBehavior` | `Rethrow` | `Rethrow` lässt die Ausnahme einer Regel unverändert zum globalen Exception-Handler durch — für fast jede Anwendung richtig, denn eine `BusinessException` aus einem Filter ist immer noch ein Domänenfehler. `LogAndContinue` fängt sie, protokolliert über `ILogger<ResponseFilterPipeline>` und liefert eine teilweise gefilterte Antwort. |
| `SkipUnaffectedResponses` | `true` | Einmalige Erreichbarkeitsanalyse je Wurzeltyp der Antwort. Ist im Typgraphen kein Zieltyp eines registrierten Filters erreichbar, wird die ganze Pipeline übersprungen — keine Reflection, kein Durchlauf. |
| `SkipResponseType` | `null` | Ausschluss-Prädikat auf dem Wurzeltyp, wird *vor* der Erreichbarkeitsprüfung ausgewertet. |
| `ShouldHandle` | `null` | Schranke pro Request, `(HttpRequest, Type) → Task<bool>`, vom ASP.NET-Core-Adapter noch vor jedem Graphendurchlauf ausgewertet. `null` bedeutet „immer verarbeiten". |

`OperationCanceledException` wird in beiden Ausnahmemodi immer weitergegeben, damit abgebrochene
Requests und das Herunterfahren des Hosts weiter funktionieren.

### Die Pipeline auf einen Teil der API begrenzen

```csharp
options.ShouldHandle = (request, type) =>
    Task.FromResult(request.Path.StartsWithSegments("/api/app"));
```

Das Prädikat läuft nur für ein `ObjectResult` mit einem Wert ungleich `null`, `type` ist daher immer
der Laufzeittyp dieses Werts.

### Typen ausschließen

```csharp
options.SkipResponseType = t =>
    typeof(Stream).IsAssignableFrom(t) ||
    t.Namespace?.StartsWith("Volo.Abp") == true;
```

### Wann der Erreichbarkeits-Cache abgeschaltet werden muss

`SkipUnaffectedResponses` arbeitet mit **statischer** Typanalyse. Wenn Ihre Antworten Polymorphie
enthalten, die der Analysator nicht sehen kann — etwa eine `List<object>` mit gemischten DTOs —
setzen Sie die Option auf `false`, sonst werden solche Antworten übersprungen.

## Laufzeitverhalten

- **Abkürzung über Erreichbarkeit.** Der Typgraph jedes Wurzeltyps wird einmal prozessweit analysiert
  und zwischengespeichert. Antworten, die kein Filter berühren kann, betreten den Durchlauf nie.
- **Sammlungen auf oberster Ebene** verwenden für die Erreichbarkeitsprüfung den Elementtyp, da eine
  `List<T>` selbst keine filterbaren Eigenschaften hat.
- **Zyklensicherheit** über `ReferenceEqualityComparer` — eine bidirektionale Navigation wird einmal
  besucht.
- **Eigenschaftsfilterung zur Bauzeit.** `WhenProperty` und `PropertiesWhere` lösen beim Erzeugen des
  Filters auf, nicht pro Request.
- **Kein JSON-Umweg ohne Anlass.** `StructuralEditBook.HasAny` ist im Normalfall `false`, die
  Baumtransformation entfällt dann vollständig.
- **`ValueTask`** durchgehend bei Regeln und Prädikaten, damit synchrone Pfade nichts allozieren.

## Einen Filter testen

Filter sind gewöhnliche Klassen — kein Host, kein HTTP.

```csharp
[Fact]
public async Task Setzt_Kosten_fuer_Nicht_Finance_auf_null()
{
    var services = new ServiceCollection()
        .AddSingleton<ICurrentUser>(new FakeUser(roles: []))
        .BuildServiceProvider();

    var context = new TestFilterContext(services);      // eigener IResponseFilterContext-Stub
    var dto = new OrderDto { TotalCost = 42m };

    await new OrderResponseFilter().ApplyAsync(dto, context);

    Assert.Null(dto.TotalCost);
}
```

Für Strukturregeln prüfen Sie gegen den transformierten Baum:

```csharp
await new OrderResponseFilter().ApplyAsync(dto, context);

var node = JsonStructuralTransformer.Transform(dto, context.StructuralEdits);
Assert.Null(node!["internalRef"]);
Assert.Equal("#1001", node["displayName"]!.GetValue<string>());
```

`InlineFilter<T>` baut einen Filter ohne eigene Klasse, was Einzelfälle kurz hält:

```csharp
var filter = new InlineFilter<OrderDto>();
filter.Mask(x => x.CreditCard).KeepLast(4).Always();

await filter.ApplyAsync(dto, context);
```

Die mitgelieferte Testsuite liegt unter
[`Tests/Nextended.ResponseFilters.Tests`](https://github.com/fgilde/Nextended/tree/main/Tests/Nextended.ResponseFilters.Tests).

## Vergleich mit Attributen

| Anwendungsfall | Attribut wie `[JsonIgnore]` | `ResponseFilter<T>` |
| --- | --- | --- |
| Nullsetzen nach Berechtigung | ✅ | ✅ |
| DTO aus einer Fremdbibliothek | ❌ | ✅ |
| Maskieren statt entfernen | ❌ | ✅ |
| Abhängig von einer anderen Eigenschaft | ❌ | ✅ |
| Mandanten- oder benutzerabhängig | ❌ | ✅ |
| Schlüssel pro Request umbenennen oder ergänzen | ❌ | ✅ |
| Isoliert unittestbar | ⚠️ | ✅ |
| Kostenlos, wenn keine Regel greift | ✅ | ✅ (Erreichbarkeits-Cache) |

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Verwandt

- [Nextended.ResponseFilters.AspNetCore](responsefilters-aspnetcore.md) — der MVC-Adapter
- [Nextended.Web](web.md) — OData- und Controller-Helfer
- [Nextended.Core](core.md) — die Basisbibliothek

## Links

- 📦 [Nextended.ResponseFilters auf NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters/)
- 📦 [Nextended.ResponseFilters.AspNetCore auf NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters)
- 🧪 [Tests](https://github.com/fgilde/Nextended/tree/main/Tests/Nextended.ResponseFilters.Tests)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
