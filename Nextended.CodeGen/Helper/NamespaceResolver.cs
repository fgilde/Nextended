using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Helper
{
    public class NamespaceResolver
    {
        public delegate bool OptionsTryGetFunc(string key, out string value);

        private string originFilePath;
        private readonly string fallBackRootNamespace;
        private readonly OptionsTryGetFunc optionsTryGetFunc;

        public NamespaceResolver(AdditionalText file, GeneratorExecutionContext context)
            : this(file.Path, context)
        { }

        public NamespaceResolver(string originFilePath, GeneratorExecutionContext context)
            : this(originFilePath, context.Compilation.AssemblyName, context.AnalyzerConfigOptions.GlobalOptions.TryGetValue)
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
            if (result.EndsWith("."))
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
                var toPath = EnsurePathEndsWithDirectorySeparator(Path.GetDirectoryName(this.originFilePath));
                var relativPath = GetRelativePath(fromPath, toPath);

                return $"{rootNamespace}.{relativPath.Replace(Path.DirectorySeparatorChar, '.')}";
            }

            return rootNamespace;
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {
            var relativeUri = new Uri(fromPath).MakeRelativeUri(new(toPath));
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
                    || (Path.DirectorySeparatorChar == '/' && path.StartsWith("/"))
                    || (Path.DirectorySeparatorChar == '\\' && path.Contains(":")));

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
            // Ergebnis immer Unix-kompatibel machen (mit forward slashes)
            return result.Replace(Path.DirectorySeparatorChar, '/');
        }

        public static string EnsurePathEndsWithDirectorySeparator(string path)
            => path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
