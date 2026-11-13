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
    /// <summary>The [GeneratedCode] line (with its line break) of a type in a raw string template, or nothing.</summary>
    internal static string GeneratedCode(CodeGenerationConfigBase cfg) => new StringBuilder().AppendGeneratedCodeIf(cfg, "    ").ToString();

    public static string GenerateCode(IXLWorksheet ws, ExcelGenerationConfig cfg)
    {
        var headerRow = ws.Row(cfg.HeaderRowIndex);
        var props = CodeGenHelper.ResolveColumns(headerRow, cfg);

        var sb = new StringBuilder()
            .AppendFileHeaderIf(cfg.CreateFileHeaders, cfg.RootClassName)
            .AppendLine($$"""
                                     namespace {{cfg.Namespace}}
                                     {
                                     {{GeneratedCode(cfg)}}    public partial {{cfg.ModelType.ToCSharpKeyword()}} {{cfg.Prefix}}{{cfg.RootClassName}}{{cfg.Suffix}}
                                         {
                                     """)
            .AppendLine();

        foreach (var p in props)
            sb.AppendLine($"    public {p.Type} {p.Name} {{ get; set; }}");

        sb.AppendLine("  } }");
        return sb.ToString();
    }
}
