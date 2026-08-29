---
title: Nextended.Aspire.Hosting.WebDataStudio
---
# Nextended.Aspire.Hosting.WebDataStudio

📚 **[Full API reference](/projects/aspire-webdatastudio-api)** — every public type and member, generated from the compiled assembly.

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-webdatastudio)

A [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) integration for
[WebDataStudio](https://fgilde.github.io/WebDataStudio/) — a browser-based database studio for
PostgreSQL, MySQL, SQL Server, SQLite, Oracle, DuckDB, ClickHouse, MongoDB, Redis and object storage. One call on a
database resource and the studio comes up with that database already configured.

## Overview

The package wraps the `ghcr.io/fgilde/webdatastudio` container into an Aspire resource and writes
the studio's `WDS_CONN_*` environment variables from the connection strings your stack already
produces. Nothing is typed twice, and no connection string ends up in source control.

Sharing is the interesting part: several databases normally want *one* studio, but a stack may
also want a second, differently configured one. Both are the same call with a different name.

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.WebDataStudio
```

## Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var shop = builder.AddPostgres("pg").AddDatabase("shop").WithWebDataStudio();
var orders = builder.AddSqlServer("sql").AddDatabase("orders").WithWebDataStudio();
builder.AddRedis("cache").WithWebDataStudio();

builder.Build().Run();
```

One studio resource appears in the dashboard, with `SHOP`, `ORDERS` and `CACHE` in its explorer.

## What you get, in one app host

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var shop = builder.AddPostgres("pg").AddDatabase("shop");
var orders = builder.AddSqlServer("sql").AddDatabase("orders");
var storage = builder.AddAzureStorage("storage").RunAsEmulator();   // Azurite while developing

builder.AddWebDataStudio("studio")
    .WithReference(shop)                                    // every engine, one call each
    .WithReference(orders, readOnly: true, color: "#e03131")
    .WithBlobStorage(storage.AddBlobs("exports"))           // a container, browsable and queryable
    .WithStorage("LAKE", "s3://lake?region=eu-central-1")   // AWS, MinIO, R2, Wasabi, Ceph
    .WithStorage("DROP", "file:///data/incoming")           // or just a folder
    .WithSchemas("shop", "public", "sales")                 // don't read 5000 tables to show 12
    .WithExportTemplates("export-templates")                // export formats as text, not as code
    .WithSeedScript("seed")                                 // a fresh stack with data in it
    .WithSavedQueriesFromDirectory("queries")               // the five queries everybody needs
    .WithLogin("admin", builder.AddParameter("studio-password", secret: true))
    .WithSingleSignOn(authority, clientId, oidcSecret)      // or the provider you already have
    .WithAuditTrail(days: 365)                              // who did what, for a year
    .WithMcpEndpoint("mcp")                                 // the studio as a tool for AI agents
    .WithOllamaAssistant(ollama, "llama3.1");               // optional SQL assistance, local

builder.Build().Run();
```

And in the studio itself, without any of it being configured here: the object tree and the query
editor, the data tab with its filter language, **Find data** for "which table has 4711 in it", the
**Jobs** tab for what the server runs on a schedule (Agent, pg_cron, events), **Capture** for what ran
in the next minute and what the index advisor makes of it, a read of every statement before it runs,
an interactive Entra sign-in for Azure SQL, Synapse and Fabric, and **Add a bucket** for attaching one
from the UI rather than from here.

Also without configuration: what is inside a JSON column and the `SELECT` that flattens it, a table
followed on a timer with the new rows tinted, a file in a bucket turned into a real table, how much
every table grew since the studio last looked, what this studio keeps running and whether it is
getting slower, **Data quality** rules that count the rows breaking them and report with the health
findings, and a **development subset** — these rows, the rows they point at, what is about people
replaced — as one SQL script that `WithSeedScript` can load into the next fresh stack.

**One account for the whole demo.** Two parameters — `demo-user` (`admin`) and `demo-password`
(`change-me-please`) — are what every part of it asks for: the admin studio's login, MinIO's root
account and its access keys, the Keycloak administrator, and the three people inside the Keycloak
realm. The realm file carries `${WDS_DEMO_USER}` and `${WDS_DEMO_PASSWORD}` placeholders, which the
import substitutes from the environment, so changing the parameter changes the sign-in everywhere.
The Keycloak client secret is its own parameter, because a client secret is not a person's password.

A runnable version of exactly this is in the repository:
[WebDataStudio.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)
— it starts PostgreSQL, SQL Server, MongoDB, Redis, Azurite, a MinIO and a Keycloak behind four
studios, and every one of those comes up with data in it: a shop with a document column, a
partitioned table, row-level security and 60 000 page views; carriers and 20 000 scans on SQL Server;
five people worth anonymising in a SQLite file; sessions and telemetry in MongoDB; a key of every
type in Redis; a bucket with a CSV, an NDJSON and a prefix that reads as one table; and a folder with
a PDF and a PNG in it. So the first things you can do after `dotnet run` are open a file in a bucket
as a table and sign in to a studio with the demo account (`admin` / `change-me-please`) without that
account existing in the studio at all.

The two engines without SQL are worth a click as well: **Open data** on a MongoDB collection in the
analytics studio pages it with a `find`, sorted and filtered by the server, and **Open data** on the
Redis `db0` or a key prefix lists the keys with their type, TTL, length and memory — the inventory of
a cache, in a grid that sorts and exports. Both arrived in the studio image after 1.2.0.

## Sharing one studio, or running several

`WithWebDataStudio()` creates the studio the first time and attaches to it every time after. The
studio's resource name is the key:

```csharp
// The default studio, shared by everything that does not ask for another
shop.WithWebDataStudio();
orders.WithWebDataStudio();

// A second studio, chosen by name
warehouse.WithWebDataStudio(studioName: "analytics-studio");
events.WithWebDataStudio(studioName: "analytics-studio");

// A third, built by hand and handed to the databases that belong in it
var admin = builder.AddWebDataStudio("admin-studio")
    .WithLogin("admin", builder.AddParameter("studio-password", secret: true))
    .WithReadOnly();

production.WithWebDataStudio(admin, group: "Production", color: "#e03131");
```

From the studio's side the same stack reads like this:

```csharp
builder.AddWebDataStudio("studio")
    .WithReference(shop)
    .WithReference(orders, connectionName: "ORDERS_PROD", readOnly: true)
    .WithConnection("LEGACY", "Host=old-box;Database=legacy;Username=ro;Password=pw",
        WebDataStudioEngine.PostgreSql, group: "Legacy");
```

`WithReference` on a studio is this package's own overload. Aspire's built-in one writes a
`ConnectionStrings__*` variable, which the studio does not read; this one writes the `WDS_CONN_*`
variables it does.

## Configuration

| Call | Effect |
|------|--------|
| `AddWebDataStudio(name = "webdatastudio", port?, image?, tag?)` | Add the studio container: HTTP endpoint, health check, per-instance data volume. |
| `.WithReference(resource, connectionName?, engine?, readOnly?, group?, color?)` | Attach any resource with a connection string. |
| `.WithConnection(name, connectionString, engine, …)` | Attach a database outside the stack. Also accepts a `ReferenceExpression`. |
| `.WithStorage(name, url, readOnly?, group?, color?)` | Attach object storage by URL: `s3://`, `azblob://`, `gs://`, `file://`. |
| `.WithBlobStorage(blobs, container?, connectionName?, prefix?, …)` | Attach the blob resource the app host models — Azurite while developing, the real account once deployed. |
| `.WithLogin(user, password)` | Guard the studio with an admin account. **Chaining adds accounts** rather than replacing them. Both halves also accept a `ParameterResource`. |
| `.WithUser(user, password, role, connections…)` | One account with a role — `StudioRoles.Admin`, `Editor`, `Viewer` — and, optionally, the connections it may see. |
| `.WithSingleSignOn(authority, clientId, secret?, label?, scopes…)` | Sign people in through the identity provider you already have — Entra, Keycloak, Auth0, Okta — instead of accounts in the environment. |
| `.WithSignInRoles(admins?, editors?, viewers?, defaultRole?)` | Which of that provider's groups, roles or addresses get which studio role. |
| `.WithAuditTrail(days = 90)` / `.WithoutAuditTrail()` | How long the studio keeps its record of who did what — or turn it off for a deployment that keeps its own. |
| `.WithAssistant(server, model, …)` | Point the studio's optional assistance at a model server in the stack, a URL, a `ReferenceExpression`, or a URL with a `ParameterResource` key. |
| `.WithOllamaAssistant(ollama, model)` / `.WithLocalAiAssistant(localai, model)` | The same, named for the two servers people reach for first. |
| `.WithClaudeAssistant(key)`, `.WithChatGptAssistant(key)`, `.WithOpenRouterAssistant(key)`, `.WithGroqAssistant(key)`, `.WithMistralAssistant(key)`, `.WithDeepSeekAssistant(key)`, `.WithGeminiAssistant(key)`, `.WithAzureOpenAiAssistant(…)` | The hosted providers, one call each. |
| `.WithMaskedColumns("ssn", "iban")` | Mask these columns as well, whatever the studio's name heuristic thinks. Chaining adds to the list. |
| `.WithUnmaskedColumns("token_type")` | Leave these alone, whatever it thinks. |
| `.WithoutColumnMasking()` | Turn the heuristic off, leaving only the columns you named. |
| `.WithMcpEndpoint(path?, key?, allowWrite?)` | Serve the studio as an MCP server for AI agents. Read-only unless `allowWrite`. |
| `.WithScheduledQueries(jobs…)` | Run reading queries on a schedule and write each result as a file. |
| `.WithSavedQueriesFromDirectory(path)` | Mount a folder of `.sql` files and import them as saved queries at start. |
| `.WithSeedScript(path)` | Run a seed script once per connection — a file, or `{CONNECTION}.sql` per connection. |
| `.WithSchemas(connectionName, schemas…)` | Read only these schemas on that connection. On a server with thousands of tables that is the difference between a tree that opens and one that does not. |
| `.WithExportTemplates(path)` | Mount a folder of export templates — an export format written as text with placeholders rather than as code. |
| `.WithQualityRules(path)` | Mount the data quality rules the deployment owns, as JSON: rules about the rows rather than the schema, kept in the repository. |
| `.WithSavedQueries(params SavedStudioQuery[])` | The same queries written here instead of as files — name, statement, optional folder and connection. |
| `.WithExportTemplates(params StudioExportTemplate[])` | An export format written in the app host rather than as a `.json` file. |
| `.WithQualityRules(params StudioQualityRule[])` | Data quality rules written in the app host, typed, instead of hand-kept JSON. |
| `.WithSeedScript(connection, sql)` | A seed script for one connection, written here rather than as `{CONNECTION}.sql`. |
| `.WithConnections(params StudioConnectionEntry[])` | Connections that are not resources in this stack — a legacy server, somebody else's replica. Read-only in the UI, like every environment connection. |
| `.WithConnectionsFromFile(path)` | The same array, kept as JSON in your repository. |
| `.WithDashboards(params StudioDashboard[])` | A page of statements everybody sees the first time they open the studio. It belongs to the deployment: shown, not editable there. |
| `.WithDashboardsFromFile(path)` | The same, as JSON in your repository. |
| `.WithSnippets(params StudioSnippet[])` | Editor snippets for everybody. A person's own snippet with the same prefix wins for that person. |
| `.WithSnippetsFromFile(path)` | The same, as JSON. |
| `.WithMaskingFromFile(path)` | The masking baseline as a file: `{ "maskByDefault": true, "extra": [...], "never": [...] }`. Counts alongside `WithMaskedColumns`. |
| `.WithDefaultPreferences(timeZone?, pageSize?, …)` | What a studio starts with before anybody changed it — the time zone timestamps are shown in, rows per page, and the rest. A starting point, not a lock. |
| `.WithBackupSchedule(directory, params StudioBackup[])` | Dumps the studio takes on its own: every so many minutes or daily at a time in UTC, keeping the newest N. Mount a volume at `directory`, or they live as long as the container does. |
| `.WithBackupScheduleFromFile(path, directory)` | The same schedule, as JSON in your repository. |
| `.WithSeedFrom(params StudioSeedCopy[])` | Fills one connection from another when the stack comes up: the tables are created in the target and filled. A table that already exists is left alone. |
| `.WithSchemaSnapshots(path?)` | Snapshot every connection's schema on start and report the drift since the last one. |
| `.WithOpenTelemetry(collector \| url?, serviceName?)` | Send the studio's traces and metrics to an OTLP collector — a resource in the stack, or a URL. |
| `.WithSharedResults(ttl?, isPublic?, maxRows?)` | Let people keep a result and share it as a link. Off by default. |
| `.WithArchives(path?, maxRows?)` | Move or cap the results the studio keeps as files. They are on by default; this decides where and how big. |
| `.WithAlertWebhook(url, interval?, minSeverity?, connections?)` | Post new health findings — missing indexes, tables without a key, bloat — to Slack, Teams or any webhook. |
| `.WithMcpTools(WebDataStudioMcpTools.SchemaOnly)` | Narrow the endpoint to named tools. `ReadOnly` and `SchemaOnly` are ready-made sets. |
| `.WithoutAssistantTools()` | Keep the studio's own assistant from using the MCP tools. |
| `.WithTitle(name)` | Name in the studio's header and browser tab. Defaults to the resource name; `null` leaves it unnamed. |
| `.WithTheme(WebDataStudioTheme.Ocean)` | The theme the studio comes up in — an enum of the studio's own themes, or a string for one this package does not know yet. A person who picks another keeps their choice. |
| `.WithReadOnly(readOnly = true)` | Every connection read-only, enforced in the driver. |
| `.WithQueryTimeout(TimeSpan)` | Default statement timeout. |
| `.WithMaxRows(int)` | Default row cap per result. |
| `.WithSessionLimits(maxSessions?, idleTimeout?)` | Cap open sessions per connection and their idle life. |
| `.WithTransactionTimeout(TimeSpan)` | How long a transaction a query tab holds open may sit untouched before the studio rolls it back (default 15 minutes). |
| `.WithSecretKey(base64)` | Key for the secrets the studio stores; also takes a `ParameterResource`. |
| `.WithDataVolume(name?)` / `.WithDataBindMount(path)` | Move the studio's own data. |
| `resource.WithWebDataStudio(configure?, studioName?, connectionName?, engine?)` | Attach from the database's side. |
| `resource.WithWebDataStudio(studio, …)` | Attach to a studio you built yourself. |

## The theme it comes up in

```csharp
builder.AddWebDataStudio("studio")
    .WithTheme(WebDataStudioTheme.AspireDashboard);
```

One of `Ocean` (the studio's default), `GitHubDark`, `GitHubLight`, `AspireDashboard`, `Blazor`,
`Dracula`, `Nord`, `OneDark`, `Monokai`, `Terminal`, `SolarizedDark`, `SolarizedLight`, `NeonGlow`,
`Synthwave`, `Hologram`, `Nightlife`, `Obsidian`, `Stage`, `Dev`, `LinkHub` or `Kiosk`. Each value
carries the studio's own theme id as its `[Description]`, so the two lists cannot drift apart, and
`WithTheme("some-new-theme")` is there for a newer studio image that has one this package does not
know yet.

It is the **initial** theme, not a lock. Whoever opens the studio may pick another one from the
header, that choice belongs to their browser and wins over this — and it is never overwritten, so
raising the deployment's default later still reaches everybody who never picked one. An id the studio
does not have is ignored (a line in the browser's console), because a stack should not fail to start
over a colour scheme.

Three studios in one stack, told apart at a glance:

```csharp
var shop = builder.AddPostgres("pg").AddDatabase("shop");

shop.WithWebDataStudio(s => s.WithTitle("Development").WithTheme(WebDataStudioTheme.Dev));
shop.WithWebDataStudio(s => s.WithTitle("Production").WithTheme(WebDataStudioTheme.Stage)
    .WithReadOnly(), studioName: "prod-studio");
```

## From a folder, or written here — or both

Everything the studio reads from a repository can also be written in the app host:

```csharp
builder.AddWebDataStudio()
    // what the repository ships, and a review can catch
    .WithSavedQueriesFromDirectory("queries")
    .WithQualityRules("quality")
    // what belongs to this stack
    .WithSavedQueries(
        new SavedStudioQuery("Orders without a customer",
            "SELECT * FROM orders WHERE customer_id IS NULL", folder: "Ad hoc", connection: "SHOP"))
    .WithQualityRules(new StudioQualityRule(
        "SHOP", "orders", "NotNull", Column: "customer_id",
        Message: "an order without a customer is one nobody can invoice"))
    .WithExportTemplates(new StudioExportTemplate(
        "wiki", "Wiki table", "txt", "text/plain",
        Row: "| {{values}} |", Header: "| {{columns}} |", Separator: " | "))
    .WithSeedScript("SCRATCH", "INSERT INTO people (name) VALUES ('ada');");
```

**Both at once is the point.** Each of these settings takes a list of paths, so the folder version
and the inline version add up rather than one replacing the other — which is what happened before,
silently and in call order.

The inline files are created *inside the container* rather than mounted from the host, so a
published stack carries them the same way a local one does and there is no folder to keep in step.
Calling one of these twice adds to what the earlier call wrote; a saved query with the same name
replaces itself rather than appearing twice.

What stays a file-only thing on purpose: **accounts**. `WithUser` and `WithLogin` take a parameter
for the secret, and a list of people with passwords does not belong in a repository file — that is
what [an identity provider](#signing-in-with-the-provider-you-already-have) is for.

### The rest of it: connections, dashboards, snippets, preferences

The same two ways — written here, or read from a file, or both — for everything else a deployment
brings with it:

```csharp
builder.AddWebDataStudio()
    // servers that are not resources in this stack
    .WithConnections(new StudioConnectionEntry("LEGACY", "sqlserver",
        "Server=old;Database=erp;Trusted_Connection=True", ReadOnly: true, Group: "Old"))
    // the page everybody sees on the first morning
    .WithDashboards(new StudioDashboard("Morning",
    [
        new StudioTile("Orders today", "SHOP", "SELECT count(*) FROM orders WHERE placed > current_date"),
        new StudioTile("By status", "SHOP",
            "SELECT status, count(*) FROM orders GROUP BY status", View: "chart", Width: 2),
    ], RefreshSeconds: 60))
    // the filter everybody types
    .WithSnippets(new StudioSnippet("tenant", "tenant filter", "WHERE tenant_id = ${1:1}"))
    // and what a studio starts with: UTC, so a screenshot cannot be misread
    .WithDefaultPreferences(timeZone: "utc", pageSize: 500);
```

**What belongs to the deployment stays its own.** A shipped dashboard is shown with a
*from the deployment* badge and its edit and delete buttons are off — somebody who wants it
different saves a copy under another name. A shipped snippet is offered to everybody, and a person's
own snippet with the same prefix wins for that person. The preferences are a starting point: the
first person to change one keeps their change.

**A connection string with a secret in it belongs in a parameter.** `WithConnections` takes plain
text and is meant for what a repository may hold; `WithConnection(name, connectionString, …)` takes
a `ParameterResource`, which is where a password goes.

### Backups, and data that already exists somewhere

Two things that make a stack you leave running:

```csharp
builder.AddWebDataStudio()
    // a dump every night, seven kept, into a volume
    .WithBackupSchedule("/backups",
        new StudioBackup("nightly", "SHOP", DailyAtUtc: "02:00", Keep: 7))
    // and a development database that does not start out empty
    .WithSeedFrom(new StudioSeedCopy("STAGING", "DEV",
        ["countries", "products", "customers"], MaxRows: 500));
```

**The dump is the engine's own tool** — `pg_dump`, `mysqldump`, `mongodump` — which has to be in the
studio's image; the run says so rather than writing an empty file when it is not. Two ways of saying
when: `EveryMinutes`, or `DailyAtUtc`. There is no cron parser on purpose. `Keep` prunes this job's
own files and nobody else's, because a volume that fills up is how a backup schedule stops being
one. `GET /api/admin/backup-schedule` says what the jobs are and how the last run of each went.

**`WithSeedFrom` is the other kind of seed.** `WithSeedScript` is the answer when you can write the
data down; this is the answer when you cannot, because the tables already exist on a staging server
or in a container this stack brought up. Each table is created in the target and filled, at most
`MaxRows` rows — a seed, not a replica.

It carries the seed script's guards and one more: **a table that already exists is left alone**.
Nothing is written into a read-only connection, nothing into one coloured red — the studio's
convention for production — and a restart never overwrites what somebody has been working on.

## Several accounts, with roles

`WithLogin` is additive: two calls mean two people can sign in. `WithUser` adds one with a role and,
optionally, a whitelist of connections.

```csharp
builder.AddWebDataStudio("studio")
    .WithReference(shop)
    .WithReference(warehouse)
    .WithLogin("hans", "hans")                                    // admin
    .WithLogin("pete", "pete")                                    // admin as well
    .WithUser("grace", "read-only", StudioRoles.Viewer, "shop")   // sees shop, read-only
    .WithUser("eve", evePassword, StudioRoles.Editor);            // writes, does not administer
```

- `admin` reaches the administration panel, `editor` may read and write, `viewer` gets every
  connection read-only.
- The fourth argument is a whitelist of connection names; none means all of them. A connection an
  account may not see does not exist for it — not in the explorer, and not by guessing its id.
- One plain admin still writes `WDS_USER`/`WDS_PASSWORD`, so nothing changes for a stack that
  already uses `WithLogin` once. More than one — or one with a role — writes `WDS_USERS`.
- Saying the same name twice replaces that account instead of adding a second one with the same
  login.
- A role the studio does not have throws in the app host rather than becoming a silent `viewer`
  inside the container.

The studio itself lists who exists under *Administration → Studio users*, and its header carries a
user menu with the role and a way out. Accounts stay deployment configuration: nobody can promote
themselves through the UI.

## Signing in with the provider you already have

`WithLogin` and `WithUser` put accounts in the container's environment: fine for one team, wrong for
a company that already decides who works there somewhere else.

```csharp
builder.AddWebDataStudio("studio")
    .WithReference(shop)
    .WithSingleSignOn(
        "https://login.microsoftonline.com/<tenant>/v2.0",
        "00000000-0000-0000-0000-000000000000",
        builder.AddParameter("oidc-secret", secret: true),
        label: "Sign in with Entra",
        "openid", "profile", "email")
    .WithSignInRoles(
        admins: ["dba-group"],
        editors: ["developers"],
        defaultRole: StudioRoles.Viewer)
    .WithAuditTrail(days: 365);
```

- The flow is the authorization code flow with PKCE, and the redirect URI to register with the
  provider is `https://<the studio>/signin-oidc`. A provider checks it exactly, so **pin the
  studio's port** — `AddWebDataStudio("studio", port: 8082)` — because a port that changes every run
  cannot be registered anywhere.
- Needs a studio image that has the feature: it arrived after 1.2.0.
- **Configuring a provider closes the door.** A studio with a provider and no accounts is not an open
  studio with a login button on it — every API call needs a sign-in.
- **The role stays the studio's own.** A provider knows its groups; it does not know what an admin may
  do here. Matching reads its `roles`, `role`, `groups` and `wids` claims *and* the person's own name,
  address and UPN, so `admins: ["ada@example.com"]` works in a tenant with no groups. Admin beats
  editor beats viewer for somebody in two of them, and anybody who matches nothing gets
  `defaultRole` — a viewer unless said otherwise.
- A provider **and** accounts can both be configured: the login screen then shows the button and the
  form.
- An authority on `http://` — a Keycloak in the same app host — is allowed to serve its metadata over
  plain http, and the call sets that for you. Anything that is not an issuer URL throws here rather
  than failing inside the container.

## Who did what

The studio keeps one line per request that changed something or took data out of the building — a
statement run, an export, a change applied, a backup downloaded, a request refused — with who asked,
against which connection, and what came of it. It is read in *Administration → Audit*, and it is on
by default with 90 days of history.

```csharp
.WithAuditTrail(days: 365)     // say the number your compliance people asked for
.WithoutAuditTrail()           // or turn it off, if a gateway already records this
```

Request bodies are never recorded: a connection body carries a password. What lands in the trail is
the route, and a detail the studio deliberately writes down — the statement itself for a run, the
format and scope for an export.

## The optional assistance

The studio can explain a statement and draft one from a question. It is **off unless configured** —
no endpoint means no button, no calls, and `/api/health` reports `assist: false`.

Point it at a model server in the same stack and the conversation never leaves the machine:

```csharp
var ollama = builder.AddOllama("ollama").WithDataVolume();   // CommunityToolkit
ollama.AddModel("llama3.2");

builder.AddWebDataStudio()
    .WithReference(shop)
    .WithOllamaAssistant(ollama, "llama3.2");
```

The studio uses that resource's own endpoint, so the traffic stays on the container network, and it
waits for the server — an assistant button that answers "connection refused" for the first minute is
worse than one that arrives a moment later.

LocalAI is the same call under another name, and anything else that speaks the OpenAI
chat-completions shape works through the general one:

```csharp
studio.WithLocalAiAssistant(localai, "qwen3-8b");
studio.WithAssistant(vllm, "mixtral", path: "/v1/chat/completions");
```

For a hosted model there is one call per provider, so nobody has to look a URL up:

| Call | Provider | Default model |
|------|----------|---------------|
| `.WithClaudeAssistant(key, model?)` | Anthropic, through their OpenAI-compatible endpoint | `claude-sonnet-4-5` |
| `.WithChatGptAssistant(key, model?)` | OpenAI | `gpt-4o-mini` |
| `.WithOpenRouterAssistant(key, model?)` | OpenRouter — the model name carries the provider | `anthropic/claude-sonnet-4.5` |
| `.WithGroqAssistant(key, model?)` | Groq | `llama-3.3-70b-versatile` |
| `.WithMistralAssistant(key, model?)` | Mistral | `mistral-large-latest` |
| `.WithDeepSeekAssistant(key, model?)` | DeepSeek | `deepseek-chat` |
| `.WithGeminiAssistant(key, model?)` | Google, through their OpenAI-compatible endpoint | `gemini-2.5-flash` |
| `.WithAzureOpenAiAssistant(resource, deployment, key, apiVersion?)` | Azure OpenAI — builds the deployment URL for you | the deployment name |
| `.WithOllamaAssistant(ollama, model?)` / `.WithLocalAiAssistant(localai, model)` | a model server in your own stack | `llama3.2` / — |

Every key also takes an Aspire `ParameterResource`, which is how it stays out of the manifest.

## Masked columns

```csharp
studio
    .WithMaskedColumns("ssn", "customer_note")
    .WithUnmaskedColumns("token_type");
```

The studio masks columns whose names say they hold a secret before the values leave the server;
these two calls correct that guess for a schema it reads wrong, and `WithoutColumnMasking()` turns
the guessing off entirely. What somebody sets from the studio's column menu wins over them.

## Sharing a result

```csharp
studio.WithSharedResults(ttl: TimeSpan.FromDays(3), isPublic: false);
```

A result grows a **Share** button, and the link shows the rows as they were — a snapshot, not a
query: it cannot run anything, and masking is applied before the rows are stored, so a masked column
stays masked in that link. `isPublic: true` lets anybody with the link open it without signing in,
which is the point of a link and a decision worth making on purpose.

## Traces and metrics

```csharp
var collector = builder.AddOpenTelemetryCollector("otel");   // Nextended.Aspire.Hosting.Grafana

studio.WithOpenTelemetry(collector);        // or WithOpenTelemetry("http://collector:4317")
```

The studio then reports its own work to the same collector as the rest of the stack: a span per run
(`query.execute`, tagged with engine, rows and outcome), a span per MCP tool call, and counters for
statements, rows and tool calls. It reports as the resource's name unless you say otherwise, so three
studios are told apart, and it waits for the collector so the first traces are not thrown away.

## Alerts

```csharp
studio.WithAlertWebhook(builder.AddParameter("slack-webhook", secret: true),
    interval: TimeSpan.FromHours(2), minSeverity: "warning");
```

The studio runs the analysis behind its health report on that interval and posts what is **new** —
missing indexes, tables without a primary key, bloat — to the webhook. The payload's `text` field is
what Slack, Mattermost, Discord and Teams render; the findings ride along structured, each with the
statement that would fix it. Only new findings are sent, and a failed post is retried on the next
sweep.

## Queries and data that ship with the stack

```csharp
builder.AddWebDataStudio()
    .WithReference(shop)
    .WithSavedQueriesFromDirectory("./queries")   // .sql files -> the Saved panel
    .WithSeedScript("./seed");                    // SHOP.sql -> run once on SHOP
```

Both folders are mounted read-only and read at start. Saved queries are imported idempotently — a
restart replaces rather than duplicates — and a file may name its connection and folder in comments
(`-- wds:connection SHOP`, `-- wds:folder Ops`).

A seed script runs **once per content**: editing it makes it run again, restarting does not. It never
runs on a read-only connection, and never on one marked as production.

## Scheduled reports

```csharp
studio.WithScheduledQueries(
    new ScheduledStudioQuery("orders-per-day", "SHOP",
        "SELECT date(created_at) AS day, count(*) FROM orders GROUP BY 1", DailyAtUtc: "03:00"),
    new ScheduledStudioQuery("queue-depth", "SHOP",
        "SELECT count(*) FROM jobs WHERE state = 'pending'", EveryMinutes: 15, Format: "json"));
```

The schedule is generated as a file and mounted read-only, so it lives in the app host rather than in
a volume somebody has to remember. Results land in `/data/exports` on the studio's own volume, masked
like every other export. Only reading statements run, and a job that says neither `EveryMinutes` nor
`DailyAtUtc` throws here rather than never running.

## Archives

```csharp
studio.WithArchives();                          // /data/archives, on the studio's own volume
studio.WithArchives("/mnt/archives", maxRows: 50_000);
```

A result can be kept as a file the studio holds on to: what a table looked like before the migration,
what the report said last Tuesday. The panel lists them, opening one shows its rows, and the rows can
be scripted back out as `INSERT`s for wherever they should go next.

The format is NDJSON — a header line naming the columns and where they came from, then one row per
line — so anything can read it. Masked columns are masked *in the file*: an archive of them would be a
way around the masking. Archives work without this call; it is for putting them on a different volume,
or for capping how much one keeps.

## Object storage

```csharp
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var exports = storage.AddBlobs("exports");

studio.WithBlobStorage(exports);                                  // Azurite now, the account later
studio.WithStorage("LAKE", "s3://bucket/exports?region=eu-central-1");
studio.WithStorage("DROP", "file:///data/incoming", readOnly: true);
```

A bucket is a connection like any other: the studio browses containers, prefixes and objects in the
same tree, one page at a time, and reads a file as a table — a Parquet or a CSV in a bucket opens in
the data tab with sorting, the filter language, paging and export, through a DuckDB the studio holds.

`WithBlobStorage` takes the blob resource the app host already models and passes its connection
string through as it is: a connection string for the emulator, the blob service URI once deployed —
where the studio then uses its own managed identity, because the account name is inside either form.
`WithStorage` covers everything the app host does not model: `s3://` for AWS, MinIO, R2, Wasabi and
Ceph (with `?endpoint=` for those), `azblob://account/container`, `gs://bucket`, and `file://` for a
folder the container can reach.

With no credentials in the URL the studio uses the identity it runs as. Where keys are unavoidable,
pass them through an Aspire parameter rather than writing them into the app host. `readOnly: true`
and a production `color:` both refuse every upload and delete, in the server rather than in the UI.

One stated limit: DuckDB reaches Google Cloud Storage over the S3 protocol, which wants HMAC keys
(`?hmac=…&hmacsecret=…`). With a service account alone the tree, the preview and the download all
work and a query does not.

## Schemas and export templates

```csharp
studio.WithSchemas("shop", "public", "sales");     // nothing else is read at all
studio.WithExportTemplates("./export-templates");  // formats written as text, not as code
```

`WithSchemas` limits what a connection reads: the tree's first level, the completion cache, the object
search and the schema snapshot each walk what they are given, and on a server with five thousand
tables naming two schemas is what keeps the studio quick. Set here it is the deployment's decision and
the studio cannot widen it; left unset, somebody can choose a scope for their own studio in the
connection's properties, and empty still means everything.

`WithExportTemplates` mounts a folder of `.json` templates. Each is an id, a label, a file extension,
a content type and up to three pieces of text — header, row, footer — with `{{table}}`, `{{columns}}`,
`{{values}}`, `{{index}}`, `{{comma}}` and `{{col.NAME}}` as placeholders, each taking a filter for the
escaping that format needs (`sql`, `json`, `csv`, `html`, `upper`, `lower`). So an `INSERT` writer is
three lines of text and nothing the studio has to execute:

```json
{
  "id": "inserts", "label": "INSERT statements", "extension": "sql", "contentType": "application/sql",
  "header": "INSERT INTO {{table}} ({{columns}}) VALUES
",
  "row": "  ({{values|sql}}){{comma}}
",
  "footer": ";
"
}
```

A mounted template belongs to the deployment: the studio exports with it and cannot edit it, and a
copy under another id is the way to change one.

## Schema drift

```csharp
studio.WithSchemaSnapshots();          // /data/snapshots, on the studio's own volume
```

The studio writes a snapshot of every connection's schema shortly after start and reports what moved
since the last one — tables added or removed, and per table which columns, indexes and foreign keys
came or went. It lands on `GET /api/schema/{connection}/drift`, in the log, and in a message when
`WithAlertWebhook` is configured. `POST /api/schema/snapshot` takes one now.

This is not a migration tool: it catches the drift a migration tool cannot see, like the column
somebody added by hand on staging.

## The studio as an MCP server

`WithMcpEndpoint()` makes the studio answer the [Model Context
Protocol](https://modelcontextprotocol.io), so an agent can work with the databases of this stack:

```csharp
var mcpKey = builder.AddParameter("mcp-key", secret: true);

builder.AddWebDataStudio()
    .WithReference(shop)
    .WithMcpEndpoint(mcpKey)                        // read-only
    .WithClaudeAssistant(anthropicKey);
```

| Tool | What it does |
|---|---|
| `list_connections` | the databases the studio can reach, with their ids |
| `list_objects` | walks the object tree |
| `describe_object` | columns, indexes, keys, triggers — and which columns are masked |
| `browse_rows` | a page of rows, masked and capped |
| `run_query` | one reading statement, masked and capped |
| `explain_plan` | the query plan for a statement |
| `health_report` | the studio's analysis, each finding with its fix |
| `server_activity` | what is running, and who waits on whom |
| `redis_value` | one Redis key |
| `list_tables` | every table and view of a connection in one call |
| `find_data` | looks for a value in every text column of every table |
| `json_shape` | what is inside a JSON column, and the `SELECT` that flattens it |
| `table_sizes` | how big every table is, and how much bigger than it was |
| `query_stats` | what the studio has run, grouped by shape, and whether it is getting slower |
| `inspect_sql` | reads a statement without running it: no `WHERE`, a cartesian join, a `NOT IN` a NULL will break |
| `quality_rules` / `run_quality_rules` | the rules somebody wrote about the data, and what they count |
| `preview_script` / `apply_script` / `save_quality_rule` | only with `allowWrite: true`: a write is shown, then applied by its hash |

The rules are the studio's own: a read-only connection stays read-only, a masked column stays
masked, `run_query` refuses anything that writes (including a read with a second statement behind
it), and a tool call returns at most 200 rows. **A studio with accounts requires the key**, because
the MCP endpoint sits outside the login screen.

A bucket needs no tools of its own: object storage is a connection like any other, so `list_tables`
lists its objects and `browse_rows` reads a Parquet file through the reader that opens it.

`WithMcpTools(WebDataStudioMcpTools.SchemaOnly)` narrows the endpoint to named tools — a whitelist, enforced on the call as well as the listing.

The studio's header shows a plug icon once the endpoint is on, with the URL and ready-to-paste
configuration for Claude Code, Claude Desktop, VS Code and Cursor. And when both MCP and an
assistant are configured, the assistant answers from the database through the same tools —
`WithoutAssistantTools()` if you would rather it did not.

What leaves the studio is the statement or the question, and — only when the user turns the switch
on in the dialog — the table and column names of the connection. Never a row of data. Nothing the
model answers is executed: a suggested statement lands in the editor and goes through the same run
and the same preview as anything typed by hand.

## Engines

The engine is read from the resource type, so `AddPostgres`, `AddSqlServer`, `AddMySql`,
`AddOracle`, `AddMongoDB`, `AddRedis`, `AddValkey` and `AddGarnet` need no help. For anything else
pass `engine:` explicitly:

```csharp
studio.WithReference(clickhouse, engine: WebDataStudioEngine.ClickHouse);
```

Without one, the studio guesses from the connection string and skips the connection rather than
attaching it to the wrong driver.

## Accessing Resources

```csharp
var studio = builder.AddWebDataStudio();

studio.Resource.HttpEndpoint;       // the endpoint serving the studio
studio.Resource.ConnectionNames;    // labels of everything attached, in order
studio.Resource.Username;           // null while there is no login
studio.Resource.Accounts;           // name, role and connections per account — never a password
studio.Resource.AssistantModel;     // null while there is no assistance
studio.Resource.McpPath;            // null while the studio is not an MCP server
studio.Resource.McpAllowsWrite;     // whether an agent may change data
studio.Resource.Title;              // the name shown in the studio, resource name by default
```

## Notes

- Connection names become environment variables: `shop-db` shows up as `SHOP_DB`. Pass
  `connectionName` for something nicer. A name ending in `_ENGINE`, `_READONLY`, `_GROUP` or
  `_COLOR` is rejected, because the studio reads those as settings for another connection.
- Without `WithLogin` the studio has no login screen at all — the right default while it listens
  on your machine only. Put one on it before the endpoint becomes public; the package prints a
  warning when you publish an external endpoint without one.
- Without `WithAssistant` there is no assistance: no button in the UI and no calls anywhere.
- Every studio gets its own named volume, so two of them never share saved connections.

## Default Ports

| Resource | Container port | Host port |
|---|---|---|
| WebDataStudio | 8080 | assigned by Aspire unless `port:` says otherwise |

## Supported frameworks

.NET 8, .NET 9, .NET 10.

## Related projects

- [Nextended.Aspire](aspire.md) — the shared Aspire helpers
- [Nextended.Aspire.Hosting.N8n](aspire-n8n.md), [Supabase](aspire-supabase.md) — other hosting integrations

## The sample AppHost

`Tests/TestProjects/WebDataStudio.AppHost` runs PostgreSQL, SQL Server, MongoDB, Redis, Azurite and a
MinIO — the MinIO with a bucket and a CSV already in it — behind three studios: the shared one, an
analytics one, and an admin studio with a login, read-only connections, an MCP endpoint, a schema
scope and a folder of export templates. `dotnet run` in that folder is the fastest way to see what
this package does.

`Tests/TestProjects/AiStack.AppHost` shows the other half: a studio on that stack's PostgreSQL with
its assistance pointed at the Ollama running next to it, so *explain this statement* works without
anything leaving the machine.

## Links

- [Sample AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)
- [WebDataStudio documentation](https://fgilde.github.io/WebDataStudio/guide/)
- [WebDataStudio on GitHub](https://github.com/fgilde/WebDataStudio)
- [NuGet](https://www.nuget.org/packages/Nextended.Aspire.Hosting.WebDataStudio/)
