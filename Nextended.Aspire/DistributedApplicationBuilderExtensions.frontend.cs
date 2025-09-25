//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace Nextended.Aspire;


//public class NpmBehavior
//{
//    public string Library { get; set; }
//}

//public static partial class DistributedApplicationBuilderExtensions
//{
    
//    public static IResourceBuilder<NodeAppResource>[] AddAllNpmAppsInPath(this IDistributedApplicationBuilder builder, string path, )
//    {
//        var results = new List<IResourceBuilder<NodeAppResource>>();
//        var apps = Directory.EnumerateDirectories(Path.Combine(builder.AppHostDirectory, path)).ToArray();
//        CreatePackageJson(apps, path);
//        foreach (string app in apps.Where(s => File.Exists(Path.Combine(s, "Dockerfile"))))
//        {
//            var packageJson = Path.Combine(app, "package.json");
//            var appSettings = File.Exists(packageJson) ? Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(packageJson)) : null;
//            var scriptNames = appSettings?.scripts != null ? ((JObject)appSettings.scripts)?.Properties().Select(p => p.Name).ToArray() : null;
//            var dependencies = appSettings?.dependencies != null ? ((JObject)appSettings.dependencies)?.Properties().Select(p => p.Name).ToArray() : null;
//            var hasLibraryReference = dependencies?.Any(d => d == CargonerdsConsts.ServiceNames.UiLib) == true;

//            string appName = EscapeProjectname((appSettings?.name ?? Path.GetFileName(app)).ToString());
//            if (!builder.ExecutionContext.IsRunMode && ignoredOnDeploy.Contains(appName))
//            {
//                continue;
//            }
//            string startScript = (scriptNames?.FirstOrDefault(n => n.Contains("aspire") || n.Contains("start")) ?? "start");

//            var nodeApp = builder.AddNpmApp(appName, app, startScript)
//                                 .WithEnvironment("BROWSER", "none")
//                                 .WithHttpEndpoint(env: "PORT", name: "http")
//                                 .WithExternalHttpEndpoints()
//                                 .WithHttpHealthCheck("/", 200)
//                                 .WithOtlpExporter()
//                                 .PublishAsDockerFile(c =>
//                                 {
//                                     if (!builder.ExecutionContext.IsRunMode && hasLibraryReference)
//                                     {
//                                         c.WithDockerfile(
//                                             contextPath: path,
//                                             dockerfilePath: Path.Combine(app, "Dockerfile")
//                                         );
//                                     }
//                                 }
//                                  );
//            if (builder.ExecutionContext.IsRunMode)
//                nodeApp.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
//            results.Add(nodeApp);

//        }
//        return results.ToArray();
//    }

//    private static void CreatePackageJson(string[] absolutePaths, string targetDir)
//    {
//        var dockerignorePatterns = new HashSet<string>();
//        var dockerignorePath = Path.Combine(targetDir, ".dockerignore");
//        if (File.Exists(dockerignorePath))
//        {
//            dockerignorePatterns = File.ReadAllLines(dockerignorePath)
//                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
//                                       .Select(line => line.Trim().TrimEnd('/'))
//                                       .ToHashSet();
//        }

//        var workspacesInPath = absolutePaths
//                        .Select(absolutePath => Path.GetRelativePath(targetDir, absolutePath))
//                        .Where(relativePath => !IsIgnored(relativePath, dockerignorePatterns))
//                        .OrderBy(path => path)
//                        .ToArray();

//        var packageJson = new
//        {
//            workspaces = workspacesInPath
//        };

//        var json = JsonConvert.SerializeObject(packageJson, Formatting.Indented);
//        var packageJsonPath = Path.Combine(targetDir, "package.json");
//        File.WriteAllText(packageJsonPath, json);
//    }

//    private static bool IsIgnored(string relativePath, HashSet<string> dockerignorePatterns)
//    {
//        if (!dockerignorePatterns.Any()) return false;

//        var normalizedPath = relativePath.Replace('\\', '/');

//        return dockerignorePatterns.Any(pattern =>
//        {
//            if (pattern.EndsWith("*"))
//            {
//                return normalizedPath.StartsWith(pattern.TrimEnd('*'));
//            }
//            return normalizedPath == pattern || normalizedPath.StartsWith(pattern + "/");
//        });
//    }
//}