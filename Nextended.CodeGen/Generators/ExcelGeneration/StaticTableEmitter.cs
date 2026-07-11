using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nextended.CodeGen.Config;
using System.Text;
using ClosedXML.Excel;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Generators.ExcelGeneration;

internal static class StaticTableEmitter
{
    public static string GenerateCode(IXLWorksheet ws, ExcelGenerationConfig cfg)
    {
        var rows = ws.RangeUsed()
                     .RowsUsed()
                     .Skip(cfg.DataStartRowIndex - 1)   // 1‑basiert ‑> 0‑basiert
                     .ToList();

        var headerRow = ws.Row(cfg.HeaderRowIndex);
        // Same resolution the model class uses — guarantees the property names (and types) match,
        // so the object initializers below actually reference existing members.
        var columns = CodeGenHelper.ResolveColumns(headerRow, cfg);

        // If any target property is a custom type (own struct/enum via PropertyTypeOverrides), we can't
        // emit a compile‑time literal for it. In that case build an anonymous object of the raw cell
        // values and let MapTo<T>() convert every member to its target type at runtime.
        var needsMapping = columns.Any(c => !CodeGenHelper.IsBuiltInType(c.Type));

        var sb = new StringBuilder()
            .AppendFileHeaderIf(cfg.CreateFileHeaders, cfg.StaticClassName);

        if (needsMapping)
            sb.AppendLine("using Nextended.Core.Extensions;");

        sb.AppendLine($$"""
        namespace {{cfg.Namespace}}
        {
            public static partial class {{cfg.StaticClassName}}
            {
        """)
            .AppendLine();

        var rootType = cfg.GenerateModelClass
            ? cfg.RootClassName
            : $"{cfg.Namespace}.{cfg.Prefix}{cfg.RootClassName}{cfg.Suffix}";

        var keyColLetter = cfg.KeyColumn;
        var generatedProps = new HashSet<string>();
        var emittedProps = new List<string>();
        foreach (var row in rows)
        {
            var key = row.Cell(keyColLetter).GetString();
            var propId = GetUniquePropertyName(key.ToCSharpIdentifier(allowDigitsPrefix: false), generatedProps);
            generatedProps.Add(propId);
            emittedProps.Add(propId);

            var initializer = BuildInitializer(row, columns, rootType, needsMapping);

            // A T? backing field lazily caches the built instance; `??=` yields the non‑nullable T for
            // both reference and value types, so the property can return it directly.
            sb.AppendLine($"    private static {rootType}? __{propId} = null;");
            sb.AppendLine($"    public static {rootType} {propId} => __{propId} ??= {initializer};");
            sb.AppendLine();
        }

        if (cfg.GenerateAllCollection)
        {
            sb.AppendLine($"    public static System.Collections.Generic.IReadOnlyList<{rootType}> All =>");
            sb.AppendLine("        new[] { " + string.Join(", ", emittedProps) + " };");
        }

        sb.AppendLine("  } }");
        return sb.ToString();
    }

    private static string BuildInitializer(
        IXLRangeRow row,
        IReadOnlyList<(string Name, string Type, string Column)> columns,
        string rootType,
        bool needsMapping)
    {
        var sb = new StringBuilder();
        // Anonymous object + MapTo for custom types, typed object initializer otherwise.
        sb.Append(needsMapping ? "new" : $"new {rootType}").Append(" { ");

        var assignments = columns.Select(col =>
        {
            var cell = row.Cell(col.Column);
            var literal = needsMapping
                ? CodeGenHelper.FormatLiteral(cell)              // raw value, MapTo converts it
                : CodeGenHelper.FormatLiteral(cell, col.Type);   // literal already typed to the property
            return $"{col.Name} = {literal}";
        });

        sb.Append(string.Join(", ", assignments)).Append(" }");

        if (needsMapping)
            sb.Append($".MapTo<{rootType}>()");

        return sb.ToString();
    }
    private static string GetUniquePropertyName(string baseName, HashSet<string> existingNames)
    {
        string name = baseName;
        int i = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName}_{i}";
            i++;
        }
        return name;
    }
}
