using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// Everything the studio can read from a repository, written in the app host instead — and both at
/// once, which is what used to be impossible: one setting, one path, last call wins.
public class WebDataStudioInlineTests
{
    private static IResourceBuilder<WebDataStudioResource> Add() =>
        DistributedApplication.CreateBuilder().AddWebDataStudio();

    private static async Task<Dictionary<string, string>> EnvOf(WebDataStudioResource resource) =>
        await resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run);

    /// The files a resource will create inside the container, per destination folder.
    private static Dictionary<string, List<ContainerFile>> FilesOf(WebDataStudioResource resource) =>
        resource.Annotations.OfType<ContainerFileSystemCallbackAnnotation>()
            .ToDictionary(
                annotation => annotation.DestinationPath,
                annotation => annotation.Callback(
                        new ContainerFileSystemCallbackContext { Model = resource, Services = null! },
                        CancellationToken.None)
                    .GetAwaiter().GetResult()
                    .OfType<ContainerFile>()
                    .ToList());

    [Fact]
    public async Task A_saved_query_written_here_arrives_as_the_file_the_studio_reads()
    {
        var studio = Add().WithSavedQueries(
            new SavedStudioQuery("Orders today", "SELECT count(*) FROM orders", "Ops", "SHOP"));

        var files = FilesOf(studio.Resource)["/data/queries-inline"];
        var file = Assert.Single(files);

        Assert.Equal("Orders today.sql", file.Name);

        // The header comments are how a .sql file says the same thing.
        Assert.Contains("-- wds:connection SHOP", file.Contents);
        Assert.Contains("-- wds:folder Ops", file.Contents);
        Assert.Contains("SELECT count(*) FROM orders", file.Contents);

        Assert.Equal("/data/queries-inline", (await EnvOf(studio.Resource))["WDS_SAVED_QUERIES_DIR"]);
    }

    /// The point of the whole change: a repository folder and app-host queries add up.
    [Fact]
    public async Task A_folder_and_the_app_hosts_own_queries_live_side_by_side()
    {
        var studio = Add()
            .WithSavedQueriesFromDirectory(Directory.GetCurrentDirectory())
            .WithSavedQueries(new SavedStudioQuery("Ad hoc", "SELECT 1"));

        var setting = (await EnvOf(studio.Resource))["WDS_SAVED_QUERIES_DIR"];

        Assert.Equal("/data/queries;/data/queries-inline", setting);
    }

    [Fact]
    public async Task The_order_of_the_two_calls_does_not_matter()
    {
        var studio = Add()
            .WithSavedQueries(new SavedStudioQuery("Ad hoc", "SELECT 1"))
            .WithSavedQueriesFromDirectory(Directory.GetCurrentDirectory());

        Assert.Equal("/data/queries-inline;/data/queries",
            (await EnvOf(studio.Resource))["WDS_SAVED_QUERIES_DIR"]);
    }

    [Fact]
    public void Several_queries_all_arrive_rather_than_the_last_one_only()
    {
        var studio = Add().WithSavedQueries(
            new SavedStudioQuery("One", "SELECT 1"),
            new SavedStudioQuery("Two", "SELECT 2"));

        var files = FilesOf(studio.Resource)["/data/queries-inline"];

        Assert.Equal(["One.sql", "Two.sql"], files.Select(file => file.Name));
    }

    [Fact]
    public void A_name_somebody_typed_becomes_a_file_name_that_works()
    {
        var studio = Add().WithSavedQueries(new SavedStudioQuery("Orders / last week", "SELECT 1"));

        Assert.Equal("Orders - last week.sql",
            Assert.Single(FilesOf(studio.Resource)["/data/queries-inline"]).Name);
    }

    [Fact]
    public void A_query_without_a_name_or_a_statement_is_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithSavedQueries(new SavedStudioQuery("", "SELECT 1")));

        Assert.Throws<ArgumentException>(() =>
            Add().WithSavedQueries(new SavedStudioQuery("One", "  ")));
    }

    // --- export templates ---------------------------------------------------------------------------

    [Fact]
    public async Task An_export_template_is_written_as_the_json_the_studio_reads()
    {
        var studio = Add().WithExportTemplates(new StudioExportTemplate(
            "wiki", "Wiki table", "txt", "text/plain", "| {{values}} |", Header: "| {{columns}} |"));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/export-templates-inline"]);

        Assert.Equal("wiki.json", file.Name);
        Assert.Contains("\"id\": \"wiki\"", file.Contents);
        Assert.Contains("{{values}}", file.Contents);

        Assert.Equal("/data/export-templates-inline",
            (await EnvOf(studio.Resource))["WDS_EXPORT_TEMPLATES_DIR"]);
    }

    // --- quality rules ------------------------------------------------------------------------------

    [Fact]
    public async Task Quality_rules_are_written_as_one_list()
    {
        var studio = Add().WithQualityRules(
            new StudioQualityRule("SHOP", "invoices", "NotNull", Column: "customer_id",
                Message: "every invoice needs a customer"),
            new StudioQualityRule("SHOP", "orders", "Range", Column: "total", Argument: "0..100000"));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/quality-inline"]);

        Assert.Equal("rules.json", file.Name);
        Assert.Contains("\"kind\": \"NotNull\"", file.Contents);
        Assert.Contains("\"argument\": \"0..100000\"", file.Contents);

        Assert.Equal("/data/quality-inline", (await EnvOf(studio.Resource))["WDS_QUALITY_FILE"]);
    }

    [Fact]
    public void A_rule_without_a_connection_a_table_or_a_kind_is_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithQualityRules(new StudioQualityRule("", "invoices", "NotNull")));

        Assert.Throws<ArgumentException>(() =>
            Add().WithQualityRules(new StudioQualityRule("SHOP", "invoices", "")));
    }

    // --- seed scripts -------------------------------------------------------------------------------

    [Fact]
    public async Task A_seed_script_is_named_after_its_connection()
    {
        var studio = Add().WithSeedScript("SHOP", "INSERT INTO people (name) VALUES ('ada');");

        var file = Assert.Single(FilesOf(studio.Resource)["/data/seed-inline"]);

        Assert.Equal("SHOP.sql", file.Name);
        Assert.Contains("INSERT INTO people", file.Contents);

        Assert.Equal("/data/seed-inline", (await EnvOf(studio.Resource))["WDS_SEED_SQL"]);
    }

    [Fact]
    public void Two_connections_get_two_scripts()
    {
        var studio = Add()
            .WithSeedScript("SHOP", "SELECT 1")
            .WithSeedScript("ORDERS", "SELECT 2");

        Assert.Equal(["ORDERS.sql", "SHOP.sql"],
            FilesOf(studio.Resource)["/data/seed-inline"].Select(file => file.Name));
    }

    // --- connections that are not resources -----------------------------------------------------

    [Fact]
    public async Task Connections_written_here_arrive_as_the_array_the_studio_reads()
    {
        var studio = Add().WithConnections(
            new StudioConnectionEntry("LEGACY", "sqlserver", "Server=old;Database=erp",
                ReadOnly: true, Group: "Old"));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/connections-inline"]);

        Assert.Equal("connections.json", file.Name);
        Assert.Contains("\"name\": \"LEGACY\"", file.Contents);
        Assert.Contains("\"readOnly\": true", file.Contents);

        Assert.Equal("/data/connections-inline",
            (await EnvOf(studio.Resource))["WDS_CONNECTIONS_FILE"]);
    }

    [Fact]
    public void A_connection_without_the_three_things_it_needs_is_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithConnections(new StudioConnectionEntry("", "sqlite", "Data Source=:memory:")));

        Assert.Throws<ArgumentException>(() =>
            Add().WithConnections(new StudioConnectionEntry("A", "sqlite", "  ")));
    }

    [Fact]
    public async Task A_connection_file_and_the_app_hosts_own_both_count()
    {
        var studio = Add()
            .WithConnectionsFromFile(Path.Combine(Directory.GetCurrentDirectory(), "connections.json"))
            .WithConnections(new StudioConnectionEntry("A", "sqlite", "Data Source=:memory:"));

        Assert.Equal("/data/connections/connections.json;/data/connections-inline",
            (await EnvOf(studio.Resource))["WDS_CONNECTIONS_FILE"]);
    }

    // --- dashboards ------------------------------------------------------------------------------

    [Fact]
    public async Task A_dashboard_written_here_is_one_the_studio_shows_and_cannot_change()
    {
        var studio = Add().WithDashboards(new StudioDashboard("Morning",
            [new StudioTile("Orders today", "SHOP", "SELECT count(*) FROM orders")],
            RefreshSeconds: 60));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/dashboards-inline"]);

        Assert.Equal("dashboards.json", file.Name);
        Assert.Contains("\"name\": \"Morning\"", file.Contents);
        Assert.Contains("\"connectionId\": \"SHOP\"", file.Contents);
        Assert.Contains("\"refreshSeconds\": 60", file.Contents);

        Assert.Equal("/data/dashboards-inline", (await EnvOf(studio.Resource))["WDS_DASHBOARD_FILE"]);
    }

    [Fact]
    public void A_tile_without_a_statement_or_a_connection_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Add().WithDashboards(
            new StudioDashboard("Morning", [new StudioTile("Orders", "SHOP", "")])));

        Assert.Throws<ArgumentException>(() => Add().WithDashboards(
            new StudioDashboard("", [])));
    }

    // --- snippets --------------------------------------------------------------------------------

    [Fact]
    public async Task Snippets_written_here_reach_everybody_who_opens_the_studio()
    {
        var studio = Add().WithSnippets(
            new StudioSnippet("tenant", "tenant filter", "WHERE tenant_id = ${1:1}"));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/snippets-inline"]);

        Assert.Contains("\"prefix\": \"tenant\"", file.Contents);
        Assert.Contains("tenant_id", file.Contents);

        Assert.Equal("/data/snippets-inline", (await EnvOf(studio.Resource))["WDS_SNIPPETS_FILE"]);
    }

    [Fact]
    public void A_snippet_without_a_prefix_or_a_body_is_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithSnippets(new StudioSnippet("", "x", "SELECT 1")));

        Assert.Throws<ArgumentException>(() =>
            Add().WithSnippets(new StudioSnippet("x", "x", "   ")));
    }

    // --- preferences -----------------------------------------------------------------------------

    [Fact]
    public async Task The_preferences_a_studio_starts_with_are_written_as_a_file()
    {
        var studio = Add().WithDefaultPreferences(timeZone: "utc", pageSize: 500);

        var file = Assert.Single(FilesOf(studio.Resource)["/data/preferences-inline"]);

        Assert.Contains("\"timeZone\": \"utc\"", file.Contents);
        Assert.Contains("\"pageSize\": 500", file.Contents);

        // What it does not name keeps the studio's own default, so it stays null rather than 0.
        Assert.Contains("\"inspectBeforeRun\": null", file.Contents);

        Assert.Equal("/data/preferences-inline",
            (await EnvOf(studio.Resource))["WDS_PREFERENCES_FILE"]);
    }

    [Fact]
    public void A_page_of_no_rows_and_a_negative_delay_are_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithDefaultPreferences(pageSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithDefaultPreferences(notifyAfterSeconds: -1));
    }

    // --- masking ---------------------------------------------------------------------------------

    [Fact]
    public async Task The_masking_baseline_can_come_from_a_file()
    {
        var studio = Add()
            .WithMaskingFromFile(Path.Combine(Directory.GetCurrentDirectory(), "masking.json"));

        Assert.Equal("/data/masking/masking.json", (await EnvOf(studio.Resource))["WDS_MASK_FILE"]);
    }
}
