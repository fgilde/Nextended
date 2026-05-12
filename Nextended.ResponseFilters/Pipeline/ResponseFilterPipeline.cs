using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Default pipeline. Walks the graph depth-first using the static
/// <see cref="TypeGraphInspector"/> cache, dispatches matching filters per visited node, and
/// guards against cycles with <see cref="ReferenceEqualityComparer"/>.
/// </summary>
/// <remarks>
/// All exceptions thrown inside filter execution are caught and logged. The pipeline never
/// propagates failures up to the HTTP layer — a misbehaving filter MUST NOT take down a request.
/// </remarks>
public sealed class ResponseFilterPipeline : IResponseFilterPipeline
{
    private readonly IResponseFilterRegistry _registry;
    private readonly ILogger<ResponseFilterPipeline>? _logger;

    public ResponseFilterPipeline(IResponseFilterRegistry registry, ILogger<ResponseFilterPipeline>? logger = null)
    {
        _registry = registry;
        _logger = logger;
    }

    public async ValueTask ProcessAsync(object? root, IResponseFilterContext context)
    {
        if (root is null) return;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        try
        {
            await VisitAsync(root, visited, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "ResponseFilterPipeline failed on root type {RootType}. Response returned unsanitized.",
                root.GetType().FullName);
        }
    }

    private async ValueTask VisitAsync(object? value, HashSet<object> visited, IResponseFilterContext context)
    {
        if (value is null || !visited.Add(value))
        {
            return;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                await VisitAsync(item, visited, context).ConfigureAwait(false);
            }
            return;
        }

        var type = value.GetType();

        // 1) Apply all filters registered for this exact type
        if (_registry.HasFilters(type))
        {
            foreach (var filter in _registry.GetFilters(type))
            {
                try
                {
                    await filter.ApplyAsync(value, context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex,
                        "Filter {Filter} failed on {Type}; remaining filters continue.",
                        filter.GetType().FullName, type.FullName);
                }
            }
        }

        // 2) Recurse into navigable properties
        foreach (var nav in TypeGraphInspector.GetNavigableProperties(type))
        {
            object? childValue;
            try
            {
                childValue = nav.Accessor.GetValue(value);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Failed to read {Type}.{Property} during graph walk; skipping.",
                    type.FullName, nav.Accessor.Property.Name);
                continue;
            }

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
