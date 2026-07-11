using System;
using System.Collections.Generic;
using System.Threading;

namespace Nextended.ResponseFilters;

/// <summary>
/// Per-pipeline context handed to every predicate and rule.
/// Hosts the service provider, cancellation, and a scratch bag for memoizing async values
/// (e.g. permission checks) so repeated predicates within the same response don't re-fetch.
/// </summary>
public interface IResponseFilterContext
{
    /// <summary>DI container scope for the current request.</summary>
    IServiceProvider Services { get; }

    /// <summary>Cancellation token bound to the host request/scope.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Free-form bag for transporting arbitrary state between rules of the same pipeline run
    /// (e.g. an authenticated user object, a tenant id, a cached permission map).
    /// Not thread-safe; rules are applied sequentially per object.
    /// </summary>
    IDictionary<string, object?> Items { get; }

    /// <summary>
    /// Ledger of structural edits (remove / rename / key-transform / add) recorded by structural rules.
    /// A POCO can't drop or rename a property at runtime, so these are collected here and replayed
    /// against the serialized JSON tree by the serialization layer (e.g. the ASP.NET Core adapter).
    /// </summary>
    StructuralEditBook StructuralEdits { get; }
}
