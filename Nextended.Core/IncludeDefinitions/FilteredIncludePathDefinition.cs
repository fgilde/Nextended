using System;
using System.Collections.Generic;
using System.Linq;
using Nextended.Core.Contracts;

namespace Nextended.Core.IncludeDefinitions;

public sealed class FilteredIncludePathDefinition(
    IIncludePathDefinition inner,
    IEnumerable<Func<string, bool>> predicates)
    : IIncludePathDefinition
{
    private readonly List<Func<string, bool>> _predicates = predicates.ToList();

    public IEnumerable<string> GetPaths()
        => inner.GetPaths()
            .Where(p => !_predicates.Any(pred => pred(p)))
            .Distinct(StringComparer.Ordinal);
}