using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.AspireUI.Tests;

// What the seed methods actually hand to the container: one ASPIREUI_SEED document, plus the mounts
// that keep key material out of the environment.
public class AspireUISeedTests
{
    private static IResourceBuilder<AspireUIResource> Add() =>
        DistributedApplication.CreateBuilder().AddAspireUI();

    // Runs the environment callbacks the way the app model would, so the assertions are about the
    // values the container really sees.
    private static async Task<Dictionary<string, string>> EnvAsync(IResource resource)
    {
        var ctx = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(ctx);
        return ctx.EnvironmentVariables.ToDictionary(e => e.Key, e => e.Value?.ToString() ?? "");
    }

    private static async Task<JsonElement> SeedAsync(IResourceBuilder<AspireUIResource> builder)
    {
        var env = await EnvAsync(builder.Resource);
        return JsonDocument.Parse(env["ASPIREUI_SEED"]).RootElement;
    }

    [Fact]
    public async Task No_seed_no_environment_variable()
    {
        Assert.DoesNotContain("ASPIREUI_SEED", (await EnvAsync(Add().Resource)).Keys);
    }

    [Fact]
    public async Task Users_carry_their_permissions_and_view_modes()
    {
        var seed = await SeedAsync(Add()
            .WithUser("ops", "ops-password-1", AspireUIPermissions.Operator)
            .WithUser("kim", "kim-password-1", $"{AspireUIPermissions.Deploy},{AspireUIPermissions.Files}")
            .WithViewer("guest", "guest-password-1"));

        var users = seed.GetProperty("users").EnumerateArray().ToList();
        Assert.Equal(3, users.Count);

        Assert.Equal("ops", users[0].GetProperty("username").GetString());
        Assert.Equal("ops-password-1", users[0].GetProperty("password").GetString());
        Assert.Equal(["operator"], users[0].GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));

        Assert.Equal(["deploy", "files"], users[1].GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));

        Assert.Equal(["viewer"], users[2].GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(["simple"], users[2].GetProperty("viewModes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task An_admin_needs_no_permission_list()
    {
        var seed = await SeedAsync(Add().WithUsers(new AspireUIUser("boss", "boss-password-1", Admin: true)));
        var boss = seed.GetProperty("users").EnumerateArray().Single();
        Assert.True(boss.GetProperty("admin").GetBoolean());
        Assert.False(boss.TryGetProperty("permissions", out _));
    }

    [Fact]
    public async Task A_password_from_a_parameter_is_resolved_at_start()
    {
        var b = DistributedApplication.CreateBuilder();
        var pw = b.AddParameter("ops-password", secret: true, value: "from-the-parameter");
        var seed = await SeedAsync(b.AddAspireUI().WithUser("ops", pw, AspireUIPermissions.AppUser));

        Assert.Equal("from-the-parameter", seed.GetProperty("users").EnumerateArray().Single()
            .GetProperty("password").GetString());
    }

    [Fact]
    public async Task An_api_token_names_its_user()
    {
        var seed = await SeedAsync(Add()
            .WithUser("ci", "ci-password-1", AspireUIPermissions.Deploy)
            .WithApiToken("pipeline", "ci", "aspireui_known_value"));

        var token = seed.GetProperty("tokens").EnumerateArray().Single();
        Assert.Equal("pipeline", token.GetProperty("name").GetString());
        Assert.Equal("ci", token.GetProperty("username").GetString());
        Assert.Equal("aspireui_known_value", token.GetProperty("token").GetString());
    }

    [Fact]
    public async Task An_ssh_key_file_is_mounted_and_referenced_by_its_path_inside()
    {
        var keyFile = Path.Combine(Path.GetTempPath(), "aspireui-seedtest-" + Guid.NewGuid().ToString("n"), "id_ed25519");
        Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);
        File.WriteAllText(keyFile, "-----BEGIN OPENSSH PRIVATE KEY-----");

        var builder = Add().WithSshTarget("nas", "nas.local", "deploy", 2222, keyFile: keyFile,
            publicHost: "apps.example.com", isDefault: true);
        var seed = await SeedAsync(builder);

        var target = seed.GetProperty("targets").EnumerateArray().Single();
        Assert.Equal("ssh", target.GetProperty("kind").GetString());
        Assert.Equal("nas.local", target.GetProperty("host").GetString());
        Assert.Equal(2222, target.GetProperty("port").GetInt32());
        Assert.Equal("deploy", target.GetProperty("user").GetString());
        Assert.Equal("/seed/keys/id_ed25519", target.GetProperty("key").GetString());
        Assert.True(target.GetProperty("default").GetBoolean());

        // The key travels as a read-only mount, never as an environment value.
        var mount = Assert.Single(builder.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/seed/keys/id_ed25519");
        Assert.Equal(keyFile, mount.Source);
        Assert.True(mount.IsReadOnly);
        Assert.DoesNotContain("OPENSSH", (await EnvAsync(builder.Resource))["ASPIREUI_SEED"]);
    }

    [Fact]
    public async Task A_docker_tcp_target_builds_its_docker_host()
    {
        var seed = await SeedAsync(Add().WithDockerTcpTarget("box", "10.0.0.5"));
        var target = seed.GetProperty("targets").EnumerateArray().Single();
        Assert.Equal("dockerTcp", target.GetProperty("kind").GetString());
        Assert.Equal("tcp://10.0.0.5:2376", target.GetProperty("dockerHost").GetString());
    }

    [Fact]
    public async Task A_kubernetes_target_passes_its_exposure_settings()
    {
        var seed = await SeedAsync(Add().WithKubernetesTarget("cluster", "prod-context",
            @namespace: "apps", expose: "ingress", ingressHost: "{service}.example.com", storageClass: "fast"));

        var target = seed.GetProperty("targets").EnumerateArray().Single();
        Assert.Equal("k8s", target.GetProperty("kind").GetString());
        Assert.Equal("prod-context", target.GetProperty("context").GetString());
        Assert.Equal("apps", target.GetProperty("namespace").GetString());
        Assert.Equal("ingress", target.GetProperty("expose").GetString());
        Assert.Equal("{service}.example.com", target.GetProperty("ingressHost").GetString());
        Assert.Equal("fast", target.GetProperty("storageClass").GetString());
    }

    [Fact]
    public async Task Apps_and_sources_go_over_by_id_and_url()
    {
        var seed = await SeedAsync(Add()
            .WithApps("vaultwarden", "gitea")
            .WithApp("uptime-kuma", "Monitoring", deploy: true)
            .WithAppSource("acme", "https://apps.acme.test/apps.json"));

        Assert.Equal(["vaultwarden", "gitea", "uptime-kuma"],
            seed.GetProperty("apps").EnumerateArray().Select(a => a.GetProperty("id").GetString()));
        Assert.Equal("Monitoring", seed.GetProperty("apps").EnumerateArray().Last().GetProperty("name").GetString());
        Assert.Equal("acme", seed.GetProperty("appSources").EnumerateArray().Single().GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_seeded_folder_is_mounted_read_only_and_imported_from_inside()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aspireui-seedtest-" + Guid.NewGuid().ToString("n"), "edge");
        Directory.CreateDirectory(dir);

        var builder = Add().WithSeedFromDirectory(dir, "Edge");
        var seed = await SeedAsync(builder);

        var stack = seed.GetProperty("stacks").EnumerateArray().Single();
        Assert.Equal("Edge", stack.GetProperty("name").GetString());
        Assert.Equal("/seed/stacks/edge", stack.GetProperty("path").GetString());
        Assert.True(Assert.Single(builder.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/seed/stacks/edge").IsReadOnly);
    }

    [Fact]
    public async Task A_repository_is_cloned_inside_and_nothing_is_mounted_for_it()
    {
        var builder = Add().WithSeedFromGit("https://github.com/acme/app.git", "main", "deploy", "App");
        var seed = await SeedAsync(builder);

        var stack = seed.GetProperty("stacks").EnumerateArray().Single();
        Assert.Equal("https://github.com/acme/app.git", stack.GetProperty("git").GetString());
        Assert.Equal("main", stack.GetProperty("branch").GetString());
        Assert.Equal("deploy", stack.GetProperty("subdir").GetString());
        Assert.DoesNotContain(builder.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target.StartsWith("/seed/stacks", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WithAutoDeploy_asks_for_the_seeded_stacks_to_be_deployed()
    {
        Assert.True((await SeedAsync(Add().WithApps("gitea").WithAutoDeploy())).GetProperty("deploy").GetBoolean());
        Assert.False((await SeedAsync(Add().WithApps("gitea"))).GetProperty("deploy").GetBoolean());
    }

    [Fact]
    public async Task Settings_go_over_as_the_generic_setting_variables()
    {
        var env = await EnvAsync(Add()
            .WithPublicHost("apps.example.com")
            .WithNginxProxyManager("http://npm:81", "admin@example.com", "npm-password")
            .WithNotifications(webhookUrl: "https://hooks.example.com/x")
            .WithBackupSchedule(12, 3)
            .WithHostedDashboards("dash-token")
            .WithSetting("SomethingElse", "42")
            .Resource);

        Assert.Equal("apps.example.com", env["ASPIREUI_SET_PublicHost"]);
        Assert.Equal("true", env["ASPIREUI_SET_NpmEnabled"]);
        Assert.Equal("http://npm:81", env["ASPIREUI_SET_NpmBaseUrl"]);
        Assert.Equal("https://hooks.example.com/x", env["ASPIREUI_SET_NotifyWebhookUrl"]);
        Assert.Equal("12", env["ASPIREUI_SET_BackupIntervalHours"]);
        Assert.Equal("3", env["ASPIREUI_SET_BackupRetain"]);
        Assert.Equal("true", env["ASPIREUI_SET_HostDashboard"]);
        Assert.Equal("dash-token", env["ASPIREUI_SET_DashboardToken"]);
        Assert.Equal("42", env["ASPIREUI_SET_SomethingElse"]);
    }

    [Fact]
    public void WithoutDockerSocket_removes_the_mount_and_WithDockerHost_replaces_it()
    {
        var plain = Add().WithoutDockerSocket().Resource;
        Assert.DoesNotContain(plain.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target.EndsWith("docker.sock", StringComparison.Ordinal));

        var remote = Add().WithDockerHost("ssh://deploy@nas.local").Resource;
        Assert.DoesNotContain(remote.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target.EndsWith("docker.sock", StringComparison.Ordinal));
    }

    [Fact]
    public void WithDataBindMount_replaces_the_named_volume()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aspireui-seedtest-" + Guid.NewGuid().ToString("n"));
        var res = Add().WithDataBindMount(dir).Resource;

        var data = Assert.Single(res.Annotations.OfType<ContainerMountAnnotation>(), m => m.Target == "/data");
        Assert.Equal(ContainerMountType.BindMount, data.Type);
        Assert.Equal(dir, data.Source);
    }

    [Fact]
    public async Task A_seed_file_is_mounted_and_pointed_at()
    {
        var file = Path.Combine(Path.GetTempPath(), "aspireui-seedtest-" + Guid.NewGuid().ToString("n"), "aspireui.seed.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{}");

        var builder = Add().WithSeedFile(file);
        Assert.Equal("/seed/file/aspireui.seed.json", (await EnvAsync(builder.Resource))["ASPIREUI_SEED_FILE"]);
    }
}
