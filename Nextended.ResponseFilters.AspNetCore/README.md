# Nextended.ResponseFilters.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.AspNetCore.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)

ASP.NET Core adapter for [Nextended.ResponseFilters](../Nextended.ResponseFilters/README.md). Wires the response-filter pipeline into MVC as a global `IAsyncResultFilter` — every `ObjectResult.Value` runs through the configured filters before serialization.

## Installation

```bash
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Quick Start

Define a filter (see [Nextended.ResponseFilters](../Nextended.ResponseFilters/README.md) for the full API):

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

## Supported Frameworks

- .NET 8.0, .NET 9.0, .NET 10.0

## License

GPL-3.0-or-later.
