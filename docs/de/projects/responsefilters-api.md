---
title: Nextended.ResponseFilters — API-Referenz
---

# Nextended.ResponseFilters — API-Referenz

🇬🇧 [This page in English](/projects/responsefilters-api)

Die vollständige öffentliche Oberfläche von `Nextended.ResponseFilters`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/responsefilters)

## Nextended.ResponseFilters

### `AsyncPredicate<T>`

`delegate`

Async predicate used by rule builders.

**Konstruktoren**

- `AsyncPredicate(object object, IntPtr method)`

**Methoden**

- `BeginInvoke(T instance, IResponseFilterContext context, AsyncCallback callback, object object) : IAsyncResult`
- `EndInvoke(IAsyncResult result) : ValueTask<bool>`
- `Invoke(T instance, IResponseFilterContext context) : ValueTask<bool>`

### `FilterExceptionBehavior`

`enum`

How `ExceptionBehavior` shapes the pipeline's response to thrown filter rules.

**Werte**

- `LogAndContinue`
  <br>Catch exceptions thrown by filter rules, log them via `ILogger<ResponseFilterPipeline>`, and continue with remaining filters. The response is returned partially filtered. Use only in pipelines where filter robustness matters more than visibility (e.g. a public CMS that must never 500).
- `Rethrow`
  <br>Let exceptions propagate (default). This is the right choice for almost every app — a filter throwing a `BusinessException`, `UserFriendlyException`, or any other domain error should reach the framework's global exception handler unchanged.
- `value__`

### `InlineFilter<T>`

`class`

Concrete `ResponseFilter`1` used internally by `ForEach` sub-filters, also exposed for ad-hoc filters configured at runtime (e.g. in tests).

**Konstruktoren**

- `InlineFilter()`

**Methoden**

- `AddProperty(string name) : AddPropertyBuilder<T>`
- `Apply(Action<T, IResponseFilterContext> action) : ApplyBuilder<T>`
- `ApplyAsync(Func<T, IResponseFilterContext, Task> action) : ApplyBuilder<T>`
- `Clear<TProp>(Expression<Func<T, TProp>> selector) : ClearBuilder<T>`
- `ForEach<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector, Action<InlineFilter<TItem>> configure) : ResponseFilter<T>`
- `Hash(Expression<Func<T, string>> selector) : HashBuilder<T>`
- `KeepOnly<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector) : KeepOnlyBuilder<T, TItem>`
- `Mask(Expression<Func<T, string>> selector) : MaskBuilder<T>`
- `Nullify<TProp>(Expression<Func<T, TProp>>[] selectors) : NullifyBuilder<T>`
- `Properties(Expression<Func<T, object>>[] selectors) : PropertySetBuilder<T>`
- `PropertiesWhere(Func<PropertyInfo, bool> predicate) : PropertySetBuilder<T>`
- `Remove(Expression<Func<T, object>>[] selectors) : RemoveBuilder<T>`
- `RemoveItems<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector) : RemoveItemsBuilder<T, TItem>`
- `Rename<TProp>(Expression<Func<T, TProp>> selector) : RenameBuilder<T>`
- `Replace<TProp>(Expression<Func<T, TProp>> selector) : ReplaceBuilder<T, TProp>`
- `Round<TNum>(Expression<Func<T, TNum>> selector) : RoundBuilder<T>`
- `Round<TNum>(Expression<Func<T, TNum?>> selector) : RoundBuilder<T>`
- `SetToDefault(Expression<Func<T, object>>[] selectors) : SetToDefaultBuilder<T>`
- `SetValue<TProp>(Expression<Func<T, TProp>> selector) : SetValueBuilder<T, TProp>`
- `Take<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector) : TakeBuilder<T, TItem>`
- `Transform<TProp>(Expression<Func<T, TProp>> selector) : TransformBuilder<T, TProp>`
- `TransformKey<TProp>(Expression<Func<T, TProp>> selector) : TransformKeyBuilder<T>`
- `TransformKeys() : TransformKeyBuilder<T>`
- `Truncate(Expression<Func<T, string>> selector) : TruncateBuilder<T>`

### `IResponseFilter`

`interface`

Non-generic marker for filters keyed by `TargetType`. Implemented by `ResponseFilter`1`; consumers typically don't implement this directly.

**Methoden**

- `ApplyAsync(object instance, IResponseFilterContext context) : ValueTask`
  <br>Apply all configured rules to `instance`. Implementations MUST tolerate `instance` being of a derived type or null-safe assignable.

**Eigenschaften**

- `TargetType : Type { get; }`
  <br>The exact DTO type this filter applies to (no inheritance walking).

### `IResponseFilterContext`

`interface`

Per-pipeline context handed to every predicate and rule. Hosts the service provider, cancellation, and a scratch bag for memoizing async values (e.g. permission checks) so repeated predicates within the same response don't re-fetch.

**Eigenschaften**

- `CancellationToken : CancellationToken { get; }`
  <br>Cancellation token bound to the host request/scope.
- `Items : IDictionary<string, object> { get; }`
  <br>Free-form bag for transporting arbitrary state between rules of the same pipeline run (e.g. an authenticated user object, a tenant id, a cached permission map). Not thread-safe; rules are applied sequentially per object.
- `Services : IServiceProvider { get; }`
  <br>DI container scope for the current request.
- `StructuralEdits : StructuralEditBook { get; }`
  <br>Ledger of structural edits (remove / rename / key-transform / add) recorded by structural rules. A POCO can't drop or rename a property at runtime, so these are collected here and replayed against the serialized JSON tree by the serialization layer (e.g. the ASP.NET Core adapter).

### `IResponseFilterRule<T>`

`interface`

Single rule attached to a `ResponseFilter`1`.

**Methoden**

- `ApplyAsync(T instance, IResponseFilterContext context) : ValueTask`

### `ResponseFilter<T>`

`abstract class`

Base class for declarative response filters. Inherit and configure rules in the constructor via the protected fluent builders (`Nullify``1`, `Replace``1`, `Transform``1`, `ForEach``1`).

**Methoden**

- `ApplyAsync(T instance, IResponseFilterContext context) : ValueTask`
- `ApplyAsync(object instance, IResponseFilterContext context) : ValueTask`

**Eigenschaften**

- `TargetType : Type { get; }`

### `ResponseFilterContext`

`class`

Default `IResponseFilterContext`.

**Konstruktoren**

- `ResponseFilterContext(IServiceProvider services, CancellationToken cancellationToken = null)`

**Eigenschaften**

- `CancellationToken : CancellationToken { get; }`
- `Items : IDictionary<string, object> { get; }`
- `Services : IServiceProvider { get; }`
- `StructuralEdits : StructuralEditBook { get; }`

### `ResponseFilterOptions`

`class`

Pipeline-wide options. Configure once at registration time via `services.AddResponseFilters(..., configure: o => { … })`.

**Konstruktoren**

- `ResponseFilterOptions()`

**Eigenschaften**

- `ExceptionBehavior : FilterExceptionBehavior { get; set; }`
  <br>How the pipeline reacts when a filter rule throws. Default: `Rethrow` — surface bugs early.
- `SkipResponseType : Func<Type, bool> { get; set; }`
  <br>Optional opt-out predicate. When set and returns `true` for the response root type, the pipeline is skipped for that response (evaluated before `SkipUnaffectedResponses`).
- `SkipUnaffectedResponses : bool { get; set; }`
  <br>When `true` (default), the pipeline performs a one-time reachability analysis per response root type. If no registered filter's target type is reachable in the type graph, the entire pipeline is skipped — no reflection, no graph walk.

### `StructuralEdit`

`class`

A structural change to apply to an object's serialized representation. Unlike value mutators (`Nullify`, `Mask`, …) which mutate the DTO in place, structural edits cannot be expressed on a strongly-typed POCO — a property can't be removed or its key renamed at runtime. They are therefore recorded per-instance in the `StructuralEditBook` and applied at serialization time (see `JsonStructuralTransformer`).

**Methoden**

- `AddProperty(string name, object value) : StructuralEdit`
  <br>Inject a new key `name` with the given `value`.
- `Remove(string propertyName) : StructuralEdit`
  <br>Drop `propertyName` from the output.
- `Rename(string propertyName, string newName) : StructuralEdit`
  <br>Rename `propertyName`'s serialized key to `newName`.
- `TransformKey(string propertyName, Func<string, string> keyTransform) : StructuralEdit`

**Eigenschaften**

- `KeyTransform : Func<string, string> { get; }`
  <br>For `TransformKey`: maps the current serialized key to the new one.
- `Kind : StructuralEditKind { get; }`
  <br>What this edit does.
- `NewName : string { get; }`
  <br>For `Rename` the new serialized key; for `AddProperty` the key of the injected property.
- `PropertyName : string { get; }`
  <br>The CLR property name the edit targets (for `Remove`, `Rename`, `TransformKey`). The transformer resolves this to the actual serialized JSON key. `null` for `AddProperty`.
- `Value : object { get; }`
  <br>For `AddProperty`: the already-computed value to serialize.

### `StructuralEditBook`

`class`

Per-pipeline-run ledger of `StructuralEdit`s, keyed by the object instance the edit applies to (reference identity). Structural rules record into it while the pipeline walks the graph; the serialization layer replays it against the produced JSON tree.

**Konstruktoren**

- `StructuralEditBook()`

**Methoden**

- `ForOwner(object owner) : IReadOnlyList<StructuralEdit>`
  <br>The edits recorded for `owner`, or `null` when there are none.
- `Record(object owner, StructuralEdit edit) : void`
  <br>Record an edit against `owner`. No-op when `owner` is null.

**Eigenschaften**

- `HasAny : bool { get; }`
  <br>True when at least one edit has been recorded — lets the host skip the JSON transform entirely.

### `StructuralEditKind`

`enum`

The kind of structural change a `StructuralEdit` describes.

**Werte**

- `AddProperty`
  <br>Inject an additional key/value pair that does not exist on the CLR type.
- `Remove`
  <br>Drop a property entirely so it no longer appears in the serialized output.
- `Rename`
  <br>Rename a property's serialized key to a fixed name.
- `TransformKey`
  <br>Transform a property's serialized key through a function.
- `value__`

### `SyncPredicate<T>`

`delegate`

Sync predicate used by rule builders.

**Konstruktoren**

- `SyncPredicate(object object, IntPtr method)`

**Methoden**

- `BeginInvoke(T instance, IResponseFilterContext context, AsyncCallback callback, object object) : IAsyncResult`
- `EndInvoke(IAsyncResult result) : bool`
- `Invoke(T instance, IResponseFilterContext context) : bool`

## Nextended.ResponseFilters.Builders

### `AddPropertyBuilder<T>`

`class`

Two-step builder that injects an extra key into the serialized output — a key that does not exist on the CLR type. First specify the value via `From(...)`/`WithValue(...)`, then close with the predicate vocabulary.

**Methoden**

- `From(Func<T, IResponseFilterContext, object> valueFactory) : AddPropertyTerminal<T>`
- `From(Func<T, object> valueFactory) : AddPropertyTerminal<T>`
- `WithValue(object value) : AddPropertyTerminal<T>`
  <br>Inject a constant value.

### `AddPropertyTerminal<T>`

`class`

Terminal phase of an `AddProperty` rule — applies the predicate vocabulary.

### `ApplyBuilder<T>`

`class`

Builder for the catch-all `Apply` rule: runs an arbitrary `Action` on the instance when the predicate matches.

### `ClearBuilder<T>`

`class`

Sets a property to its "empty" state: `String` → `Empty``IList` with `IsReadOnly = false` → in-place `.Clear()`Arrays → new zero-length array of the element typeAnything else → `null` (logged warning at pipeline level if assignment fails)

### `HashBuilder<T>`

`class`

Builder that replaces a `String` property with a hash of its current value. Default algorithm is SHA-256, emitted as lowercase hex.

**Methoden**

- `AsMd5() : HashBuilder<T>`
- `AsSha1() : HashBuilder<T>`
- `AsSha256() : HashBuilder<T>`
- `AsSha512() : HashBuilder<T>`
- `Using(Func<string, string> hasher) : HashBuilder<T>`

### `IRuleBuilder<T>`

`interface`

Marker interface for every fluent rule builder produced by `ResponseFilter`1`.

**Methoden**

- `When(AsyncPredicate<T> predicate) : ResponseFilter<T>`

### `KeepOnlyBuilder<T, TItem>`

`class`

Mirror of `RemoveItemsBuilder`2` with inverted semantics: items matching the predicate are kept; everything else is removed.

**Methoden**

- `Where(AsyncPredicate<TItem> itemPredicate) : RemoveItemsTerminal<T, TItem>`
- `Where(Func<TItem, bool> itemPredicate) : RemoveItemsTerminal<T, TItem>`
- `Where(SyncPredicate<TItem> itemPredicate) : RemoveItemsTerminal<T, TItem>`

### `MaskBuilder<T>`

`class`

Builder for masking `String` properties.

**Methoden**

- `KeepFirst(int count) : MaskBuilder<T>`
  <br>Keep the first `count` characters visible. Capped at the string length.
- `KeepLast(int count) : MaskBuilder<T>`
  <br>Keep the last `count` characters visible. Capped at the string length.
- `With(Char maskChar) : MaskBuilder<T>`
  <br>Use a different mask character (default: `'*'`).
- `WithPattern(string pattern) : MaskBuilder<T>`
  <br>Replace the whole value with a fixed pattern (ignores Keep* settings).

### `NullifyBuilder<T>`

`class`

Builder for "set property to null when predicate matches" rules.

### `PropertySetBuilder<T>`

`class`

Transposed entry point: select a set of properties first (by name via `Properties`, or by metadata via `PropertiesWhere`), then choose a type-agnostic operation to apply to all of them. Every operation returns the same builder the direct API returns, so the full terminal vocabulary (`When`/`Unless`/`Always`/`WhenProperty`) stays available.

**Methoden**

- `Nullify() : NullifyBuilder<T>`
- `Remove() : RemoveBuilder<T>`
- `SetToDefault() : SetToDefaultBuilder<T>`
- `TransformKey() : TransformKeyBuilder<T>`

### `RemoveBuilder<T>`

`class`

Builder for "drop one or more properties from the serialized output when the predicate matches" rules.

### `RemoveItemsBuilder<T, TItem>`

`class`

Two-step builder: first specify the per-item predicate via `Where(...)`, then close with the standard predicate vocabulary (`When/Unless/Always/...`).

**Methoden**

- `Where(AsyncPredicate<TItem> itemPredicate) : RemoveItemsTerminal<T, TItem>`
- `Where(Func<TItem, bool> itemPredicate) : RemoveItemsTerminal<T, TItem>`
- `Where(SyncPredicate<TItem> itemPredicate) : RemoveItemsTerminal<T, TItem>`

### `RemoveItemsTerminal<T, TItem>`

`class`

Terminal phase of a `RemoveItems` rule.

### `RenameBuilder<T>`

`class`

Two-step builder: first specify the new key via `To(...)`, then close with the standard predicate vocabulary (`When/Unless/Always/...`).

**Methoden**

- `To(string newName) : RenameTerminal<T>`
  <br>Rename the property's serialized key to `newName`.

### `RenameTerminal<T>`

`class`

Terminal phase of a `Rename` rule — applies the predicate vocabulary.

### `ReplaceBuilder<T, TProp>`

`class`

Two-step builder: first specify the replacement value via `With(...)`, then close with `When(...)`/`Unless(...)`/`Always()`.

**Methoden**

- `With(Func<T, IResponseFilterContext, TProp> valueFactory) : ReplaceTerminal<T, TProp>`
- `With(Func<T, TProp> valueFactory) : ReplaceTerminal<T, TProp>`
- `With(TProp value) : ReplaceTerminal<T, TProp>`

### `ReplaceTerminal<T, TProp>`

`class`

Terminal phase of a `Replace` rule — applies the predicate vocabulary.

### `RoundBuilder<T>`

`class`

Two-step builder: first specify the precision via `To(n)`, then close with the standard predicate vocabulary.

**Methoden**

- `To(int decimals) : RoundTerminal<T>`
  <br>Round to `decimals` places using `ToEven` (banker's rounding).
- `To(int decimals, MidpointRounding mode) : RoundTerminal<T>`
  <br>Round to `decimals` places with an explicit midpoint rule.
- `ToInteger() : RoundTerminal<T>`

### `RoundTerminal<T>`

`class`

Terminal phase of a `Round` rule.

### `RuleBuilderBase<TBuilder, T>`

`abstract class`

Common terminal vocabulary (`When`, `Unless`, `Always`, `WhenAll`, `WhenAny`) shared by all rule builders. Materializes the rule and registers it on the owning filter when a terminal is called.

**Methoden**

- `Always() : ResponseFilter<T>`
- `Unless(AsyncPredicate<T> predicate) : ResponseFilter<T>`
- `Unless(Func<IResponseFilterContext, Task<bool>> predicate) : ResponseFilter<T>`
- `Unless(Func<IResponseFilterContext, bool> predicate) : ResponseFilter<T>`
- `Unless(Func<T, Task<bool>> predicate) : ResponseFilter<T>`
- `Unless(Func<T, bool> predicate) : ResponseFilter<T>`
- `Unless(Func<Task<bool>> predicate) : ResponseFilter<T>`
- `Unless(Func<bool> predicate) : ResponseFilter<T>`
- `Unless(SyncPredicate<T> predicate) : ResponseFilter<T>`
- `When(AsyncPredicate<T> predicate) : ResponseFilter<T>`
- `When(Func<IResponseFilterContext, Task<bool>> predicate) : ResponseFilter<T>`
- `When(Func<IResponseFilterContext, bool> predicate) : ResponseFilter<T>`
- `When(Func<T, Task<bool>> predicate) : ResponseFilter<T>`
- `When(Func<T, bool> predicate) : ResponseFilter<T>`
- `When(Func<Task<bool>> predicate) : ResponseFilter<T>`
- `When(Func<bool> predicate) : ResponseFilter<T>`
- `When(SyncPredicate<T> predicate) : ResponseFilter<T>`
- `WhenAll(AsyncPredicate<T>[] predicates) : ResponseFilter<T>`
- `WhenAny(AsyncPredicate<T>[] predicates) : ResponseFilter<T>`
- `WhenProperty(Func<PropertyInfo, bool> predicate) : TBuilder`

### `SetToDefaultBuilder<T>`

`class`

Builder for "reset properties to their `default(TProperty)`" rules.

### `SetValueBuilder<T, TProp>`

`class`

Two-step builder: first specify the value via `To(...)`, then close with `When(...)`/`Unless(...)`/`Always()`.

**Methoden**

- `To(Func<T, IResponseFilterContext, TProp> valueFactory) : SetValueTerminal<T, TProp>`
- `To(Func<T, TProp> valueFactory) : SetValueTerminal<T, TProp>`
- `To(TProp value) : SetValueTerminal<T, TProp>`

### `SetValueTerminal<T, TProp>`

`class`

Terminal phase of a `SetValue` rule — applies the predicate vocabulary.

### `TakeBuilder<T, TItem>`

`class`

Two-step builder for limiting a collection property to the first `N` elements (or the last `N`).

**Methoden**

- `First(int count) : TakeTerminal<T, TItem>`
  <br>Keep only the first `count` items.
- `Last(int count) : TakeTerminal<T, TItem>`
  <br>Keep only the last `count` items.

### `TakeTerminal<T, TItem>`

`class`

Terminal phase of a `Take` rule.

### `TransformBuilder<T, TProp>`

`class`

Two-step builder: first specify the transform via `Using(...)`, then close with `When(...)`/`Unless(...)`/`Always()`.

**Methoden**

- `Using(Func<T, TProp, IResponseFilterContext, TProp> transform) : TransformTerminal<T, TProp>`
- `Using(Func<T, TProp, TProp> transform) : TransformTerminal<T, TProp>`
- `Using(Func<TProp, TProp> transform) : TransformTerminal<T, TProp>`

### `TransformKeyBuilder<T>`

`class`

Two-step builder: first specify the key transform via `Using(...)`, then close with the standard predicate vocabulary. The transform receives the property's serialized key (i.e. after any `JsonNamingPolicy` / `[JsonPropertyName]`) and returns the new key.

**Methoden**

- `Using(Func<string, string> keyTransform) : TransformKeyTerminal<T>`

### `TransformKeyTerminal<T>`

`class`

Terminal phase of a `TransformKey`/`TransformKeys` rule — applies the predicate vocabulary.

### `TransformTerminal<T, TProp>`

`class`

Terminal phase of a `Transform` rule — applies the predicate vocabulary.

### `TruncateBuilder<T>`

`class`

Two-step builder: first specify the cutoff via `After(...)`, then close with `When(...)`/`Unless(...)`/`Always()`.

**Methoden**

- `After(int maxLength) : TruncateTerminal<T>`
  <br>Truncate after `maxLength` characters. No suffix appended.
- `After(int maxLength, string suffix) : TruncateTerminal<T>`
  <br>Truncate after `maxLength` and append `suffix` if a cut occurs.

### `TruncateTerminal<T>`

`class`

Terminal phase of a `Truncate` rule.

## Nextended.ResponseFilters.Extensions

### `ServiceCollectionExtensions`

`static class`

DI registration for Nextended.ResponseFilters.

**Extension Methods**

- `AddResponseFilter<TFilter>(this IServiceCollection services, ServiceLifetime lifetime = 1) : IServiceCollection`
  <br>Register a single filter manually (useful for tests or runtime-built filters).
- `AddResponseFilters(this IServiceCollection services, Assembly[] assemblies = null, ServiceLifetime lifetime = 1, Action<ResponseFilterOptions> configure = null) : IServiceCollection`

## Nextended.ResponseFilters.Json

### `JsonStructuralTransformer`

`static class`

Serializes an object graph to a `JsonNode` and replays the `StructuralEdit`s recorded in a `StructuralEditBook` against it — the only place a property can actually be removed, renamed, or have an extra key added, since a POCO can't express that at runtime.

**Methoden**

- `Transform(object root, StructuralEditBook edits, JsonSerializerOptions options = null) : JsonNode`
  <br>Serialize `root` and apply all edits from `edits`. Returns the resulting `JsonNode` (which may be `null` when `root` is null).

## Nextended.ResponseFilters.Pipeline

### `IResponseFilterPipeline`

`interface`

Entry point for filter execution. Walks an arbitrary object graph and applies all registered `IResponseFilter` instances whose target type matches a visited object.

**Methoden**

- `ProcessAsync(object root, IResponseFilterContext context) : ValueTask`

### `IResponseFilterRegistry`

`interface`

Look-up by target type. Implementations should resolve filters from DI per request (so filters can have scoped dependencies) and may cache the type-to-implementation map.

**Methoden**

- `GetFilters(Type type) : IReadOnlyList<IResponseFilter>`
  <br>All filters registered for `type`. Multiple filters per type are allowed and applied in registration order.
- `HasFilters(Type type) : bool`
  <br>True if any filter is registered for `type`.

### `ResponseFilterPipeline`

`class`

Default pipeline. Walks the response graph depth-first, dispatches matching filters per visited node, and (by default) lets exceptions propagate so domain errors reach the host's exception handler unchanged. Cycle-safe via `ReferenceEqualityComparer`.

**Methoden**

- `ProcessAsync(object root, IResponseFilterContext context) : ValueTask`

### `ResponseFilterRegistry`

`class`

Default registry: scoped, looks up `IResponseFilter` implementations via DI and caches the type → filter-implementations mapping for the lifetime of the host.

**Konstruktoren**

- `ResponseFilterRegistry(IServiceProvider services, ResponseFilterTypeMap typeMap)`

**Methoden**

- `GetFilters(Type type) : IReadOnlyList<IResponseFilter>`
- `HasFilters(Type type) : bool`

### `ResponseFilterTypeMap`

`class`

Process-wide cache of target type → filter implementation types. Populated at startup by `AddResponseFilters`; thread-safe for read.

**Konstruktoren**

- `ResponseFilterTypeMap()`

**Methoden**

- `Add(Type targetType, Type filterImplType) : void`
- `TryGet(Type targetType, out Type[] implTypes) : bool`

**Eigenschaften**

- `TargetTypes : IEnumerable<Type> { get; }`
  <br>All registered target types (snapshot — safe to enumerate).

### `TypeReachabilityCache`

`class`

Precomputes "is any registered filter target reachable from `rootType`?" so the pipeline can early-out for responses that have nothing to filter. Walks the type graph once per root, caches the answer.

**Konstruktoren**

- `TypeReachabilityCache()`

**Methoden**

- `MayBeAffected(Type rootType) : bool`
  <br>True if `rootType` itself or any reachable navigable property type matches a registered filter target. Conservative: returns true on uncertainty (e.g. `Object` in the graph) to avoid false negatives.
- `SetTargetTypes(IEnumerable<Type> targetTypes) : void`

## Nextended.ResponseFilters.Reflection

### `PropertyAccessor`

`class`

Compiled delegate-based getter/setter for a `PropertyInfo`. Replaces `PropertyInfo.GetValue` / `SetValue` for hot-path use; ~10-50x faster than raw reflection. Instances are cached per `PropertyInfo`.

**Methoden**

- `For(PropertyInfo property) : PropertyAccessor`
- `GetValue(object instance) : object`
- `SetValue(object instance, object value) : void`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanWrite : bool { get; }`
- `DeclaringType : Type { get; }`
- `Getter : Func<object, object> { get; }`
- `Property : PropertyInfo { get; }`
- `PropertyType : Type { get; }`
- `Setter : Action<object, object> { get; }`

↩ [Zurück zur Paketseite](/de/projects/responsefilters)
