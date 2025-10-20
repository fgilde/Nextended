#if !NETSTANDARD2_0
using System;
using System.Globalization;

namespace Nextended.Core.Facets;

public interface IODataLiteralFormatter
{
    /// <summary>Formats a CLR value to an OData literal (v4) suitable for $filter.</summary>
    string ToLiteral(object? value, Type type);
    /// <summary>Builds a (field ge from and field lt to) clause for range-like filters.</summary>
    string RangeClause(string field, object fromInclusive, object toExclusive, Type type);
}


public sealed class DefaultODataLiteralFormatter : IODataLiteralFormatter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public string ToLiteral(object? value, Type type)
    {
        if (value is null) return "null";
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string))
            return $"'{value.ToString()!.Replace("'", "''")}'";

        if (t == typeof(bool))
            return (bool)value ? "true" : "false";

        if (t == typeof(Guid))
            return $"guid'{value}'";

        if (t == typeof(DateTimeOffset))
        {
            var dto = value is DateTimeOffset d ? d : new DateTimeOffset(Convert.ToDateTime(value, Invariant), TimeSpan.Zero);
            // OData v4 accepts ISO 8601; some servers require an explicit 'datetimeoffset' literal.
            return $"datetimeoffset'{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}'";
        }

        if (t == typeof(DateTime))
        {
            // Normalize to UTC to avoid TZ ambiguities; use datetimeoffset'…' literal for broad server compatibility.
            var dt = value is DateTime d ? (d.Kind == DateTimeKind.Utc ? d : d.ToUniversalTime()) : Convert.ToDateTime(value, Invariant).ToUniversalTime();
            return $"datetimeoffset'{dt:yyyy-MM-ddTHH:mm:ssZ}'";
        }

        if (t.IsEnum)
        {
            // Accept enum instance, underlying numeric, or string name (case-insensitive).
            var name = ResolveEnumName(t, value);
            // If we can't resolve, fall back to the raw string representation.
            var final = name ?? value.ToString()!;
            return $"'{final.Replace("'", "''")}'";
        }

        // numeric types: unquoted, culture-invariant
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
            t == typeof(double) || t == typeof(float) || t == typeof(decimal))
        {
            return Convert.ToString(value, Invariant)!;
        }

        // Fallback to string
        return $"'{value.ToString()!.Replace("'", "''")}'";
    }

    public string RangeClause(string field, object fromInclusive, object toExclusive, Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        var fromLit = ToLiteral(fromInclusive, t);
        var toLit = ToLiteral(toExclusive, t);
        return $"({field} ge {fromLit} and {field} lt {toLit})";
    }

    /// <summary>
    /// Returns the enum member name for a given value, tolerating numeric or string input; null if it can't be resolved.
    /// </summary>
    private static string? ResolveEnumName(Type enumType, object value)
    {
        // 1) Direct enum instance
        if (value.GetType().IsEnum && value.GetType() == enumType)
            return Enum.GetName(enumType, value);

        // 2) String name (case-insensitive)
        if (value is string s)
        {
            // Try parse to confirm it's a valid name; then return the normalized casing (GetName).
            if (Enum.TryParse(enumType, s, ignoreCase: true, out object? parsed) && parsed is not null)
                return Enum.GetName(enumType, parsed);
            // Not a valid name -> return null, so caller can fall back
            return null;
        }

        // 3) Numeric (underlying type)
        try
        {
            var underlying = Enum.GetUnderlyingType(enumType);
            var numeric = Convert.ChangeType(value, underlying, Invariant);
            return Enum.GetName(enumType, numeric!);
        }
        catch
        {
            return null;
        }
    }
}
#endif