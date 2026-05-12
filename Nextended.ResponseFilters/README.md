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

        // Mask credit card: 1234########5678
        Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).When((_, ctx) => !ctx.IsAdmin());

        // Pattern-replace email for unauthenticated callers
        Mask(x => x.CustomerEmail).WithPattern("***@***.***")
            .When((_, ctx) => !ctx.Services.GetRequiredService<ICurrentUser>().IsAuthenticated);

        // Truncate notes after 200 chars with ellipsis
        Truncate(x => x.Notes).After(200, "…").Always();

        // Reset multiple heterogeneous fields to their default values
        SetToDefault(x => x.InternalScore, x => x.IsBookmarked, x => x.HiddenTags)
            .When(NotInRole("Internal"));

        // Hash a token (default: SHA-256 hex)
        Hash(x => x.AuditToken).Always();

        // Round prices for non-premium users
        Round(x => x.Price).To(0).When(NotInRole("Premium"));

        // Clear an internal-only collection
        Clear(x => x.DebugTrace).When(NotInRole("Internal"));

        // Strip hidden line items, then cap at 10
        RemoveItems<LineDto>(x => x.Lines)
            .Where(l => l.Hidden)
            .Always();
        Take<LineDto>(x => x.Lines).First(10).When(NotInRole("Premium"));

        // Recurse into a collection — each line item gets its own sub-filter
        ForEach(x => x.Lines, line =>
        {
            line.Nullify(l => l.UnitCost).Unless(_ => HasRole("Finance"));
            line.Truncate(l => l.Description).After(80).Always();
        });

        // Escape hatch for cross-property logic
        Apply((order, _) =>
        {
            if (order.Status == "Cancelled") order.PaymentDetails = null;
        }).Always();
    }

    private static bool HasRole(string role) => /* check current principal */ false;
    private static SyncPredicate<OrderDto> NotInRole(string role) => (_, _) => !HasRole(role);
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
| `IResponseFilterContext` | Per-request bag: `IServiceProvider`, `CancellationToken`, `Items` for memoizing async work. |
| `IResponseFilterPipeline` | Walks the object graph depth-first and applies all matching filters. |
| `IResponseFilterRegistry` | Resolves filters per type from DI. |

### Rule builders

#### Property mutators

| Builder | Purpose | Example |
| --- | --- | --- |
| `Nullify(...)` | Set one or more nullable properties to `null`. | `Nullify(x => x.Cost, x => x.Notes).When(...)` |
| `SetValue(...).To(...)` | Set a property to a constant or computed value. | `SetValue(x => x.Status).To("hidden").When(...)` |
| `SetToDefault(...)` | Reset properties to `default(TProperty)` — handles nullable, non-nullable value types, and reference types in one call. | `SetToDefault(x => x.Cost, x => x.IsActive, x => x.Notes).When(...)` |
| `Replace(...).With(...)` | Synonym for `SetValue` (reads better when there's an existing value). | `Replace(x => x.Email).With("***").When(...)` |
| `Transform(...).Using(...)` | Map a property through a pure function. | `Transform(x => x.Notes).Using(s => s?.ToUpper()).Always()` |
| `Clear(...)` | Empty a property: string → `""`, mutable list → in-place `.Clear()`, array → empty array, else → null. | `Clear(x => x.Lines).When(...)` |

#### String operations

| Builder | Purpose | Example |
| --- | --- | --- |
| `Mask(...)` | String masking with `KeepFirst(n)`, `KeepLast(n)`, `With(char)`, `WithPattern(string)`. | `Mask(x => x.Card).KeepFirst(4).KeepLast(4).When(...)` |
| `Truncate(...).After(n)` | Cut strings at N chars, optionally with suffix. | `Truncate(x => x.Notes).After(200, "…").Always()` |
| `Hash(...)` | Replace string with a hash. Defaults to SHA-256 hex; `.AsSha1()`, `.AsSha512()`, `.AsMd5()`, or `.Using(fn)`. | `Hash(x => x.Token).AsSha256().When(...)` |

#### Numeric operations

| Builder | Purpose | Example |
| --- | --- | --- |
| `Round(...).To(n)` | Round `decimal`/`double`/`float`. Choose midpoint rule with `.To(n, mode)`. `.ToInteger()` for whole numbers. | `Round(x => x.Price).To(2).Always()` |

#### Collection operations

| Builder | Purpose | Example |
| --- | --- | --- |
| `ForEach(...)` | Recurse into a collection property; configure a sub-filter inline. | `ForEach(x => x.Lines, line => line.Nullify(l => l.Cost).When(...))` |
| `RemoveItems(...).Where(pred)` | Remove items matching the predicate. Mutates `IList<T>` in place; rebuilds arrays. | `RemoveItems<Line>(x => x.Lines).Where(l => l.IsHidden).When(...)` |
| `KeepOnly(...).Where(pred)` | Inverse of `RemoveItems` — keep matching items, drop the rest. | `KeepOnly<Line>(x => x.Lines).Where(l => l.IsPublic).When(...)` |
| `Take(...).First(n)` / `.Last(n)` | Limit a collection to the first/last N items. | `Take<Line>(x => x.Lines).First(10).When(...)` |

#### Escape hatch

| Builder | Purpose | Example |
| --- | --- | --- |
| `Apply(...)` / `ApplyAsync(...)` | Arbitrary `Action`/`Func<…, Task>` on the instance for anything the structured builders don't cover. | `Apply((dto, ctx) => dto.Status = "redacted").When(...)` |

### Predicate vocabulary

All builders end with the same terminal vocabulary. Each terminal accepts predicates in **every shape**
(no-arg, context-only, instance-only, or both), in both sync and async (Task) variants. Pick the
overload that reads best at the call site — the library adapts it to the canonical
`AsyncPredicate<T>` internally.

| Terminal | Fires when … |
| --- | --- |
| `.When(predicate)` | predicate returns `true` |
| `.Unless(predicate)` | predicate returns `false` |
| `.Always()` | unconditional |
| `.WhenAll(p1, p2, …)` | all `AsyncPredicate<T>` predicates true (short-circuits on first false) |
| `.WhenAny(p1, p2, …)` | at least one `AsyncPredicate<T>` predicate true (short-circuits on first true) |

#### Supported predicate shapes (each on `When` and `Unless`)

| Shape | Use case |
| --- | --- |
| `Func<bool>` | Feature flag, constant. `.When(() => Config.HideCost)` |
| `Func<Task<bool>>` | Async no-arg signal. `.When(async () => await CheckExternalAsync())` |
| `Func<IResponseFilterContext, bool>` | Context-only sync check. `.When(ctx => ctx.Items["env"] == "prod")` |
| `Func<IResponseFilterContext, Task<bool>>` | **Context-only async**, ideal for DI-resolved permission checks. `.When(async ctx => !await ctx.Services.GetRequiredService<IPermissionChecker>().IsGrantedAsync("…"))` |
| `Func<T, bool>` | Pure instance check. `.When(o => o.IsPublic)` |
| `Func<T, Task<bool>>` | Instance check that touches IO. `.When(async o => await IsAllowedAsync(o.Id))` |
| `SyncPredicate<T>` (= `Func<T, ctx, bool>`) | Canonical sync. `.When((o, ctx) => …)` |
| `AsyncPredicate<T>` (= `Func<T, ctx, ValueTask<bool>>`) | Canonical async. The shape `WhenAll`/`WhenAny` consume directly. |

### Extending the vocabulary

Every builder implements `IRuleBuilder<T>`, so consumer projects can plug in their own domain-specific
terminals as ordinary extension methods — without having to wire them per builder type:

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

Then at the call site:

```csharp
Nullify(x => x.TotalCost).WhenMissingPermission("Insights.ViewFinancial");
```

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
* `TypeReachabilityCache` precomputes per response root type whether any registered filter's target is reachable in the graph. When the answer is no, the pipeline is a one-cache-lookup no-op for that response — no reflection, no walk, no allocation.
* The walker also short-circuits per branch: if a nested property's static type can't reach a filtered type, that subtree is skipped entirely.
* Cycle detection via `ReferenceEqualityComparer` prevents infinite recursion on back-references.

## Configuration

`AddResponseFilters` / `AddNextendedResponseFilters` accept an optional `Action<ResponseFilterOptions>`:

```csharp
builder.Services.AddNextendedResponseFilters(
    assemblies: new[] { typeof(OrderResponseFilter).Assembly },
    configure: opts =>
    {
        // Default: propagate exceptions thrown by filter rules to the host's exception handler.
        // Switch to LogAndContinue if you'd rather absorb filter bugs at the cost of visibility.
        opts.ExceptionBehavior = FilterExceptionBehavior.Rethrow;

        // Default: skip the entire pipeline if no registered filter's target type is reachable
        // in the response's type graph. Turn off only if you have run-time polymorphism that
        // the static analyzer can't see (e.g. List<object> holding heterogeneous DTOs).
        opts.SkipUnaffectedResponses = true;

        // Custom opt-out predicate evaluated against the response root type.
        opts.SkipResponseType = t => typeof(System.IO.Stream).IsAssignableFrom(t)
                                  || t.Namespace?.StartsWith("Volo.Abp") == true;
    });
```

## Exception handling

By default, exceptions thrown inside a filter rule **propagate** — they reach the host's global exception handler unchanged. This is the right behaviour for almost every app: a filter throwing a `BusinessException` or `UserFriendlyException` is intentional and must be visible.

If you'd rather absorb filter failures (e.g. for a public CMS that must never 500), switch to `FilterExceptionBehavior.LogAndContinue` — exceptions are caught, logged via `ILogger<ResponseFilterPipeline>`, and remaining filters keep running.

`OperationCanceledException` **always propagates** regardless of the chosen behaviour, so request aborts and host shutdown work correctly.

## Supported Frameworks

- .NET 8.0, .NET 9.0, .NET 10.0

## License

GPL-3.0-or-later (same as the rest of the Nextended ecosystem).
