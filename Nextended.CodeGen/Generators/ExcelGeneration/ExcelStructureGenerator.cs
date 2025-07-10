using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Generators.ExcelGeneration;

public class ExcelStructureGenerator : ISourceSubGenerator
{
    public bool RequireConfig => true;

    public IEnumerable<GeneratedFile> Execute(GenerationContext context)
    {
        foreach (var cfg in context.Config?.ExcelGenerations ?? [])
        {
            var op = cfg.OutputPath;
            var path = NamespaceResolver.GetAbsolutePath(cfg.SourceFile, context.AdditionalFile.Path);

            if (!File.Exists(path) || ! new[]{".xlsx", ".xls"}.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                continue;

            using var wb = new ClosedXML.Excel.XLWorkbook(path);
            var ws = cfg.SheetName is null ? wb.Worksheet(1) : wb.Worksheet(cfg.SheetName);
            if (ws is null) continue;

            if (cfg.GenerateModelClass)
            {
                var modelSource = ModelClassEmitter.GenerateCode(ws, cfg);
                var fn = Path.ChangeExtension(Path.GetFileName(cfg.SourceFile), ".row.g.cs");
                yield return new GeneratedFile(fn, cfg.Namespace, modelSource, op);
            }

            if (cfg.GenerateStaticTable)
            {
                var staticSource = StaticTableEmitter.GenerateCode(ws, cfg);
                var fn = Path.ChangeExtension(Path.GetFileName(cfg.SourceFile), ".table.g.cs");
                File.WriteAllText("D:\\"+fn, staticSource);

                yield return new GeneratedFile(fn, cfg.Namespace, staticSource, op);
            }
        }
    }
}
