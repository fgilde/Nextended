using System.Text.Json;
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

    [Fact]
    public async Task A_dashboard_as_a_canvas_is_written_in_the_studios_own_shape()
    {
        var studio = Add().WithDashboards(new StudioCanvas("Morning",
        [
            new StudioWidget("Overview", StudioWidgetType.Row, Width: 24, Height: 1),
            new StudioWidget("Customers", StudioWidgetType.Stat, "SHOP",
                "SELECT count(*) FROM customers", Width: 6, Height: 4,
                Thresholds: [new StudioThreshold(100, "good")]),
            new StudioWidget("Disk", StudioWidgetType.Gauge, "SHOP", "SELECT 61", Width: 6,
                Height: 4, Min: 0, Max: 100, Unit: "percent"),
            new StudioWidget("Orders over time", StudioWidgetType.StackedArea, "SHOP",
                "SELECT day, region, n FROM daily WHERE $__timeFilter(day)",
                Width: 12, Height: 6, Category: "day", Series: "region", Value: "n"),
        ], RefreshSeconds: 60, From: "now-7d",
            Variables: [new StudioVariable("region", ["eu", "us"], Multi: true, Default: "eu")]));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/dashboards-inline"]);

        Assert.Equal("canvas.json", file.Name);
        Assert.Contains("\"widgets\"", file.Contents);
        Assert.Contains("\"type\": \"Stat\"", file.Contents);
        Assert.Contains("\"from\": \"now-7d\"", file.Contents);
        Assert.Contains("\"level\": \"good\"", file.Contents);
        // A stacked type carries the flag as well, so a reader of the file sees both.
        Assert.Contains("\"stacked\": true", file.Contents);
        Assert.Contains("\"multi\": true", file.Contents);

        Assert.Equal("/data/dashboards-inline", (await EnvOf(studio.Resource))["WDS_DASHBOARD_FILE"]);
    }

    /// Widgets that do not say where they go flow left to right and wrap, which is what a page
    /// written as a list is meant to look like.
    [Fact]
    public void Widgets_without_a_position_lay_themselves_out()
    {
        var studio = Add().WithDashboards(new StudioCanvas("Morning",
        [
            new StudioWidget("a", StudioWidgetType.Stat, "SHOP", "SELECT 1", Width: 12, Height: 4),
            new StudioWidget("b", StudioWidgetType.Stat, "SHOP", "SELECT 2", Width: 12, Height: 4),
            new StudioWidget("c", StudioWidgetType.Stat, "SHOP", "SELECT 3", Width: 12, Height: 4),
            new StudioWidget("d", StudioWidgetType.Stat, "SHOP", "SELECT 4", Width: 6, Height: 4,
                X: 18, Y: 0),
        ]));

        var contents = FilesOf(studio.Resource)["/data/dashboards-inline"].Single().Contents;
        using var document = JsonDocument.Parse(contents);

        var widgets = document.RootElement[0].GetProperty("widgets").EnumerateArray()
            .ToDictionary(one => one.GetProperty("title").GetString()!,
                one => one.GetProperty("position"));

        Assert.Equal(0, widgets["a"].GetProperty("x").GetInt32());
        Assert.Equal(12, widgets["b"].GetProperty("x").GetInt32());
        // The third does not fit beside them, so it starts the next row.
        Assert.Equal(0, widgets["c"].GetProperty("x").GetInt32());
        Assert.Equal(4, widgets["c"].GetProperty("y").GetInt32());
        // And the one that said where it belongs is there.
        Assert.Equal(18, widgets["d"].GetProperty("x").GetInt32());
        Assert.Equal(0, widgets["d"].GetProperty("y").GetInt32());
    }

    [Fact]
    public void A_widget_over_two_connections_is_written_as_a_federated_one()
    {
        var studio = Add().WithDashboards(new StudioCanvas("Morning",
        [
            new StudioWidget("Both", StudioWidgetType.Bar,
                Sql: "SELECT s.region, s.n, w.total FROM shop s JOIN warehouse w ON w.region = s.region",
                Sources:
                [
                    new StudioWidgetSource("SHOP", "SELECT region, count(*) AS n FROM orders GROUP BY region", "shop"),
                    new StudioWidgetSource("WAREHOUSE", "SELECT region, sum(total) AS total FROM stock GROUP BY region", "warehouse"),
                ]),
        ]));

        var contents = FilesOf(studio.Resource)["/data/dashboards-inline"].Single().Contents;

        Assert.Contains("\"kind\": \"Federated\"", contents);
        Assert.Contains("\"alias\": \"warehouse\"", contents);
    }

    /// The studio refuses to draw a gauge without a range rather than inventing a scale, so an app
    /// host that can catch it says so before the container is built.
    [Fact]
    public void A_widget_that_could_not_be_drawn_is_refused_here()
    {
        Assert.Throws<ArgumentException>(() => Add().WithDashboards(new StudioCanvas("Morning",
            [new StudioWidget("Disk", StudioWidgetType.Gauge, "SHOP", "SELECT 61")])));

        Assert.Throws<ArgumentException>(() => Add().WithDashboards(new StudioCanvas("Morning",
            [new StudioWidget("Orders", StudioWidgetType.Bar, "SHOP", "")])));

        Assert.Throws<ArgumentException>(() => Add().WithDashboards(new StudioCanvas("Morning",
            [new StudioWidget("Orders", StudioWidgetType.Bar, Sql: "SELECT 1")])));

        // A colour a dashboard picked is not a state a threshold can mean.
        Assert.Throws<ArgumentException>(() => Add().WithDashboards(new StudioCanvas("Morning",
            [new StudioWidget("Customers", StudioWidgetType.Stat, "SHOP", "SELECT 1",
                Thresholds: [new StudioThreshold(1, "chartreuse")])])));
    }

    /// A prose widget and a band need no statement at all.
    [Fact]
    public void A_text_widget_needs_no_connection()
    {
        var studio = Add().WithDashboards(new StudioCanvas("Morning",
        [
            new StudioWidget("How to read this", StudioWidgetType.Text,
                Markdown: "**Orders** are counted when they are paid."),
        ]));

        Assert.Contains("counted when they are paid",
            FilesOf(studio.Resource)["/data/dashboards-inline"].Single().Contents);
    }

    /// A team with thirteen exported Grafana dashboards has them in exactly that shape: point this
    /// at the folder, and nothing says which format they are in — the studio decides per file.
    [Fact]
    public async Task A_folder_of_grafana_dashboards_is_mounted_and_pointed_at()
    {
        var folder = Directory.CreateTempSubdirectory("wds-grafana");

        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder.FullName, "ops.json"),
                "{\"title\":\"Ops\",\"panels\":[]}");

            var studio = Add().WithGrafanaDashboards(folder.FullName);

            var mount = Assert.Single(studio.Resource.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(one => one.Target.Contains("grafana", StringComparison.Ordinal)));

            Assert.Equal(folder.FullName, mount.Source);
            Assert.True(mount.IsReadOnly);
            Assert.Contains("grafana", (await EnvOf(studio.Resource))["WDS_DASHBOARD_FILE"]);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    /// Both at once: what the app host wrote and the Grafana folder both count, because the setting
    /// takes a list.
    [Fact]
    public async Task A_grafana_folder_and_a_canvas_live_side_by_side()
    {
        var folder = Directory.CreateTempSubdirectory("wds-grafana-both");

        try
        {
            var studio = Add()
                .WithGrafanaDashboards(folder.FullName)
                .WithDashboards(new StudioCanvas("Mine",
                    [new StudioWidget("Customers", StudioWidgetType.Stat, "SHOP", "SELECT 1")]));

            var setting = (await EnvOf(studio.Resource))["WDS_DASHBOARD_FILE"];

            Assert.Contains("grafana", setting);
            Assert.Contains("/data/dashboards-inline", setting);
            Assert.Contains(";", setting);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
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

    // --- seeding one connection from another --------------------------------------------------------

    [Fact]
    public async Task A_seed_copy_is_written_as_the_file_the_studio_reads()
    {
        var studio = Add().WithSeedFrom(
            new StudioSeedCopy("STAGING", "DEV", ["countries", "customers"], MaxRows: 500));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/seed-from-inline"]);

        Assert.Equal("seed-from.json", file.Name);
        Assert.Contains("\"from\": \"STAGING\"", file.Contents);
        Assert.Contains("\"countries\"", file.Contents);
        Assert.Contains("\"maxRows\": 500", file.Contents);

        Assert.Equal("/data/seed-from-inline", (await EnvOf(studio.Resource))["WDS_SEED_FROM_FILE"]);
    }

    [Fact]
    public void A_copy_that_cannot_work_is_refused()
    {
        // No tables named is a copy that does nothing and looks like it should.
        Assert.Throws<ArgumentException>(() =>
            Add().WithSeedFrom(new StudioSeedCopy("STAGING", "DEV", [])));

        // A connection cannot be seeded from itself.
        Assert.Throws<ArgumentException>(() =>
            Add().WithSeedFrom(new StudioSeedCopy("DEV", "dev", ["countries"])));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithSeedFrom(new StudioSeedCopy("STAGING", "DEV", ["countries"], MaxRows: 0)));
    }

    // --- backups the studio takes on its own -------------------------------------------------------

    [Fact]
    public async Task A_backup_schedule_is_written_as_the_file_the_studio_reads()
    {
        var studio = Add().WithBackupSchedule("/backups",
            new StudioBackup("nightly", "SHOP", DailyAtUtc: "02:00", Keep: 3));

        var file = Assert.Single(FilesOf(studio.Resource)["/data/backup-schedule-inline"]);

        Assert.Equal("backups.json", file.Name);
        Assert.Contains("\"name\": \"nightly\"", file.Contents);
        Assert.Contains("\"dailyAtUtc\": \"02:00\"", file.Contents);
        Assert.Contains("\"keep\": 3", file.Contents);

        var env = await EnvOf(studio.Resource);
        Assert.Equal("/data/backup-schedule-inline", env["WDS_BACKUP_SCHEDULE_FILE"]);
        Assert.Equal("/backups", env["WDS_BACKUP_DIR"]);
    }

    [Fact]
    public void A_job_that_never_runs_is_refused()
    {
        // Neither EveryMinutes nor DailyAtUtc means a job that exists and never fires, which is
        // worse than no job at all: somebody believes there are backups.
        Assert.Throws<ArgumentException>(() =>
            Add().WithBackupSchedule("/backups", new StudioBackup("nightly", "SHOP")));

        Assert.Throws<ArgumentException>(() =>
            Add().WithBackupSchedule("/backups", new StudioBackup("", "SHOP", EveryMinutes: 60)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithBackupSchedule("/backups", new StudioBackup("x", "SHOP", EveryMinutes: 0)));
    }

    [Fact]
    public async Task A_schedule_can_come_from_a_file_in_the_repository()
    {
        var studio = Add().WithBackupScheduleFromFile(
            Path.Combine(Directory.GetCurrentDirectory(), "backups.json"), "/backups");

        var env = await EnvOf(studio.Resource);
        Assert.Equal("/data/backup-schedule/backups.json", env["WDS_BACKUP_SCHEDULE_FILE"]);
        Assert.Equal("/backups", env["WDS_BACKUP_DIR"]);
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
