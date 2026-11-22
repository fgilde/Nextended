---
title: Nextended.Aspire.Hosting.SigNoz
description: SigNoz — traces, metrics, logs, dashboards and alerts on ClickHouse — as Aspire resources, with a provisioned admin, dashboards and alert rules for .NET services and OpenID Connect sign-in in front.
---
# Nextended.Aspire.Hosting.SigNoz

📚 **[Full API reference](/projects/aspire-signoz-api)** — every public type and member, generated from the compiled assembly.

SigNoz — traces, metrics, logs, dashboards and alerts in one UI on ClickHouse — with a provisioned admin, ready-made dashboards and alert rules for .NET services, and OpenID Connect sign-in in front.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.SigNoz.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.SigNoz/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-signoz)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.SigNoz
```

Into the **AppHost** project.

## Runnable sample

**[SigNoz.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/SigNoz.AppHost)** starts SigNoz and a sample service (HTTP endpoints, Postgres, logs, runtime metrics) that sends its telemetry there, so every dashboard has data within a minute.

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/SigNoz.AppHost
dotnet run                               # SigNoz's login: admin@example.com / Sample-Passw0rd!
dotnet run --launch-profile anonymous    # no sign-in
dotnet run --launch-profile keycloak     # sign-in at Keycloak through the proxy: ada/ada gets in, nora/nora is turned away
```

[SigNoz](https://signoz.io/) for .NET Aspire: traces, metrics, logs, dashboards and alerts in one UI, stored in ClickHouse. One call adds the whole stack, provisions its admin, and creates dashboards and alert rules for .NET services on every start.

## Quick start

```csharp
using Nextended.Aspire.Hosting.SigNoz;

var builder = DistributedApplication.CreateBuilder(args);

var signoz = builder.AddSigNoz("signoz")
    .WithAdminUser("admin@example.com", builder.AddParameter("signoz-admin-password", secret: true))
    .WithClickHouse(c => c.WithDataVolume())   // keep the telemetry across restarts
    .WithDataVolume();                         // keep users, dashboards and settings

builder.AddProject<Projects.Api>("api")
    .WithSigNozTelemetry(signoz);              // OTEL_EXPORTER_OTLP_ENDPOINT -> SigNoz's collector

builder.Build().Run();
```

`AddSigNoz` adds three containers, nested under `signoz` in the Aspire dashboard:

| Resource | Image | What it does |
| --- | --- | --- |
| `signoz` | `signoz/signoz:v0.151.0` | UI and API (port 8080). Creates the admin, then the dashboards and alert rules. |
| `signoz-collector` | `signoz/signoz-otel-collector:v0.144.18` | OTLP receiver (gRPC 4317, HTTP 4318). Applies ClickHouse's schema migrations before it starts. |
| `signoz-clickhouse` | `clickhouse/clickhouse-server:25.12.5` | The telemetry store. ClickHouse Keeper runs inside it, so there is no ZooKeeper container. The `histogramQuantile` function SigNoz uses for percentiles is downloaded on its first start. |

The images and versions are the ones SigNoz's own deployment tool ([Foundry](https://github.com/SigNoz/foundry)) uses. `WithImageTag` on a resource changes one of them.

## Sending telemetry

`WithSigNozTelemetry(signoz)` sets `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL` on a project or container. While running, it uses gRPC. In a deployment it uses `http/protobuf`, because the collector receives OTLP over HTTP only there.

The defaults replace the Aspire dashboard as the target. To keep both, pass names of your own and add a second exporter for them:

```csharp
api.WithSigNozTelemetry(signoz, "SIGNOZ_OTLP_ENDPOINT", "SIGNOZ_OTLP_PROTOCOL");
```

```csharp
// in the service (ServiceDefaults)
if (builder.Configuration["SIGNOZ_OTLP_ENDPOINT"] is { Length: > 0 } signoz)
{
    var http = builder.Configuration["SIGNOZ_OTLP_PROTOCOL"] == "http/protobuf";
    Action<OtlpExporterOptions> To(string signal) => o => (o.Endpoint, o.Protocol) = http
        ? (new Uri($"{signoz.TrimEnd('/')}/v1/{signal}"), OtlpExportProtocol.HttpProtobuf)
        : (new Uri(signoz), OtlpExportProtocol.Grpc);
    telemetry.WithLogging(l => l.AddOtlpExporter("signoz-logs", To("logs")))
        .WithMetrics(m => m.AddOtlpExporter("signoz-metrics", To("metrics")))
        .WithTracing(t => t.AddOtlpExporter("signoz-traces", To("traces")));
}
```

## Dashboards and alert rules

On every start, SigNoz gets these dashboards (tag `stack:aspire`, each with a `service` variable):

| Dashboard | Panels |
| --- | --- |
| **HTTP services** | Requests per second, error rate, p95 latency, errors; rate, errors and p95 per service; p50/p99; responses by status code; p95 by route (histogram); slowest endpoints (p95, calls, errors); outgoing HTTP calls; active requests |
| **.NET runtime** | CPU, working set; GC collections per generation, GC pause time, heap size per generation, allocation rate; thread pool threads and queue; exceptions by type; lock contentions; Kestrel connections; JIT |
| **Database calls** | Calls per second, p95 and failed calls (overall and per service), p95 from `db.client.operation.duration`, slowest statements, connection pool |
| **Logs and traces** | Logs by level, errors and warnings per service, the latest error logs, the latest failed spans |

It also gets these alert rules:

| Rule | Fires when |
| --- | --- |
| HTTP error rate above 5% | more than 5% of a service's incoming requests failed over 5 minutes |
| HTTP p95 latency above 1 s | a service's p95 request duration is above 1 s over 5 minutes |
| Database p95 latency above 500 ms | a service's p95 database call is above 500 ms |
| Error logs | a service logs more than 10 records at error level and above in 5 minutes |
| Thread pool queue growing | more than 50 work items wait for a thread |

The rules are routed by SigNoz's notification policies instead of named channels. They fire in the UI right away. To get notified, add a channel and a policy under *Alerts → Configuration*.

The panels read what the OpenTelemetry .NET SDK and its ASP.NET Core, HttpClient and runtime instrumentation send: server and client spans, `http.server.request.duration`, the `System.Runtime` metrics (`dotnet.*`), and Npgsql's `db.client.*` metrics. Database panels select spans with `db.system.name`, the current semantic conventions. Libraries that still send the older `db.system` attribute need the filter changed.

Dashboards and rules are created when missing and otherwise left alone, so changes made in the UI survive restarts. To get a shipped version back, delete it in the UI. The next start then creates it again. You can also add your own:

```csharp
signoz.WithDashboards("./signoz/dashboards")   // SigNoz dashboard JSON, schema v6 (the UI's "JSON" button)
      .WithAlertRules("./signoz/alerts")       // bodies of POST /api/v2/rules
      .WithoutBuiltInContent();                // only your own
```

A file with the same name as a built-in one replaces it.

Provisioning is a shell script inside the SigNoz container (`/etc/signoz-provisioning/provision.sh`). It works the same in a deployment, where no app host runs.

## Sign-in

| Call | Who gets in |
| --- | --- |
| *(default)* | `admin@signoz.local` with a generated password (parameter `signoz-password`) |
| `WithAdminUser(email, password, organization)` | That admin. SigNoz creates it on every start and it cannot be changed in the UI. SigNoz requires a valid email address and a password of at least 12 characters with upper and lower case letters, a digit and a symbol. |
| `WithAnonymousAdmin()` | Everyone, as the admin (SigNoz's impersonation; API keys and the login form are off). For local development only. |
| `WithOidcLogin(options)` | Whoever your OpenID Connect provider lets in (see below) |

`SigNozBuilderExtensions.IsValidAdminPassword(password)` checks a password against SigNoz's rule before you hand it over.

### OpenID Connect (Keycloak, Entra ID, an app's auth server …)

**SigNoz's own single sign-on is not part of its free edition.** OIDC and SAML sign-in need an enterprise license: the community edition offers email/password and Google sign-in only. `WithOidcLogin` therefore puts an [oauth2-proxy](https://oauth2-proxy.github.io/oauth2-proxy/) (`signoz-signin`) in front of SigNoz. The proxy signs the user in at the provider, checks roles or groups, and forwards the request. SigNoz behind it acts as its admin for every request. SigNoz itself is then reachable only through the proxy: it has no external endpoint and no link in the dashboard.

This means:

- every user who gets through is SigNoz's admin, so decide in the provider who gets through;
- SigNoz does not see the individual user (its audit and "created by" show the admin);
- API keys are off. Automation reaches SigNoz on the container network instead.

```csharp
signoz.WithOidcLogin(SigNozOidcOptions.Keycloak(
    realmUrl: "https://sso.example.com/realms/company",
    clientId: "signoz",
    clientSecret: ReferenceExpression.Create($"{builder.AddParameter("signoz-client-secret", secret: true)}")));
```

Register `{proxy-url}/oauth2/callback` as the redirect URI. The Keycloak preset admits users with the client role `admin` of the client (`signoz:admin`); pass `allowedRoles` to change that. If the proxy reaches Keycloak at another address than the browser does (a Keycloak container next to it), pass `backchannelRealmUrl: "http://keycloak:8080/realms/company"`; Keycloak then needs `KC_HOSTNAME` and `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`, as in the sample.

Any other provider:

```csharp
signoz.WithOidcLogin(new SigNozOidcOptions
{
    IssuerUrl = "https://login.microsoftonline.com/<tenant>/v2.0",
    ClientId = "<client id>",
    ClientSecret = ReferenceExpression.Create($"{secret}"),
    AllowedGroups = ["<group object id>"],     // read from the groups claim
});
```

With `IssuerUrl` alone, the proxy reads the provider's discovery document. Set `LoginUrl`, `RedeemUrl` and `JwksUrl` together to skip discovery.

## Deploying (Azure Container Apps, Docker Compose)

In publish mode the stack deploys without bind mounts:

- **Configs baked into images.** The generated configs, scripts, dashboards and rules are baked into small images under `{configRoot}/.generated/signoz/<name>/publish/<resource>/`. Each is built `FROM` the component's image with one `COPY` per folder.
- **TCP ports of their own.** A TCP port is unique per Container Apps environment, and Grafana's collector may already use 4317/4318. So ClickHouse is published as `signoz-clickhouse:19000` and the collector receives OTLP over HTTP on `signoz-collector:14318`. `WithSigNozTelemetry` resolves both.
- **No OpAMP.** The collector runs with its static configuration, so SigNoz's log pipelines (pushed over OpAMP) are a local feature.

For a deployment, give ClickHouse and SigNoz volumes (`WithDataVolume`). Size ClickHouse with at least 1 CPU and 2 GiB.

## Notes

- Generated files land under `{configRoot}/.generated/signoz/<name>/`, so you can inspect what the containers loaded.
- SigNoz's usage statistics are off (`SIGNOZ_STATSREPORTER_ENABLED=false`).
- The stack costs about 1–2 GB of memory while running, mostly ClickHouse. Add it only where it is wanted.
