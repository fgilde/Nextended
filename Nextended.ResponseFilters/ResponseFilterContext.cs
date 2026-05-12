using System;
using System.Collections.Generic;
using System.Threading;

namespace Nextended.ResponseFilters;

/// <summary>Default <see cref="IResponseFilterContext"/>.</summary>
public sealed class ResponseFilterContext : IResponseFilterContext
{
    public ResponseFilterContext(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        CancellationToken = cancellationToken;
        Items = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }
    public IDictionary<string, object?> Items { get; }
}
