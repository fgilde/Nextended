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

    /// <summary>Ersatz für Dictionary&lt;TKey,TValue&gt;.GetValueOrDefault() bei älteren Frameworks.</summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                         TKey key, TValue? fallback = default)
        => dict.TryGetValue(key!, out var val) ? val : fallback;
}
