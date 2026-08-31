using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.DbTools.Tests;

/// What `WithCloneFrom` actually puts in the model.
///
/// A clone is a container resource rather than code in the app host, and that is the whole design:
/// the app host does not run when a stack is published, so anything that has to happen when a new
/// system is built out of an old one has to be *in the model*. These tests are about that model.
public class CloneResourceTests
{
    private static IDistributedApplicationBuilder Builder() =>
        DistributedApplication.CreateBuilder([]);

    private static ContainerResource Clone(IDistributedApplicationBuilder builder, string name) =>
        Assert.Single(builder.Resources.OfType<ContainerResource>(), r => r.Name == name);

    /// The script the clone runs: the last argument of `sh -c`.
    private static string ScriptOf(ContainerResource clone)
    {
        var args = clone.Annotations.OfType<CommandLineArgsCallbackAnnotation>();
        var collected = new List<object>();

        foreach (var annotation in args)
            annotation.Callback(new CommandLineArgsCallbackContext(collected)).GetAwaiter().GetResult();

        return collected.OfType<string>().Last();
    }

    /// What the container will be started with, read rather than resolved.
    ///
    /// Resolving would mean asking Aspire for the real values, and a value that comes from
    /// configuration — a connection string this stack was only *given* — has none in a test, which
    /// makes that call wait for something that is never coming. What each variable *is* answers
    /// every question here: a literal is its own text, and a reference is the expression that will
    /// produce one.
    private static Dictionary<string, string> EnvOf(ContainerResource clone)
    {
        var values = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            clone, values);

        foreach (var annotation in clone.Annotations.OfType<EnvironmentCallbackAnnotation>())
            annotation.Callback(context).GetAwaiter().GetResult();

        return values.ToDictionary(pair => pair.Key, pair => pair.Value switch
        {
            string text => text,
            ReferenceExpression expression => expression.Format,
            var other => other?.ToString() ?? "",
        });
    }

    [Fact]
    public void A_clone_is_a_container_named_after_the_target()
    {
        var builder = Builder();
        var source = builder.AddPostgres("prod").AddDatabase("shop");
        var target = builder.AddPostgres("dev").AddDatabase("dev-shop");

        target.WithCloneFrom(source);

        var clone = Clone(builder, "dev-shop-clone");
        Assert.Contains("pg_dump", ScriptOf(clone));
    }

    [Fact]
    public void The_name_can_be_said_instead()
    {
        var builder = Builder();
        var source = builder.AddPostgres("prod").AddDatabase("shop");
        var target = builder.AddPostgres("dev").AddDatabase("dev-shop");

        target.WithCloneFrom(source, new DbCloneOptions { Name = "northwind-copy" });

        Assert.NotNull(Clone(builder, "northwind-copy"));
    }

    [Fact]
    public void It_runs_in_the_engines_own_image_unless_told_otherwise()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("Host=elsewhere;Database=shop;Username=u;Password=p");

        var image = Assert.Single(Clone(builder, "shop-clone")
            .Annotations.OfType<ContainerImageAnnotation>());

        Assert.Equal("postgres", image.Image);
        Assert.Equal("17-alpine", image.Tag);
    }

    [Fact]
    public void An_air_gapped_stack_can_name_its_own_image()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("Host=elsewhere;Database=shop;Username=u;Password=p",
            new DbCloneOptions { Image = "registry.internal/postgres:17" });

        var image = Assert.Single(Clone(builder, "shop-clone")
            .Annotations.OfType<ContainerImageAnnotation>());

        // Aspire splits a tag off the name it is given, which is why the image is passed whole.
        Assert.Equal("registry.internal/postgres", image.Image);
        Assert.Equal("17", image.Tag);
    }

    [Fact]
    public void A_connection_string_is_taken_apart_now()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("postgres://ada:l0velace@staging.example.org:6543/shop");

        var env = EnvOf(Clone(builder, "shop-clone"));

        Assert.Equal("staging.example.org", env["CLONE_SOURCE_HOST"]);
        Assert.Equal("6543", env["CLONE_SOURCE_PORT"]);
        Assert.Equal("ada", env["CLONE_SOURCE_USER"]);
        Assert.Equal("l0velace", env["CLONE_SOURCE_PASSWORD"]);
        Assert.Equal("shop", env["CLONE_SOURCE_DB"]);

        // Nothing to take apart later, so no whole string travels.
        Assert.False(env.ContainsKey("CLONE_SOURCE_URL"));
    }

    [Fact]
    public void A_connection_string_resource_travels_whole()
    {
        var builder = Builder();
        var staging = builder.AddConnectionString("staging");
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom(staging);

        var env = EnvOf(Clone(builder, "shop-clone"));

        // Its value comes from configuration when the stack runs, so the script takes it apart.
        Assert.True(env.ContainsKey("CLONE_SOURCE_URL"));
        Assert.False(env.ContainsKey("CLONE_SOURCE_HOST"));

        // Which is why the prologue is in front of every recipe.
        Assert.Contains("_split \"$CLONE_SOURCE_URL\"", ScriptOf(Clone(builder, "shop-clone")));
    }

    [Fact]
    public void The_options_reach_the_script_as_the_questions_it_asks()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("Host=x;Database=shop;Username=u;Password=p",
            new DbCloneOptions { Overwrite = true, SchemaOnly = true, TimeoutSeconds = 90 });

        var env = EnvOf(Clone(builder, "shop-clone"));

        Assert.Equal("1", env["CLONE_OVERWRITE"]);
        Assert.Equal("1", env["CLONE_SCHEMA_ONLY"]);
        Assert.Equal("90", env["CLONE_TIMEOUT"]);

        // Overwriting and "only when empty" are the same question answered twice; overwrite wins.
        Assert.Equal("0", env["CLONE_ONLY_WHEN_EMPTY"]);
    }

    [Fact]
    public void Only_when_empty_is_what_a_clone_does_unless_told_otherwise()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("Host=x;Database=shop;Username=u;Password=p");

        Assert.Equal("1", EnvOf(Clone(builder, "shop-clone"))["CLONE_ONLY_WHEN_EMPTY"]);
    }

    [Fact]
    public void A_clone_waits_for_the_target_and_for_a_source_in_this_stack()
    {
        var builder = Builder();
        var source = builder.AddPostgres("prod").AddDatabase("shop");
        var target = builder.AddPostgres("dev").AddDatabase("dev-shop");

        target.WithCloneFrom(source);

        var waits = Clone(builder, "dev-shop-clone")
            .Annotations.OfType<WaitAnnotation>()
            .Select(wait => wait.Resource.Name)
            .ToList();

        Assert.Contains("dev-shop", waits);
        Assert.Contains("shop", waits);
    }

    [Fact]
    public void There_is_nothing_to_wait_for_outside_this_stack()
    {
        var builder = Builder();
        var target = builder.AddPostgres("dev").AddDatabase("shop");

        target.WithCloneFrom("Host=elsewhere;Database=shop;Username=u;Password=p");

        var waits = Clone(builder, "shop-clone")
            .Annotations.OfType<WaitAnnotation>()
            .Select(wait => wait.Resource.Name)
            .ToList();

        // The target, and the server it lives on, which Aspire adds for us. Nothing else: a source
        // outside this stack is not something the model can wait for, and the script's own waiting
        // is what covers it.
        Assert.Equal(["dev", "shop"], waits.Order().ToList());
    }

    [Fact]
    public void Sql_server_gets_a_second_container_only_when_it_has_to_replace_something()
    {
        var builder = Builder();
        var target = builder.AddSqlServer("dev").AddDatabase("orders");

        target.WithCloneFrom("Server=old,1433;Database=orders;User Id=sa;Password=p");

        Assert.DoesNotContain(builder.Resources, r => r.Name == "orders-clone-prepare");

        var second = Builder();
        var replacing = second.AddSqlServer("dev").AddDatabase("orders");

        replacing.WithCloneFrom("Server=old,1433;Database=orders;User Id=sa;Password=p",
            new DbCloneOptions { Overwrite = true });

        var prepare = Clone(second, "orders-clone-prepare");
        Assert.Contains("DROP DATABASE", ScriptOf(prepare));

        // And the clone itself does not start until the way has been cleared.
        Assert.Contains("orders-clone-prepare",
            Clone(second, "orders-clone").Annotations.OfType<WaitAnnotation>()
                .Select(wait => wait.Resource.Name));
    }

    [Fact]
    public void Every_engine_is_cloned_with_its_own_tools()
    {
        var builder = Builder();

        builder.AddPostgres("pg").AddDatabase("a").WithCloneFrom("Host=x;Database=a;Username=u;Password=p");
        builder.AddMySql("my").AddDatabase("b").WithCloneFrom("Server=x;Database=b;Uid=root;Pwd=p");
        builder.AddSqlServer("ms").AddDatabase("c").WithCloneFrom("Server=x;Database=c;User Id=sa;Password=p");
        builder.AddMongoDB("mo").AddDatabase("d").WithCloneFrom("mongodb://x:27017/d");
        builder.AddRedis("re").WithCloneFrom("redis://x:6379");

        Assert.Contains("pg_dump", ScriptOf(Clone(builder, "a-clone")));
        Assert.Contains("mysqldump", ScriptOf(Clone(builder, "b-clone")));
        Assert.Contains("sqlpackage", ScriptOf(Clone(builder, "c-clone")));
        Assert.Contains("mongodump", ScriptOf(Clone(builder, "d-clone")));
        Assert.Contains("REPLICAOF", ScriptOf(Clone(builder, "re-clone")));
    }

    [Fact]
    public void An_option_an_engine_cannot_honour_is_refused_before_anything_is_built()
    {
        var builder = Builder();
        var mongo = builder.AddMongoDB("mo").AddDatabase("events");
        var redis = builder.AddRedis("cache");

        // A collection is its documents and a key is its value: there is no shape to copy on its own.
        Assert.Throws<ArgumentException>(() =>
            mongo.WithCloneFrom("mongodb://x/events", new DbCloneOptions { SchemaOnly = true }));

        Assert.Throws<ArgumentException>(() =>
            redis.WithCloneFrom("redis://x", new DbCloneOptions { SchemaOnly = true }));

        // And the one that says both is nonsense whatever the engine.
        Assert.Throws<ArgumentException>(() =>
            builder.AddPostgres("pg").AddDatabase("shop").WithCloneFrom(
                "Host=x;Database=shop;Username=u;Password=p",
                new DbCloneOptions { SchemaOnly = true, DataOnly = true }));
    }

    [Fact]
    public void A_script_never_carries_windows_line_endings_into_a_container()
    {
        // A raw string in a C# file keeps that file's line endings, and a shell reading `do\r`
        // answers "syntax error near unexpected token". It cost a day once.
        var builder = Builder();
        builder.AddPostgres("pg").AddDatabase("shop")
            .WithCloneFrom("Host=x;Database=shop;Username=u;Password=p");

        Assert.DoesNotContain("\r", ScriptOf(Clone(builder, "shop-clone")));
    }
}
