using System.Collections.Generic;
using System.Globalization;

namespace Nextended.Core;

/// <summary>
/// Interface for generated localization resources that provides access to localized strings and dictionaries.
/// </summary>
public interface IGeneratedLocalization
{
    /// <summary>
    /// Gets all localized strings as a dictionary for the specified culture.
    /// </summary>
    /// <param name="cultureInfo">The culture to retrieve strings for. If null, uses the current culture.</param>
    /// <returns>A dictionary containing all localized string key-value pairs.</returns>
    IDictionary<string, string> GetAll(CultureInfo cultureInfo = null);
    
    /// <summary>
    /// Gets all localized strings as a dictionary for the specified culture, optionally including parent culture strings.
    /// </summary>
    /// <param name="includeParents">If true, includes strings from parent cultures.</param>
    /// <param name="cultureInfo">The culture to retrieve strings for. If null, uses the current culture.</param>
    /// <returns>A dictionary containing all localized string key-value pairs.</returns>
    IDictionary<string, string> GetAll(bool includeParents, CultureInfo cultureInfo = null);
    
    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The key of the localized string to retrieve.</param>
    /// <param name="cultureInfo">The culture to retrieve the string for. If null, uses the current culture.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key, CultureInfo cultureInfo = null);
    
    /// <summary>
    /// Gets a localized string for the specified key and culture, optionally including parent culture strings.
    /// </summary>
    /// <param name="key">The key of the localized string to retrieve.</param>
    /// <param name="includeParents">If true, falls back to parent cultures if the key is not found in the specified culture.</param>
    /// <param name="cultureInfo">The culture to retrieve the string for. If null, uses the current culture.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key, bool includeParents, CultureInfo cultureInfo = null);
}