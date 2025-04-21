using System.Collections.Generic;
using System.Globalization;

namespace Nextended.Core;

public interface IGeneratedLocalization
{
    IDictionary<string, string> GetAll(CultureInfo cultureInfo = null);
    IDictionary<string, string> GetAll(bool includeParents, CultureInfo cultureInfo = null);
    string GetString(string key, CultureInfo cultureInfo = null);
    string GetString(string key, bool includeParents, CultureInfo cultureInfo = null);
}