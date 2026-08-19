---
title: Nextended.ResponseFilters.AspNetCore — API-Referenz
---

# Nextended.ResponseFilters.AspNetCore — API-Referenz

🇬🇧 [This page in English](/projects/responsefilters-aspnetcore-api)

Die vollständige öffentliche Oberfläche von `Nextended.ResponseFilters.AspNetCore`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/responsefilters-aspnetcore)

## Nextended.ResponseFilters.AspNetCore

### `ResponseFilterResultFilter`

`class`

MVC result filter that runs the `IResponseFilterPipeline` over `Value` before the action result is serialized.

### `ServiceCollectionExtensions`

`static class`

DI registration for the ASP.NET Core adapter.

**Extension Methods**

- `AddNextendedResponseFilters(this IServiceCollection services, Assembly[] assemblies = null, ServiceLifetime lifetime = 1, Action<ResponseFilterOptions> configure = null) : IServiceCollection`

↩ [Zurück zur Paketseite](/de/projects/responsefilters-aspnetcore)
