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
        var props = CodeGenHelper.ResolveColumns(headerRow, cfg);

        var sb = new StringBuilder()
            .AppendFileHeaderIf(cfg.CreateFileHeaders, cfg.RootClassName)
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
}
