using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;

namespace Nextended.CodeGen.Helper
{
    public class NamespaceResolver
    {
        public delegate bool OptionsTryGetFunc(string key, out string value);

        private readonly string originFilePath;
        private readonly string fallBackRootNamespace;
        private readonly OptionsTryGetFunc optionsTryGetFunc;

        public NamespaceResolver(AdditionalText file, GeneratorExecutionContext context)
            : this(file.Path, context)
        { }

        public NamespaceResolver(string originFilePath, GeneratorExecutionContext context)
            : this(originFilePath,
                context.Compilation.AssemblyName ?? string.Empty,
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue)
        { }

        public NamespaceResolver(AdditionalText file, Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
            : this(file.Path,
                compilation.AssemblyName ?? string.Empty,
                optionsProvider.GlobalOptions.TryGetValue)
        { }

        public NamespaceResolver(string originFilePath, Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
            : this(originFilePath,
                compilation.AssemblyName ?? string.Empty,
                optionsProvider.GlobalOptions.TryGetValue)
        { }

        public NamespaceResolver(string originFilePath, string fallBackRootNamespace, OptionsTryGetFunc optionsGetterFunc)
        {
            this.originFilePath = originFilePath;
            this.fallBackRootNamespace = fallBackRootNamespace;
            this.optionsTryGetFunc = optionsGetterFunc;
        }

        public string Resolve()
        {
            var result = ExecuteResolve();
            if (result.EndsWith(".", StringComparison.Ordinal))
                result = result.Substring(0, result.Length - 1);
            return result;
        }

        private string ExecuteResolve()
        {
            if (!this.optionsTryGetFunc("build_property.rootnamespace", out var rootNamespace))
                rootNamespace = fallBackRootNamespace;

            if (this.optionsTryGetFunc("build_property.projectdir", out var projectDir))
            {
                var fromPath = EnsurePathEndsWithDirectorySeparator(projectDir);
                var toPath = EnsurePathEndsWithDirectorySeparator(Path.GetDirectoryName(this.originFilePath) ?? projectDir);
                var relativPath = GetRelativePath(fromPath, toPath);

                return $"{rootNamespace}.{relativPath.Replace(Path.DirectorySeparatorChar, '.')}";
            }

            return rootNamespace;
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {
            var relativeUri = new Uri(fromPath).MakeRelativeUri(new Uri(toPath));
            return Uri.UnescapeDataString(relativeUri.ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static string GetAbsolutePath(string path, string? basePath = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or empty.", nameof(path));

            bool isAbsolute =
                Path.IsPathRooted(path)
                && (Path.GetFullPath(path) == path
                    || (Path.DirectorySeparatorChar == '/' && path.StartsWith("/", StringComparison.Ordinal))
                    || (Path.DirectorySeparatorChar == '\\' && path.Contains(":", StringComparison.Ordinal)));

            if (isAbsolute)
                return Path.GetFullPath(path);

            basePath ??= Directory.GetCurrentDirectory();

            if (File.Exists(basePath))
                basePath = Path.GetDirectoryName(basePath);

            if (basePath == null)
                throw new ArgumentException("Base path must not be null or empty.", nameof(basePath));

            string normalizedPath = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string combinedPath = Path.Combine(basePath, normalizedPath);
            string result = Path.GetFullPath(combinedPath);

            return result.Replace(Path.DirectorySeparatorChar, '/');
        }

        public static string EnsurePathEndsWithDirectorySeparator(string path)
            => path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
