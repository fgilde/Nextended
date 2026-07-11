using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using Nextended.CodeGen.Config;

namespace Nextended.CodeGen.Helper;

internal static class CodeGenHelper
{
    private static readonly HashSet<string> _csharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked",
            "class","const","continue","decimal","default","delegate","do","double","else",
            "enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is",
            "lock","long","namespace","new","null","object","operator","out","override",
            "params","private","protected","public","readonly","ref","return","sbyte",
            "sealed","short","sizeof","stackalloc","static","string","struct","switch",
            "this","throw","true","try","typeof","uint","ulong","unchecked","unsafe",
            "ushort","using","virtual","void","volatile","while"
        };

    /// <summary>Wandelt beliebige Zeichenketten in gültige C#‑Identifier um.</summary>
    public static string ToCSharpIdentifier(this string text, bool allowDigitsPrefix = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "_";

        // 1) Trim, Unicode‐Normalize, Leerzeichen entfernen
        var sb = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        }

        if (sb.Length == 0)
            sb.Append('_');

        // 2) Erste Stelle muss Buchstabe oder _ sein
        if (!allowDigitsPrefix && char.IsDigit(sb[0]))
            sb.Insert(0, '_');

        var ident = sb.ToString();

        // 3) Keywords maskieren
        return _csharpKeywords.Contains(ident) ? "@" + ident : ident;
    }

    /// <summary>
    /// Löst die Header‑Zeile zu (PropertyName, Typ, Spaltenbuchstabe) auf und wendet dabei
    /// <see cref="ExcelGenerationConfig.ColumnMappings"/> (Key = Header ODER Spaltenbuchstabe A/B/C…)
    /// und <see cref="ExcelGenerationConfig.PropertyTypeOverrides"/> an. Beide Emitter (Modell‑Klasse
    /// und statische Tabelle) nutzen diese Methode, damit Namen und Typen garantiert übereinstimmen.
    /// </summary>
    public static IReadOnlyList<(string Name, string Type, string Column)> ResolveColumns(
        IXLRow headerRow, ExcelGenerationConfig cfg)
    {
        var result = new List<(string Name, string Type, string Column)>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var rawHeader = cell.GetString();
            var columnLetter = cell.WorksheetColumn().ColumnLetter();

            var mappedName = rawHeader;
            if (cfg.ColumnMappings is { } maps)
            {
                if (maps.TryGetValue(rawHeader, out var byHeader))
                    mappedName = byHeader;
                else if (maps.TryGetValue(columnLetter, out var byLetter))
                    mappedName = byLetter;
            }

            var propName = mappedName.ToCSharpIdentifier();
            var type = cfg.PropertyTypeOverrides?.GetValueOrDefault(propName)
                       ?? InferType(cell.WorksheetColumn(), cfg);

            result.Add((propName, type, columnLetter));
        }
        return result;
    }

    private static readonly HashSet<string> _builtInTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string","bool","boolean","byte","sbyte","short","int16","ushort","uint16",
        "int","int32","uint","uint32","long","int64","ulong","uint64","nint","nuint",
        "float","single","double","decimal","char","object",
        "datetime","datetimeoffset","dateonly","timeonly","timespan","guid"
    };

    /// <summary>
    /// True, wenn <paramref name="type"/> ein von der Literal‑Erzeugung direkt unterstützter
    /// eingebauter Typ ist. Für alles andere (eigene Structs, Enums …) muss die statische Tabelle
    /// über <c>MapTo&lt;T&gt;()</c> konvertieren.
    /// </summary>
    public static bool IsBuiltInType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return true;
        var t = NormalizeTypeName(type!);
        return _builtInTypes.Contains(t);
    }

    private static string NormalizeTypeName(string type)
    {
        var t = type.Trim().TrimEnd('?').Trim();
        var lastDot = t.LastIndexOf('.');
        if (lastDot >= 0) t = t.Substring(lastDot + 1);
        return t;
    }

    /// <summary> Liefert den wahrscheinlichsten .NET‑Typ für die Spalte. </summary>
    public static string InferType(IXLColumn col, ExcelGenerationConfig cfg)
    {
        // Nur eine Stichprobe der ersten max. 20 "sinnvollen" Datenzeilen
        List<IXLCell> samples = col.CellsUsed()
            .Where(c => c.Address.RowNumber >= cfg.DataStartRowIndex) // nur Daten­bereich
            .Take(20)
            .ToList();

        if (samples.Count == 0)
            return "string";        // Spalte leer? -> string

        bool allDate = samples.All(c => c.DataType == XLDataType.DateTime);
        bool allBool = samples.All(c => c.DataType == XLDataType.Boolean);

        // „Ganzzahl“ = Zelltyp Number und alle Werte ohne Nach­komma­anteil
        bool allInt = samples.All(c => c.DataType == XLDataType.Number &&
                                       IsInteger(c.GetDouble()));

        bool allNum = samples.All(c => c.DataType == XLDataType.Number);

        return allInt ? "int"
            : allDate ? "DateTime"
            : allBool ? "bool"
            : allNum ? "double"
            : "string";
    }

    private static bool IsInteger(double d) =>
        Math.Abs(d - Math.Truncate(d)) < 0.0000001;

    /// <summary>Formatiert Zell‑Werte als C#‑Literal.</summary>
    public static string FormatLiteral(IXLCell cell)
    {
        return cell.DataType switch
        {
            XLDataType.Number => FormatNumber(cell.GetDouble()),
            XLDataType.DateTime => $"new System.DateTime({cell.GetDateTime():yyyy, M, d})",
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            _ => $"\"{cell.GetString().EscapeForCSharp()}\""
        };

        static string FormatNumber(double d)
            => d == Math.Truncate(d) ? ((int)d).ToString(CultureInfo.InvariantCulture)
                                     : d.ToString("G", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formatiert einen Zell‑Wert als C#‑Literal, das zum <paramref name="targetType"/> passt
    /// (z. B. Zahl in String‑Property quoten, decimal‑Suffix setzen, bool/DateTime korrekt erzeugen).
    /// Für unbekannte/eigene Typen fällt die Methode auf die zell‑basierte Formatierung zurück —
    /// dort greift in der statischen Tabelle statt des Literals der <c>MapTo&lt;T&gt;()</c>‑Pfad.
    /// </summary>
    public static string FormatLiteral(IXLCell cell, string? targetType)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            return FormatLiteral(cell);

        var t = NormalizeTypeName(targetType!).ToLowerInvariant();
        switch (t)
        {
            case "string":
                return $"\"{CellAsString(cell).EscapeForCSharp()}\"";
            case "char":
            {
                var s = cell.GetString();
                return $"'{(s.Length > 0 ? s[0].ToString() : " ").Replace("'", "\\'")}'";
            }
            case "bool":
            case "boolean":
                return ParseBool(cell) ? "true" : "false";
            case "byte": case "sbyte":
            case "short": case "int16": case "ushort": case "uint16":
            case "int": case "int32": case "uint": case "uint32":
            case "nint": case "nuint":
                return FormatIntegral(cell, suffix: "");
            case "long": case "int64":
            case "ulong": case "uint64":
                return FormatIntegral(cell, suffix: "L");
            case "double":
                return FormatFloating(cell, suffix: "d");
            case "float": case "single":
                return FormatFloating(cell, suffix: "f");
            case "decimal":
                return FormatFloating(cell, suffix: "m");
            case "datetime":
                return cell.DataType == XLDataType.DateTime
                    ? $"new System.DateTime({cell.GetDateTime():yyyy, M, d})"
                    : FormatLiteral(cell);
            case "datetimeoffset":
                return cell.DataType == XLDataType.DateTime
                    ? $"new System.DateTimeOffset(new System.DateTime({cell.GetDateTime():yyyy, M, d}))"
                    : FormatLiteral(cell);
            case "guid":
                return $"new System.Guid(\"{cell.GetString().EscapeForCSharp()}\")";
            default:
                return FormatLiteral(cell);
        }
    }

    // A numeric/bool cell targeting a string property must be formatted invariantly, otherwise the
    // generated literal would depend on the build machine's culture (e.g. "123,5" vs "123.5").
    private static string CellAsString(IXLCell cell)
        => cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            _ => cell.GetString()
        };

    private static bool ParseBool(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Boolean) return cell.GetBoolean();
        if (cell.DataType == XLDataType.Number) return cell.GetDouble() != 0d;
        return bool.TryParse(cell.GetString(), out var b) && b;
    }

    private static double CellAsNumber(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number) return cell.GetDouble();
        if (cell.DataType == XLDataType.Boolean) return cell.GetBoolean() ? 1d : 0d;
        return double.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0d;
    }

    private static string FormatIntegral(IXLCell cell, string suffix)
    {
        var value = (long)Math.Truncate(CellAsNumber(cell));
        return value.ToString(CultureInfo.InvariantCulture) + suffix;
    }

    private static string FormatFloating(IXLCell cell, string suffix)
    {
        var value = CellAsNumber(cell);
        return value.ToString("R", CultureInfo.InvariantCulture) + suffix;
    }

    /// <summary>Ersatz für Dictionary&lt;TKey,TValue&gt;.GetValueOrDefault() bei älteren Frameworks.</summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                         TKey key, TValue? fallback = default)
        => dict.TryGetValue(key!, out var val) ? val : fallback;
}
