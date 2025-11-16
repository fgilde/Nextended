using ColorCode;
using DocumentFormat.OpenXml.Wordprocessing;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Generators.CodeToText
{
    public class CodeToTextGenerator : ISourceSubGenerator
    {
        public bool RequireConfig => true;

        public IEnumerable<GeneratedFile> Execute(GenerationContext context)
        {
            
            if (context?.Config?.CodeToDocs == null)
                yield break;

            foreach (var cfg in context.Config.CodeToDocs)
            {
                if (cfg == null || cfg.DisableGeneration)
                    continue;

                var path = NamespaceResolver.GetAbsolutePath(cfg.InputFolder, context.AdditionalFile.Path);

            if (string.IsNullOrWhiteSpace(cfg.InputFolder) || !Directory.Exists(path))
                    continue;

                if (cfg.SourceExportTypes == null || cfg.SourceExportTypes.Length == 0)
                    continue;

                var pattern = string.IsNullOrWhiteSpace(cfg.FileIncludePattern)
                    ? "*.*"
                    : cfg.FileIncludePattern;

                foreach (var file in Directory.GetFiles(path, pattern, SearchOption.AllDirectories))
                {
                    var source = File.ReadAllText(file);

                    foreach (var exportType in cfg.SourceExportTypes)
                    {
                        var fileName = BuildOutputFileName(file, exportType);
                        var content = RenderContent(source, file, exportType);
                        if(!string.IsNullOrEmpty(content))
                            yield return new GeneratedFile(fileName, "CodeToText", content, cfg.OutputPath);
                    }
                }
            }
        }

        private static string BuildOutputFileName(string inputFile, SourceExportType exportType)
        {
            var ext = exportType switch
            {
                SourceExportType.PlainText => ".txt",
                SourceExportType.Markdown => ".md",
                SourceExportType.Html => ".html",
                _ => ".txt"
            };

            return Path.ChangeExtension(Path.GetFileName(inputFile), ext);
        }

        private static string RenderContent(string source, string inputFile, SourceExportType exportType)
        {
            try
            {
                return exportType switch
                {
                    SourceExportType.PlainText => source,
                    SourceExportType.Markdown => ToMarkdown(source, inputFile),
                    SourceExportType.Html => ToHtml(source, inputFile),
                    _ => source
                };
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


        private static string ToMarkdown(string source, string inputFile)
        {
            var language = DetectMarkdownLanguage(inputFile);

            var sb = new StringBuilder();
            sb.Append("```");
            if (!string.IsNullOrEmpty(language))
                sb.Append(language);

            sb.AppendLine();
            sb.AppendLine(source);
            sb.AppendLine("```");

            return sb.ToString();
        }


        private static string DetectMarkdownLanguage(string inputFile)
        {
            var ext = Path.GetExtension(inputFile).ToLowerInvariant();

            return ext switch
            {
                ".cs" => "csharp",
                ".razor" => "razor",
                ".html" => "html",
                ".htm" => "html",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".css" => "css",
                ".xml" => "xml",
                ".json" => "json",
                _ => string.Empty    // kein Language-Token
            };
        }

        // -------- HTML via ColorCode --------

        private static string ToHtml(string source, string inputFile)
        {
            var formatter = new HtmlClassFormatter();
            var language = DetectColorCodeLanguage(inputFile);

            return formatter.GetHtmlString(source, language);
        }

        /// <summary>
        /// ColorCode-Sprache anhand der Dateiendung bestimmen.
        /// Minimal gehalten, damit es sicher kompiliert.
        /// </summary>
        private static ILanguage DetectColorCodeLanguage(string inputFile)
        {
            var ext = Path.GetExtension(inputFile).ToLowerInvariant();

            return ext switch
            {
                ".html" or ".htm" or ".razor" => new ColorCode.Compilation.Languages.Html(),
                ".cs" => new ColorCode.Compilation.Languages.CSharp(),
                _ => new ColorCode.Compilation.Languages.CSharp() // Fallback
            };
        }
    }
}
