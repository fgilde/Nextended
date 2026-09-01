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

        // The server, not the database it fills: the database carries a health check that answers
        // "has the clone finished", so waiting for the database would be waiting for itself.
        Assert.Contains("dev", waits);
        Assert.DoesNotContain("dev-shop", waits);

        // The source is another matter — nothing can be dumped out of a server that has not started.
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

        // The server the target lives on, and nothing else: a source outside this stack is not
        // something the model can wait for, and the script's own waiting is what covers it. The
        // target itself is not waited for — it is what waits for this clone.
        Assert.Equal(["dev"], waits.Order().ToList());
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

    /// A MySQL dump carries the server's GTID state unless told not to, and restoring that into a
    /// server that already knows those transactions is refused — which is every clone whose source
    /// and target are two databases on the same server.
    [Fact]
    public void A_mysql_clone_does_not_carry_the_source_servers_gtid_state()
    {
        var builder = Builder();
        var mysql = builder.AddMySql("my");

        mysql.AddDatabase("copy").WithCloneFrom(mysql.AddDatabase("source"));

        var script = ScriptOf(Clone(builder, "copy-clone"));
        Assert.Contains("--set-gtid-purged=OFF", script);
        // Asked for rather than assumed, because MariaDB's dump tool has no such option.
        Assert.Contains("mysqldump --help", script);
    }

    /// A database resource cannot wait for anything, so what a stack waits for is the clone itself.
    [Fact]
    public void The_clone_can_be_waited_for_by_name()
    {
        var builder = Builder();
        var copy = builder.AddSqlServer("ms").AddDatabase("orders-copy");

        copy.WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p");

        var clone = builder.CloneOf("orders-copy");
        Assert.Equal("orders-copy-clone", clone.Resource.Name);

        // What the point of it is: a resource that does support waiting can wait for it.
        builder.AddContainer("consumer", "busybox").WaitForCompletion(clone);
    }

    [Fact]
    public void A_clone_that_is_not_there_says_which_ones_are()
    {
        var builder = Builder();
        builder.AddSqlServer("ms").AddDatabase("orders-copy")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p");

        var error = Assert.Throws<ArgumentException>(() => builder.CloneOf("orders"));
        Assert.Contains("orders-copy-clone", error.Message);
    }

    /// SQL Server's format is schema and rows in one file; half of it is not on offer, and saying so
    /// when the app host is built beats a container saying it three minutes later.
    [Fact]
    public void A_data_only_clone_of_sql_server_is_refused_up_front()
    {
        var builder = Builder();
        var target = builder.AddSqlServer("ms").AddDatabase("orders");

        var error = Assert.Throws<ArgumentException>(() =>
            target.WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { DataOnly = true }));

        Assert.Contains("SQL Server", error.Message);
    }

    /// The line a recipe prints, as the database's own state text.
    [Theory]
    [InlineData("##progress 5 reading the schema of 169 table(s)", "Cloning 5% — reading the schema of 169 table(s)")]
    [InlineData("2026-08-31T19:45:58Z ##progress 20 copying", "Cloning 20% — copying")]
    [InlineData("##progress 47 copying rows (20 of 169 tables)", "Cloning 47% — copying rows (20 of 169 tables)")]
    [InlineData("##progress 100 Cloned", CloneProgress.Finished)]
    [InlineData("nothing to see here", null)]
    [InlineData("##progress not-a-number", null)]
    public void A_progress_marker_becomes_the_state_the_database_shows(string line, string? expected) =>
        Assert.Equal(expected, CloneProgress.Read(line));

    /// "Why is this database still empty" is asked at the database, so the clone's log has to be
    /// reachable from there: in the dashboard it hangs under the database it fills.
    [Fact]
    public void The_clone_hangs_under_the_database_it_fills()
    {
        var builder = Builder();

        var target = builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p");

        var clone = Clone(builder, "orders-clone");
        var parent = Assert.Single(clone.Annotations.OfType<ResourceRelationshipAnnotation>()
            .Where(a => a.Type == "Parent"));

        Assert.Same(target.Resource, parent.Resource);
    }

    /// A database whose contents are still being copied is not ready, and a database resource cannot
    /// be made to wait — so it carries a health check that answers for the clone instead.
    [Fact]
    public void The_target_is_unhealthy_until_the_clone_has_finished()
    {
        var builder = Builder();

        var target = builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p");

        // Next to Aspire's own check on the database, not instead of it: both have to pass, so the
        // target is ready when the server answers *and* the copy is there.
        var checks = target.Resource.Annotations.OfType<HealthCheckAnnotation>().Select(a => a.Key).ToList();
        Assert.Contains("dbtools-clone-orders-clone", checks);
    }

    /// The way in for a source whose login may read the database but not use its schema tools.
    [Fact]
    public void A_metadata_clone_reads_the_schema_itself()
    {
        var builder = Builder();

        builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { FromMetadata = true });

        var script = ScriptOf(Clone(builder, "orders-clone"));
        Assert.DoesNotContain("sqlpackage", script);
        Assert.Contains("SqlManagementObjects", script);
        Assert.Contains("SqlBulkCopy", script);
        Assert.Contains("CLONE_SOURCE_CS", script);
    }

    /// The other four engines' tools ask for no permission that reading the database does not give,
    /// so the option would only be a second way of doing the same thing.
    [Fact]
    public void A_metadata_clone_is_refused_where_it_is_not_needed()
    {
        var builder = Builder();
        var target = builder.AddPostgres("pg").AddDatabase("shop");

        var error = Assert.Throws<ArgumentException>(() =>
            target.WithCloneFrom("Host=old;Database=shop;Username=u;Password=p",
                new DbCloneOptions { FromMetadata = true }));

        Assert.Contains("PostgreSQL", error.Message);
    }

    /// A clone that hangs is worse than one that fails: TimeoutSeconds has to reach the script, or a
    /// stalled query waits for ever while the resource looks busy.
    [Fact]
    public void The_timeout_reaches_the_script()
    {
        var builder = Builder();

        builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { TimeoutSeconds = 90 });

        var clone = Clone(builder, "orders-clone");
        Assert.Equal("90", EnvOf(clone)["CLONE_TIMEOUT"]);
        Assert.Contains("timeout \"${CLONE_TIMEOUT:-3600}\"", ScriptOf(clone));
    }

    /// Sixty attempts at a refusal is four minutes of hiding it. Only a source that has not started
    /// yet is worth waiting for.
    [Fact]
    public void A_refusal_is_not_retried()
    {
        var builder = Builder();

        builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p");

        var script = ScriptOf(Clone(builder, "orders-clone"));
        Assert.Contains("View Definition permission", script);
        Assert.Contains("GRANT VIEW DEFINITION", script);
        Assert.Contains("could not open a connection", script);
    }

    /// Overwriting a schema is a publish, and a publish drops nothing — so nothing has to be cleared
    /// out of its way. Dropping anyway would leave the database missing for as long as the extract
    /// takes, and everything connected to it saying "cannot open database".
    [Fact]
    public void A_schema_only_overwrite_does_not_drop_the_target_first()
    {
        var builder = Builder();

        builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { SchemaOnly = true, Overwrite = true });

        Assert.DoesNotContain(builder.Resources, r => r.Name == "orders-clone-prepare");

        // A full overwrite still clears the way, because sqlpackage refuses to import over objects.
        var second = Builder();

        second.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { Overwrite = true });

        Assert.Contains(second.Resources, r => r.Name == "orders-clone-prepare");
    }

    /// The schema alone goes through DACPAC rather than BACPAC — minutes instead of hours, and it can
    /// leave out what a container has no answer for.
    [Fact]
    public void A_schema_only_clone_of_sql_server_extracts_and_publishes()
    {
        var builder = Builder();

        builder.AddSqlServer("ms").AddDatabase("orders")
            .WithCloneFrom("Server=old;Database=orders;User Id=sa;Password=p",
                new DbCloneOptions { SchemaOnly = true });

        var script = ScriptOf(Clone(builder, "orders-clone"));
        Assert.Contains("/a:Extract", script);
        Assert.Contains("/a:Publish", script);
        Assert.Contains("ExtractAllTableData=False", script);
        Assert.Contains("AllowIncompatiblePlatform=True", script);
        Assert.Contains("ExcludeObjectTypes=Users;Logins", script);
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
