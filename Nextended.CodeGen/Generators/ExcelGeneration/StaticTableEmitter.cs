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
        var columns = headerRow.CellsUsed().ToDictionary(
                          c => c.WorksheetColumn().ColumnLetter(),
                          c => c.GetString().ToCSharpIdentifier());

        var sb = new StringBuilder()
            .AppendFileHeaderIf(cfg.CreateFileHeaders,cfg.StaticClassName)
            .AppendLine($$"""
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
        foreach (var row in rows)
        {
            var key = row.Cell(keyColLetter).GetString();
            var propId = GetUniquePropertyName(key.ToCSharpIdentifier(allowDigitsPrefix: false), generatedProps);
            generatedProps.Add(propId);
            
            sb.AppendLine($"    private static {rootType}? __{propId} = null;");
            sb.AppendLine($"    public static {rootType} {propId} => __{propId} ??= new {rootType}");
            sb.AppendLine("    {");

            foreach (var col in columns)
            {
                var valueCell = row.Cell(col.Key);
                var propName = col.Value;
                sb.AppendLine($"        {propName} = {CodeGenHelper.FormatLiteral(valueCell)},");
            }

            sb.AppendLine("    };");
            sb.AppendLine();
        }

        if (cfg.GenerateAllCollection)
        {
            sb.AppendLine($"    public static System.Collections.Generic.IReadOnlyList<{rootType}> All =>");
            sb.AppendLine("        new[] { " + string.Join(", ", generatedProps) + " };");
        }

        sb.AppendLine("  } }");
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
