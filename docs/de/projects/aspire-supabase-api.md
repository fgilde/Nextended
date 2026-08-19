---
title: Nextended.Aspire.Hosting.Supabase — API-Referenz
---

# Nextended.Aspire.Hosting.Supabase — API-Referenz

🇬🇧 [This page in English](/projects/aspire-supabase-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.Supabase`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-supabase)

## Nextended.Aspire.Hosting.Observability

### `ObservabilityStack`

`static class`

Supabase-aware entry point for the observability stack. The stack itself (Grafana, Prometheus, Loki, Promtail, cAdvisor, Tempo, OTel-Collector, postgres_exporter) lives in the `Nextended.Aspire.Hosting.Grafana` package — see `ObservabilityStackExtensions` for the generic options-based overload and `GrafanaBuilderExtensions` for the fluent piecemeal API.

## Nextended.Aspire.Hosting.Supabase.Builders

### `AuthBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Auth (GoTrue).

### `DatabaseBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Database (PostgreSQL).

### `EdgeRuntimeBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Edge Runtime.

### `EnvironmentReferenceExtensions`

`static class`

_Keine Beschreibung._

### `KongBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Kong API Gateway.

### `MetaBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Postgres-Meta service.

### `MinioOnNfsStorageExtensions`

`static class`

Durable object storage for supabase-storage on Azure Container Apps, via a bundled MinIO (S3) server backed by the persistent Azure Files NFS share. supabase-storage's FILE backend cannot run durably on ACA: the only persistent volume ACA can mount is Azure Files, and SMB rejects the backend's open flags (EINVAL) while NFS 4.1 has no extended attributes (xattr -&gt; ENOTSUP), which the FILE backend requires for object metadata. So instead we run MinIO on the NFS share (MinIO keeps its metadata in its own xl.meta files — no xattr) and switch supabase-storage to the S3 backend pointing at it. This wires (publish only): 1. a MinIO container, mounting the NFS env-storage named `nfsEnvStorageName` (created by PersistentNfsStorageExtensions.AddSupabaseNfsStorage — pass the SAME name) at /data, internal-only S3 ingress on :9000, pinned to a single writer, 2. a one-shot mc init container that waits for MinIO and creates the bucket (idempotent), 3. `StorageS3Backend` pointing the storage container at MinIO. MinIO/mc are public images, so ACA pulls them directly (no local build/push, unaffected by the docker registry push path). NOTE: MinIO discourages network filesystems for large clusters, but a single-node single-drive instance on NFS is fine for this low-traffic app.

### `PersistentNfsStorageExtensions`

`static class`

Persistent file storage for the Supabase storage container on Azure Container Apps. supabase-storage uses the local FILE backend. Azure Files SMB is incompatible with it (EINVAL on the backend's open flags) and ephemeral disk loses uploads on every restart. The only Azure-native, in-region, POSIX option that works is an Azure Files **NFS** share — which is only reachable from a VNet. This wires, entirely in code (all in the ACA environment's own bicep module so the resources cross-reference directly): 1. a VNet + a subnet delegated to the ACA managed environment (+ a Microsoft.Storage service endpoint), and the env's VnetConfiguration pointing at that subnet, 2. a Premium FileStorage account with an NFS file share, locked to the subnet (NFS is unencrypted, so Azure only allows it network-isolated), 3. a managedEnvironmentStorage (NfsAzureFile) on the ACA environment, named `nfsEnvStorageName`. Consumers mount that env-storage by name — either the storage container itself (via `PersistentStorageVolumeName`; note Azure Files NFS has no xattr, which the FILE backend needs) or a MinIO fronting it (see MinioOnNfsStorageExtensions). Files survive restarts and redeploys. NOTE: VNet/subnet are built with Azure.Provisioning.Network directly rather than Aspire's experimental AddSubnet — the latter emits an invalid fully-qualified child name (BCP170).

### `RestBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase REST API (PostgREST).

### `StorageBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Storage API.

### `StudioBuilderExtensions`

`static class`

Provides extension methods for configuring the Supabase Studio Dashboard. Note: The SupabaseStackResource IS the Studio container, so these methods configure the stack directly.

### `SupabaseBuilderExtensions`

`static class`

Provides the main extension method for adding Supabase to an Aspire application.

**Eigenschaften**

- `PersistentStorageVolumeName : string { get; set; }`
  <br>Optional name of a managedEnvironmentStorage (e.g. an NFS Azure Files share) to mount at the storage container's backend path (/var/lib/storage) in publish mode, so uploaded files persist across restarts/redeploys. This is a generic Supabase-storage concern — the HOST app owns creating the actual storage resource and just sets this name here. When null/empty, publish mode falls back to ephemeral container-local storage.
- `PostgresDataVolumeName : string { get; set; }`
  <br>Optional name of a managedEnvironmentStorage (e.g. an NFS Azure Files share) to mount at the PostgreSQL data directory (/var/lib/postgresql/data) in publish mode, so the whole database survives container restarts/redeploys. Without it the ACA database is ephemeral (a restart wipes ALL data). This is a generic Supabase concern — the HOST app owns creating the actual storage resource and just sets this name here. When null/empty, publish mode stays ephemeral. NOTE: PostgreSQL requires POSIX semantics (fsync/locking), so the backing share must be Azure Files *NFS* (Premium), never SMB; the DB container is pinned to a single replica.
- `StorageS3Backend : SupabaseStorageS3Options { get; set; }`
  <br>Optional S3-compatible backend for the storage container (publish mode). When set, the storage container runs with STORAGE_BACKEND=s3 against this endpoint instead of the local FILE backend. This is a generic Supabase-storage concern: supabase-storage's FILE backend needs a local POSIX disk WITH extended attributes, which Azure Files can't provide (SMB rejects its open flags; NFS 4.1 has no xattr) — so a durable container-apps deploy needs an S3-compatible store. The HOST app owns the S3 server (e.g. a bundled MinIO), its bucket and credentials, and just points this at it. When null, the FILE backend is used (see `PersistentStorageVolumeName` / local bind mount).

### `SupabaseReferenceExtensions`

`static class`

Extension methods for referencing Supabase from client projects.

### `SupabaseStackExtensions`

`static class`

Provides extension methods for the SupabaseStackResource.

### `SupabaseStorageS3Options`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `SupabaseStorageS3Options()`

**Eigenschaften**

- `AccessKey : string { get; set; }`
- `Bucket : string { get; set; }`
- `ForcePathStyle : bool { get; set; }`
- `Region : string { get; set; }`
- `SecretKey : string { get; set; }`

## Nextended.Aspire.Hosting.Supabase.Client

### `SupabaseClientExtensions`

`static class`

Extension methods for adding Supabase client to service projects.

### `SupabaseClientSettings`

`class`

Settings for configuring a Supabase client connection.

**Konstruktoren**

- `SupabaseClientSettings()`

**Eigenschaften**

- `AutoConnectRealtime : bool { get; set; }`
  <br>Whether to automatically connect to Realtime on initialization. Default: true
- `AutoRefreshToken : bool { get; set; }`
  <br>Whether to automatically refresh the token. Default: true
- `Headers : Dictionary<string, string> { get; set; }`
  <br>Custom headers to include in requests.
- `Key : string { get; set; }`
  <br>The Supabase API key (anon/public key for client-side, service role key for server-side).
- `PersistSession : bool { get; set; }`
  <br>Whether to persist the session. Default: true
- `Url : string { get; set; }`
  <br>The Supabase URL (e.g., https://xxx.supabase.co or http://localhost:8000 for local).

## Nextended.Aspire.Hosting.Supabase.Config

### `ISupabaseFullSyncInfo`

`interface`

_Keine Beschreibung._

**Eigenschaften**

- `DbPassword : string { get; }`
- `ManagementToken : string { get; }`

### `ISupabaseReferenceInfo`

`interface`

_Keine Beschreibung._

**Methoden**

- `GetApiUrl() : string`

**Eigenschaften**

- `ProjectRefId : string { get; }`
- `ServiceKey : string { get; }`

## Nextended.Aspire.Hosting.Supabase.Resources

### `RegisteredUser`

`class`

Represents a registered development user.

**Konstruktoren**

- `RegisteredUser(string Email, string Password, string DisplayName)`
  <br>Represents a registered development user.

**Eigenschaften**

- `DisplayName : string { get; set; }`
- `Email : string { get; set; }`
- `Password : string { get; set; }`

### `SupabaseAuthResource`

`class`

Represents a Supabase GoTrue authentication container resource.

**Konstruktoren**

- `SupabaseAuthResource(string name)`
  <br>Creates a new instance of the SupabaseAuthResource.

**Eigenschaften**

- `AnonymousUsersEnabled : bool { get; }`
  <br>Gets or sets whether anonymous users are enabled.
- `AutoConfirm : bool { get; }`
  <br>Gets or sets whether email auto-confirmation is enabled.
- `DisableSignup : bool { get; }`
  <br>Gets or sets whether signup is disabled.
- `JwtExpiration : int { get; }`
  <br>Gets or sets the JWT expiration time in seconds.
- `SiteUrl : string { get; }`
  <br>Gets or sets the site URL for authentication redirects.

### `SupabaseDatabaseResource`

`class`

Represents a Supabase PostgreSQL database container resource.

**Konstruktoren**

- `SupabaseDatabaseResource(string name)`
  <br>Creates a new instance of the SupabaseDatabaseResource.

**Eigenschaften**

- `ExternalPort : int { get; }`
  <br>Gets or sets the external port for PostgreSQL connections.
- `Password : string { get; }`
  <br>Gets or sets the database password.

### `SupabaseEdgeRuntimeResource`

`class`

Represents a Supabase Edge Runtime container resource for Edge Functions.

**Konstruktoren**

- `SupabaseEdgeRuntimeResource(string name)`
  <br>Creates a new instance of the SupabaseEdgeRuntimeResource.

**Eigenschaften**

- `FunctionNames : List<string> { get; }`
  <br>Gets the list of function names available in this runtime.
- `FunctionsPath : string { get; }`
  <br>Gets or sets the path to the edge functions directory.
- `Port : int { get; }`
  <br>Gets or sets the internal port for the edge runtime.

### `SupabaseKongResource`

`class`

Represents a Supabase Kong API Gateway container resource.

**Konstruktoren**

- `SupabaseKongResource(string name)`
  <br>Creates a new instance of the SupabaseKongResource.

**Eigenschaften**

- `ExternalPort : int { get; }`
  <br>Gets or sets the external port for the API gateway.
- `Plugins : string[] { get; }`
  <br>Gets or sets the Kong plugins to enable.

### `SupabaseMetaResource`

`class`

Represents a Supabase Postgres-Meta container resource.

**Konstruktoren**

- `SupabaseMetaResource(string name)`
  <br>Creates a new instance of the SupabaseMetaResource.

**Eigenschaften**

- `Port : int { get; }`
  <br>Gets or sets the internal port for the meta service.

### `SupabaseRealtimeResource`

`class`

Represents a Supabase Realtime container resource.

**Konstruktoren**

- `SupabaseRealtimeResource(string name)`
  <br>Creates a new instance of the SupabaseRealtimeResource.

**Eigenschaften**

- `Port : int { get; }`
  <br>Gets or sets the port the Realtime service listens on.

### `SupabaseRestResource`

`class`

Represents a Supabase PostgREST container resource.

**Konstruktoren**

- `SupabaseRestResource(string name)`
  <br>Creates a new instance of the SupabaseRestResource.

**Eigenschaften**

- `AnonRole : string { get; }`
  <br>Gets or sets the anonymous role name.
- `Schemas : string[] { get; }`
  <br>Gets or sets the database schemas to expose.

### `SupabaseStackResource`

`class`

Represents a complete Supabase stack resource containing all sub-services. This resource IS the Studio Dashboard container and serves as the visual parent for all other Supabase containers in the Aspire dashboard.

**Konstruktoren**

- `SupabaseStackResource(string name)`
  <br>Creates a new instance of the SupabaseStackResource.

**Methoden**

- `GetApiUrl() : string`
- `GetPostgresConnectionString() : string`
- `GetStudioUrl() : string`

**Eigenschaften**

- `AnonKey : string { get; }`
  <br>Gets the Anon Key for client-side authentication.
- `JwtSecret : string { get; }`
  <br>Gets or sets the JWT secret used for token signing.
- `ProjectRefId : string { get; }`
- `ServiceKey : string { get; }`
- `ServiceRoleKey : string { get; }`
  <br>Gets the Service Role Key for server-side authentication.
- `UsesExternalDatabase : bool { get; }`
  <br>True when the caller passed their own Postgres resource to AddSupabase (external mode); the stack then owns no database container and `Database` is null.

### `SupabaseStorageResource`

`class`

Represents a Supabase Storage API container resource.

**Konstruktoren**

- `SupabaseStorageResource(string name)`
  <br>Creates a new instance of the SupabaseStorageResource.

**Eigenschaften**

- `Backend : string { get; }`
  <br>Gets or sets the storage backend type.
- `EnableImageTransformation : bool { get; }`
  <br>Gets or sets whether image transformation is enabled.
- `FileSizeLimit : long { get; }`
  <br>Gets or sets the maximum file size limit in bytes.

## Nextended.Aspire.Hosting.Supabase.Sync

### `ProjectSyncExtensions`

`static class`

Provides extension methods for project synchronization.

### `SyncOptions`

`enum`

Specifies what to synchronize from an online Supabase project.

**Werte**

- `All`
  <br>Everything - complete sync of all database objects, data, storage, and Edge Functions. Requires database password and Management API token for Edge Functions.
- `AllSchema`
  <br>All schema-related options (Schema, Policies, Functions, Triggers, Types, Views, Indexes). Requires database password.
- `AllStorage`
  <br>All storage-related options (StorageBuckets, StorageFiles).
- `Data`
  <br>Sync table data.
- `EdgeFunctions`
  <br>Sync Edge Functions from the remote project. Requires Supabase Management API token (personal access token from Dashboard → Account → Access Tokens).
- `Functions`
  <br>Sync stored procedures and functions.
- `Indexes`
  <br>Sync indexes.
- `None`
  <br>No synchronization.
- `Policies`
  <br>Sync Row Level Security policies.
- `Schema`
  <br>Sync table structures (columns, types, constraints).
- `StorageBuckets`
  <br>Sync storage buckets.
- `StorageFiles`
  <br>Sync storage files (downloads files from remote storage).
- `Triggers`
  <br>Sync database triggers.
- `Types`
  <br>Sync custom types and enums.
- `Views`
  <br>Sync views.
- `value__`

↩ [Zurück zur Paketseite](/de/projects/aspire-supabase)
