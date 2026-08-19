---
title: Nextended.EF
description: Entity-Framework-Core-Erweiterungen — Graph-Laden, deklarative Include-Definitionen, Abfragehelfer, Paging, dynamisches Sortieren und Bulk-Operationen.
---

# Nextended.EF

📚 **[Vollständige API-Referenz](/de/projects/ef-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/ef)

Erweiterungen für Entity Framework Core: Graphen laden, ohne `Include`-Ketten zu schreiben,
wiederverwendbare Include-Definitionen, bedingte Abfragezusammenstellung, Paging mit dynamischer
Sortierung und Bulk-Operationen.

[![NuGet](https://img.shields.io/nuget/v/Nextended.EF.svg)](https://www.nuget.org/packages/Nextended.EF/)

## Installation

```bash
dotnet add package Nextended.EF
```

Den EF-Core-Provider bringen Sie selbst mit (`Npgsql.EntityFrameworkCore.PostgreSQL`,
`Microsoft.EntityFrameworkCore.SqlServer`, …).

## Übersicht

| Bereich | API |
| --- | --- |
| **Graph-Laden** | `LoadGraphAsync`, `IncludeAll`, `MultiInclude`, `IncludeDetails`, `ThenIncludeDetails` |
| **Deklarative Includes** | `IncludeDefinitionFor<T>`, `IncludeAllVirtual`, `IncludeAllWhere`, `Without`, `WithoutRegex`, `WithoutPrefix`, `Except` |
| **Abfragehelfer** | `WhereContains`, `WhereKeyMatches`, `WhereBetween`, `WhereIn`, `WhereIf`, `ExistsAsync`, `AlternateQueryMatch` |
| **Paging & Sortierung** | `Page`, `ToPagedResultAsync`, `PagedResult<T>`, `OrderByMember`, `OrderByMembers`, `ThenByMember` |
| **Bedingte Bausteine** | `IncludeIf`, `AsTrackingIf`, `AsNoTrackingIf` |
| **DbContext-Helfer** | `FindEntityType<T>`, `GetPrimaryKeyPropertyNames<T>`, `GetPrimaryKeyValues`, `IsTrackedBy`, `DetachAll`, `GetOrAddAsync`, `GetOrCreateAsync` |
| **Bulk-Operationen** | `BulkInsertAsync`, `BulkDeleteWhereAsync`, `UpsertAsync`, `UpsertRangeAsync` |

## Graph laden

### LoadGraphAsync

Lädt die Navigationen ab einer bereits geladenen Entität — tiefenbegrenzt und zyklensicher.

```csharp
using Nextended.EF;

var user = await db.Users.FindAsync(userId);
await db.LoadGraphAsync(user, maxDepth: 2);
```

Besuchte Instanzen werden in einem `HashSet` geführt, eine bidirektionale Navigation läuft also
nicht endlos im Kreis.

### IncludeAll

Alle Navigationen des Entitätstyps einbinden — abzüglich dessen, was Sie nicht wollen:

```csharp
// alles
var products = await db.Products.IncludeAll(db).ToListAsync();

// alles außer bestimmten Pfaden
var users = await db.Users
    .IncludeAll(db, new[] { "Orders.OrderItems" })
    .ToListAsync();

// Ausschluss über Ausdrücke statt Zeichenketten
var orders = await db.Orders
    .IncludeAll(db, o => o.Customer)
    .ToListAsync();
```

Es gibt die Variante direkt auf dem `DbSet<T>` und die auf `IQueryable<T>` mit `DbContext`.

### MultiInclude

Mehrstufige Includes ohne `ThenInclude`-Treppe:

```csharp
var orders = await db.Orders
    .MultiInclude(
        q => q.Include(o => o.OrderItems),
        oi => oi.Product,
        oi => oi.Product.Category)
    .ToListAsync();
```

## Deklarative Include-Definitionen

Statt dieselben `Include`-Ketten in jeder Abfrage zu wiederholen, deklarieren Sie sie einmal:

```csharp
using Nextended.Core.IncludeDefinitions;
using Nextended.EF;

var detail = new IncludeDefinitionFor<Order>()
    .Include(o => o.Customer)
    .Include(o => o.OrderItems)
    .IncludeAllVirtual()                 // jede virtuelle Navigation
    .Without(o => o.InternalAuditTrail); // minus dieser

var orders = await db.Orders.IncludeDetails(detail).ToListAsync();
```

Definitionen sind kombinierbar und filterbar:

| Baustein | Wirkung |
| --- | --- |
| `Include(...)` | Einzelnen Pfad ergänzen |
| `IncludeAllVirtual()` | Alle virtuellen Navigationen |
| `IncludeAllWhere(...)` | Alle Navigationen, die einem Prädikat entsprechen |
| `IncludeWithPrefix(...)` | Eine Definition unter einem Präfix einhängen, für verschachtelte Nutzung |
| `Without(...)` / `Except(...)` | Pfade wieder herausnehmen |
| `WithoutRegex(...)` / `WithoutWhere(...)` | Pfade per Regex oder Prädikat ausschließen |
| `CompositeIncludePathDefinition` | Mehrere Definitionen zu einer zusammenfassen |
| `AttributeIncludePathDefinition<T>` | Definition aus Attributen auf der Entität ableiten |

Für verschachtelte Ebenen gibt es `ThenIncludeDetails`:

```csharp
var orders = await db.Orders
    .Include(o => o.OrderItems)
    .ThenIncludeDetails(o => o.OrderItems, itemDetail)
    .ToListAsync();
```

## Abfragen

```csharp
// über mehrere Eigenschaften suchen
var found = await db.Users.AlternateQueryMatch("john").ToListAsync();

// Teilstring über benannte Eigenschaften
var byName = db.Products.WhereContains(p => p.Name, "kabel");

// gegen den Primärschlüssel, ohne den Namen zu kennen
var one = db.Products.WhereKeyMatches(db, id);

// Bereich und Menge
var priced = db.Products.WhereBetween(p => p.Price, 10m, 100m);
var some   = db.Orders.WhereIn(o => o.Status, allowedStates);

// existiert überhaupt etwas?
if (await db.Orders.ExistsAsync(o => o.CustomerId == customerId)) { … }
```

## Bedingte Zusammenstellung

```csharp
var query = db.Orders
    .AsNoTrackingIf(readOnly)
    .IncludeIf(withCustomer, o => o.Customer)
    .WhereIf(from.HasValue, o => o.Created >= from!.Value)
    .WhereIf(!string.IsNullOrEmpty(search), o => o.Number.Contains(search));
```

Jeder `*If`-Helfer gibt die Abfrage unverändert zurück, wenn die Bedingung nicht zutrifft. Damit
entfällt die `if`-Kaskade, in der sonst derselbe Abfrageaufbau mehrfach dasteht.

## Paging und dynamische Sortierung

```csharp
var page = await db.Products
    .WhereIf(!string.IsNullOrEmpty(search), p => p.Name.Contains(search))
    .OrderByMembers("Category.Name asc", "Price desc")   // Sortierung als String vom Client
    .ToPagedResultAsync(pageIndex: 2, pageSize: 25);

page.Items;       // die Zeilen
page.TotalCount;  // Gesamtzahl vor dem Blättern
page.PageIndex;
page.PageSize;
```

`OrderByMember` und `ThenByMember` sortieren nach einem Member-Namen, auch über Punktpfade.
`Page` blättert ohne die Gesamtzahl zu ermitteln, wenn Sie sie nicht brauchen.
`PagedResult<T>.Empty` liefert ein leeres Ergebnis.

::: tip Warum das sicher ist
Die Sortierung wird gegen das EF-Modell aufgelöst, nicht als SQL zusammengesetzt. Ein
Client-String kann daher keine Injektion bewirken; ein unbekannter Member führt zu einem Fehler,
nicht zu einer manipulierten Abfrage.
:::

## DbContext-Helfer

```csharp
// Metadaten
var entityType = db.FindEntityType<Order>();
var keyNames   = db.GetPrimaryKeyPropertyNames<Order>();
var keyValues  = db.GetPrimaryKeyValues(order);

// Change Tracker
if (db.IsTrackedBy(order)) { … }
db.DetachAll();

// Finden oder anlegen
var tag = await db.Tags.GetOrCreateAsync(t => t.Name == "urgent", () => new Tag("urgent"));
var cat = await db.GetOrAddAsync(() => new Category("Neu"));
```

`DetachAll` ist der übliche Ausweg, wenn ein langlebiger Kontext zu viele verfolgte Entitäten
angesammelt hat.

## Bulk-Operationen

```csharp
await db.BulkInsertAsync(newRows);
await db.UpsertAsync(row, r => r.ExternalId);
await db.UpsertRangeAsync(rows, r => r.ExternalId);
await db.BulkDeleteWhereAsync<LogEntry>(l => l.Created < cutoff);
```

Wo der Provider einen nativen Weg anbietet, wird er genutzt; auf dem InMemory-Provider fallen die
Operationen auf den Change Tracker zurück. Derselbe Code läuft damit auch im Unit-Test.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Plattform

Plattformübergreifend.

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- `Microsoft.EntityFrameworkCore` (9.0+)
- `Microsoft.EntityFrameworkCore.Relational`

## Verwandt

[Nextended.Web](/de/projects/web) baut darauf auf und übersetzt OData-`$expand` in
`Include`-Ketten, damit eine Expansion nicht zu N+1-Abfragen führt.

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.EF/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.EF)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.EF/README.md)
- 🧪 [Tests](https://github.com/fgilde/Nextended/tree/main/Tests/Nextended.EF.Tests)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
