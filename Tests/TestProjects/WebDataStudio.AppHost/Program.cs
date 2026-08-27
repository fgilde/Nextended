using Aspire.Hosting.ApplicationModel;
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
        // Kept results land on the studio's own volume, capped so one archive cannot fill it.
        studio => studio
            .WithMaxRows(50_000)
            .WithQueryTimeout(TimeSpan.FromMinutes(10))
            .WithArchives(maxRows: 50_000),
        studioName: "analytics-studio");

// --- buckets: three ways in ----------------------------------------------------------------------
// A bucket is a connection like any other. The studio browses containers, prefixes and objects, and
// reads a CSV or a Parquet in there as a table — sorting, the filter language, paging and export all
// work, because a file is queried through a DuckDB the studio holds.

// 1. Azure Blob Storage, through the emulator while developing and the real account once deployed.
//    WithBlobStorage passes the resource's connection string through as it is; the account name is
//    inside it either way, so nothing has to be repeated here.
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var exports = storage.AddBlobs("exports");

// 2. MinIO, which is what an S3 endpoint looks like when it is part of your own stack. The URL is
//    only known once the stack runs, so it is a reference expression rather than a string.
var minioUser = builder.AddParameter("minio-user", "wds-demo");
var minioPassword = builder.AddParameter("minio-password", "wds-demo-secret", secret: true);

var minio = builder.AddContainer("minio", "minio/minio", "RELEASE.2025-04-22T22-12-26Z")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPassword)
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console");

// A bucket with something in it, so the studio has a file to open on the first run. One shot: mc
// waits for the server, makes the bucket and puts a CSV in it.
builder.AddContainer("minio-setup", "minio/mc", "RELEASE.2025-04-16T18-13-26Z")
    .WithEntrypoint("/bin/sh")
    .WithEnvironment("MINIO_USER", minioUser)
    .WithEnvironment("MINIO_PASSWORD", minioPassword)
    .WithArgs("-c", """
        until mc alias set demo http://minio:9000 "$MINIO_USER" "$MINIO_PASSWORD"; do sleep 1; done
        mc mb -p demo/lake
        printf 'name,city,orders\nada,london,7\ngrace,new york,4\nalan,manchester,9\n' > /tmp/people.csv
        mc cp /tmp/people.csv demo/lake/exports/people.csv
        mc ls -r demo/lake
        """)
    .WaitFor(minio);

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
    .WithMcpEndpoint("mcp", studioPassword)   // Studio als MCP-Server
    .WithSessionLimits(maxSessions: 4, idleTimeout: TimeSpan.FromMinutes(2))
    .WithReference(shop, connectionName: "SHOP_PROD", group: "Production", color: "#e03131")
    .WithReference(orders, connectionName: "ORDERS_PROD", group: "Production", color: "#e03131")
    // A database that is not part of this stack at all.
    .WithConnection("LOCAL_FILE", "Data Source=/data/demo.db", WebDataStudioEngine.Sqlite,
        group: "Scratch")
    // 3. A folder — the version of a bucket that needs nothing installed at all.
    .WithStorage("DROP", "file:///data/incoming", group: "Scratch")
    // The Azure emulator, and the MinIO from above with its endpoint and keys resolved at run time.
    .WithBlobStorage(exports, group: "Buckets")
    .WithStorage("LAKE", ReferenceExpression.Create(
        $"s3://lake?endpoint={minio.GetEndpoint("api")}&access={minioUser}&secret={minioPassword}&region=us-east-1"),
        group: "Buckets")
    // Only these schemas are read on a big server: the tree, the completion cache and the object
    // search each walk what they are given.
    .WithSchemas("SHOP_PROD", "public")
    // Export formats written as text with placeholders rather than as code to run.
    .WithExportTemplates("export-templates");

builder.Build().Run();
