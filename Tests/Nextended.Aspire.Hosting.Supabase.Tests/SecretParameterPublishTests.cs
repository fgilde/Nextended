using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Observability;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// A stack whose secrets come from Aspire parameters, published for real: manifest and Azure
/// Container Apps bicep. Every container app shows its environment to anyone with read access
/// to it, so no secret may be in there as a value — each is a container app secret, filled from
/// a secure parameter at deployment. Locally the same parameters resolve to their values.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public sealed class SecretParameterPublishTests : IDisposable
{
    private static readonly Dictionary<string, string> Secrets = new()
    {
        ["jwt-secret"] = "jwt-secret-value-that-is-long-enough-1",
        ["anon-key"] = "anon.key.value-2",
        ["service-key"] = "service.key.value-3",
        ["db-password"] = "db-password-value-4",
        ["minio-password"] = "minio-password-value-5",
        ["admin-password"] = "admin-password-value-6",
    };

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"secrets-{Guid.NewGuid():N}");

    public SecretParameterPublishTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "observability", "grafana", "dashboards"));
        File.WriteAllText(Path.Combine(_root, "observability", "grafana", "dashboards", "app.json"), "{\"title\":\"app\"}");
    }

    public void Dispose()
    {
        SupabaseBuilderExtensions.StorageS3Backend = null;
        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private IDistributedApplicationBuilder Stack(string[] args, bool configured = true)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args });
        builder.Configuration["DcpPublisher:CliPath"] = "unused";
        builder.Configuration["DcpPublisher:DashboardPath"] = "unused";
        if (configured)
            foreach (var (name, value) in Secrets)
                builder.Configuration[$"Parameters:{name}"] = value;
        IResourceBuilder<ParameterResource> Secret(string name) => builder.AddParameter(name, secret: true);

        var admin = Secret("admin-password");
        if (builder.ExecutionContext.IsPublishMode)
        {
            builder.AddAzureContainerAppEnvironment("env");
            builder.AddMinioS3OnNfs("nfs", Secret("minio-password"));
        }

        var supabase = builder.AddSupabase("sb")
            .ConfigureDatabase(db => db.WithPassword(Secret("db-password")))
            .WithJwtSecret(Secret("jwt-secret"))
            .WithAnonKey(Secret("anon-key"))
            .WithServiceRoleKey(Secret("service-key"))
            .ConfigureStudio(studio => studio.WithLogin("admin", admin))
            .WithRegisteredUser("admin@example.com", admin, "Admin")
            .WithKongOpenTelemetry();

        builder.AddContainer("frontend", "nginx").WithSupabaseVite(supabase);
        builder.AddObservabilityStack(supabase, options =>
        {
            options.ConfigRootPath = Path.Combine(_root, "observability");
            options.DashboardsPath = Path.Combine(_root, "observability", "grafana", "dashboards");
            options.GrafanaAnonymousAdmin = false;
            options.GrafanaAdminPasswordParameter = admin;
        });
        return builder;
    }

    private Dictionary<string, string> Publish(bool configured = true)
    {
        var output = Path.Combine(_root, "out");
        var builder = Stack(["--operation", "publish", "--publisher", "manifest", "--output-path", Path.Combine(output, "aspire-manifest.json")], configured);
        using (var app = builder.Build())
            app.Run();
        return Directory.GetFiles(output, "*", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileName, File.ReadAllText)!;
    }

    [Fact]
    public void No_secret_value_is_in_the_manifest_or_bicep()
    {
        var files = Publish();

        Assert.Contains("sb-auth-containerapp.module.bicep", files.Keys);
        foreach (var (file, content) in files)
            foreach (var (name, value) in Secrets)
                Assert.False(content.Contains(value, StringComparison.Ordinal), $"{file} carries the value of '{name}'");
    }

    [Fact]
    public void Publishing_needs_no_value_the_deployment_supplies_them()
    {
        // azd runs the AppHost for every command (down, show, …) without the values; it fills
        // the parameters itself at deployment.
        var manifest = Publish(configured: false)["aspire-manifest.json"];

        foreach (var name in Secrets.Keys)
            Assert.Contains($"\"{name}\": {{", manifest);
    }

    [Theory]
    [InlineData("sb-db", "POSTGRES_PASSWORD")]
    [InlineData("sb-auth", "GOTRUE_DB_DATABASE_URL")]
    [InlineData("sb-auth", "GOTRUE_JWT_SECRET")]
    [InlineData("sb-rest", "PGRST_DB_URI")]
    [InlineData("sb-rest", "PGRST_JWT_SECRET")]
    [InlineData("sb-storage", "DATABASE_URL")]
    [InlineData("sb-storage", "SERVICE_KEY")]
    [InlineData("sb-storage", "AWS_SECRET_ACCESS_KEY")]
    [InlineData("sb-realtime", "DB_PASSWORD")]
    [InlineData("sb-realtime", "API_JWT_SECRET")]
    [InlineData("sb-meta", "PG_META_DB_PASSWORD")]
    [InlineData("sb-kong", "SUPABASE_ANON_KEY")]
    [InlineData("sb-kong", "SUPABASE_SERVICE_KEY")]
    [InlineData("sb", "POSTGRES_PASSWORD")]
    [InlineData("sb", "AUTH_JWT_SECRET")]
    [InlineData("sb", "DASHBOARD_PASSWORD")]
    [InlineData("sb-init", "DB_PASSWORD")]
    [InlineData("sb-init", "POST_INIT_VAR_user_password_0")]
    [InlineData("sb-init", "POST_INIT_VAR_" + ObservabilityStack.GrafanaPasswordVariable)]
    [InlineData("minio", "MINIO_ROOT_PASSWORD")]
    [InlineData("minio-init", "MINIO_PASS")]
    [InlineData("frontend", "SUPABASE_SERVICE_ROLE_KEY")]
    [InlineData("monitoring-grafana", "GF_SECURITY_ADMIN_PASSWORD")]
    [InlineData("monitoring-grafana", "GF_DATABASE_PASSWORD")]
    [InlineData("monitoring-grafana", "APP_DB_PASSWORD")]
    [InlineData("monitoring-postgres-exporter", "DATA_SOURCE_NAME")]
    [InlineData("monitoring-loki", "LOKI_S3_SECRET_KEY")]
    public void Each_secret_reaches_its_container_app_as_a_secret(string app, string variable)
    {
        var bicep = Publish()[$"{app}-containerapp.module.bicep"];

        Assert.Matches(new Regex($@"name: '{Regex.Escape(variable)}'\s+secretRef: ", RegexOptions.Multiline), bicep);
    }

    [Fact]
    public void The_key_expressions_hand_on_the_parameters_not_their_values()
    {
        // For an AppHost's own containers: the string properties resolve, the expressions don't.
        var builder = Stack(["--operation", "publish", "--publisher", "manifest"], configured: false);
        var stack = builder.Resources.OfType<Resources.SupabaseStackResource>().Single();

        Assert.Equal("{jwt-secret.value}", stack.JwtSecretExpression.ValueExpression);
        Assert.Equal("{anon-key.value}", stack.AnonKeyExpression.ValueExpression);
        Assert.Equal("{service-key.value}", stack.ServiceRoleKeyExpression.ValueExpression);
    }

    [Fact]
    public async Task The_post_init_sql_reads_the_user_password_at_runtime()
    {
        var builder = Stack(["--operation", "publish", "--publisher", "manifest"]);
        var init = (IResourceWithEnvironment)builder.Resources.Single(r => r.Name == "sb-init");

#pragma warning disable CS0618 // no model-only equivalent yet
        var env = await init.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
        var blob = string.Concat(Enumerable.Range(0, int.Parse(env["POST_INIT_SQL_GZ_PARTS"])).Select(i => env[$"POST_INIT_SQL_GZ_BASE64_{i}"]));
        using var gz = new System.IO.Compression.GZipStream(new MemoryStream(Convert.FromBase64String(blob)), System.IO.Compression.CompressionMode.Decompress);
        var sql = await new StreamReader(gz).ReadToEndAsync();

        Assert.Equal("{admin-password.value}", env["POST_INIT_VAR_user_password_0"]);
        Assert.Contains("user_password_0", env["POST_INIT_VAR_NAMES"].Split(' '));
        Assert.Contains("SELECT set_config('nextended.user_password_0', :'user_password_0', false)", sql);
        Assert.Contains("extensions.crypt(current_setting('nextended.user_password_0'), ", sql);
        Assert.DoesNotContain(Secrets["admin-password"], sql);
    }

    /// <summary>
    /// The environment as the callbacks produce it, without resolving endpoints — those only get
    /// values once a run has allocated them.
    /// </summary>
    private static async Task<Dictionary<string, object>> RawEnv(IDistributedApplicationBuilder builder, IResource resource)
    {
        var env = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(builder.ExecutionContext, env);
        foreach (var callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await callback.Callback(context);
        return env;
    }

    private static async Task<string?> ValueOf(object value) => value switch
    {
        string text => text,
        IValueProvider provider => await provider.GetValueAsync(),
        _ => value.ToString(),
    };

    [Fact]
    public async Task Locally_the_parameters_resolve_to_their_values()
    {
        var builder = Stack([]);
        var stack = builder.Resources.OfType<Resources.SupabaseStackResource>().Single();

        var auth = await RawEnv(builder, stack.Auth!.Resource);
        var studio = await RawEnv(builder, stack);
        Assert.Equal(Secrets["jwt-secret"], await ValueOf(auth["GOTRUE_JWT_SECRET"]));
        Assert.Equal(Secrets["admin-password"], await ValueOf(studio["DASHBOARD_PASSWORD"]));
        Assert.Equal(Secrets["db-password"], await ValueOf(studio["POSTGRES_PASSWORD"]));
        Assert.Contains(((ReferenceExpression)auth["GOTRUE_DB_DATABASE_URL"]).ValueProviders,
            provider => provider is ReferenceExpression { ValueProviders: [ParameterResource { Name: "db-password" }] });

        var kong = File.ReadAllText(Path.Combine(stack.InfraRootDirForTests()!, "config", "kong.yml"));
        Assert.Contains(Secrets["anon-key"], kong);
        var users = File.ReadAllText(Path.Combine(stack.InfraRootDirForTests()!, "scripts", "users.sql"));
        Assert.Contains($"extensions.crypt('{Secrets["admin-password"]}', ", users);
    }
}
