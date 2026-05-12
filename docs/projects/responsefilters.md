---
layout: default
title: Nextended.ResponseFilters
parent: Projects
nav_order: 11
---

# Nextended.ResponseFilters

Fluent, attribute-aware response-filtering pipeline for redacting, masking or transforming object graphs before serialization.

## Overview

A `ResponseFilter<T>` looks like a `FluentValidator<T>` — but instead of validating, it **mutates** the DTO right before it leaves your service: null out fields the user must not see, mask emails, hash tokens, round prices, drop collection items conditionally.

The library ships in two packages:

- **`Nextended.ResponseFilters`** — provider-agnostic core (rules, pipeline, builders). No ASP.NET dependency, usable in worker services, tests, anywhere.
- **`Nextended.ResponseFilters.AspNetCore`** — adapter that wires the pipeline into MVC as a global `IAsyncResultFilter`. One extension call and every controller response is filtered.

## Installation

```bash
# Provider-agnostic core
dotnet add package Nextended.ResponseFilters

# ASP.NET Core adapter (transitively pulls the core)
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Quick Start

### 1. Define a filter

```csharp
using Nextended.ResponseFilters;

public class OrderResponseFilter : ResponseFilter<OrderDto>
{
    public OrderResponseFilter()
    {
        // Null cost fields unless the user is in the Finance role
        Nullify(x => x.TotalCost, x => x.UnitCost)
            .Unless(WhenInstance(_ => CurrentUser.IsInRole("Finance")));

        // Mask credit card: 1234########5678
        Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).Always();

        // Hash audit tokens
        Hash(x => x.AuditToken).AsSha256().Always();

        // Round prices for non-premium users
        Round(x => x.Price).To(0).When((_, ctx) => !ctx.IsPremium());

        // Truncate notes with ellipsis
        Truncate(x => x.Notes).After(200, "…").Always();

        // Recurse into a sub-collection
        ForEach(x => x.Lines, line =>
            line.Nullify(l => l.UnitCost).Unless(_ => CurrentUser.IsInRole("Finance")));

        // Drop hidden lines, then cap at 10
        RemoveItems<LineDto>(x => x.Lines).Where(l => l.Hidden).Always();
        Take<LineDto>(x => x.Lines).First(10).Always();
    }
}
```

### 2. Wire it up

```csharp
// ASP.NET Core — Program.cs
builder.Services.AddNextendedResponseFilters(new[] { typeof(OrderResponseFilter).Assembly });
```

That's it. Every controller that returns an `OrderDto` (or anything containing one) now runs through the pipeline before serialization.

For non-ASP.NET hosts (workers, tests):

```csharp
services.AddResponseFilters(new[] { typeof(OrderResponseFilter).Assembly });
// …
var pipeline = sp.GetRequiredService<IResponseFilterPipeline>();
await pipeline.ProcessAsync(myDto, new ResponseFilterContext(sp));
```

## Why use this over attributes?

| Use case | `[RequiresPermission]` etc. | `ResponseFilter<T>` |
| --- | --- | --- |
| Permission-based nulling | ✅ | ✅ |
| DTO from a 3rd-party library (no attribute access) | ❌ | ✅ |
| Mask instead of null | ❌ | ✅ |
| Conditional on another property | ❌ | ✅ |
| Tenant/user-context-aware | ❌ | ✅ |
| Unit-testable in isolation | ⚠️ | ✅ |

## Rule builders

### Property mutators

| Builder | Purpose |
| --- | --- |
| `Nullify(...)` | Set one or more nullable properties to `null`. Accepts multiple selectors in one call. |
| `SetValue(...).To(...)` | Set a property to a constant or computed value. |
| `SetToDefault(...)` | Reset properties to `default(TProperty)` — handles nullable, non-nullable value types, and reference types in one mixed call. |
| `Replace(...).With(...)` | Synonym for `SetValue` — reads better when there's an existing value. |
| `Transform(...).Using(...)` | Map a property through a pure function. |
| `Clear(...)` | Empty a property: string → `""`, mutable `IList` → in-place `.Clear()`, array → empty array. |

### String operations

| Builder | Purpose |
| --- | --- |
| `Mask(...)` | Mask a string with `KeepFirst(n)` / `KeepLast(n)` / `With(char)` / `WithPattern(string)`. |
| `Truncate(...).After(n)` | Cut at N characters; `.After(n, "…")` appends a suffix on truncation. |
| `Hash(...)` | Replace with a hash (default: SHA-256 hex). Picks algorithm via `.AsSha256() / .AsSha1() / .AsSha512() / .AsMd5() / .Using(fn)`. |

### Numeric operations

| Builder | Purpose |
| --- | --- |
| `Round(...).To(n)` | Round `decimal` / `double` / `float` to N decimals. `.To(n, mode)` for explicit `MidpointRounding`. `.ToInteger()` for whole numbers. Constrained to `INumber<T>` — compile-time prevents misuse on non-numeric properties. |

### Collection operations

| Builder | Purpose |
| --- | --- |
| `ForEach(...)` | Recurse into a collection property; configure a sub-filter inline. |
| `RemoveItems(...).Where(pred)` | Drop items matching the predicate. Mutates `IList<T>` in place; rebuilds arrays. |
| `KeepOnly(...).Where(pred)` | Inverse of `RemoveItems`. |
| `Take(...).First(n)` / `.Last(n)` | Limit a collection to the first/last N items. |

### Escape hatch

| Builder | Purpose |
| --- | --- |
| `Apply(...)` / `ApplyAsync(...)` | Arbitrary `Action<T, IResponseFilterContext>` or `Func<…, Task>` for anything the structured builders don't cover. |

## Predicate vocabulary

Every builder closes with the same terminals. They accept **any predicate shape** — no-arg, context-only, instance-only, or both — in sync or async (`Task`) form. The library adapts each to the canonical `AsyncPredicate<T>` internally.

| Terminal | Fires when … |
| --- | --- |
| `.When(predicate)` | predicate returns `true` |
| `.Unless(predicate)` | predicate returns `false` |
| `.Always()` | unconditional |
| `.WhenAll(p1, p2, …)` | every `AsyncPredicate<T>` returns true (short-circuits on first false) |
| `.WhenAny(p1, p2, …)` | at least one returns true (short-circuits on first true) |

**Supported predicate shapes** (each on `When` and `Unless`):

| Shape | Example |
| --- | --- |
| `Func<bool>` | `.When(() => Config.HideCost)` |
| `Func<Task<bool>>` | `.When(async () => await CheckExternal())` |
| `Func<IResponseFilterContext, bool>` | `.When(ctx => ctx.Items["env"] == "prod")` |
| `Func<IResponseFilterContext, Task<bool>>` | `.When(async ctx => !await IsGrantedAsync(ctx))` |
| `Func<T, bool>` | `.When(o => o.IsPublic)` |
| `Func<T, Task<bool>>` | `.When(async o => await IsAllowedAsync(o.Id))` |
| `SyncPredicate<T>` | `.When((o, ctx) => …)` (canonical sync) |
| `AsyncPredicate<T>` | the type `WhenAll`/`WhenAny` consume directly |

## Extending the vocabulary

Every builder implements `IRuleBuilder<T>`, so domain-specific terminals are ordinary extension methods. Define them once for any builder type:

```csharp
public static class PermissionRuleBuilderExtensions
{
    public static ResponseFilter<T> WhenMissingPermission<T>(this IRuleBuilder<T> b, string policy)
        where T : class
        => b.When(async (_, ctx) =>
        {
            var checker = ctx.Services.GetRequiredService<IPermissionChecker>();
            return !await checker.IsGrantedAsync(policy).ConfigureAwait(false);
        });
}
```

Call site stays vocabulary-driven:

```csharp
Nullify(x => x.TotalCost).WhenMissingPermission("Insights.ViewFinancial");
```

## Configuration

`AddResponseFilters` / `AddNextendedResponseFilters` take an optional `Action<ResponseFilterOptions>`:

```csharp
builder.Services.AddNextendedResponseFilters(
    assemblies: new[] { typeof(OrderResponseFilter).Assembly },
    configure: opts =>
    {
        opts.ExceptionBehavior     = FilterExceptionBehavior.Rethrow;   // default
        opts.SkipUnaffectedResponses = true;                             // default
        opts.SkipResponseType        = t => typeof(System.IO.Stream).IsAssignableFrom(t);
    });
```

| Option | Default | Purpose |
| --- | --- | --- |
| `ExceptionBehavior` | `Rethrow` | `Rethrow` lets filter exceptions reach the host's global handler. `LogAndContinue` catches them, logs a warning, and continues with remaining filters. `OperationCanceledException` always propagates. |
| `SkipUnaffectedResponses` | `true` | Skip the pipeline when no registered filter's target type is reachable in the response's static type graph. Turn off for runtime polymorphism (`List<object>` etc.). |
| `SkipResponseType` | `null` | Custom predicate on the response root type. Evaluated before reachability — useful for blanket-opting-out infrastructure types (streams, framework wrappers). |

## Pipeline behavior

- **Graph walk** — Depth-first traversal of the response object graph; every node whose type has a registered filter is processed before serialization.
- **Cycle detection** — `ReferenceEqualityComparer`-backed visited set, no `StackOverflowException` on back-references.
- **Top-level collections** — Arrays, lists, and `IEnumerable<T>` returned directly from controllers are detected and iterated.
- **Reachability fast-path** — If no registered filter target is reachable from the response root type (and `SkipUnaffectedResponses` is on), the pipeline is a one-cache-lookup no-op. Subtree walks are also short-circuited per nested property.
- **Exception transparency** — Default behaviour propagates filter exceptions so domain errors reach the host's handler unchanged. Toggle to `LogAndContinue` if filter robustness matters more than visibility.
- **Indexer safety** — Properties with index parameters (`this[int]`, `Item` on collections) are skipped to avoid `TargetParameterCountException`.

## Performance

- **Compiled accessors** — `PropertyAccessor` uses Expression-Tree-compiled get/set delegates, cached per `PropertyInfo`. Typically 10-50× faster than `PropertyInfo.GetValue/SetValue`.
- **Type metadata cache** — `TypeGraphInspector` caches per-type metadata; the graph walker never reflects twice on the same type.
- **Reachability cache** — `TypeReachabilityCache` precomputes per type whether the subgraph contains a registered filter target. Misses cost one cache lookup.
- **Per-request predicate memoization** — `IResponseFilterContext.Items` is a scratch bag for caching async predicate results across rules in the same response.

## API reference

### Core types

| Type | Purpose |
| --- | --- |
| `ResponseFilter<T>` | Abstract base. Inherit, configure rules in constructor via the protected builders. |
| `IRuleBuilder<T>` | Marker interface implemented by every builder; the extension surface for custom terminals. |
| `IResponseFilterContext` | Per-request bag: `IServiceProvider`, `CancellationToken`, `IDictionary<string, object?> Items`. |
| `IResponseFilterPipeline` | Walks the graph and applies matching filters. |
| `IResponseFilterRegistry` | Resolves filters per type from DI. |
| `AsyncPredicate<T>` | `Func<T, IResponseFilterContext, ValueTask<bool>>` — the canonical predicate shape. |
| `SyncPredicate<T>` | `Func<T, IResponseFilterContext, bool>`. |

### DI extensions

| Method | Purpose |
| --- | --- |
| `services.AddResponseFilters(assemblies, lifetime)` | Core — scans assemblies for `ResponseFilter<T>` implementations and wires the pipeline. |
| `services.AddResponseFilter<TFilter>()` | Register a single filter manually (tests, ad-hoc). |
| `services.AddNextendedResponseFilters(assemblies, lifetime)` | AspNetCore — calls `AddResponseFilters` and plugs `ResponseFilterResultFilter` into MVC globally. |

## Supported frameworks

- .NET 8.0
- .NET 9.0
- .NET 10.0

`Round` uses `System.Numerics.INumber<T>` (introduced in .NET 7+), so older TFMs are not supported by this package.

## Dependencies

- `Nextended.Core`
- `Microsoft.Extensions.Logging.Abstractions`
- (AspNetCore adapter) `Microsoft.AspNetCore.App` framework reference

## Related projects

- [Nextended.Core](core.md) — Foundation library
- [Nextended.Web](web.md) — Other ASP.NET helpers

## Links

- [Nextended.ResponseFilters on NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters/)
- [Nextended.ResponseFilters.AspNetCore on NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)
- [Source — Core](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters)
- [Source — AspNetCore adapter](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters.AspNetCore)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
