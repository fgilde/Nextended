---
title: Nextended.ResponseFilters
---
# Nextended.ResponseFilters

📚 **[Full API reference](/projects/responsefilters-api)** — every public type and member, generated from the compiled assembly.

A fluent, provider-agnostic pipeline that redacts, masks, rounds, truncates, hashes, prunes and
restructures response DTOs before serialization — per request, per user, per permission.
[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.svg)](https://www.nuget.org/packages/Nextended.ResponseFilters/)
[![NuGet](https://img.shields.io/nuget/v/Nextended.ResponseFilters.AspNetCore.svg?label=AspNetCore)](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/responsefilters)

---

## The problem

A DTO is shaped for the endpoint, not for the caller. The same `OrderDto` must show the cost
breakdown to someone in Finance, hide it from a customer, mask the card number for everybody except
an admin, and drop internal line items entirely for an anonymous caller.

The three usual answers all have a cost:

| Approach | Cost |
| --- | --- |
| One DTO per audience | Combinatorial explosion; mapping code multiplies |
| `[JsonIgnore]`-style attributes | Static — cannot depend on the current user, and impossible on a DTO from a third-party library |
| Manual clean-up in the controller | Scattered, untested, forgotten on the second endpoint that returns the same type |

`Nextended.ResponseFilters` moves the decision into one declarative class per DTO. A
`ResponseFilter<T>` reads like a `FluentValidator<T>` — except that instead of validating, it
**mutates** the object graph on its way out.

## Architecture

```
Controller returns ObjectResult(value)
        │
        ▼
ResponseFilterResultFilter                (Nextended.ResponseFilters.AspNetCore)
        │   ShouldHandle(request, type)?   ── no ──▶ pass through untouched
        ▼
IResponseFilterPipeline.ProcessAsync
        │   SkipResponseType(type)?        ── yes ─▶ return
        │   reachability cache says no
        │   registered filter can match?   ── no ──▶ return
        ▼
depth-first walk of the object graph (cycle-safe, ReferenceEqualityComparer)
        │
        ├─ per visited node: IResponseFilterRegistry resolves filters for its runtime type
        ├─ value rules mutate the instance in place        (Nullify, Mask, Round, …)
        └─ structural rules record into StructuralEditBook (Remove, Rename, AddProperty, …)
        │
        ▼
StructuralEdits.HasAny?
        │   yes ─▶ JsonStructuralTransformer.Transform(value, edits, jsonOptions)
        │          ObjectResult.Value = JsonNode, DeclaredType = null
        ▼
MVC formatter serializes
```

Two kinds of rule exist because they can't work the same way. A **value mutation** (`Nullify`,
`Mask`, `Round`) can be applied to the POCO directly. A **structural change** (`Remove`, `Rename`,
`AddProperty`) cannot — a CLR object can't drop or rename a property at runtime. Those are recorded
per instance in a `StructuralEditBook` and replayed against the serialized JSON tree.

### Core types

| Type | Role |
| --- | --- |
| `ResponseFilter<T>` | Abstract base class. Inherit and configure rules in the constructor. |
| `InlineFilter<T>` | Concrete filter for `ForEach` sub-filters and for ad-hoc filters built at runtime (tests). |
| `IResponseFilterContext` | Per-run context: `Services`, `CancellationToken`, `Items` scratch bag, `StructuralEdits`. |
| `IResponseFilterPipeline` | Walks the graph depth-first and dispatches matching filters. |
| `IResponseFilterRegistry` | Resolves the filters registered for a given type from DI. |
| `ResponseFilterOptions` | Pipeline-wide configuration. |
| `StructuralEdit` / `StructuralEditBook` | The ledger of key-level changes, keyed by instance identity. |
| `JsonStructuralTransformer` | Replays the ledger against a `JsonNode` tree. |
| `SyncPredicate<T>` / `AsyncPredicate<T>` | `Func<T, ctx, bool>` and `Func<T, ctx, ValueTask<bool>>`. |

## Installation

```bash
# provider-agnostic core
dotnet add package Nextended.ResponseFilters

# ASP.NET Core adapter (pulls the core transitively)
dotnet add package Nextended.ResponseFilters.AspNetCore
```

## Registration

```csharp
using Nextended.ResponseFilters;
using Nextended.ResponseFilters.AspNetCore;

builder.Services.AddNextendedResponseFilters(
    assemblies: [typeof(OrderResponseFilter).Assembly],
    lifetime: ServiceLifetime.Scoped,
    configure: options =>
    {
        options.ExceptionBehavior = FilterExceptionBehavior.Rethrow;
        options.SkipUnaffectedResponses = true;
        options.SkipResponseType = t => typeof(Stream).IsAssignableFrom(t);
        options.ShouldHandle = (request, type) =>
            Task.FromResult(request.Path.StartsWithSegments("/api/app"));
    });
```

Filters are discovered by scanning the given assemblies for `ResponseFilter<T>` implementations; the
calling assembly is used when none are supplied. Because they are registered as **scoped** by
default, a filter can take scoped dependencies in its constructor.

Without ASP.NET Core, register the core only and drive the pipeline yourself:

```csharp
using Nextended.ResponseFilters.Extensions;

services.AddResponseFilters([typeof(OrderResponseFilter).Assembly]);

// later
await pipeline.ProcessAsync(dto, context);
```

`AddResponseFilter<TFilter>()` registers a single filter — useful in tests and for filters built at
runtime.

## A complete filter

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nextended.ResponseFilters;

public class OrderResponseFilter : ResponseFilter<OrderDto>
{
    public OrderResponseFilter()
    {
        // ---- value mutations -------------------------------------------------
        Nullify(x => x.TotalCost, x => x.UnitCost).When(NotInRole("Finance"));

        Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).When(NotInRole("Admin"));
        Mask(x => x.CustomerEmail).WithPattern("***@***.***")
            .When(async ctx => !await ctx.IsAuthenticatedAsync());

        Truncate(x => x.Notes).After(200, "…").Always();
        Hash(x => x.AuditToken).AsSha256().Always();
        Round(x => x.Price).To(2).Always();
        Round(x => x.Score).ToInteger().When(NotInRole("Premium"));
        Clear(x => x.DebugTrace).When(NotInRole("Internal"));
        SetToDefault(x => x.InternalScore, x => x.IsBookmarked, x => x.HiddenTags)
            .When(NotInRole("Internal"));
        SetValue(x => x.Status).To("redacted").When(NotInRole("Support"));

        // ---- collections -----------------------------------------------------
        RemoveItems<LineDto>(x => x.Lines).Where(l => l.Hidden).Always();
        KeepOnly<LineDto>(x => x.Attachments).Where(a => a.IsPublic).When(NotInRole("Internal"));
        Take<LineDto>(x => x.Lines).First(10).When(NotInRole("Premium"));

        ForEach(x => x.Lines, line =>
        {
            line.Nullify(l => l.UnitCost).When(NotInRole("Finance"));
            line.Truncate(l => l.Description).After(80).Always();
        });

        // ---- structural (key-level) ------------------------------------------
        Remove(x => x.InternalRef, x => x.DebugInfo).When(NotInRole("Internal"));
        Rename(x => x.Id).To("orderId").Always();
        AddProperty("displayName").From(o => $"#{o.Number} — {o.CustomerName}").Always();

        // ---- metadata-driven -------------------------------------------------
        PropertiesWhere(p => p.GetCustomAttribute<SecretAttribute>() is not null)
            .Remove().When(NotInRole("Admin"));

        // ---- escape hatch ----------------------------------------------------
        Apply((order, _) =>
        {
            if (order.Status == "Cancelled") order.PaymentDetails = null;
        }).Always();
    }

    private static SyncPredicate<OrderDto> NotInRole(string role) =>
        (_, ctx) => !ctx.Services.GetRequiredService<ICurrentUser>().IsInRole(role);
}
```

## Rule reference

Every builder is opened by a method on `ResponseFilter<T>`, optionally refined, and closed by a
terminal from the [predicate vocabulary](#predicate-vocabulary). A rule that is never closed with a
terminal is **never registered** — that is the most common mistake.

### Property mutators

| Builder | Effect | Example |
| --- | --- | --- |
| `Nullify(...)` | Sets one or more nullable properties to `null`. Accepts several selectors in one call. | `Nullify(x => x.Cost, x => x.Notes).When(...)` |
| `SetValue(...).To(...)` | Sets a property to a constant, or to a value computed from the instance and/or context. | `SetValue(x => x.Status).To("hidden").When(...)` |
| `Replace(...).With(...)` | Same as `SetValue`, phrased for when a value already exists. | `Replace(x => x.Email).With("***").When(...)` |
| `SetToDefault(...)` | Resets properties to `default(TProperty)` — handles nullable, non-nullable value types and reference types in one call with mixed types. | `SetToDefault(x => x.Cost, x => x.IsActive).When(...)` |
| `Transform(...).Using(...)` | Maps the current value through a function. Overloads give access to the instance and the context. | `Transform(x => x.Notes).Using(s => s?.ToUpper()).Always()` |
| `Clear(...)` | Empties a property: `string` → `""`, mutable `IList` → in-place `Clear()`, array → empty array, anything else → `null`. | `Clear(x => x.Lines).When(...)` |

### String operations

| Builder | Effect | Example |
| --- | --- | --- |
| `Mask(...)` | Masks a string. Refine with `KeepFirst(n)`, `KeepLast(n)`, `With(char)`, `WithPattern(string)`. Keep counts are capped at the string length. | `Mask(x => x.Card).KeepFirst(4).KeepLast(4).When(...)` |
| `Truncate(...).After(n[, suffix])` | Cuts after `n` characters, appending the suffix only when a cut actually happened. | `Truncate(x => x.Notes).After(200, "…").Always()` |
| `Hash(...)` | Replaces the string with a hash. `AsSha256()` (default), `AsSha1()`, `AsSha512()`, `AsMd5()`, or `Using(fn)` for your own. | `Hash(x => x.Token).AsSha512().When(...)` |

`WithPattern` ignores the `Keep*` settings and substitutes the whole value — use it when the shape of
the value itself is sensitive (`***@***.***` rather than `j***@e***.com`).

### Numeric operations

| Builder | Effect | Example |
| --- | --- | --- |
| `Round(...).To(n)` | Rounds to `n` decimals using banker's rounding (`MidpointRounding.ToEven`). | `Round(x => x.Price).To(2).Always()` |
| `Round(...).To(n, mode)` | Rounds with an explicit midpoint rule. | `Round(x => x.Price).To(2, MidpointRounding.AwayFromZero)` |
| `Round(...).ToInteger()` | Rounds to whole numbers. | `Round(x => x.Score).ToInteger().When(...)` |

The selector is constrained to `INumber<TSelf>`, so pointing `Round` at a `string` is a compile
error. At runtime, rounding is applied for `decimal`, `double` and `float`; integral types pass
through unchanged.

### Collection operations

| Builder | Effect | Example |
| --- | --- | --- |
| `ForEach(...)` | Recurses into a collection property and applies an inline sub-filter to every element. | `ForEach(x => x.Lines, l => l.Nullify(i => i.Cost).When(...))` |
| `RemoveItems(...).Where(pred)` | Removes matching items. Mutates `IList<T>` in place; rebuilds arrays. | `RemoveItems<Line>(x => x.Lines).Where(l => l.Hidden).Always()` |
| `KeepOnly(...).Where(pred)` | Inverse of `RemoveItems`. | `KeepOnly<Line>(x => x.Lines).Where(l => l.IsPublic).When(...)` |
| `Take(...).First(n)` / `.Last(n)` | Caps a collection at the first or last `n` items. | `Take<Line>(x => x.Lines).First(10).When(...)` |

The item predicate on `RemoveItems` / `KeepOnly` comes in sync, async and instance-only shapes, so an
item-level permission check can await.

### Structural operations

These change the **serialized keys**, which a POCO cannot express. They are recorded in
`context.StructuralEdits` and replayed at serialization time. With the ASP.NET Core adapter this is
automatic. When you drive the pipeline yourself, apply them explicitly:

```csharp
await pipeline.ProcessAsync(dto, context);

if (context.StructuralEdits.HasAny)
{
    JsonNode? node = JsonStructuralTransformer.Transform(dto, context.StructuralEdits, jsonOptions);
    return node;   // serialize this instead of the DTO
}
```

| Builder | Effect | Example |
| --- | --- | --- |
| `Remove(...)` | Drops properties from the output entirely — the key disappears, unlike `Nullify`, which keeps a `null`. | `Remove(x => x.Internal, x => x.Debug).When(...)` |
| `Rename(...).To(name)` | Renames a property's serialized key. | `Rename(x => x.Id).To("orderId").Always()` |
| `TransformKey(...).Using(fn)` | Transforms one property's serialized key. | `TransformKey(x => x.Id).Using(k => "x_" + k).Always()` |
| `TransformKeys().Using(fn)` | Transforms **every** property's key — e.g. forcing a naming convention on a single response. | `TransformKeys().Using(k => k.ToUpperInvariant()).When(...)` |
| `AddProperty(name).From(...)` / `.WithValue(...)` | Injects a key that does not exist on the CLR type, computed per instance and context or constant. | `AddProperty("displayName").From(o => $"#{o.Id}").Always()` |

Notes and limits:

- The key transform receives the **serialized** key — after any `JsonNamingPolicy` and
  `[JsonPropertyName]` — while edits resolve against the CLR member. Both keep working under any
  naming policy.
- Children are visited before an owner's own edits are applied, so a rename at one level never hides
  an edited subtree beneath it.
- Structural edits inside `ForEach` sub-filters apply per element.
- **Limitation:** values reached *only* through dictionary entries are not descended into for nested
  structural edits. Top-level, collection, array and complex-property graphs are. Value mutations are
  unaffected — they run earlier, in place.
- Setting `ObjectResult.DeclaredType = null` is required once the value is a `JsonNode`, otherwise the
  formatter tries to shape the node as the original DTO type. The adapter does this for you.

### Metadata-aware property selection

Property metadata is static, so both mechanisms below resolve **at build time** — non-matching
properties are dropped from the rule and cost nothing at runtime.

`.WhenProperty(p => …)` is available on **every** property-targeting builder. It narrows the
already-selected properties and composes with `When`/`Unless`. Chaining it is a logical AND.

```csharp
Nullify(x => x.A, x => x.B, x => x.C)
    .WhenProperty(p => p.GetCustomAttribute<SecretAttribute>() is not null)
    .When(NotInRole("Admin"));

Remove(x => x.Token).WhenProperty(p => p.PropertyType == typeof(string)).Always();
```

`Properties(...)` and `PropertiesWhere(...)` are the *transposed* entry points: pick the property set
first, then the operation (`.Nullify()`, `.Remove()`, `.SetToDefault()`, `.TransformKey()`).

```csharp
// Every [Secret] property, without enumerating them
PropertiesWhere(p => p.GetCustomAttribute<SecretAttribute>() is not null).Remove().Always();

// A named set
Properties(x => x.Name, x => x.Id).Nullify().When(...);
```

This is why there is no `NullifyWhere` / `RemoveWhere` / `SetToDefaultWhere` family: one
`WhenProperty` covers every operation, and `PropertiesWhere` exposes the type-agnostic operations from
one builder. `WhenProperty` has no effect on `Apply`, which targets no property.

### Escape hatch

| Builder | Effect |
| --- | --- |
| `Apply(Action<T, ctx>)` | Arbitrary synchronous mutation for logic the structured builders don't cover — typically several properties that must change together. |
| `ApplyAsync(Func<T, ctx, Task>)` | The async form. |

## Predicate vocabulary

Every builder closes with the same terminals, and each terminal accepts a predicate in **any** shape.
The library adapts all of them to the canonical `AsyncPredicate<T>` internally.

| Terminal | Fires when |
| --- | --- |
| `.When(predicate)` | the predicate returns `true` |
| `.Unless(predicate)` | the predicate returns `false` |
| `.Always()` | unconditionally |
| `.WhenAll(p1, p2, …)` | all predicates are true (short-circuits on the first false) |
| `.WhenAny(p1, p2, …)` | at least one predicate is true (short-circuits on the first true) |

`WhenAll` / `WhenAny` take `AsyncPredicate<T>` directly.

### Supported predicate shapes

Available on both `When` and `Unless`:

| Shape | Typical use |
| --- | --- |
| `Func<bool>` | Feature flag or constant — `.When(() => Config.HideCost)` |
| `Func<Task<bool>>` | Async no-arg signal — `.When(async () => await CheckExternalAsync())` |
| `Func<IResponseFilterContext, bool>` | Context-only sync check — `.When(ctx => ctx.Items["env"] as string == "prod")` |
| `Func<IResponseFilterContext, Task<bool>>` | Context-only async — the natural shape for a DI-resolved permission check |
| `Func<T, bool>` | Pure instance check — `.When(o => o.IsPublic)` |
| `Func<T, Task<bool>>` | Instance check that touches IO |
| `SyncPredicate<T>` | Canonical sync `(instance, ctx)` |
| `AsyncPredicate<T>` | Canonical async `(instance, ctx)` — what the combinators consume |

Three protected helpers on the base class make short predicates read better: `Always()`,
`WhenContext(ctx => …)` and `WhenInstance(instance => …)`.

### Extending the vocabulary

The intended pattern is a project-specific base class that names your domain's conditions once:

```csharp
public abstract class AppResponseFilter<T> : ResponseFilter<T> where T : class
{
    protected static AsyncPredicate<T> HasPermission(string name) =>
        async (_, ctx) => await ctx.Services
            .GetRequiredService<IPermissionChecker>()
            .IsGrantedAsync(name);

    protected static AsyncPredicate<T> LacksPermission(string name) =>
        async (instance, ctx) => !await HasPermission(name)(instance, ctx);

    protected static SyncPredicate<T> InRole(string role) =>
        (_, ctx) => ctx.Services.GetRequiredService<ICurrentUser>().IsInRole(role);
}
```

```csharp
public class InvoiceFilter : AppResponseFilter<InvoiceDto>
{
    public InvoiceFilter()
    {
        Nullify(x => x.Margin).When(LacksPermission("Invoices.SeeMargin"));
        Remove(x => x.InternalNotes).WhenAll(LacksPermission("Invoices.Internal"),
                                            LacksPermission("Admin"));
    }
}
```

### Memoizing expensive predicates

A permission check evaluated by ten rules on one response should run once. `context.Items` is the
scratch bag for exactly that:

```csharp
protected static AsyncPredicate<T> HasPermissionCached(string name) => async (_, ctx) =>
{
    var key = $"perm:{name}";
    if (ctx.Items.TryGetValue(key, out var cached) && cached is bool b) return b;

    var granted = await ctx.Services.GetRequiredService<IPermissionChecker>().IsGrantedAsync(name);
    ctx.Items[key] = granted;
    return granted;
};
```

`Items` is scoped to one pipeline run and is not thread-safe — rules are applied sequentially per
object, so no locking is needed.

## Configuration

`ResponseFilterOptions`:

| Option | Default | Purpose |
| --- | --- | --- |
| `ExceptionBehavior` | `Rethrow` | `Rethrow` lets a rule's exception reach the global exception handler unchanged — right for almost every app, since a `BusinessException` from a filter is still a domain error. `LogAndContinue` catches, logs via `ILogger<ResponseFilterPipeline>` and returns a partially filtered response. |
| `SkipUnaffectedResponses` | `true` | One-time reachability analysis per response root type. If no registered filter's target type is reachable in the type graph, the whole pipeline is skipped — no reflection, no walk. |
| `SkipResponseType` | `null` | Opt-out predicate on the root type, evaluated *before* the reachability check. |
| `ShouldHandle` | `null` | Per-request gate `(HttpRequest, Type) → Task<bool>`, evaluated by the ASP.NET Core adapter before any graph walk. `null` means "always handle". |

`OperationCanceledException` always propagates, in both exception modes, so request aborts and host
shutdown keep working.

### Scoping the pipeline to part of your API

```csharp
options.ShouldHandle = (request, type) =>
    Task.FromResult(request.Path.StartsWithSegments("/api/app"));
```

The predicate only runs for an `ObjectResult` with a non-null value, so `type` is always the runtime
CLR type of that value.

### Opting types out

```csharp
options.SkipResponseType = t =>
    typeof(Stream).IsAssignableFrom(t) ||
    t.Namespace?.StartsWith("Volo.Abp") == true;
```

### When to turn the reachability cache off

`SkipUnaffectedResponses` uses **static** type analysis. If your responses carry run-time
polymorphism the analyzer cannot see — a `List<object>` holding heterogeneous DTOs, for instance —
set it to `false`, otherwise those responses are skipped.

## Performance

- **Reachability short-circuit.** The type graph of each response root type is analysed once,
  process-wide, and cached. Responses that no filter can touch never enter the walk.
- **Top-level enumerables** use the element type for the reachability check, since a `List<T>` itself
  carries no filterable properties.
- **Cycle safety** via `ReferenceEqualityComparer` — a bidirectional navigation is visited once.
- **Build-time property filtering.** `WhenProperty` and `PropertiesWhere` resolve when the filter is
  constructed, not per request.
- **No JSON round trip unless needed.** `StructuralEditBook.HasAny` is false for the common case, so
  the tree transform is skipped entirely.
- **`ValueTask` throughout** the rule and predicate surface to avoid allocating for synchronous paths.

## Testing a filter

Filters are plain classes — no host, no HTTP.

```csharp
[Fact]
public async Task Nulls_cost_for_non_finance_users()
{
    var services = new ServiceCollection()
        .AddSingleton<ICurrentUser>(new FakeUser(roles: []))
        .BuildServiceProvider();

    var context = new TestFilterContext(services);      // your IResponseFilterContext stub
    var dto = new OrderDto { TotalCost = 42m };

    await new OrderResponseFilter().ApplyAsync(dto, context);

    Assert.Null(dto.TotalCost);
}
```

For structural rules, assert against the transformed tree:

```csharp
await new OrderResponseFilter().ApplyAsync(dto, context);

var node = JsonStructuralTransformer.Transform(dto, context.StructuralEdits);
Assert.Null(node!["internalRef"]);
Assert.Equal("#1001", node["displayName"]!.GetValue<string>());
```

`InlineFilter<T>` builds a filter without declaring a class, which keeps one-off test cases short:

```csharp
var filter = new InlineFilter<OrderDto>();
filter.Mask(x => x.CreditCard).KeepLast(4).Always();

await filter.ApplyAsync(dto, context);
```

See [`Tests/Nextended.ResponseFilters.Tests`](https://github.com/fgilde/Nextended/tree/main/Tests/Nextended.ResponseFilters.Tests)
for the suite that ships with the repository.

## Recipes

### GDPR-style export: hash instead of remove

```csharp
Hash(x => x.Email).AsSha256().When(NotInRole("DataProtection"));
Hash(x => x.NationalId).Using(v => $"anon-{v.GetHashCode():x8}").Always();
```

### Progressive disclosure by tier

```csharp
Take<LineDto>(x => x.Lines).First(3).When(OnTier("free"));
Take<LineDto>(x => x.Lines).First(50).When(OnTier("pro"));
Remove(x => x.Analytics).When(OnTier("free"));
```

### Forcing snake_case on one legacy endpoint

```csharp
TransformKeys().Using(k => string.Concat(
    k.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLower(c) : char.ToLower(c).ToString())))
    .Always();
```

### Redacting a third-party DTO you cannot annotate

```csharp
// No attribute access needed — the filter lives in your assembly.
public class StripeChargeFilter : ResponseFilter<Stripe.Charge>
{
    public StripeChargeFilter() => Remove(x => x.Source, x => x.Customer).Always();
}
```

## Comparison with attributes

| Use case | `[JsonIgnore]`-style attribute | `ResponseFilter<T>` |
| --- | --- | --- |
| Permission-based nulling | ✅ | ✅ |
| DTO from a third-party library | ❌ | ✅ |
| Masking rather than removing | ❌ | ✅ |
| Conditional on another property | ❌ | ✅ |
| Tenant- or user-context-aware | ❌ | ✅ |
| Renaming or adding keys per request | ❌ | ✅ |
| Unit-testable in isolation | ⚠️ | ✅ |
| Zero-cost when no rule applies | ✅ | ✅ (reachability cache) |

## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Related

- [Nextended.ResponseFilters.AspNetCore](responsefilters-aspnetcore.md) — the MVC adapter
- [Nextended.Web](web.md) — OData and controller helpers
- [Nextended.Core](core.md) — the foundation library

## Links

- 📦 [Nextended.ResponseFilters on NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters/)
- 📦 [Nextended.ResponseFilters.AspNetCore on NuGet](https://www.nuget.org/packages/Nextended.ResponseFilters.AspNetCore/)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.ResponseFilters)
- 🧪 [Tests](https://github.com/fgilde/Nextended/tree/main/Tests/Nextended.ResponseFilters.Tests)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)
