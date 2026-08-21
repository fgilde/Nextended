using Nextended.Aspire.Hosting.WebDataStudio;

// Test/demo AppHost for the Nextended.Aspire.Hosting.WebDataStudio.
//
// Three studios on purpose, to show how sharing works:
//   * "webdatastudio"   — the default: every WithWebDataStudio() without a name lands here
//   * "analytics-studio" — a second studio, picked by name
//   * "admin-studio"     — built by hand, with a login and read-only connections
var builder = DistributedApplication.CreateBuilder(args);

// --- the shared studio ---------------------------------------------------------------------
// Two databases, one call each, one studio with both connections in it.
var postgres = builder.AddPostgres("pg")
    // The image runs everything in this folder against POSTGRES_DB the first time it starts, so
    // the seed lands in "shop" rather than in the maintenance database.
    .WithEnvironment("POSTGRES_DB", "shop")
    // Customers, products, orders, order items and a view — something to actually click around in.
    .WithInitFiles("init");

var shop = postgres.AddDatabase("shop").WithWebDataStudio();

var sqlServer = builder.AddSqlServer("sql");
var orders = sqlServer.AddDatabase("orders").WithWebDataStudio();

// A cache is a connection like any other; Redis is detected from the resource type.
builder.AddRedis("cache").WithWebDataStudio();

// --- a second studio, by name ------------------------------------------------------------------
// Everything analytical in its own window, with the row cap raised for exploratory queries.
builder.AddMongoDB("mongo").AddDatabase("events")
    .WithWebDataStudio(
        studio => studio.WithMaxRows(50_000).WithQueryTimeout(TimeSpan.FromMinutes(10)),
        studioName: "analytics-studio");

// --- a third studio, built by hand ----------------------------------------------------------------
// A password from a parameter (prompted once, then kept in user secrets), read-only everywhere,
// and connections labelled and coloured the way an operator wants to see them.
// A literal default keeps this demo runnable with `dotnet run`; in a real app host drop the
// value and let Aspire prompt for it or read it from user secrets.
var studioPassword = builder.AddParameter("studio-password", "change-me-please", secret: true);

builder.AddWebDataStudio("admin-studio")
    // Each studio shows its resource name in its header and browser tab; this one says more.
    .WithTitle("Production · read only")
    .WithLogin("admin", studioPassword)
    .WithLogin("hans", "hans")
    .WithLogin("pete", "pete")
    .WithReadOnly()
    .WithMcpEndpoint("mcp", allowWrite: false)   // Studio als MCP-Server
    .WithClaudeAssistant(builder.AddParameter("anthropic-key", secret: true))
    .WithSessionLimits(maxSessions: 4, idleTimeout: TimeSpan.FromMinutes(2))
    .WithReference(shop, connectionName: "SHOP_PROD", group: "Production", color: "#e03131")
    .WithReference(orders, connectionName: "ORDERS_PROD", group: "Production", color: "#e03131")
    // A database that is not part of this stack at all.
    .WithConnection("LOCAL_FILE", "Data Source=/data/demo.db", WebDataStudioEngine.Sqlite,
        group: "Scratch");

builder.Build().Run();
