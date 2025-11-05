using System.Collections.Generic;
using System.Linq;
using Nextended.Core.Contracts;

namespace Nextended.Core.IncludeDefinitions;

public sealed class PrefixedIncludePathDefinition(string prefix, IIncludePathDefinition inner) : IIncludePathDefinition
{
    public IEnumerable<string> GetPaths()
        => inner.GetPaths().Select(p => $"{prefix}.{p}");
}
