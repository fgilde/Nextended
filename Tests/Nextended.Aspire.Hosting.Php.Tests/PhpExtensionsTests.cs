using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.Php.Tests;

public class PhpExtensionsTests
{
    private static IResourceBuilder<PhpResource> AddFolder()
        => DistributedApplication.CreateBuilder().AddPhp("php", "www");

    /// <summary>Runs the resource's args callbacks the way Aspire would at start.</summary>
    private static async Task<List<string>> ArgsOf(IResource res)
    {
        var ctx = new CommandLineArgsCallbackContext(new List<object>());
        foreach (var a in res.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            await a.Callback(ctx);
        return ctx.Args.Select(a => a.ToString()!).ToList();
    }

    [Fact]
    public async Task AddPhp_Folder_ServesDocroot()
    {
        var res = AddFolder().Resource;

        Assert.Null(res.RouterScript);

        var img = Assert.Single(res.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("php", img.Image);
        Assert.Equal("8.4-cli", img.Tag);

        var http = Assert.Single(res.Annotations.OfType<EndpointAnnotation>(), e => e.Name == "http");
        Assert.Equal(80, http.TargetPort);

        var mount = Assert.Single(res.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("/app", mount.Target);
        Assert.EndsWith("www", mount.Source);

        var args = await ArgsOf(res);
        Assert.Equal(["php", "-S", "0.0.0.0:80", "-t", "/app"], args);
    }

    [Fact]
    public async Task AddPhp_SingleFile_BecomesRouterScript()
    {
        var res = DistributedApplication.CreateBuilder().AddPhp("mailer", "www/send-mail.php").Resource;

        Assert.Equal("send-mail.php", res.RouterScript);

        var mount = Assert.Single(res.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("/app/send-mail.php", mount.Target);

        var args = await ArgsOf(res);
        Assert.Equal("/app/send-mail.php", args.Last());
    }

    [Fact]
    public async Task WithPhpIni_AddsDirectivesAsArgs()
    {
        var res = AddFolder()
            .WithPhpIni("memory_limit", "256M")
            .WithPhpIni(new Dictionary<string, string> { ["display_errors"] = "1" })
            .Resource;

        var args = await ArgsOf(res);
        Assert.Contains("-d", args);
        Assert.Contains("memory_limit=256M", args);
        Assert.Contains("display_errors=1", args);
    }

    [Fact]
    public void WithPhpIni_SameKey_LastValueWins()
    {
        var res = AddFolder()
            .WithPhpIni("memory_limit", "128M")
            .WithPhpIni("memory_limit", "512M")
            .Resource;

        Assert.Equal("512M", res.IniSettings["memory_limit"]);
        Assert.Single(res.IniSettings);
    }

    [Fact]
    public void WithPhpIniFile_MountsIntoConfD()
    {
        var res = AddFolder().WithPhpIniFile("custom.ini").Resource;

        var mounts = res.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Contains(mounts, m => m.Target == "/usr/local/etc/php/conf.d/zzz-custom.ini" && m.IsReadOnly);
    }

    [Fact]
    public void CustomPortImageAndTag_Honored()
    {
        var res = DistributedApplication.CreateBuilder()
            .AddPhp("php", "www", port: 8123, image: "my/php", tag: "dev").Resource;

        var img = Assert.Single(res.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("my/php", img.Image);
        Assert.Equal("dev", img.Tag);

        var http = Assert.Single(res.Annotations.OfType<EndpointAnnotation>(), e => e.Name == "http");
        Assert.Equal(8123, http.Port);
    }

    [Fact]
    public void WorkerEnv_IsSet_SoParallelRequestsDontDeadlock()
    {
        var res = AddFolder().Resource;
        Assert.NotEmpty(res.Annotations.OfType<EnvironmentCallbackAnnotation>()); // PHP_CLI_SERVER_WORKERS
    }
}
