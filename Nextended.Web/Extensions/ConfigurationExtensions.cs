using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nextended.Web.Extensions;

public static class ConfigurationExtensions
{
    public static IDictionary<string, object?> ToDictionary(this IConfiguration cfg)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in cfg.GetChildren())
        {
            if (child.GetChildren()?.Any() == true)
            {
                result[child.Key] = child.ToDictionary();   
            }
            else
            {
                result[child.Key] = child.Value;
            }
        }

        return result;
    }
}