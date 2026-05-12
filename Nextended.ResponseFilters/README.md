# Nextended.ResponseFilters

[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters/)

Fluent, attribute-aware response-filtering pipeline for redacting, masking, or transforming object graphs before serialization.

A `ResponseFilter<T>` looks like a `FluentValidator<T>` — but instead of validating, it **mutates** the DTO right before it leaves your service: null out fields the user must not see, mask emails, replace internal flags, drop collection items conditionally.

## Installation

```bash
dotnet add package Nextended.ResponseFilters
# ASP.NET Core integration:
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Quick Start

```csharp
public class OrderResponseFilter : ResponseFilter<OrderDto>
{
    public OrderResponseFilter()
    {
        // Null out cost fields unless the user has the "Finance" role
        Nullify(x => x.TotalCost, x => x.UnitCost)
            .Unless(WhenInstance(_ => HasRole("Finance")));

        // Mask the email for unauthenticated callers
        Replace(x => x.CustomerEmail)
            .With("***@***.***")
            .When((order, ctx) => !ctx.Services.GetRequiredService<ICurrentUser>().IsAuthenticated);

        // Truncate notes after 200 chars
        Transform(x => x.Notes)
            .Using(n => n?.Length > 200 ? n[..200] + "…" : n)
            .Always();

        // Recurse into a collection — each line item gets its own sub-filter
        ForEach(x => x.Lines, line =>
            line.Nullify(l => l.UnitCost).Unless(_ => HasRole("Finance")));
    }

    private static bool HasRole(string role) => /* check current principal */ false;
}
```

Then wire it up:

```csharp
// Program.cs / Startup.cs
services.AddResponseFilters(new[] { typeof(OrderResponseFilter).Assembly });

// Manually run it (e.g. in a worker service)
var pipeline = sp.GetRequiredService<IResponseFilterPipeline>();
await pipeline.ProcessAsync(myOrderDto, new ResponseFilterContext(sp));
```

For ASP.NET Core: see [Nextended.ResponseFilters.AspNetCore](../Nextended.ResponseFilters.AspNetCore/README.md) — one extension call and every controller response is filtered automatically.

## Concepts

| Concept | Purpose |
| --- | --- |
| `ResponseFilter<T>` | Abstract base class. Inherit, configure rules in the constructor. |
| `Nullify(...)` | Set one or more properties to `null` when predicate matches. |
| `Replace(...)With(...)` | Replace property with a constant or per-instance value. |
| `Transform(...)Using(...)` | Map a property through a function. |
| `ForEach(...)` | Recurse into a collection property; configure a sub-filter inline. |
| `.When(...) / .Unless(...) / .Always() / .WhenAll(...) / .WhenAny(...)` | Predicate vocabulary applied as the terminal step. |
| `IResponseFilterContext` | Per-request bag: `IServiceProvider`, `CancellationToken`, `Items` for memoizing async work. |
| `IResponseFilterPipeline` | Walks the object graph depth-first and applies all matching filters. |
| `IResponseFilterRegistry` | Resolves filters per type from DI. |

## Why use this over attributes?

| Use case | Attribute | Fluent (`ResponseFilter<T>`) |
| --- | --- | --- |
| Permission-based nulling | ✅ | ✅ |
| DTO from a 3rd-party library (no attribute access) | ❌ | ✅ |
| Masking instead of nulling | ❌ | ✅ |
| Conditional on another property | ❌ | ✅ |
| Tenant/user-context-aware | ❌ | ✅ |
| Unit-testable in isolation | ⚠️ | ✅ |

If your needs are simple (always-null-on-missing-permission), attributes are fine. Use this package when you need real conditional logic, transformation, or testability.

## Performance

* `PropertyAccessor` uses compiled Expression-Tree get/set delegates (cached per `PropertyInfo`) — typically 10-50× faster than `PropertyInfo.SetValue`.
* `TypeGraphInspector` caches per-type metadata so the graph walker never reflects twice on the same type.
* Cycle detection via `ReferenceEqualityComparer` prevents infinite recursion on back-references.
* No filter registered for a type ⇒ pipeline only walks children and returns immediately.

## Robustness

Every filter and every property mutation is wrapped in a `try/catch`. If a single rule throws, it's logged via `ILogger<ResponseFilterPipeline>` and the rest of the pipeline continues — a misbehaving filter never takes down a request.

## Supported Frameworks

- .NET 8.0, .NET 9.0, .NET 10.0

## License

GPL-3.0-or-later (same as the rest of the Nextended ecosystem).
