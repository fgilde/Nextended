---
layout: default
title: Nextended.EF
parent: Projects
nav_order: 1
---

# Nextended.EF

Entity Framework Core extensions: graph loading, declarative includes,
paging & sorting, query-comfort helpers, DbContext utilities and bulk operations.

## Installation

```bash
dotnet add package Nextended.EF
dotnet add package Microsoft.EntityFrameworkCore
```

## Feature Overview

| Area | API |
|------|-----|
| **Graph loading** | `DbContext.LoadGraphAsync`, `DbSet.IncludeAll`, `DbSet.MultiInclude` |
| **Declarative includes** | `IncludeDefinitionFor<T>`, `IncludePathDefinition`, `IIncludePathDefinition`, `AttributeIncludePathDefinition<T>`, `CompositeIncludePathDefinition`, `PrefixedIncludePathDefinition`, `FilteredIncludePathDefinition`, `IncludeDetails`, `ThenIncludeDetails`, `Without` / `WithoutPrefix` / `WithoutRegex` / `Except` |
| **Querying** | `WhereContains`, `WhereKeyMatches`, `WhereBetween`, `WhereIn`, `WhereIf` |
| **Paging & sorting** | `Page`, `ToPagedResultAsync`, `OrderByMember`, `ThenByMember`, `OrderByMembers`, `PagedResult<T>` |
| **Conditional query** | `IncludeIf`, `AsTrackingIf`, `AsNoTrackingIf`, `ExistsAsync` |
| **DbContext helpers** | `FindEntityType<T>`, `GetPrimaryKeyPropertyNames<T>`, `GetPrimaryKeyValues<T>`, `DetachAll`, `IsTrackedBy`, `GetOrAddAsync`, `GetOrCreateAsync` |
| **Bulk** | `BulkInsertAsync`, `BulkDeleteWhereAsync`, `UpsertAsync`, `UpsertRangeAsync` |

---

## Graph Loading

### LoadGraphAsync

Walks the navigation graph of a single entity and loads everything reachable
within `maxDepth`. Cycle-safe via a visited set.

```csharp
using Nextended.EF;

var order = await db.Orders.FindAsync(orderId);
await db.LoadGraphAsync(order, maxDepth: 2);

// order.Customer, order.Customer.Address, order.Lines, order.Lines[].Product, …
```

### IncludeAll

Walks the EF model and adds an `Include` per navigation. Navigations on the
dependent side (`IsOnDependent`) are skipped so cycles are avoided — typically
you only get the principal-side collections.

```csharp
// Everything reachable from the principal side
var bob = await db.Customers.IncludeAll().FirstAsync(c => c.Id == 2);

// Exclude specific paths
var products = await db.Products
    .IncludeAll(new[] { "Reviews", "Reviews.User" })
    .ToListAsync();

// Exclude by expression
var users = await db.Customers.IncludeAll(c => c.Orders).ToListAsync();
```

### MultiInclude

Compact alternative to repeated `Include(...).ThenInclude(...)` chains.

```csharp
var customers = db.Customers.MultiInclude(
    c => c.Include(x => x.Orders),
    o => o.Lines,
    o => o.Customer!);
```

---

## Declarative Includes — `IncludeDefinitionFor<T>`

A reusable, composable description of which navigations to load. Definitions
are framework-agnostic (live in `Nextended.Core.IncludeDefinitions`) and become
real `Include` strings when applied via `IncludeDetails`.

### Building a definition

```csharp
using Nextended.Core.IncludeDefinitions;
using Nextended.EF;

var def = new IncludeDefinitionFor<Customer>()
    .Include(c => c.Address)
    .Include(c => c.Orders)
    .IncludeWithPrefix<Customer, Order>(
        c => c.Orders,
        new IncludePathDefinition().Include("Lines", "Lines.Product"));

// def.GetPaths() → ["Address", "Orders", "Orders.Lines", "Orders.Lines.Product"]
```

### Applying it

```csharp
var customers = await db.Customers.IncludeDetails(def).ToListAsync();

// Or on a sub-navigation while building the query:
var orders = await db.Orders
    .Include(o => o.Customer)
    .ThenIncludeDetails(o => o.Customer!.Address, addressDef)
    .ToListAsync();
```

### Reusable + filterable

```csharp
// Combine two definitions
var combined = new CompositeIncludePathDefinition(customerDef, orderDef);

// Strip subtrees
var lean = def.WithoutPrefix("Orders");

// Drop by expression
var noOrders = def.Without<Customer>(c => c.Orders);

// Glob & regex filtering
var noProducts = def.Without("Orders.Lines.Product");
var noNestedLines = def.WithoutRegex(@"^Orders\.Lines\..*$");
var noTransitive = def.Without("Orders.**");
```

### Attribute-driven

Mark navigations with `[IncludeInDetails]` and let the definition discover them.

```csharp
public class Customer
{
    public int Id { get; set; }
    [IncludeInDetails] public virtual Address? Address { get; set; }
    [IncludeInDetails] public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

var def = new AttributeIncludePathDefinition<Customer>(maxDepth: 3);
var customers = await db.Customers.IncludeDetails(def).ToListAsync();
```

### Discover by reflection

```csharp
// Pull in everything virtual (typical "lazy-load convention")
var allVirtual = new IncludeDefinitionFor<Customer>()
    .IncludeAllVirtual(maxDepth: 4, includeCollections: true);

// Or any predicate
var onlyAudited = new IncludeDefinitionFor<Customer>()
    .IncludeAllWhere(p => p.PropertyType.IsClass && p.Name.EndsWith("History"));
```

---

## Querying

### WhereContains

Case-insensitive substring search across one or more string properties.

```csharp
var hits = await db.Customers
    .WhereContains("cargo", c => c.Name, c => c.Email)
    .ToListAsync();
```

### WhereKeyMatches

Match a stringified key against the entity's primary key (or candidate name
properties), with auto-coercion to `string` / `Guid` / `int`.

```csharp
// Auto-detect PK from the model
var hits = await db.Customers.WhereKeyMatches(db, "2").ToListAsync();

// Or against arbitrary candidate property names
var orders = db.Orders
    .WhereKeyMatches(new[] { nameof(Order.OrderNumber) }, "ORD-101");

// Or against selector expressions
var matches = db.Customers
    .WhereKeyMatches(new Expression<Func<Customer, object>>[]
    {
        c => c.Id,
        c => c.Email!,
    }, "alice@example.com");
```

### WhereBetween / WhereIn / WhereIf

```csharp
var winter = db.Orders.WhereBetween(o => o.CreatedAt,
    new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));

var subset = db.Customers.WhereIn(c => c.Id, new[] { 1, 3, 7 });

// Build queries up incrementally without ugly if/else trees
var query = db.Customers
    .WhereIf(!string.IsNullOrWhiteSpace(filter.Term), c => c.Name.Contains(filter.Term!))
    .WhereIf(filter.MinCredit.HasValue, c => c.CreditLimit >= filter.MinCredit!.Value);
```

### ExistsAsync

```csharp
var hasAlice = await db.Customers.ExistsAsync(c => c.Name == "Alice");
```

---

## Paging & Sorting

### Page + ToPagedResultAsync

`PagedResult<T>` carries `Items`, `TotalCount`, `PageIndex`, `PageSize`,
`TotalPages`, `HasNext`, `HasPrevious`.

```csharp
var page = await db.Orders
    .OrderBy(o => o.Id)
    .ToPagedResultAsync(pageIndex: 0, pageSize: 25);

// page.Items, page.TotalCount, page.HasNext, …

// If you already have a count, just slice:
var slice = db.Orders.OrderBy(o => o.Id).Page(2, 25);
```

### Dynamic sorting

String-based ordering (e.g. from a UI table header) with support for dotted
property paths and case-insensitive matching.

```csharp
var customers = db.Customers
    .OrderByMember("name")                          // case-insensitive
    .ThenByMember(nameof(Customer.CreditLimit), descending: true)
    .ToList();

// Multi-column from any source
var orderings = new[]
{
    (nameof(Order.CustomerId), false),
    (nameof(Order.TotalCost), true),
};
var sorted = db.Orders.OrderByMembers(orderings);

// Nested paths work too
var sorted = db.Orders.OrderByMember("Customer.Address.City");
```

Throws `ArgumentException` if the property doesn't exist.

---

## Conditional query helpers

```csharp
var query = db.Customers
    .IncludeIf(opts.LoadAddress, c => c.Address)
    .IncludeIf(opts.LoadOrders,  "Orders")
    .AsNoTrackingIf(opts.ReadOnly)
    .AsTrackingIf(opts.ForceTracking);
```

Each helper is a no-op when the condition is `false`.

---

## DbContext helpers

```csharp
var entityType = db.FindEntityType<Customer>();              // EF IEntityType
var pkNames    = db.GetPrimaryKeyPropertyNames<Customer>();  // e.g. ["Id"]
var pkValues   = db.GetPrimaryKeyValues(alice);              // composite-key safe

if (db.IsTrackedBy(alice)) { /* … */ }

// Wipe the change tracker (handy between test steps or long-running services)
db.DetachAll();

// Find-or-create. GetOrAddAsync does NOT call SaveChanges — handy when you
// want to insert as part of a larger unit of work. GetOrCreateAsync persists.
var alice = await db.Customers.GetOrAddAsync(
    c => c.Email == "alice@example.com",
    () => new Customer { Name = "Alice", Email = "alice@example.com" });

var bob = await db.GetOrCreateAsync(
    db.Customers,
    c => c.Email == "bob@example.com",
    () => new Customer { Name = "Bob", Email = "bob@example.com" });
```

---

## Bulk Operations

Built on EF Core 7+'s `ExecuteUpdateAsync` / `ExecuteDeleteAsync`, with a
tracker-based fallback for providers that don't support bulk SQL (e.g. the
InMemory provider used in tests).

### BulkInsertAsync — batched AddRange + SaveChanges

```csharp
var products = LoadCatalog();         // tens of thousands of rows
await db.BulkInsertAsync(products, batchSize: 1000);
```

### BulkDeleteWhereAsync

```csharp
// Single server-side DELETE on relational providers; tracker-based on InMemory.
var removed = await db.OrderLines.BulkDeleteWhereAsync(l => l.UnitPrice < 5m);
```

### UpsertAsync / UpsertRangeAsync

Find by key selector, run `updateExisting` if found, otherwise insert.

```csharp
await db.UpsertAsync(
    new Product { Id = 2, Name = "Gizmo v2", Price = 25m },
    keySelector: p => p.Id,
    updateExisting: (existing, incoming) =>
    {
        existing.Name  = incoming.Name;
        existing.Price = incoming.Price;
    });

await db.UpsertRangeAsync(
    incomingProducts,
    keySelector: p => p.Id,
    updateExisting: (existing, src) =>
    {
        existing.Name  = src.Name;
        existing.Price = src.Price;
    });
```

> **Note**: For very large workloads on production-grade providers (SQL Server,
> PostgreSQL, …) consider a dedicated bulk library — `BulkInsertAsync` here
> still uses the EF change tracker. The helpers above hit the sweet spot
> between "obvious" and "fast enough for most cases".

---

## End-to-end example

```csharp
public async Task<PagedResult<Customer>> SearchCustomersAsync(
    CustomerFilter filter,
    CancellationToken ct = default)
{
    var query = _db.Customers
        .WhereIf(!string.IsNullOrWhiteSpace(filter.Term),
                 c => c.Name.Contains(filter.Term!) || c.Email!.Contains(filter.Term!))
        .WhereIf(filter.MinCredit.HasValue,
                 c => c.CreditLimit >= filter.MinCredit!.Value)
        .IncludeIf(filter.IncludeAddress, c => c.Address)
        .IncludeIf(filter.IncludeOrders,  c => c.Orders)
        .AsNoTrackingIf(filter.ReadOnly);

    if (!string.IsNullOrWhiteSpace(filter.SortBy))
        query = query.OrderByMember(filter.SortBy, filter.SortDescending);

    return await query.ToPagedResultAsync(filter.PageIndex, filter.PageSize, ct);
}

public sealed class CustomerFilter
{
    public string? Term { get; set; }
    public decimal? MinCredit { get; set; }
    public bool IncludeAddress { get; set; }
    public bool IncludeOrders { get; set; }
    public bool ReadOnly { get; set; } = true;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 25;
}
```

---

## Supported Frameworks

- .NET 8.0 / 9.0 / 10.0

## Dependencies

- `Nextended.Core`
- `Microsoft.EntityFrameworkCore`

## Tested with

`Nextended.EF.Tests` covers the public surface with 60+ MSTest cases against
the EF Core InMemory provider — see the `Nextended.EF.Tests` project in the
repo for runnable examples of every helper above.

## Related Projects

- [Nextended.Core](core.md) — `IncludeDefinitionFor<T>` and friends live here
- [Nextended.Web](web.md) — OData support layered on top of `IQueryable`

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.EF/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.EF)
- [Test Project](https://github.com/fgilde/Nextended/tree/main/Nextended.EF.Tests)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
