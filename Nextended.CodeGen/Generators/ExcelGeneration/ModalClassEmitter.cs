using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nextended.CodeGen.Config;
using System.Text;
using ClosedXML.Excel;
using Nextended.CodeGen.Helper;
using Nextended.Core.Enums;

namespace Nextended.CodeGen.Generators.ExcelGeneration;

internal static class ModelClassEmitter
{
    public static string GenerateCode(IXLWorksheet ws, ExcelGenerationConfig cfg)
    {
        var headerRow = ws.Row(cfg.HeaderRowIndex);
        var props = ResolveColumns(headerRow, cfg);

        var sb = new StringBuilder()
            .AppendFileHeader(cfg.RootClassName)
            .AppendLine($$"""
                                     namespace {{cfg.Namespace}}
                                     {
                                         public partial {{cfg.ModelType.ToCSharpKeyword()}} {{cfg.Prefix}}{{cfg.RootClassName}}{{cfg.Suffix}}
                                         {
                                     """)
            .AppendLine();

        foreach (var p in props)
            sb.AppendLine($"    public {p.Type} {p.Name} {{ get; set; }}");

        sb.AppendLine("  } }");
        return sb.ToString();
    }

    private static IEnumerable<(string Name, string Type, string Column)> ResolveColumns(
        IXLRow headerRow, ExcelGenerationConfig cfg)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var rawHeader = cell.GetString();
            var column = cell.WorksheetColumn().ColumnLetter();
            if (cfg.ColumnMappings?.TryGetValue(rawHeader, out var mapped) is true)
                rawHeader = mapped;

            var propName = rawHeader.ToCSharpIdentifier();
            var type = cfg.PropertyTypeOverrides?.GetValueOrDefault(propName)
                       ?? CodeGenHelper.InferType(cell.WorksheetColumn(), cfg);

            yield return (propName, type, column);
        }
    }
}
