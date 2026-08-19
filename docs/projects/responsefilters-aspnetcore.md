---
title: Nextended.ResponseFilters.AspNetCore
---
# Nextended.ResponseFilters.AspNetCore

📚 **[Full API reference](/projects/responsefilters-aspnetcore-api)** — every public type and member, generated from the compiled assembly.

ASP.NET Core adapter for Nextended.ResponseFilters — registers the pipeline as a global IAsyncResultFilter and replays structural edits against the serialized JSON tree.
[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.AspNetCore.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/responsefilters-aspnetcore)

---

## Installation

```bash
dotnet add package Nextended.ResponseFilters.AspNetCore
```


ASP.NET Core adapter for [Nextended.ResponseFilters](https://github.com/fgilde/Nextended/blob/main/Nextended.ResponseFilters/README.md). Wires the response-filter pipeline into MVC as a global `IAsyncResultFilter` — every `ObjectResult.Value` runs through the configured filters before serialization.

## Installation

```bash
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Quick Start

Define a filter (see [Nextended.ResponseFilters](https://github.com/fgilde/Nextended/blob/main/Nextended.ResponseFilters/README.md) for the full API):

```csharp
public class OrderResponseFilter : ResponseFilter<OrderDto>
{
    public OrderResponseFilter()
    {
        Nullify(x => x.TotalCost).Unless(WhenContext(ctx =>
            ctx.Services.GetRequiredService<ICurrentUser>().IsInRole("Finance")));
    }
}
```

Wire it up once in `Program.cs`:

```csharp
builder.Services.AddNextendedResponseFilters(new[]
{
    typeof(OrderResponseFilter).Assembly
});
```

That's it — every controller that returns an `OrderDto` (or anything containing one) now ships through the pipeline before serialization.

## What it does

```
HTTP request
  ↓
[ … middleware … ]
  ↓
Controller action → ObjectResult { Value = OrderDto }
  ↓
ResponseFilterResultFilter (IAsyncResultFilter)
  → IResponseFilterPipeline.ProcessAsync(value, ctx)
      → walks the graph, applies registered filters
  ↓
JSON serializer
  ↓
HTTP response
```

The result filter is registered globally via `MvcOptions.Filters.AddService<ResponseFilterResultFilter>()`. Failures inside the pipeline are caught and logged — a buggy rule cannot 500 a request.

## Notes

* The `IResponseFilterContext` handed to predicates has `HttpContext.RequestServices` and `HttpContext.RequestAborted` pre-wired.
* Filter scope follows the request scope by default. Override via the `lifetime` parameter of `AddNextendedResponseFilters` if needed.
* If you also use ABP / FluentValidation / OData — the filter sits *after* model binding and before serialization, so it composes cleanly with all of them.

## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Dependencies

- Nextended.ResponseFilters

## Links

- 📦 [NuGet package](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters.AspNetCore)
- 📄 [Package README](https://github.com/fgilde/Nextended/blob/main/Nextended.ResponseFilters.AspNetCore/README.md)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)