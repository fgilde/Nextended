---
title: Nextended.EF — API-Referenz
---

# Nextended.EF — API-Referenz

🇬🇧 [This page in English](/projects/ef-api)

Die vollständige öffentliche Oberfläche von `Nextended.EF`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/ef)

## Nextended.EF

### `AlternateQueryMatchExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `WhereKeyMatches<T>(this IQueryable<T> query, Expression<Func<T, object>>[] propertySelectors, string key) : IQueryable<T>`
- `WhereKeyMatches<T>(this IQueryable<T> query, string[] propertyNames, string key) : IQueryable<T>`

### `BulkExtensions`

`static class`

Bulk-style operations layered on top of EF Core 7+'s `ExecuteUpdateAsync`/`ExecuteDeleteAsync` plus convenience batchers for insert and upsert that don't require a third-party library.

### `DbContextExtensions`

`static class`

Helpers around `DbContext` for metadata, change-tracker manipulation, and the find-or-create pattern.

### `DbSetExtensions`

`static class`

_Keine Beschreibung._

### `IncludeDetailsExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `DistinctPaths(this IIncludePathDefinition def) : IEnumerable<string>`
- `IncludeDetails<TNav>(this IQueryable<TNav> query, IIncludePathDefinition def) : IQueryable<TNav>`
- `IncludeDetails<TParent, TNav>(this IQueryable<TParent> query, Expression<Func<TParent, IEnumerable<TNav>>> navigation, IIncludePathDefinition def) : IQueryable<TParent>`
- `IncludeDetails<TParent, TNav>(this IQueryable<TParent> query, Expression<Func<TParent, TNav>> navigation, IIncludePathDefinition def) : IQueryable<TParent>`

### `PagedResult<T>`

`class`

Result of a paged query: the slice of items plus the total count and paging metadata.

**Konstruktoren**

- `PagedResult()`

**Methoden**

- `Empty(int pageIndex = 0, int pageSize = 0) : PagedResult<T>`

**Eigenschaften**

- `HasNext : bool { get; }`
- `HasPrevious : bool { get; }`
- `Items : IReadOnlyList<T> { get; set; }`
- `PageIndex : int { get; set; }`
- `PageSize : int { get; set; }`
- `TotalCount : int { get; set; }`
- `TotalPages : int { get; }`

### `PagingSortingExtensions`

`static class`

Paging and dynamic sorting helpers for `IQueryable`1`.

**Extension Methods**

- `OrderByMember<T>(this IQueryable<T> source, string memberPath, bool descending = false) : IOrderedQueryable<T>`
- `OrderByMembers<T>(this IQueryable<T> source, IEnumerable<ValueTuple<string, bool>> orderings) : IQueryable<T>`
- `Page<T>(this IQueryable<T> source, int pageIndex, int pageSize) : IQueryable<T>`
- `ThenByMember<T>(this IOrderedQueryable<T> source, string memberPath, bool descending = false) : IOrderedQueryable<T>`
- `ToPagedResultAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize, CancellationToken cancellationToken = null) : Task<PagedResult<T>>`
- `WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate) : IQueryable<T>`

### `QueryableExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `WhereContains<T>(this IQueryable<T> source, string search, Expression<Func<T, string>>[] propertySelectors) : IQueryable<T>`

### `QueryComfortExtensions`

`static class`

Convenience extensions that round out common query patterns: range filters, set membership, conditional Include / tracking, existence checks.

**Extension Methods**

- `AsNoTrackingIf<T>(this IQueryable<T> source, bool condition) : IQueryable<T>`
- `AsTrackingIf<T>(this IQueryable<T> source, bool condition) : IQueryable<T>`
- `ExistsAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = null) : Task<bool>`
- `ExistsAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = null) : Task<bool>`
- `IncludeIf<T, TProperty>(this IQueryable<T> source, bool condition, Expression<Func<T, TProperty>> navigation) : IQueryable<T>`
- `IncludeIf<T>(this IQueryable<T> source, bool condition, string navigationPath) : IQueryable<T>`
- `WhereBetween<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> selector, TKey from, TKey to) : IQueryable<T>`
- `WhereIn<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> selector, IEnumerable<TKey> values) : IQueryable<T>`

↩ [Zurück zur Paketseite](/de/projects/ef)
