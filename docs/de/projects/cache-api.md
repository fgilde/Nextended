---
title: Nextended.Cache — API-Referenz
---

# Nextended.Cache — API-Referenz

🇬🇧 [This page in English](/projects/cache-api)

Die vollständige öffentliche Oberfläche von `Nextended.Cache`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/cache)

## Nextended.Cache

### `CacheProvider`

`class`

CacheProvider

**Methoden**

- `Clear() : void`
- `ClearWhen(Func<CacheProvider, bool> predicate) : CacheProvider`
- `Count() : int`
- `ExecuteWithCache<TInstance, T>(TInstance owner, Expression<Func<TInstance, T>> expression) : T`

**Eigenschaften**

- `ClearCheckInterval : TimeSpan { get; set; }`
- `LastWriteTime : DateTime { get; set; }`

**Ereignisse**

- `Cleared : EventHandler<EventArgs>`

### `CacheProviderExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ExecuteWithCache<TInstance, T>(this TInstance owner, CacheProvider cache, Expression<Func<TInstance, T>> expression) : T`

## Nextended.Cache.Extensions

### `CacheExecutionInfo<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `CacheExecutionInfo(T result, string key, bool isNewEntry)`

**Eigenschaften**

- `IsNewEntry : bool { get; }`
- `Key : string { get; }`
- `Result : T { get; }`

### `CacheExtensions`

`static class`

Simple cache extensions

↩ [Zurück zur Paketseite](/de/projects/cache)
