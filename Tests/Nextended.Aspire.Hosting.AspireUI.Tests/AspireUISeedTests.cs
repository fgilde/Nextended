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

        // A value is a literal, a parameter (which has its value here), or something only the running
        // app can resolve — an endpoint's url. For the last kind the manifest expression is both what
        // there is to see and what is worth asserting: waiting for a real url would wait for ever,
        // because nothing has allocated the port.
        var values = new Dictionary<string, string>();
        foreach (var (key, value) in ctx.EnvironmentVariables)
            values[key] = value switch
            {
                string s => s,
                ParameterResource p => await ((IValueProvider)p).GetValueAsync(default) ?? "",
                IManifestExpressionProvider expression => expression.ValueExpression,
                IValueProvider provider => await provider.GetValueAsync(default) ?? "",
                _ => value?.ToString() ?? "",
            };
        return values;
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

    [Fact]
    public async Task Single_sign_on_goes_over_as_settings_and_the_secret_can_be_a_parameter()
    {
        var b = DistributedApplication.CreateBuilder();
        var secret = b.AddParameter("sso-secret", secret: true, value: "from-the-parameter");
        var env = await EnvAsync(b.AddAspireUI()
            .WithSingleSignOn("https://id.example.com/realms/main/", "aspireui", secret,
                label: "Keycloak", groupsClaim: "groups", adminGroup: "aspireui-admins",
                defaultPermissions: AspireUIPermissions.AppUser)
            .Resource);

        Assert.Equal("true", env["ASPIREUI_SET_OidcEnabled"]);
        // The trailing slash is dropped so discovery does not build a double one.
        Assert.Equal("https://id.example.com/realms/main", env["ASPIREUI_SET_OidcAuthority"]);
        Assert.Equal("aspireui", env["ASPIREUI_SET_OidcClientId"]);
        Assert.Equal("Keycloak", env["ASPIREUI_SET_OidcLabel"]);
        Assert.Equal("groups", env["ASPIREUI_SET_OidcGroupsClaim"]);
        Assert.Equal("aspireui-admins", env["ASPIREUI_SET_OidcAdminGroup"]);
        Assert.Equal("app-user", env["ASPIREUI_SET_OidcDefaultPermissions"]);
        Assert.Equal("from-the-parameter", env["ASPIREUI_SET_OidcClientSecret"]);
    }

    [Fact]
    public async Task Off_site_backups_pick_one_kind_and_its_own_fields()
    {
        var s3 = await EnvAsync(Add().WithS3Backups("bucket", "AKIA", "shh", endpoint: "https://minio.local",
            region: "eu-central-1", pathStyle: true).Resource);
        Assert.Equal("s3", s3["ASPIREUI_SET_BackupRemoteKind"]);
        Assert.Equal("bucket", s3["ASPIREUI_SET_BackupS3Bucket"]);
        Assert.Equal("https://minio.local", s3["ASPIREUI_SET_BackupS3Endpoint"]);
        Assert.Equal("true", s3["ASPIREUI_SET_BackupS3PathStyle"]);

        var dav = await EnvAsync(Add().WithWebDavBackups("https://cloud.example.com/dav", "kim", "app-password").Resource);
        Assert.Equal("webdav", dav["ASPIREUI_SET_BackupRemoteKind"]);
        Assert.Equal("https://cloud.example.com/dav", dav["ASPIREUI_SET_BackupWebDavUrl"]);

        var keyFile = Path.Combine(Path.GetTempPath(), "aspireui-seedtest-" + Guid.NewGuid().ToString("n"), "backup_key");
        Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);
        File.WriteAllText(keyFile, "-----BEGIN OPENSSH PRIVATE KEY-----");
        var builder = Add().WithSshBackups("nas.local", "deploy", "/srv/backups", keyFile, port: 2222);
        var ssh = await EnvAsync(builder.Resource);
        Assert.Equal("sftp", ssh["ASPIREUI_SET_BackupRemoteKind"]);
        Assert.Equal("deploy", ssh["ASPIREUI_SET_BackupSftpUser"]);
        Assert.Equal("2222", ssh["ASPIREUI_SET_BackupSftpPort"]);
        // The key is mounted, and what travels is the path inside the container.
        Assert.Equal("/seed/keys/backup_key", ssh["ASPIREUI_SET_BackupSftpKeyFile"]);
        Assert.True(Assert.Single(builder.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/seed/keys/backup_key").IsReadOnly);
    }

    [Fact]
    public async Task Audit_retention_is_days_and_never_negative()
    {
        Assert.Equal("30", (await EnvAsync(Add().WithAuditRetention(30).Resource))["ASPIREUI_SET_AuditRetainDays"]);
        Assert.Equal("0", (await EnvAsync(Add().WithAuditRetention(-5).Resource))["ASPIREUI_SET_AuditRetainDays"]);
    }

    [Fact]
    public async Task WithSettings_writes_the_keys_aspireui_reads_and_leaves_the_rest_alone()
    {
        var env = await EnvAsync(Add().WithSettings(s =>
        {
            s.PublicHost = "192.168.1.50";
            s.HostDashboards = true;
            s.ProxyEnabled = true;
            s.ProxyBaseUrl = "http://npm:81";
            s.ProxyEmail = "admin@example.com";
            s.BackupIntervalHours = 24;
            s.BackupRetain = 14;
            s.MaxImportFileMb = 50;
            s.RespectGitignore = false;
            s.AuditRetainDays = 30;
            s.AiKind = "http";
            s.AiBaseUrl = "http://ollama:11434";
            s.AiModel = "llama3.2";
        }).Resource);

        Assert.Equal("192.168.1.50", env["ASPIREUI_SET_PublicHost"]);
        Assert.Equal("true", env["ASPIREUI_SET_HostDashboard"]);
        Assert.Equal("http://npm:81", env["ASPIREUI_SET_NpmBaseUrl"]);
        Assert.Equal("24", env["ASPIREUI_SET_BackupIntervalHours"]);
        Assert.Equal("14", env["ASPIREUI_SET_BackupRetain"]);
        Assert.Equal("50", env["ASPIREUI_SET_MaxImportFileMb"]);
        Assert.Equal("false", env["ASPIREUI_SET_RespectGitignore"]);
        Assert.Equal("30", env["ASPIREUI_SET_AuditRetainDays"]);
        Assert.Equal("llama3.2", env["ASPIREUI_SET_AiModel"]);

        // Nothing was invented for the properties that were left alone.
        Assert.DoesNotContain("ASPIREUI_SET_NpmPassword", env.Keys);
        Assert.DoesNotContain("ASPIREUI_SET_NotifyWebhookUrl", env.Keys);
        Assert.DoesNotContain("ASPIREUI_SET_FORCE", env.Keys);
    }

    [Fact]
    public async Task Nothing_set_writes_nothing_and_force_is_opt_in()
    {
        Assert.DoesNotContain("ASPIREUI_SET_PublicHost", (await EnvAsync(Add().WithSettings(_ => { }).Resource)).Keys);

        var forced = await EnvAsync(Add().WithSettings(s => { s.PublicHost = "h"; s.ForceOnEveryStart = true; }).Resource);
        Assert.Equal("true", forced["ASPIREUI_SET_FORCE"]);
    }

    [Fact]
    public async Task A_secret_setting_can_come_from_a_parameter()
    {
        var b = DistributedApplication.CreateBuilder();
        var password = b.AddParameter("npm-password", secret: true, value: "from-the-parameter");
        var env = await EnvAsync(b.AddAspireUI().WithSetting("NpmPassword", password).Resource);

        Assert.Equal("from-the-parameter", env["ASPIREUI_SET_NpmPassword"]);
    }

    // --- The assistant backend, in every shape the AppHost can say it -----------------------------

    [Fact]
    public async Task A_url_and_a_model_are_enough_and_the_kind_is_set_with_them()
    {
        var env = await EnvAsync(Add().WithAssistant("https://api.openai.com/", "gpt-4o-mini", "sk-x", "OpenAI").Resource);

        Assert.Equal("https://api.openai.com", env["ASPIREUI_AI_BASE_URL"]);
        Assert.Equal("gpt-4o-mini", env["ASPIREUI_AI_MODEL"]);
        Assert.Equal("sk-x", env["ASPIREUI_AI_API_KEY"]);
        // Without this a stack that used to point at a CLI would keep that setting.
        Assert.Equal("http", env["ASPIREUI_SET_AiKind"]);
        Assert.Equal("OpenAI", env["ASPIREUI_SET_AiProviderLabel"]);
    }

    [Fact]
    public async Task The_key_can_come_from_a_parameter_so_it_stays_out_of_the_manifest()
    {
        var b = DistributedApplication.CreateBuilder();
        var key = b.AddParameter("openai-key", secret: true, value: "sk-from-the-parameter");
        var env = await EnvAsync(b.AddAspireUI().WithAssistant("https://api.openai.com", key, "gpt-4o-mini").Resource);

        Assert.Equal("sk-from-the-parameter", env["ASPIREUI_AI_API_KEY"]);
        Assert.Equal("gpt-4o-mini", env["ASPIREUI_AI_MODEL"]);
    }

    [Fact]
    public async Task An_endpoint_that_is_only_known_at_start_works_too()
    {
        var b = DistributedApplication.CreateBuilder();
        var server = b.AddContainer("vllm", "vllm/vllm-openai").WithHttpEndpoint(targetPort: 8000, name: "http");
        var env = await EnvAsync(b.AddAspireUI()
            .WithAssistant(ReferenceExpression.Create($"{server.GetEndpoint("http")}/v1"), "qwen3-8b").Resource);

        Assert.Contains("/v1", env["ASPIREUI_AI_BASE_URL"]);
        Assert.Equal("qwen3-8b", env["ASPIREUI_AI_MODEL"]);
    }

    [Fact]
    public async Task A_model_server_in_the_stack_is_waited_for_and_named()
    {
        var b = DistributedApplication.CreateBuilder();
        var ollama = b.AddContainer("ollama", "ollama/ollama").WithHttpEndpoint(targetPort: 11434, name: "http");
        var ui = b.AddAspireUI().WithOllamaAssistant(ollama);

        var env = await EnvAsync(ui.Resource);
        Assert.EndsWith("/v1", env["ASPIREUI_AI_BASE_URL"]);
        Assert.Equal("llama3.2", env["ASPIREUI_AI_MODEL"]);
        Assert.Equal("Ollama", env["ASPIREUI_SET_AiProviderLabel"]);
        Assert.Equal("llama3.2", ui.Resource.AssistantModel);

        // The doc comment used to claim this and the code did not do it.
        var waits = ui.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waits, w => w.Resource.Name == "ollama");
    }

    [Fact]
    public async Task A_server_with_several_endpoints_can_be_told_which_one_and_which_path()
    {
        var b = DistributedApplication.CreateBuilder();
        var localAi = b.AddContainer("localai", "localai/localai")
            .WithHttpEndpoint(targetPort: 8080, name: "api")
            .WithHttpEndpoint(targetPort: 9090, name: "metrics");

        var env = await EnvAsync(b.AddAspireUI()
            .WithAssistant(localAi, "qwen3-8b", apiPath: "openai/v1", endpointName: "api").Resource);

        // The path is normalised whether or not it was written with a leading slash.
        Assert.EndsWith("/openai/v1", env["ASPIREUI_AI_BASE_URL"]);
    }

    [Fact]
    public void A_server_with_no_endpoint_says_what_to_do_instead()
    {
        var b = DistributedApplication.CreateBuilder();
        var bare = b.AddContainer("model-server", "some/image");
        var error = Assert.Throws<InvalidOperationException>(() => b.AddAspireUI().WithAssistant(bare, "a-model"));
        Assert.Contains("WithAssistant(endpoint, model)", error.Message);
    }

    [Fact]
    public async Task A_cli_assistant_is_a_setting_and_only_a_known_tool()
    {
        var env = await EnvAsync(Add().WithCliAssistant("ollama", "llama3.2").Resource);
        Assert.Equal("cli", env["ASPIREUI_SET_AiKind"]);
        Assert.Equal("ollama", env["ASPIREUI_SET_AiCliTool"]);
        Assert.Equal("llama3.2", env["ASPIREUI_SET_AiModel"]);
        // No endpoint: this backend is a process on the host.
        Assert.DoesNotContain("ASPIREUI_AI_BASE_URL", env.Keys);

        Assert.Throws<ArgumentException>(() => Add().WithCliAssistant("chatgpt-cli"));
    }

    [Fact]
    public async Task WithAi_still_works_and_means_the_same_thing()
    {
        var env = await EnvAsync(Add().WithAi("http://ollama:11434", "llama3.2", "ignored").Resource);
        Assert.Equal("http://ollama:11434", env["ASPIREUI_AI_BASE_URL"]);
        Assert.Equal("llama3.2", env["ASPIREUI_AI_MODEL"]);
        Assert.Equal("http", env["ASPIREUI_SET_AiKind"]);
    }
}
