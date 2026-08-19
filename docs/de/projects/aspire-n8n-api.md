---
title: Nextended.Aspire.Hosting.N8n — API-Referenz
---

# Nextended.Aspire.Hosting.N8n — API-Referenz

🇬🇧 [This page in English](/projects/aspire-n8n-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.N8n`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-n8n)

## Nextended.Aspire.Hosting.N8n.Builders

### `N8nBuilderExtensions`

`static class`

Provides the main extension method for adding n8n to an Aspire application.

### `N8nConfigurationExtensions`

`static class`

Provides fluent configuration extensions for an `N8nResource`.

### `N8nDatabaseExtensions`

`static class`

Provides extension methods for configuring the n8n database backend.

### `N8nGetterExtensions`

`static class`

Provides accessor extension methods for an `N8nResource`.

### `N8nImportExtensions`

`static class`

Provides extension methods to seed workflows and credentials into n8n on startup. A one-shot init container runs the n8n CLI import commands before the main instance starts. Intended for local development and integration tests (uses bind mounts); skipped in publish mode.

### `N8nQueueExtensions`

`static class`

Provides extension methods for enabling n8n queue mode (Redis + worker containers).

### `N8nUserExtensions`

`static class`

Provides extension methods to seed the n8n owner account on startup, so a freshly started instance is immediately usable (login + REST/public API) without the interactive setup screen.

## Nextended.Aspire.Hosting.N8n.Client

### `N8nApiClient`

`class`

A lightweight, ready-to-use client for the n8n REST API. Wraps a pre-configured `HttpClient` with the base URL and API key applied.

**Konstruktoren**

- `N8nApiClient(N8nClientSettings settings)`
  <br>Creates a new `N8nApiClient` from the given settings.

**Methoden**

- `Dispose() : void`

**Eigenschaften**

- `ApiBaseUri : Uri { get; }`
  <br>Returns the URI of the n8n public API (e.g. for calling `/api/v1/workflows`).
- `Http : HttpClient { get; }`
  <br>Gets the configured `HttpClient` targeting the n8n instance.

**Felder**

- `ApiBasePath : string`
  <br>The n8n public API base path (relative to the instance base URL).

### `N8nClientExtensions`

`static class`

Extension methods for adding an n8n REST API client to service projects.

### `N8nClientSettings`

`class`

Settings for configuring an n8n REST API client connection.

**Konstruktoren**

- `N8nClientSettings()`

**Eigenschaften**

- `ApiKey : string { get; set; }`
  <br>The n8n public API key (sent as the `X-N8N-API-KEY` header). Create one in n8n under Settings → n8n API.
- `BaseUrl : string { get; set; }`
  <br>The base URL of the n8n instance (e.g. http://localhost:5678).

## Nextended.Aspire.Hosting.N8n.Resources

### `N8nResource`

`class`

Represents an n8n workflow-automation container resource. The resource exposes the n8n editor/REST endpoint as its connection string and acts as the visual parent for all related containers (database, redis, workers) in the Aspire dashboard.

**Konstruktoren**

- `N8nResource(string name)`
  <br>Creates a new instance of the `N8nResource`.

**Eigenschaften**

- `BasicAuthPassword : string { get; }`
  <br>Gets the basic-auth password.
- `BasicAuthUser : string { get; }`
  <br>Gets the basic-auth user (null = basic auth disabled).
- `EditorBaseUrl : string { get; }`
  <br>Gets the public editor base URL.
- `EncryptionKey : string { get; }`
  <br>Gets the n8n encryption key used to encrypt stored credentials. MUST stay stable across restarts, otherwise existing credentials can no longer be decrypted. Used when no `EncryptionKeyParameter` is configured.
- `HostPort : int { get; }`
  <br>Gets the host port the n8n editor is exposed on for local development.
- `Image : string { get; }`
  <br>Gets the n8n container image (without tag).
- `ImageTag : string { get; }`
  <br>Gets the n8n container image tag.
- `QueueModeEnabled : bool { get; }`
  <br>True when queue mode (Redis + workers) is enabled.
- `Timezone : string { get; }`
  <br>Gets the timezone used by n8n (e.g. "Europe/Berlin"). Default: UTC.
- `UsesSqlite : bool { get; }`
  <br>True when n8n uses the bundled SQLite backend instead of PostgreSQL.
- `WebhookUrl : string { get; }`
  <br>Gets the public webhook base URL (used to build webhook URLs behind a proxy).

**Felder**

- `HttpEndpointName : string`
  <br>The name of the primary HTTP endpoint exposed by n8n.

### `N8nWorkerResource`

`class`

Represents an n8n worker container resource used in queue mode. Workers share the n8n image, encryption key, database and Redis with the main instance, but run the `worker` command to process executions from the queue.

**Konstruktoren**

- `N8nWorkerResource(string name)`
  <br>Creates a new instance of the `N8nWorkerResource`.

↩ [Zurück zur Paketseite](/de/projects/aspire-n8n)
