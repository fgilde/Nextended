using System;
using System.Collections.Generic;
using System.Linq;
using Nextended.Core.Contracts;

namespace Nextended.Core.IncludeDefinitions;

public class CompositeIncludePathDefinition(params IIncludePathDefinition[] defs) : IIncludePathDefinition
{
    public IEnumerable<string> GetPaths() =>
        defs.SelectMany(d => d.GetPaths()).Distinct(StringComparer.Ordinal);
}