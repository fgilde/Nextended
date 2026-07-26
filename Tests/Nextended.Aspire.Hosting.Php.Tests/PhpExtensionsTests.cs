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
    public async Task WithPhpIniConfiguration_MapsTypedProperties()
    {
        var res = AddFolder().WithPhpIniConfiguration(a =>
        {
            a.DisplayErrors = true;
            a.MemoryLimit = "256M";
            a.MaxExecutionTime = 30;
            a.DateTimezone = "Europe/Berlin";
        }).Resource;

        Assert.Equal("1", res.IniSettings["display_errors"]);      // convention + bool -> 1/0
        Assert.Equal("256M", res.IniSettings["memory_limit"]);
        Assert.Equal("30", res.IniSettings["max_execution_time"]);
        Assert.Equal("Europe/Berlin", res.IniSettings["date.timezone"]); // via [PhpIniKey]
        Assert.Equal(4, res.IniSettings.Count);                    // untouched (null) properties not emitted

        var args = await ArgsOf(res);
        Assert.Contains("display_errors=1", args);
    }

    private sealed class CustomIni : PhpIniConfiguration
    {
        [PhpIniKey("opcache.enable")]
        public bool? OpcacheEnable { get; set; }

        public string? AutoPrependFile { get; set; }
    }

    [Fact]
    public void WithPhpIniConfiguration_CustomSubclass_MapsOwnAndInheritedProperties()
    {
        var res = AddFolder().WithPhpIniConfiguration<CustomIni>(a =>
        {
            a.OpcacheEnable = true;
            a.AutoPrependFile = "/app/bootstrap.php";
            a.DisplayErrors = false;
        }).Resource;

        Assert.Equal("1", res.IniSettings["opcache.enable"]);
        Assert.Equal("/app/bootstrap.php", res.IniSettings["auto_prepend_file"]);
        Assert.Equal("0", res.IniSettings["display_errors"]);
    }

    /// <summary>Runs the resource's env callbacks the way Aspire would at start.</summary>
    private static async Task<Dictionary<string, object>> EnvOf(IResource res)
    {
        var ctx = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        foreach (var a in res.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await a.Callback(ctx);
        return new Dictionary<string, object>(ctx.EnvironmentVariables);
    }

    [Fact]
    public async Task WorkerEnv_DefaultsTo8_WithWorkersOverrides()
    {
        var env = await EnvOf(AddFolder().Resource);
        Assert.Equal("8", env["PHP_CLI_SERVER_WORKERS"]?.ToString());

        env = await EnvOf(AddFolder().WithWorkers(3).Resource);
        Assert.Equal("3", env["PHP_CLI_SERVER_WORKERS"]?.ToString());
    }

    [Fact]
    public void WithComposer_AddsInstallContainer_AndPhpWaitsForIt()
    {
        var builder = DistributedApplication.CreateBuilder();
        var php = builder.AddPhp("php", "www").WithComposer();

        var composer = Assert.IsType<ContainerResource>(Assert.Single(builder.Resources, r => r.Name == "php-composer"));

        var img = Assert.Single(composer.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("composer", img.Image);
        Assert.Equal("2", img.Tag);

        var mounts = composer.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Contains(mounts, m => m.Target == "/app" && (m.Source ?? "").EndsWith("www")); // same source as php
        Assert.Contains(mounts, m => m.Target == "/tmp/cache");                               // composer cache volume

        Assert.NotEmpty(php.Resource.Annotations.OfType<WaitAnnotation>()); // php starts after composer finished
    }

    [Fact]
    public async Task WithPhpExtensions_WrapsCommandWithExtensionInstall()
    {
        var res = AddFolder()
            .WithPhpExtensions("mysqli", "pdo_mysql")
            .WithPhpExtensions("mysqli") // duplicate ignored
            .WithPhpIni("memory_limit", "256M")
            .Resource;

        Assert.Equal(2, res.Extensions.Count);

        var args = await ArgsOf(res);
        Assert.Equal("sh", args[0]);
        Assert.Equal("-c", args[1]);
        var cmd = args[2];
        Assert.Contains("docker-php-ext-install", cmd);
        Assert.Contains("'mysqli' 'pdo_mysql'", cmd);
        Assert.Contains("&& exec php", cmd);
        Assert.Contains("'memory_limit=256M'", cmd);
        Assert.Contains("'-t' '/app'", cmd);
    }

    [Fact]
    public void WithComposer_SingleFileMode_Throws()
    {
        var php = DistributedApplication.CreateBuilder().AddPhp("mailer", "www/send-mail.php");
        Assert.Throws<InvalidOperationException>(() => php.WithComposer());
    }
}
