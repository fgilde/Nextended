---
title: Nextended.Aspire.Hosting.SigNoz — API-Referenz
---

# Nextended.Aspire.Hosting.SigNoz — API-Referenz

🇬🇧 [This page in English](/projects/aspire-signoz-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.SigNoz`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-signoz)

## Nextended.Aspire.Hosting.SigNoz

### `SigNozBuilderExtensions`

`static class`

SigNoz as Aspire resources: `AddSigNoz` adds ClickHouse, the SigNoz collector and SigNoz, with an admin, dashboards and alert rules for .NET services provisioned on every start. Point the services at it with `WithSigNozTelemetry``1`.

**Methoden**

- `IsValidAdminPassword(string password) : bool`
  <br>SigNoz's password rule: 12 characters, upper and lower case, a digit and a symbol of its list, nothing else.

### `SigNozClickHouseResource`

`class`

ClickHouse with SigNoz's configuration and an embedded ClickHouse Keeper (no ZooKeeper container).

**Konstruktoren**

- `SigNozClickHouseResource(string name)`
  <br>ClickHouse with SigNoz's configuration and an embedded ClickHouse Keeper (no ZooKeeper container).

**Felder**

- `HttpEndpointName : string`
- `HttpTargetPort : int`
  <br>HTTP port inside the container (health check).
- `NativeEndpointName : string`
- `NativeTargetPort : int`
  <br>Native protocol port inside the container.

### `SigNozCollectorResource`

`class`

The SigNoz OpenTelemetry collector: receives OTLP from the services and writes to ClickHouse. It applies the schema migrations before it starts.

**Konstruktoren**

- `SigNozCollectorResource(string name)`
  <br>The SigNoz OpenTelemetry collector: receives OTLP from the services and writes to ClickHouse. It applies the schema migrations before it starts.

**Felder**

- `OtlpGrpcEndpointName : string`
- `OtlpGrpcTargetPort : int`
- `OtlpHttpEndpointName : string`
- `OtlpHttpTargetPort : int`

### `SigNozOidcOptions`

`class`

The OpenID Connect provider of `WithOidcLogin`. With `IssuerUrl` alone the proxy reads the provider's discovery document; `LoginUrl`, `RedeemUrl` and `JwksUrl` together replace it — for a provider the proxy reaches at another address than the browser does. `Keycloak` sets everything up from a realm.

**Konstruktoren**

- `SigNozOidcOptions()`

**Eigenschaften**

- `AllowedEmailDomains : IReadOnlyList<string> { get; set; }`
  <br>Email domains allowed in; `*` (default) leaves the decision to the provider and the roles or groups.
- `AllowedGroups : IReadOnlyList<string> { get; set; }`
  <br>Groups one of which a user needs, read from `GroupsClaim`.
- `AllowedRoles : IReadOnlyList<string> { get; set; }`
  <br>Keycloak roles one of which a user needs (`keycloak-oidc`): realm roles by name, client roles as `client:role`.
- `AudienceClaims : IReadOnlyList<string> { get; set; }`
  <br>Claims that may carry the client id as audience; `null` keeps oauth2-proxy's `aud`. Keycloak's access tokens name the client in `azp` only.
- `ClientId : string { get; set; }`
- `GroupsClaim : string { get; set; }`
- `IssuerUrl : string { get; set; }`
  <br>The issuer as it appears in the tokens (`iss`), e.g. `https://sso.example.com/realms/company`.
- `JwksUrl : string { get; set; }`
  <br>The signing keys as the proxy reaches them.
- `LoginUrl : string { get; set; }`
  <br>The authorization endpoint as the browser reaches it.
- `Name : string { get; set; }`
  <br>The provider's name on the proxy's pages.
- `Provider : string { get; set; }`
  <br>oauth2-proxy's provider: `oidc`, or `keycloak-oidc` (needed for `AllowedRoles`), `entra-id` …
- `RedeemUrl : string { get; set; }`
  <br>The token endpoint as the proxy reaches it.
- `Scopes : string { get; set; }`

### `SigNozResource`

`class`

SigNoz: the UI and API for traces, metrics, logs, dashboards and alerts. It is the hub of the stack — ClickHouse, the collector and an optional sign-in proxy register themselves here and nest under it in the Aspire dashboard.

**Konstruktoren**

- `SigNozResource(string name)`
  <br>SigNoz: the UI and API for traces, metrics, logs, dashboards and alerts. It is the hub of the stack — ClickHouse, the collector and an optional sign-in proxy register themselves here and nest under it in the Aspire dashboard.

**Eigenschaften**

- `ClickHouse : SigNozClickHouseResource { get; }`
  <br>Where SigNoz keeps its telemetry.
- `Collector : SigNozCollectorResource { get; }`
  <br>The OTLP receiver every service sends to.

**Felder**

- `DefaultTargetPort : int`
  <br>Port of the UI and API inside the container.
- `HttpEndpointName : string`
- `OpAmpEndpointName : string`
- `OpAmpTargetPort : int`
  <br>Port the collector fetches its remote configuration from (OpAMP).

## Projects

### `Nextended_Aspire_Hosting_SigNoz`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-signoz)
