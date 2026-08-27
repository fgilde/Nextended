---
title: Nextended.Aspire.Hosting.WebDataStudio — API-Referenz
---

# Nextended.Aspire.Hosting.WebDataStudio — API-Referenz

🇬🇧 [This page in English](/projects/aspire-webdatastudio-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.WebDataStudio`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-webdatastudio)

## Nextended.Aspire.Hosting.WebDataStudio

### `ScheduledStudioQuery`

`class`

A query the studio runs on a schedule and writes to a file — the nightly report nobody wants to remember to run.

**Konstruktoren**

- `ScheduledStudioQuery(string Name, string Connection, string Sql, int? EveryMinutes = null, string DailyAtUtc = null, string Format = null, int? MaxRows = null)`

**Eigenschaften**

- `Connection : string { get; set; }`
  <br>Connection name, as the studio shows it.
- `DailyAtUtc : string { get; set; }`
  <br>Run once a day at this time in UTC, e.g. `03:00`.
- `EveryMinutes : int? { get; set; }`
  <br>Run this often. Use this or `DailyAtUtc`.
- `Format : string { get; set; }`
  <br>Export format: `csv` (the default), `json`, `xlsx`, …
- `MaxRows : int? { get; set; }`
  <br>Row cap for the run.
- `Name : string { get; set; }`
  <br>Names the job, and the files it writes.
- `Sql : string { get; set; }`
  <br>One reading statement. A write is refused when it runs.

### `StudioAccount`

`class`

One account of a WebDataStudio instance: who signs in, what they may do, and which connections they see. The password is deliberately not part of this — it goes to the container and nowhere else, so nothing can print it by accident.

**Konstruktoren**

- `StudioAccount(string Name, string Role, IReadOnlyList<string> Connections)`

**Eigenschaften**

- `Connections : IReadOnlyList<string> { get; set; }`
  <br>The connections this account may see, by name. Empty means all of them.
- `Name : string { get; set; }`
  <br>The login name.
- `Role : string { get; set; }`
  <br>`admin` (everything, including the administration panel), `editor` (read and write) or `viewer` (every connection read-only).

### `StudioRoles`

`static class`

The roles a `StudioAccount` can have.

**Felder**

- `Admin : string`
  <br>Everything, including the administration panel.
- `Editor : string`
  <br>Read and write, but no administration.
- `Viewer : string`
  <br>Every connection read-only.

### `WebDataStudioAssistantExtensions`

`static class`

Wires the studio's optional assistance — explain a statement, draft one from a question — to an OpenAI-compatible endpoint. Without one of these calls the feature does not exist: no button in the UI, no calls anywhere, and `/api/health` reports `assist: false`.

**Felder**

- `ChatCompletionsPath : string`
  <br>The path an OpenAI-compatible server serves chat completions on.
- `DefaultModel : string`
  <br>Model used when a call does not name one.

### `WebDataStudioAttachExtensions`

`static class`

Attaches WebDataStudio to a database from the database's own side, the way Aspire's `WithPgAdmin` and `WithRedisInsight` do it: the studio is created once and every further call attaches another connection to the same one.

### `WebDataStudioBuilderExtensions`

`static class`

Fluent API for running WebDataStudio inside your Aspire stack. Either add it yourself with `AddWebDataStudio` and attach databases with `WithReference`, or start from a database resource and call `WithWebDataStudio``1`.

### `WebDataStudioEngine`

`enum`

The database engines WebDataStudio can talk to. The value tells the studio which driver to open a connection string with, and is passed as `WDS_CONN_<NAME>_ENGINE`.

**Werte**

- `ClickHouse`
  <br>ClickHouse.
- `DuckDb`
  <br>DuckDB, backed by a file the container can reach.
- `MongoDb`
  <br>MongoDB.
- `MySql`
  <br>MySQL and MariaDB.
- `Oracle`
  <br>Oracle Database.
- `PostgreSql`
  <br>PostgreSQL (and anything speaking its wire protocol).
- `Redis`
  <br>Redis and Valkey.
- `SqlServer`
  <br>Microsoft SQL Server and Azure SQL.
- `Sqlite`
  <br>SQLite, backed by a file the container can reach.
- `Storage`
  <br>Object storage: an S3-compatible bucket, Azure Blob Storage, Google Cloud Storage, or a folder. The connection string is the storage URL — `s3://bucket/prefix`, `azblob://account/container`, `gs://bucket`, `file:///data/incoming`.
- `value__`

### `WebDataStudioEngineExtensions`

`static class`

Maps `WebDataStudioEngine` to the identifiers WebDataStudio expects.

**Extension Methods**

- `ToEngineId(this WebDataStudioEngine engine) : string`
  <br>The engine id as WebDataStudio spells it in `WDS_CONN_<NAME>_ENGINE`.

### `WebDataStudioFilesExtensions`

`static class`

Things a repository can hold and a review can catch: the queries everybody on the team needs, and the data that makes a fresh database worth opening. Both are folders on your machine, mounted into the studio and read at start.

### `WebDataStudioMcpExtensions`

`static class`

Turns the studio into an MCP server, so an agent — Claude Code, Claude Desktop, VS Code, Cursor, anything that speaks MCP — can reach the databases of this stack through it.

**Methoden**

- `MissingKeyWarning(WebDataStudioResource resource) : string`
  <br>What is wrong with this studio's MCP configuration, or null when nothing is. The app host prints it before anything starts; it is public so a test or a health check can ask the same question without starting an application.

**Felder**

- `DefaultPath : string`
  <br>Path the studio serves MCP on when nothing else is asked for.

### `WebDataStudioMcpTools`

`static class`

The tools the studio's MCP endpoint offers, by name — so `WithMcpTools` takes a constant rather than a string somebody has to spell right.

**Felder**

- `ApplyScript : string`
  <br>Runs the script a hash belongs to. Needs `allowWrite`.
- `BrowseRows : string`
  <br>A page of rows from one table, masked and capped.
- `DescribeObject : string`
  <br>Columns, indexes, keys and triggers of one object.
- `ExplainPlan : string`
  <br>The query plan for a statement.
- `HealthReport : string`
  <br>The studio's own analysis of a connection or a table.
- `ListConnections : string`
  <br>The databases the studio can reach, with their ids.
- `ListObjects : string`
  <br>Walks the object tree a level at a time.
- `ListTables : string`
  <br>Every table and view of a connection, in one call.
- `PreviewScript : string`
  <br>Splits a script and returns a hash. Runs nothing. Needs `allowWrite`.
- `ReadOnly : string[]`
  <br>Everything that only reads — the useful default for an agent you do not fully trust.
- `RedisValue : string`
  <br>One Redis key, in the shape its type has.
- `RunQuery : string`
  <br>One reading statement, masked and capped.
- `SchemaOnly : string[]`
  <br>Enough to find one's way around a schema, without reading a single row.
- `ServerActivity : string`
  <br>What the server is running, and who waits on whom.

### `WebDataStudioOpsExtensions`

`static class`

The operational side of the studio: what it watches, and who it tells. The studio already runs the analysis behind its health report — these calls arrange for somebody to hear about it.

### `WebDataStudioProviderExtensions`

`static class`

The hosted model providers, one call each. All of them are `WithAssistant` with the right URL and a sensible default model — the point is that nobody has to look the URL up to get started.

**Felder**

- `ClaudeEndpoint : string`
  <br>Anthropic's OpenAI-compatible endpoint.
- `DeepSeekEndpoint : string`
  <br>DeepSeek.
- `GeminiEndpoint : string`
  <br>Google's OpenAI-compatible Gemini endpoint.
- `GroqEndpoint : string`
  <br>Groq.
- `MistralEndpoint : string`
  <br>Mistral.
- `OpenAiEndpoint : string`
  <br>OpenAI.
- `OpenRouterEndpoint : string`
  <br>OpenRouter, which fronts many models behind one key.

### `WebDataStudioResource`

`class`

WebDataStudio — a browser-based database studio — running as a container resource. Exposes an HTTP endpoint for the studio, and carries the connections that were attached to it so several databases can share one instance.

**Konstruktoren**

- `WebDataStudioResource(string name)`
  <br>WebDataStudio — a browser-based database studio — running as a container resource. Exposes an HTTP endpoint for the studio, and carries the connections that were attached to it so several databases can share one instance.

**Eigenschaften**

- `Accounts : IReadOnlyList<StudioAccount> { get; }`
  <br>The accounts configured with `WithLogin` and `WithUser`, in the order they were added. Passwords are not here: they go to the container and nowhere else.
- `ArchivePath : string { get; }`
  <br>Where kept results are written, from `WithArchives`. Null means the default beside the application database.
- `AssistantModel : string { get; }`
  <br>The model the optional assistance uses, when it was configured with `WithAssistant`. Null means the studio has no assistance at all: no button, no calls.
- `ConnectionNames : IReadOnlyList<string> { get; }`
  <br>Names of the connections attached to this studio, in the order they were added. These are the labels the studio shows in its explorer, and the suffixes of its `WDS_CONN_*` variables.
- `MaskedColumns : IReadOnlyCollection<string> { get; }`
  <br>Columns masked on top of the studio's own word list, from `WithMaskedColumns`.
- `McpAllowsWrite : bool { get; }`
  <br>Whether the MCP endpoint may change data, through a preview and its hash.
- `McpHasKey : bool { get; }`
  <br>Whether the MCP endpoint was given a key. A studio with accounts refuses to serve MCP without one, so this is what the app host warns about.
- `McpPath : string { get; }`
  <br>Path the studio serves MCP on, when `WithMcpEndpoint` was called. Null means the studio is not an MCP server.
- `McpTools : IReadOnlyCollection<string> { get; }`
  <br>The tools the MCP endpoint is narrowed to, from `WithMcpTools`. Empty means all.
- `SavedQueriesPath : string { get; }`
  <br>Folder of `.sql` files imported as saved queries, from `WithSavedQueriesFromDirectory`.
- `Schedule : IReadOnlyList<ScheduledStudioQuery> { get; }`
  <br>The scheduled queries, from `WithScheduledQueries`.
- `SchemaSnapshotPath : string { get; }`
  <br>Where schema snapshots are written, from `WithSchemaSnapshots`. Null means none are.
- `SeedScriptPath : string { get; }`
  <br>Seed script or folder, from `WithSeedScript`.
- `SharingEnabled : bool { get; }`
  <br>Whether results can be shared as links, from `WithSharedResults`.
- `SharingIsPublic : bool { get; }`
  <br>Whether such a link opens without signing in.
- `TelemetryServiceName : string { get; }`
  <br>The name the studio reports as in traces and metrics, from `WithOpenTelemetry`. Null when it reports nothing.
- `Title : string { get; }`
  <br>The name the studio shows in its header and browser tab. Defaults to the resource name, so three studios in one stack are told apart at a glance; `WithTitle` overrides it and `WithTitle(null)` leaves the studio unnamed.
- `UnmaskedColumns : IReadOnlyCollection<string> { get; }`
  <br>Columns the studio leaves alone, from `WithUnmaskedColumns`.
- `Username : string { get; }`
  <br>Login name of the first account, when one was configured. Null means anonymous access.

**Felder**

- `DefaultImage : string`
  <br>The published WebDataStudio image.
- `DefaultResourceName : string`
  <br>Resource name used when nothing else is asked for, and the key for sharing one studio.
- `DefaultTag : string`
  <br>Default image tag.
- `DefaultTargetPort : int`
  <br>Port the studio listens on inside the container.
- `HttpEndpointName : string`
  <br>Name of the HTTP endpoint serving the studio.

### `WebDataStudioSafetyExtensions`

`static class`

The studio masks columns whose names say they hold a secret — `password`, `api_key`, `iban` and the like — before the values leave the server. These calls correct that guess for a schema the word list reads wrong, from the place the rest of the configuration lives.

### `WebDataStudioScheduleExtensions`

`static class`

Scheduled queries, written as a file the studio reads. The file is generated into the app host's output and mounted read-only, so the schedule lives in your app host next to everything else rather than in a volume somebody has to remember.

### `WebDataStudioStorageExtensions`

`static class`

Object storage as a connection: a bucket, a container or a folder, browsable in the studio's tree and queryable through DuckDB — a Parquet file in a bucket is a table that happens to live somewhere else.

## Projects

### `Nextended_Aspire_Hosting_WebDataStudio`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-webdatastudio)
