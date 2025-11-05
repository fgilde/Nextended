using System.Collections.Generic;

namespace Nextended.Core.Contracts;

public interface IIncludePathDefinition
{
    IEnumerable<string> GetPaths();
}