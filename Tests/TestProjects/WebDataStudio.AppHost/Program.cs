using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.WebDataStudio;
using Nextended.Aspire.Hosting.WebDataStudio.Resources;

// Test/demo AppHost for the Nextended.Aspire.Hosting.WebDataStudio.
//
// Three studios on purpose, to show how sharing works:
//   * "webdatastudio"   — the default: every WithWebDataStudio() without a name lands here
//   * "analytics-studio" — a second studio, picked by name
//   * "admin-studio"     — built by hand, with a login and read-only connections
// A shell script written in this file, ready for a Linux container.
//
// This file is checked out with Windows line endings, and a raw string keeps them: the container's
// shell then reads `do\r` and answers "syntax error near unexpected token". That is not a
// hypothetical — it is why the MinIO bucket and the Redis keys were missing from this demo, with
// both setup containers exiting 2 while everything around them looked fine.
static string Sh(string script) => script.ReplaceLineEndings("\n");

var builder = DistributedApplication.CreateBuilder(args);


var demoUser = builder.AddParameter("demo-user", "admin");
var demoPassword = builder.AddParameter("demo-password", "change-me-please", secret: true);
var clientSecret = builder.AddParameter("keycloak-client-secret", "studio-secret", secret: true);

// --- the studio everything shares ----------------------------------------------------------------
// Created here rather than by the first WithWebDataStudio(), so it can be told the things a studio
// only learns once: where its seed scripts, saved queries and export templates are.
//
// A folder of files is a storage connection too — this one is the demo's `drop` folder, mounted in —
// so the object preview, "Save as…" and "a file becomes a table" work without a bucket anywhere.
var studio = builder.AddWebDataStudio()
    .WithTheme(WebDataStudioTheme.GitHubLight)
    .WithTitle("WebDataStudio demo")
    // One {CONNECTION}.sql per connection, run once each: SQL Server and the SQLite file below.
    // PostgreSQL seeds itself through the image's own init folder.
    .WithSeedScript("seed")
    // The five queries this demo is about, imported as saved queries at start.
    .WithSavedQueriesFromDirectory("queries")
    // Export formats written as text with placeholders rather than as code to run.
    .WithExportTemplates("export-templates")
    // The same three things, written here instead of in a folder — and both at once, which is the
    // point: the repository ships what a review should catch, the app host adds what belongs to
    // this stack.
    .WithSavedQueries(
        new SavedStudioQuery("Everything in this database", "SELECT * FROM customers", "Ad hoc", "SHOP"),
        new SavedStudioQuery("Orders without a customer",
            "SELECT * FROM orders WHERE customer_id IS NULL", "Ad hoc", "SHOP"))
    .WithExportTemplates(new StudioExportTemplate(
        "wiki", "Wiki table", "txt", "text/plain",
        Row: "| {{values}} |", Header: "| {{columns}} |", Separator: " | "))
    .WithQualityRules(new StudioQualityRule(
        "SHOP", "orders", "NotNull", Column: "customer_id",
        Message: "an order without a customer is one nobody can invoice"))
    // A page everybody sees on the first morning, a snippet everybody has, and the clock the
    // timestamps are shown on — all three belong to this stack rather than to one browser.
    .WithDashboards(new StudioDashboard("Morning",
    [
        new StudioTile("Customers", "SHOP", "SELECT count(*) FROM customers"),
        new StudioTile("Orders by status", "SHOP",
            "SELECT status, count(*) FROM orders GROUP BY status", View: "chart", Width: 2),
    ], RefreshSeconds: 30))
    .WithSnippets(new StudioSnippet("recent", "rows from the last day",
        "WHERE ${1:placed} > now() - interval '1 day'"))
    .WithDefaultPreferences(timeZone: "utc")
    // Snapshot every connection's schema on start and report the drift since the last one.
    .WithSchemaSnapshots()
    // Who did what through this studio.
    .WithAuditTrail(days: 30)
    // The studio as a tool for AI agents, read-only.
    .WithMcpEndpoint("mcp")
    // A folder of files as a connection: CSV, NDJSON, a JSON document on one line, a PDF, a PNG, and
    // a prefix of three files with the same columns that read as one table.
    .WithBindMount("drop", "/data/incoming")
    .WithStorage("DROP", "file:///data/incoming", group: "Files")
    // A SQLite file on the studio's own volume — no server, and the connection the development
    // subset is worth trying on: people, the countries they are in, and notes about them.
    .WithConnection("SCRATCH", "Data Source=/data/scratch.db", WebDataStudioEngine.Sqlite,
        group: "Files");

// --- the shared studio ---------------------------------------------------------------------
// Two databases, one call each, one studio with both connections in it.
var postgres = builder.AddPostgres("pg")
    // The image runs everything in this folder against POSTGRES_DB the first time it starts, so
    // the seed lands in "shop" rather than in the maintenance database.
    .WithEnvironment("POSTGRES_DB", "shop")
    // 01-shop.sql is a small shop; 02-showcase.sql is one thing for each of the studio's less
    // obvious panels — a document column, a partitioned table, a materialised view, a function that
    // raises a notice, row-level security, geography, a second schema, sixty thousand page views
    // without the index they want, and a table left dirty on purpose for the data quality rules.
    .WithInitFiles("init");

var shop = postgres.AddDatabase("shop").WithWebDataStudio();

// --- the same database, seen the other way -------------------------------------------------------
// Grafana and the studio are two windows onto one server, and neither package knows about the
// other: the *resource* is what they share. That is the whole wiring — the studio gets the database
// through WithWebDataStudio above, Grafana gets the server here, and the credentials come from the
// resource's own parameters rather than being written down twice.
//
// The studio is for asking a question you have not asked before; Grafana is for the answer you want
// on a wall. Same rows.
builder.AddGrafana()
    .WithAnonymousAdmin()
    .WithPostgresDatasource(postgres, name: "Shop", database: "shop")
    // A dashboard on those rows, so "the same database" is something you can see rather than
    // something this comment claims: Customers and Orders by status are the statements the studio's
    // own Morning dashboard runs, and the numbers move together.
    .WithDashboards("grafana-dashboards", "Demo");

var sqlServer = builder.AddSqlServer("sql");
var orders = sqlServer.AddDatabase("orders").WithWebDataStudio();

// A cache is a connection like any other; Redis is detected from the resource type.
var redis = builder.AddRedis("cache");
redis.WithWebDataStudio();

// Keys of every type Redis has, so the key browser is not an empty tree. One shot: redis-cli waits
// for the server and writes, which is the Redis version of a seed script.
//
// Two things about the server Aspire starts, which a plain `redis-cli -h cache` gets wrong twice:
// it wants a password, and its 6379 speaks TLS (the plaintext port is 6380). Rather than pinning
// this demo to either of those, the script finds out which one answers and uses that.
builder.AddContainer("redis-seed", "redis", "8-alpine")
    .WithEntrypoint("/bin/sh")
    .WithEnvironment("REDIS_PASSWORD", redis.Resource.PasswordParameter)
    .WithArgs("-c", Sh("""
        auth="-a $REDIS_PASSWORD --no-auth-warning"
        cli=""

        for attempt in $(seq 1 60); do
          if redis-cli --tls --insecure -h cache $auth ping 2>/dev/null | grep -q PONG; then
            cli="--tls --insecure -h cache $auth"
            break
          fi
          if redis-cli -h cache -p 6380 $auth ping 2>/dev/null | grep -q PONG; then
            cli="-h cache -p 6380 $auth"
            break
          fi
          sleep 1
        done

        if [ -z "$cli" ]; then
          echo "redis never answered on either port"
          exit 1
        fi

        redis-cli $cli SET greeting 'hello from the demo'
        redis-cli $cli SET 'session:ada' '{"account":"ada","pages":14}'
        redis-cli $cli EXPIRE 'session:ada' 3600
        redis-cli $cli HSET 'customer:1' name 'Ada Lovelace' city London orders 7
        redis-cli $cli HSET 'customer:2' name 'Linus Torvalds' city Helsinki orders 4
        redis-cli $cli RPUSH 'queue:outgoing' 'INV-1001' 'INV-1002' 'INV-1003'
        redis-cli $cli SADD 'tags:beta' ada grace
        redis-cli $cli ZADD 'leaderboard' 2310 grace 1204 ada 689 linus
        redis-cli $cli SETEX 'lock:import' 600 'held by the importer'
        redis-cli $cli DBSIZE
        """))
    .WaitFor(redis);



// --- a second studio, by name ------------------------------------------------------------------
// Everything analytical in its own window, with the row cap raised for exploratory queries.
var mongo = builder.AddMongoDB("mongo")
    // The image runs every .js in this folder against MONGO_INITDB_DATABASE on first start:
    // sessions whose documents agree on their shape, telemetry whose documents do not, and a capped
    // collection — so the tree has collections with something in them.
    .WithEnvironment("MONGO_INITDB_DATABASE", "events")
    .WithBindMount("mongo-init", "/docker-entrypoint-initdb.d");

mongo.AddDatabase("events")
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
var blobs = storage.AddBlobs("blobs");

// The container itself, as its own resource: AddBlobs models the blob *service*, and a connection
// that names a container nobody created answers "ContainerNotFound" on the first click. This is
// what creates it — in Azurite while developing, and in the real account once deployed.
storage.AddBlobContainer("exports");

// 2. MinIO, which is what an S3 endpoint looks like when it is part of your own stack. The URL is
//    only known once the stack runs, so it is a reference expression rather than a string.
var minio = builder.AddContainer("minio", "minio/minio", "RELEASE.2025-04-22T22-12-26Z")
    .WithEnvironment("MINIO_ROOT_USER", demoUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", demoPassword)
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console");

// A bucket with something in it, so the studio has a file to open on the first run. One shot: mc
// waits for the server, makes the bucket and puts a CSV in it.
builder.AddContainer("minio-setup", "minio/mc", "RELEASE.2025-04-16T18-13-26Z")
    .WithEntrypoint("/bin/sh")
    .WithEnvironment("MINIO_USER", demoUser)
    .WithEnvironment("MINIO_PASSWORD", demoPassword)
    .WithArgs("-c", Sh("""
        until mc alias set demo http://minio:9000 "$MINIO_USER" "$MINIO_PASSWORD"; do sleep 1; done
        mc mb -p demo/lake
        printf 'name,city,orders\nada,london,7\ngrace,new york,4\nalan,manchester,9\n' > /tmp/people.csv
        mc cp /tmp/people.csv demo/lake/exports/people.csv

        # A document per line, which is what an export from an event store looks like.
        printf '{"id":1,"kind":"signup","plan":"pro"}\n{"id":2,"kind":"signup","plan":"free"}\n{"id":3,"kind":"upgrade","plan":"team","seats":12}\n' > /tmp/events.ndjson
        mc cp /tmp/events.ndjson demo/lake/exports/events.ndjson

        # One prefix, three files with the same columns: the studio reads the whole prefix as one
        # table, which is the point of a lake laid out by month.
        for m in 06 07 08; do
          printf 'month,orders,revenue\n2026-%s,1%s,90%s.50\n' "$m" "$m" "$m" > /tmp/part.csv
          mc cp /tmp/part.csv "demo/lake/monthly/2026-$m.csv"
        done

        mc ls -r demo/lake
        """))
    .WaitFor(minio);

// --- an identity provider in the stack -----------------------------------------------------------
// The studio can sign people in with the provider a company already has. A Keycloak in the app host
// is the version of that you can click through on a laptop: it starts with the demo realm imported,
// so `alice` / `alice` is an admin in the studio without an account existing in the studio at all.
// The realm names the client and its secret and puts alice in `dba-group` and bob in `developers`,
// which is where WithSignInRoles below reads the roles from.
//
// **Two addresses, one provider.** The browser reaches Keycloak on the published port; the studio,
// which runs in a container, reaches it by container name. KC_HOSTNAME is the browser-facing address
// and the backchannel is left dynamic, so the discovery document hands each side the address it can
// actually use while the issuer — what the tokens are validated against — stays the same for both.
//
// Both ports are pinned on purpose. An issuer and a redirect URI are configuration on the provider's
// side as well: the realm registers http://localhost:8082/* for this studio, so a studio published on
// a port that changes every run could not sign anybody in.
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
    // The Keycloak administrator, and — through the realm file's ${...} placeholders, which the
    // import substitutes from the environment — the people inside the realm as well. So the pair
    // above signs in to Keycloak's own console and to the studio behind it.
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", demoUser)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", demoPassword)
    .WithEnvironment("WDS_DEMO_USER", demoUser)
    .WithEnvironment("WDS_DEMO_PASSWORD", demoPassword)
    .WithEnvironment("WDS_KEYCLOAK_CLIENT_SECRET", clientSecret)
    .WithEnvironment("KC_HOSTNAME", "http://localhost:8081")
    .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
    .WithBindMount("keycloak", "/opt/keycloak/data/import")
    .WithArgs("start-dev", "--import-realm")
    .WithHttpEndpoint(port: 8081, targetPort: 8080, name: "http");

// --- a third studio, built by hand ----------------------------------------------------------------
// The same pair as everywhere else, read-only everywhere, and connections labelled and coloured the
// way an operator wants to see them.
builder.AddWebDataStudio("admin-studio")
    // Each studio shows its resource name in its header and browser tab; this one says more.
    .WithTitle("Production · read only")
    .WithLogin(demoUser, demoPassword)
    .WithReadOnly()
    .WithMcpEndpoint("mcp", demoPassword)     // the studio as an MCP server
    .WithSessionLimits(maxSessions: 4, idleTimeout: TimeSpan.FromMinutes(2))
    .WithReference(shop, connectionName: "SHOP_PROD", group: "Production", color: "#e03131")
    .WithReference(orders, connectionName: "ORDERS_PROD", group: "Production", color: "#e03131")
    // 3. A folder — the version of a bucket that needs nothing installed at all. Each studio has
    //    its own volume, so this one gets the demo's files mounted in as well.
    .WithBindMount("drop", "/data/incoming")
    .WithStorage("DROP", "file:///data/incoming", readOnly: true, group: "Files")
    // The Azure emulator, and the MinIO from above with its endpoint and keys resolved at run time.
    .WithBlobStorage(blobs, container: "exports", connectionName: "EXPORTS", group: "Buckets")
    .WithStorage("LAKE", ReferenceExpression.Create(
        $"s3://lake?endpoint={minio.GetEndpoint("api")}&access={demoUser}&secret={demoPassword}&region=us-east-1"),
        group: "Buckets")
    // Only these schemas are read on a big server: the tree, the completion cache and the object
    // search each walk what they are given.
    .WithSchemas("SHOP_PROD", "public")
    // Export formats written as text with placeholders rather than as code to run.
    .WithExportTemplates("export-templates")
    // Who did what through this studio, kept for a year: every statement, export and refused
    // request, readable under Administration -> Audit.
    .WithAuditTrail(days: 365);

// --- a fourth studio, signed in through the provider ---------------------------------------------
// Nothing here knows about accounts: who may sign in — and with which role — is the provider's
// answer. The demo account (dba-group) is an admin, bob (developers) may write, and carol is in
// neither group so she gets the default role and sees everything read-only. All three have the demo
// password, because the realm file reads it from the environment.
builder.AddWebDataStudio("sso-studio", port: 8082)
    .WithTitle("Signed in with Keycloak")
    .WithReference(shop, connectionName: "SHOP", group: "Shop")
    .WithSingleSignOn(
        // The container-facing address — the studio fetches the provider's metadata from here, and
        // that document sends the browser to localhost:8081 by itself. Aspire puts the containers on
        // one network and gives each the resource name as an alias, so "keycloak" resolves from
        // inside the studio; http://localhost:8081 would be the studio's own localhost and reach
        // nothing.
        "http://keycloak:8080/realms/webdatastudio",
        "webdatastudio",
        clientSecret,
        label: "Sign in with Keycloak",
        "openid", "profile", "email")
    // The provider knows its groups; what an admin may do here is the studio's own decision.
    .WithSignInRoles(
        admins: ["dba-group"],
        editors: ["developers"],
        defaultRole: StudioRoles.Viewer)
    .WithAuditTrail(days: 30)
    .WaitFor(keycloak);

// --- reports the studio writes by itself ---------------------------------------------------------
// Reading statements only, on the studio's own volume under /data/exports. Every two minutes is a
// demo interval: it is a file you can watch appear rather than something to wait a day for.
studio.WithScheduledQueries(
    new ScheduledStudioQuery("order-totals", "SHOP",
        "SELECT o.id, c.name AS customer, o.status, sum(i.quantity * i.unit_price) AS total "
        + "FROM orders o JOIN customers c ON c.id = o.customer_id "
        + "LEFT JOIN order_items i ON i.order_id = o.id GROUP BY o.id, c.name, o.status",
        EveryMinutes: 2, Format: "csv"),
    new ScheduledStudioQuery("busiest-paths", "SHOP",
        "SELECT path, count(*) AS views, round(avg(ms)) AS avg_ms FROM page_views "
        + "GROUP BY path ORDER BY views DESC",
        EveryMinutes: 5, Format: "json"));

// --- backups nobody has to remember --------------------------------------------------------------
// Every ten minutes is a demo interval, the way the reports above run every two: it is a file you
// can watch appear rather than something to wait a day for. The dumping is pg_dump's, which is in
// the studio's image; three are kept and the older ones go.
studio.WithBackupSchedule("/data/backups",
    new StudioBackup("shop", "SHOP", EveryMinutes: 10, Keep: 3),
    new StudioBackup("shop-schema", "SHOP", EveryMinutes: 30, SchemaOnly: true, Keep: 2));

// --- a scratch database that does not start out empty ---------------------------------------------
// The other kind of seed: not SQL written down, but tables that already exist on another connection.
// SHOP is PostgreSQL and SCRATCH is a SQLite file, so this also shows what a copy between two
// engines does to the column types — each one becomes the nearest thing SQLite has.
//
// A table that already exists is left alone, so restarting the stack does not overwrite whatever
// you did to the copy last time.
studio.WithSeedFrom(new StudioSeedCopy("SHOP", "SCRATCH",
        ["customers", "orders"], MaxRows: 200))
    // The copy runs shortly after the studio starts, and a server that is not up yet has nothing to
    // copy. Aspire already knows how to say "not before this one is healthy", so nothing in the
    // studio has to retry.
    .WaitFor(postgres);

builder.Build().Run();
