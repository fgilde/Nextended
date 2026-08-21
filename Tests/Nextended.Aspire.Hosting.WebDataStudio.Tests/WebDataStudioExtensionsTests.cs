using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

public class WebDataStudioExtensionsTests
{
    /// <summary>
    /// Stands in for a hosting package's database resource. The engine is read from the type
    /// name, so the name of this class is the thing under test in the detection cases.
    /// </summary>
    private sealed class FakePostgresDatabaseResource(string name, string connectionString)
        : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }

    private sealed class MysteryResource(string name, string connectionString)
        : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }

    private static IResourceBuilder<WebDataStudioResource> Add() =>
        DistributedApplication.CreateBuilder().AddWebDataStudio();

    private static IResourceBuilder<FakePostgresDatabaseResource> Database(
        IDistributedApplicationBuilder builder, string name = "shop") =>
        builder.AddResource(new FakePostgresDatabaseResource(name, $"Host=db;Database={name}"));

    /// <summary>Runs the environment callbacks and keeps the values that are plain strings.</summary>
    private static async Task<Dictionary<string, string>> EnvOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        return context.EnvironmentVariables
            .Where(pair => pair.Value is string)
            .ToDictionary(pair => pair.Key, pair => (string)pair.Value);
    }

    private static async Task<HashSet<string>> EnvKeysOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        return [.. context.EnvironmentVariables.Keys];
    }

    [Fact]
    public void AddWebDataStudio_CreatesContainer_WithImageEndpointAndVolume()
    {
        var resource = Add().Resource;

        Assert.Equal("webdatastudio", resource.Name);

        var image = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Contains("webdatastudio", image.Image);
        Assert.Equal("latest", image.Tag);

        var http = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>(), e => e.Name == "http");
        Assert.Equal(8080, http.TargetPort);

        var mount = Assert.Single(resource.Annotations.OfType<ContainerMountAnnotation>(), m => m.Target == "/data");
        Assert.Contains("webdatastudio-data-webdatastudio", mount.Source ?? "");
    }

    [Fact]
    public void Volume_IsPerInstance_SoStudiosDontShareSavedConnections()
    {
        var builder = DistributedApplication.CreateBuilder();
        var first = builder.AddWebDataStudio("first").Resource;
        var second = builder.AddWebDataStudio("second").Resource;

        static string Volume(IResource resource) => resource.Annotations
            .OfType<ContainerMountAnnotation>().First(m => m.Target == "/data").Source ?? "";

        Assert.NotEqual(Volume(first), Volume(second));
        Assert.Contains("first", Volume(first));
        Assert.Contains("second", Volume(second));
    }

    [Fact]
    public void CustomNameImageAndTag_AreHonored()
    {
        var resource = DistributedApplication.CreateBuilder()
            .AddWebDataStudio("studio", image: "myrepo/webdatastudio", tag: "v1").Resource;

        Assert.Equal("studio", resource.Name);
        var image = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("myrepo/webdatastudio", image.Image);
        Assert.Equal("v1", image.Tag);
    }

    [Fact]
    public async Task WithLogin_SetsBothVariablesAndRecordsTheUser()
    {
        var studio = Add().WithLogin("admin", "change-me-please");
        var env = await EnvOf(studio.Resource);

        Assert.Equal("admin", studio.Resource.Username);
        Assert.Equal("admin", env["WDS_USER"]);
        Assert.Equal("change-me-please", env["WDS_PASSWORD"]);
    }

    /// Two calls mean two people can sign in. Before this, the second call silently replaced the
    /// first and only the last account existed.
    [Fact]
    public async Task WithLogin_TwiceAddsBothAccounts()
    {
        var studio = Add()
            .WithLogin("hans", "hans")
            .WithLogin("pete", "pete");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("hans:admin:hans;pete:admin:pete", env["WDS_USERS"]);
        Assert.DoesNotContain("WDS_USER", env.Keys);
        Assert.Equal(["hans", "pete"], studio.Resource.Accounts.Select(a => a.Name));
        // The first one stays the name the publish warning talks about.
        Assert.Equal("hans", studio.Resource.Username);
    }

    [Fact]
    public async Task WithUser_CarriesTheRoleAndTheConnections()
    {
        var studio = Add()
            .WithLogin("ada", "one")
            .WithUser("grace", "two", StudioRoles.Viewer, "shop", "warehouse")
            .WithUser("eve", "three", StudioRoles.Editor);

        var env = await EnvOf(studio.Resource);

        Assert.Equal("ada:admin:one;grace:viewer:two:shop,warehouse;eve:editor:three",
            env["WDS_USERS"]);
    }

    [Fact]
    public void WithUser_RefusesARoleTheStudioDoesNotHave()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithUser("ada", "one", "wizard"));
    }

    /// Saying the same name twice is a correction, not two accounts that shadow each other.
    [Fact]
    public async Task TheSameNameTwiceKeepsTheLastOne()
    {
        var studio = Add()
            .WithLogin("ada", "old")
            .WithLogin("ada", "new");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("ada", env["WDS_USER"]);
        Assert.Equal("new", env["WDS_PASSWORD"]);
        Assert.Single(studio.Resource.Accounts);
    }

    /// One admin account is what the single-account variables were made for, and what every app
    /// host that already uses WithLogin writes today.
    [Fact]
    public async Task OneAdminStillUsesTheSingleAccountVariables()
    {
        var env = await EnvOf(Add().WithLogin("admin", "secret").Resource);

        Assert.Equal("admin", env["WDS_USER"]);
        Assert.Equal("secret", env["WDS_PASSWORD"]);
        Assert.DoesNotContain("WDS_USERS", env.Keys);
    }

    [Fact]
    public async Task AViewerAloneBecomesAUsersEntry()
    {
        var env = await EnvOf(Add().WithUser("grace", "two").Resource);

        Assert.Equal("grace:viewer:two", env["WDS_USERS"]);
        Assert.DoesNotContain("WDS_USER", env.Keys);
    }

    /// A parameterised password must stay a reference: resolved at start, never in the manifest.
    [Fact]
    public async Task AParameterPasswordKeepsItsReferenceInsideTheUsersList()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("grace-password", secret: true);
        var studio = builder.AddWebDataStudio()
            .WithLogin("ada", "one")
            .WithUser("grace", password, StudioRoles.Editor);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_USERS", keys);
        // Not a plain string, because half of it is resolved later.
        Assert.DoesNotContain("WDS_USERS", plain.Keys);
    }

    [Fact]
    public async Task WithoutLogin_NothingGuardsTheStudio()
    {
        var keys = await EnvKeysOf(Add().Resource);

        // No login variables at all is what turns the login screen off.
        Assert.DoesNotContain("WDS_USER", keys);
        Assert.DoesNotContain("WDS_PASSWORD", keys);
    }

    [Fact]
    public async Task WithLogin_TakesAParameterForThePassword()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("studio-password", secret: true);
        var studio = builder.AddWebDataStudio().WithLogin("admin", password);

        var keys = await EnvKeysOf(studio.Resource);
        Assert.Contains("WDS_PASSWORD", keys);
        Assert.Equal("admin", studio.Resource.Username);
    }

    [Fact]
    public async Task TheStudioIsNamedAfterItsResource()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio("analytics-studio");

        Assert.Equal("analytics-studio", studio.Resource.Title);
        Assert.Equal("analytics-studio", (await EnvOf(studio.Resource))["WDS_TITLE"]);
    }

    [Fact]
    public async Task WithTitle_OverridesTheName()
    {
        var studio = Add().WithTitle("Analytics");

        Assert.Equal("Analytics", studio.Resource.Title);
        Assert.Equal("Analytics", (await EnvOf(studio.Resource))["WDS_TITLE"]);
    }

    [Fact]
    public async Task WithTitle_Null_LeavesTheStudioUnnamed()
    {
        var studio = Add().WithTitle(null);

        // The studio reads an empty value as "no name" and shows nothing at all.
        Assert.Null(studio.Resource.Title);
        Assert.Equal("", (await EnvOf(studio.Resource))["WDS_TITLE"]);
    }

    [Fact]
    public async Task Options_AreWrittenAsTheStudioSpellsThem()
    {
        var studio = Add()
            .WithReadOnly()
            .WithMaxRows(25_000)
            .WithQueryTimeout(TimeSpan.FromMinutes(2))
            .WithSessionLimits(maxSessions: 4, idleTimeout: TimeSpan.FromSeconds(90))
            .WithSecretKey("dGhpcy1pcy1ub3QtYS1yZWFsLWtleS1qdXN0LTMyLWJ5dGVz");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("true", env["WDS_READONLY"]);
        Assert.Equal("25000", env["WDS_MAX_ROWS"]);
        Assert.Equal("120", env["WDS_QUERY_TIMEOUT_SECONDS"]);
        Assert.Equal("4", env["WDS_MAX_SESSIONS"]);
        Assert.Equal("90", env["WDS_IDLE_TIMEOUT_SECONDS"]);
        Assert.Equal("dGhpcy1pcy1ub3QtYS1yZWFsLWtleS1qdXN0LTMyLWJ5dGVz", env["WDS_SECRET_KEY"]);
    }

    [Fact]
    public void RejectedOptions_SayWhy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithMaxRows(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithQueryTimeout(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithSessionLimits(maxSessions: -1));
    }

    [Fact]
    public async Task WithReference_AttachesTheConnectionAndItsEngine()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio().WithReference(Database(builder));

        var env = await EnvOf(studio.Resource);
        var keys = await EnvKeysOf(studio.Resource);

        Assert.Contains("WDS_CONN_SHOP", keys);
        Assert.Equal("postgresql", env["WDS_CONN_SHOP_ENGINE"]);
        Assert.Equal(["SHOP"], studio.Resource.ConnectionNames);
    }

    [Fact]
    public async Task WithReference_CarriesTheExtrasThroughToTheStudio()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio()
            .WithReference(Database(builder), readOnly: true, group: "Production", color: "#e03131");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("true", env["WDS_CONN_SHOP_READONLY"]);
        Assert.Equal("Production", env["WDS_CONN_SHOP_GROUP"]);
        Assert.Equal("#e03131", env["WDS_CONN_SHOP_COLOR"]);
    }

    [Fact]
    public async Task AResourceNameBecomesAnEnvironmentSafeLabel()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio().WithReference(Database(builder, "shop-db"));

        Assert.Contains("WDS_CONN_SHOP_DB", await EnvKeysOf(studio.Resource));
    }

    [Fact]
    public void AConnectionMayNotBeCalledAfterAReservedSuffix()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio();

        // WDS_CONN_CACHE_GROUP would configure a connection called CACHE, not create one.
        var error = Assert.Throws<ArgumentException>(() =>
            studio.WithReference(Database(builder, "cache-group")));

        Assert.Contains("_GROUP", error.Message);
    }

    [Fact]
    public async Task AttachingTheSameConnectionTwiceChangesNothing()
    {
        var builder = DistributedApplication.CreateBuilder();
        var database = Database(builder);

        var studio = builder.AddWebDataStudio()
            .WithReference(database)
            .WithReference(database);

        Assert.Equal(["SHOP"], studio.Resource.ConnectionNames);
        Assert.Contains("WDS_CONN_SHOP", await EnvKeysOf(studio.Resource));
    }

    [Fact]
    public async Task WithConnection_TakesADatabaseFromOutsideTheStack()
    {
        var studio = Add().WithConnection("LEGACY", "Host=old-box;Database=legacy;Username=ro",
            WebDataStudioEngine.PostgreSql, readOnly: true, group: "Legacy");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("Host=old-box;Database=legacy;Username=ro", env["WDS_CONN_LEGACY"]);
        Assert.Equal("postgresql", env["WDS_CONN_LEGACY_ENGINE"]);
        Assert.Equal("true", env["WDS_CONN_LEGACY_READONLY"]);
        Assert.Equal("Legacy", env["WDS_CONN_LEGACY_GROUP"]);
    }

    [Fact]
    public async Task AnUnknownResourceGetsNoEngineRatherThanAWrongOne()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mystery = builder.AddResource(new MysteryResource("thing", "Host=x"));
        var studio = builder.AddWebDataStudio().WithReference(mystery);

        var keys = await EnvKeysOf(studio.Resource);

        Assert.Contains("WDS_CONN_THING", keys);
        Assert.DoesNotContain("WDS_CONN_THING_ENGINE", keys);
    }

    [Fact]
    public async Task AnExplicitEngineBeatsTheDetectedOne()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio()
            .WithReference(Database(builder), engine: WebDataStudioEngine.ClickHouse);

        Assert.Equal("clickhouse", (await EnvOf(studio.Resource))["WDS_CONN_SHOP_ENGINE"]);
    }

    [Fact]
    public async Task WithWebDataStudio_CreatesOneStudioAndSharesIt()
    {
        var builder = DistributedApplication.CreateBuilder();

        Database(builder, "shop").WithWebDataStudio();
        Database(builder, "orders").WithWebDataStudio();

        var studio = Assert.Single(builder.Resources.OfType<WebDataStudioResource>());
        Assert.Equal(["SHOP", "ORDERS"], studio.ConnectionNames);

        var keys = await EnvKeysOf(studio);
        Assert.Contains("WDS_CONN_SHOP", keys);
        Assert.Contains("WDS_CONN_ORDERS", keys);
    }

    [Fact]
    public void WithWebDataStudio_MakesASecondStudioWhenAskedByName()
    {
        var builder = DistributedApplication.CreateBuilder();

        Database(builder, "shop").WithWebDataStudio();
        Database(builder, "warehouse").WithWebDataStudio(studioName: "analytics-studio");
        Database(builder, "events").WithWebDataStudio(studioName: "analytics-studio");

        var studios = builder.Resources.OfType<WebDataStudioResource>().ToList();

        Assert.Equal(2, studios.Count);
        Assert.Equal(["SHOP"], studios.Single(s => s.Name == "webdatastudio").ConnectionNames);
        Assert.Equal(["WAREHOUSE", "EVENTS"],
            studios.Single(s => s.Name == "analytics-studio").ConnectionNames);
    }

    [Fact]
    public void WithWebDataStudio_AttachesToAStudioYouBuiltYourself()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio("admin-studio").WithReadOnly();

        Database(builder, "shop").WithWebDataStudio(studio, color: "#e03131");

        var only = Assert.Single(builder.Resources.OfType<WebDataStudioResource>());
        Assert.Equal("admin-studio", only.Name);
        Assert.Equal(["SHOP"], only.ConnectionNames);
    }

    [Fact]
    public async Task WithWebDataStudio_RunsTheConfigureCallbackOnTheStudio()
    {
        var builder = DistributedApplication.CreateBuilder();

        Database(builder).WithWebDataStudio(studio => studio.WithLogin("admin", "pw").WithReadOnly());

        var studio = Assert.Single(builder.Resources.OfType<WebDataStudioResource>());
        var env = await EnvOf(studio);

        Assert.Equal("admin", env["WDS_USER"]);
        Assert.Equal("true", env["WDS_READONLY"]);
    }

    [Fact]
    public void WithWebDataStudio_ReturnsTheDatabaseSoTheChainContinues()
    {
        var builder = DistributedApplication.CreateBuilder();
        var database = Database(builder);

        Assert.Same(database.Resource, database.WithWebDataStudio().Resource);
    }

    [Fact]
    public void WithDataBindMount_ReplacesTheDefaultVolume()
    {
        var resource = Add().WithDataBindMount("/tmp/wds").Resource;

        var mount = Assert.Single(resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/data");

        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Contains("wds", mount.Source ?? "");
    }

    [Fact]
    public void EngineIds_MatchWhatTheStudioReads()
    {
        Assert.Equal("postgresql", WebDataStudioEngine.PostgreSql.ToEngineId());
        Assert.Equal("sqlserver", WebDataStudioEngine.SqlServer.ToEngineId());
        Assert.Equal("mongodb", WebDataStudioEngine.MongoDb.ToEngineId());
        Assert.Equal("redis", WebDataStudioEngine.Redis.ToEngineId());
    }
}
