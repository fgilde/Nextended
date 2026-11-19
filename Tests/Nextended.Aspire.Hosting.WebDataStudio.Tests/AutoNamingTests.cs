using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// AddWithAutoNaming keeps returning a ProjectResource although Aspire 17 marks AddProject obsolete.
public class AutoNamingTests
{
    private sealed class ShopOrderApi : IProjectMetadata
    {
        private static readonly string Folder = Path.Combine(Path.GetTempPath(), "nextended-autonaming", "Shop.OrderApi");

        public string ProjectPath { get; } = Create();

        private static string Create()
        {
            Directory.CreateDirectory(Path.Combine(Folder, "Properties"));
            File.WriteAllText(Path.Combine(Folder, "Properties", "launchSettings.json"),
                """{ "profiles": { "https": { "commandName": "Project", "applicationUrl": "https://localhost:7001" } } }""");
            var project = Path.Combine(Folder, "Shop.OrderApi.csproj");
            File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
            return project;
        }
    }

    [Fact]
    public void The_project_gets_a_kebab_case_name_from_its_file()
    {
        var builder = DistributedApplication.CreateBuilder();

        var project = builder.AddWithAutoNaming<ShopOrderApi>(launchProfileName: "https");

        Assert.IsType<ProjectResource>(project.Resource);
        Assert.Equal("shop-order-api", project.Resource.Name);
    }
}
