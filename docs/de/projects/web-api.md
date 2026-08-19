---
title: Nextended.Web — API-Referenz
---

# Nextended.Web — API-Referenz

🇬🇧 [This page in English](/projects/web-api)

Die vollständige öffentliche Oberfläche von `Nextended.Web`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/web)

## 

### `ProvidedAsEdm`

`static class`

_Keine Beschreibung._

**Eigenschaften**

- `EntitySetClrMap : IReadOnlyDictionary<string, Type> { get; }`

## Nextended.Web

### `BackgroundExecutionScope`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `BackgroundExecutionScope(IServiceScopeFactory scopeFactory, Action onIn, HttpRequestSnapshot snapshot = null)`
- `BackgroundExecutionScope(IServiceScopeFactory scopeFactory, HttpRequestSnapshot snapshot = null)`

**Methoden**

- `CompleteAsync(CancellationToken ct) : Task`
- `DisposeAsync() : ValueTask`
- `ScopeEnter() : BackgroundExecutionScope`

**Eigenschaften**

- `Services : IServiceProvider { get; }`

### `BackgroundExecutor`

`class`

_Keine Beschreibung._

**Methoden**

- `ExecuteDetachedAsync(HttpRequestSnapshot snapshot, TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action) : Task`
- `ExecuteDetachedAsync(HttpRequestSnapshot snapshot, TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, Func<IServiceProvider, CancellationToken, Task> onSetup = null, Func<IServiceProvider, Exception, CancellationToken, Task> onTeardown = null) : Task`
- `ExecuteDetachedAsync(HttpRequestSnapshot snapshot, TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, IReadOnlyList<IAsyncDisposable> disposables) : Task`
- `ExecuteDetachedAsync(HttpRequestSnapshot snapshot, TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, IReadOnlyList<IDisposable> disposables) : Task`
- `ExecuteDetachedAsync<T>(HttpRequestSnapshot snapshot, TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task<T>> action) : Task<T>`
- `ExecuteDetachedWithCapturedRequestAsync(TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, CancellationToken captureCt = null) : Task`
- `ExecuteDetachedWithCapturedRequestAsync(TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, IReadOnlyList<IAsyncDisposable> disposables, CancellationToken captureCt = null) : Task`
- `ExecuteDetachedWithCapturedRequestAsync(TimeSpan timeout, Func<IServiceProvider, CancellationToken, Task> action, IReadOnlyList<IDisposable> disposables, CancellationToken captureCt = null) : Task`

### `HttpContextExtensions`

`static class`

_Keine Beschreibung._

### `HttpRequestSnapshot`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `HttpRequestSnapshot()`

**Eigenschaften**

- `Body : byte[] { get; set; }`
- `ContentLength : long? { get; set; }`
- `ContentType : string { get; set; }`
- `Method : string { get; set; }`
- `Scheme : string { get; set; }`
- `User : ClaimsPrincipal { get; set; }`

### `RequestHelper`

`class`

Hilfsklasse für Request- und URL-Behandlungen (ASP.NET Core only, ohne System.Web)

**Konstruktoren**

- `RequestHelper(Uri baseAddress, string apiPath = "api")`
  <br>In ASP.NET Core genügt die Host-Basisadresse.

**Methoden**

- `GetActionName<TController, TResult>(Expression<Func<TController, TResult>> funcExpr) : string`
- `GetActionName<TController>(Expression<Action<TController>> actionExpr) : string`
- `GetControllerName<TController>() : string`
- `UrlFor<TController, TResult>(Expression<Func<TController, TResult>> funcExpr, out IDictionary<string, object> bodyParameters) : string`
- `UrlFor<TController>(Expression<Action<TController>> actionExpr, string routeNameForAttributeRoute = "") : string`
- `UrlFor<TController>(string actionName = null, object routeValuesObject = null) : string`

### `WebExtensions`

`static class`

_Keine Beschreibung._

## Nextended.Web.Extensions

### `BrowserFileExtensions`

`static class`

_Keine Beschreibung._

### `ConfigurationExtensions`

`static class`

_Keine Beschreibung._

### `ControllerExtensions`

`static class`

_Keine Beschreibung._

### `FormFileExtensions`

`static class`

_Keine Beschreibung._

### `ODataExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AddODataAuto(this IServiceCollection services) : IServiceCollection`

## Nextended.Web.OData

### `FacetResourceSetSerializer`

`class`

_Keine Beschreibung._

**Felder**

- `FacetsAnnotationName : string`

### `FacetSerializerProvider`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `FacetSerializerProvider(IServiceProvider sp)`

### `ODataExtensions`

`static class`

_Keine Beschreibung._

### `ODataOptionsConfiguration`

`class`

_Keine Beschreibung._

↩ [Zurück zur Paketseite](/de/projects/web)
