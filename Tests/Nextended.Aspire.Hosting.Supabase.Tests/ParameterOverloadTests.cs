using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// The parameter overloads let an AppHost keep passwords and keys in configuration instead of
/// source. They resolve the parameter to its value on purpose: the password and JWT secret are
/// written into generated SQL, connection strings and container environments while the model
/// is built, so a concrete value is needed at that point.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class ParameterOverloadTests
{
    private static IDistributedApplicationBuilder BuilderWith(params (string Name, string Value)[] parameters)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        foreach (var (name, value) in parameters)
            builder.Configuration[$"Parameters:{name}"] = value;
        return builder;
    }

    [Fact]
    public void Database_password_comes_from_the_parameter_value()
    {
        var builder = BuilderWith(("db-password", "from-configuration"));
        var password = builder.AddParameter("db-password", secret: true);

        var stack = builder.AddSupabase("sb").ConfigureDatabase(db => db.WithPassword(password));

        Assert.Equal("from-configuration", stack.Resource.Database!.Resource.Password);
    }

    [Fact]
    public void Jwt_secret_and_both_keys_come_from_parameter_values()
    {
        var builder = BuilderWith(
            ("jwt-secret", "a-secret-that-is-long-enough-for-hs256"),
            ("anon-key", "anon.jwt.value"),
            ("service-key", "service.jwt.value"));

        var stack = builder.AddSupabase("sb")
            .WithJwtSecret(builder.AddParameter("jwt-secret", secret: true))
            .WithAnonKey(builder.AddParameter("anon-key", secret: true))
            .WithServiceRoleKey(builder.AddParameter("service-key", secret: true));

        Assert.Equal("a-secret-that-is-long-enough-for-hs256", stack.Resource.JwtSecret);
        Assert.Equal("anon.jwt.value", stack.Resource.AnonKey);
        Assert.Equal("service.jwt.value", stack.Resource.ServiceRoleKey);
    }

    [Fact]
    public async Task Jwt_secret_reaches_every_service_that_validates_tokens()
    {
        // Regression: setting only the property left Auth/REST/Storage/Realtime/Studio on the
        // previous secret, so Auth signed tokens that PostgREST then rejected.
        const string secret = "a-brand-new-secret-that-is-long-enough";
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var stack = builder.AddSupabase("sb").WithJwtSecret(secret);

        var expectations = new (IResourceWithEnvironment? Resource, string Variable)[]
        {
            (stack.Resource.Auth?.Resource, "GOTRUE_JWT_SECRET"),
            (stack.Resource.Rest?.Resource, "PGRST_JWT_SECRET"),
            (stack.Resource.Rest?.Resource, "PGRST_APP_SETTINGS_JWT_SECRET"),
            (stack.Resource.Storage?.Resource, "PGRST_JWT_SECRET"),
            (stack.Resource.Realtime?.Resource, "API_JWT_SECRET"),
            (stack.Resource, "AUTH_JWT_SECRET"),
        };

        foreach (var (resource, variable) in expectations)
        {
            Assert.NotNull(resource);
#pragma warning disable CS0618 // no model-only equivalent yet
            var env = await resource!.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
            Assert.True(env.TryGetValue(variable, out var value), $"{variable} missing on {resource.Name}");
            Assert.Equal(secret, value);
        }
    }

    [Fact]
    public async Task Anon_key_reaches_the_services_that_hand_it_out()
    {
        const string anon = "brand.new.anon.key";
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var stack = builder.AddSupabase("sb").WithAnonKey(anon);

#pragma warning disable CS0618
        var storage = await stack.Resource.Storage!.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
        var studio = await ((IResourceWithEnvironment)stack.Resource).GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
        Assert.Equal(anon, storage["ANON_KEY"]);
        Assert.Equal(anon, studio["SUPABASE_ANON_KEY"]);
    }

    [Fact]
    public void Replacing_the_keys_also_updates_kongs_credentials()
    {
        // Regression with teeth: Kong authenticates against keyauth_credentials in kong.yml.
        // Setting new keys without refreshing that file makes Kong answer 401 to everything —
        // the stack starts, then no login, no data, no setup status.
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var stack = builder.AddSupabase("sb")
            .WithAnonKey("brand.new.anon")
            .WithServiceRoleKey("brand.new.service");

        var kongYml = Path.Combine(stack.Resource.InfraRootDirForTests()!, "config", "kong.yml");
        Assert.True(File.Exists(kongYml), $"kong.yml missing at {kongYml}");

        var yaml = File.ReadAllText(kongYml);
        Assert.Contains("brand.new.anon", yaml);
        Assert.Contains("brand.new.service", yaml);
        // the shipped demo keys must be gone
        Assert.DoesNotContain("supabase-demo", yaml);
    }

    [Fact]
    public async Task Replacing_the_keys_also_updates_kongs_environment_and_publish_template()
    {
        // Publish mode has no bind mount: Kong gets the keys as env vars and its whole config
        // as a base64 template that already embeds them. Both are written when the Kong
        // resource is created, so both must be refreshed when the keys change.
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
        });
        var stack = builder.AddSupabase("sb")
            .WithAnonKey("published.anon.key")
            .WithServiceRoleKey("published.service.key");

#pragma warning disable CS0618
        var env = await stack.Resource.Kong!.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618

        Assert.Equal("published.anon.key", env["SUPABASE_ANON_KEY"]);
        Assert.Equal("published.service.key", env["SUPABASE_SERVICE_KEY"]);

        var template = Encoding.UTF8.GetString(Convert.FromBase64String(env["KONG_CONFIG_TEMPLATE_BASE64"]));
        Assert.Contains("published.anon.key", template);
        Assert.Contains("published.service.key", template);
        Assert.DoesNotContain("supabase-demo", template);
    }

    [Fact]
    public void The_string_overloads_keep_working_unchanged()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });

        var stack = builder.AddSupabase("sb")
            .WithJwtSecret("plain-secret-that-is-long-enough-here")
            .ConfigureDatabase(db => db.WithPassword("plain-password"));

        Assert.Equal("plain-secret-that-is-long-enough-here", stack.Resource.JwtSecret);
        Assert.Equal("plain-password", stack.Resource.Database!.Resource.Password);
    }

    [Fact]
    public void A_null_parameter_is_rejected_instead_of_silently_ignored()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var stack = builder.AddSupabase("sb");

        Assert.Throws<ArgumentNullException>(() => stack.WithJwtSecret((IResourceBuilder<ParameterResource>)null!));
    }
}
