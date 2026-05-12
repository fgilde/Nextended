using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Default pipeline. Walks the response graph depth-first, dispatches matching filters per visited
/// node, and (by default) lets exceptions propagate so domain errors reach the host's exception
/// handler unchanged. Cycle-safe via <see cref="ReferenceEqualityComparer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Configure behaviour at registration time via <see cref="ResponseFilterOptions"/> — including:
/// </para>
/// <list type="bullet">
///   <item><see cref="ResponseFilterOptions.ExceptionBehavior"/> — Rethrow (default) or LogAndContinue.</item>
///   <item><see cref="ResponseFilterOptions.SkipUnaffectedResponses"/> — skip responses whose type graph contains no registered target type.</item>
///   <item><see cref="ResponseFilterOptions.SkipResponseType"/> — custom opt-out predicate on the root type.</item>
/// </list>
/// <para><see cref="OperationCanceledException"/> always propagates, regardless of behaviour.</para>
/// </remarks>
public sealed class ResponseFilterPipeline(
    IResponseFilterRegistry registry,
    TypeReachabilityCache reachability,
    IOptions<ResponseFilterOptions> options,
    ILogger<ResponseFilterPipeline>? logger = null)
    : IResponseFilterPipeline
{
    private readonly ResponseFilterOptions _options = options.Value;

    public async ValueTask ProcessAsync(object? root, IResponseFilterContext context)
    {
        if (root is null) return;

        var rootType = root.GetType();

        // 1) Custom opt-out predicate
        if (_options.SkipResponseType is not null && _options.SkipResponseType(rootType))
        {
            return;
        }

        // 2) Reachability fast-path: nothing in the type graph matches any registered filter
        if (_options.SkipUnaffectedResponses && !reachability.MayBeAffected(GetEffectiveTypeForReachability(root, rootType)))
        {
            return;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        await VisitAsync(root, visited, context).ConfigureAwait(false);
    }

    /// <summary>
    /// For top-level enumerables (<c>List&lt;T&gt;</c>, <c>T[]</c>), use the element type for the
    /// reachability check — the collection itself never carries filterable properties.
    /// </summary>
    private static Type GetEffectiveTypeForReachability(object root, Type rootType) =>
        root is IEnumerable and not string ? TypeGraphInspector.UnwrapEnumerable(rootType) : rootType;

    private async ValueTask VisitAsync(object? value, HashSet<object> visited, IResponseFilterContext context)
    {
        if (value is null || !visited.Add(value))
        {
            return;
        }

        if (value is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                await VisitAsync(item, visited, context).ConfigureAwait(false);
            }
            return;
        }

        var type = value.GetType();

        // Apply filters registered for the exact type
        if (registry.HasFilters(type))
        {
            foreach (var filter in registry.GetFilters(type))
            {
                try
                {
                    await filter.ApplyAsync(value, context).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Always propagate so request aborts and host shutdown work as expected.
                    throw;
                }
                catch (Exception ex) when (_options.ExceptionBehavior == FilterExceptionBehavior.LogAndContinue)
                {
                    logger?.LogWarning(ex,
                        "Filter {Filter} threw on {Type}; continuing because ExceptionBehavior = LogAndContinue.",
                        filter.GetType().FullName, type.FullName);
                }
                // ExceptionBehavior.Rethrow → the catch above doesn't match, exception bubbles up.
            }
        }

        // Recurse into navigable properties
        foreach (var nav in TypeGraphInspector.GetNavigableProperties(type))
        {
            // Skip subtrees that cannot contain a filtered type — saves a getter call and child walks.
            if (_options.SkipUnaffectedResponses && !reachability.MayBeAffected(nav.MemberType))
            {
                continue;
            }

            var childValue = nav.Accessor.GetValue(value);
            if (childValue is null) continue;

            if (nav.IsEnumerable)
            {
                foreach (var item in (IEnumerable)childValue)
                {
                    await VisitAsync(item, visited, context).ConfigureAwait(false);
                }
            }
            else
            {
                await VisitAsync(childValue, visited, context).ConfigureAwait(false);
            }
        }
    }
}
