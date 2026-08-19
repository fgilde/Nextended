<p align="center">
  <img src="https://raw.githubusercontent.com/fgilde/WebDataStudio/master/docs/assets/logo.png" alt="WebDataStudio" height="80">
</p>

# Nextended.Aspire.Hosting.WebDataStudio

**[▶ See demo app](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)** — runnable sample AppHost for this integration.

Run [WebDataStudio](https://github.com/fgilde/WebDataStudio) — a browser-based database studio for
PostgreSQL, MySQL, SQL Server, SQLite, Oracle, DuckDB, ClickHouse, MongoDB and Redis — inside your
Aspire stack, with the databases of that stack already wired up.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var shop = builder.AddPostgres("pg").AddDatabase("shop").WithWebDataStudio();
var orders = builder.AddSqlServer("sql").AddDatabase("orders").WithWebDataStudio();
var cache = builder.AddRedis("cache").WithWebDataStudio();

builder.Build().Run();
```

One studio, three connections, no connection string typed anywhere. Open it from the Aspire
dashboard and the explorer already lists `SHOP`, `ORDERS` and `CACHE`.

## Sharing one studio, or running several

`WithWebDataStudio()` creates the studio on the first call and attaches to it on every one after —
sharing is keyed on the studio's resource name.

```csharp
// One shared studio (default name "webdatastudio")
shop.WithWebDataStudio();
orders.WithWebDataStudio();

// A second studio, for the databases that belong together
analytics.WithWebDataStudio(studioName: "analytics-studio");
warehouse.WithWebDataStudio(studioName: "analytics-studio");

// A studio you built yourself, with your own options
var admin = builder.AddWebDataStudio("admin-studio")
    .WithLogin("admin", builder.AddParameter("studio-password", secret: true))
    .WithReadOnly();

production.WithWebDataStudio(admin, color: "#e03131", group: "Production");
```

The same works from the studio's side, which reads better when one studio owns many databases:

```csharp
builder.AddWebDataStudio("studio")
    .WithReference(shop)
    .WithReference(orders, connectionName: "ORDERS_PROD", readOnly: true, color: "#e03131")
    .WithReference(cache)
    .WithConnection("LEGACY", "Host=old-box;Database=legacy;Username=ro;Password=pw",
        WebDataStudioEngine.PostgreSql, readOnly: true, group: "Legacy");
```

> `WithReference` on a studio is this package's own overload. Aspire's built-in one would write a
> `ConnectionStrings__*` variable, which the studio does not read; this one writes the
> `WDS_CONN_*` variables it does.

## API

| Call | Effect |
|------|--------|
| `AddWebDataStudio(name = "webdatastudio", port?, image?, tag?)` | Add the studio container: HTTP endpoint, health check, per-instance data volume. |
| `.WithReference(resource, connectionName?, engine?, readOnly?, group?, color?)` | Attach any resource that has a connection string. |
| `.WithConnection(name, connectionString, engine, …)` | Attach a database that is not part of the stack. Also takes a `ReferenceExpression`. |
| `.WithLogin(user, password)` | Guard the studio with a login. Both halves also accept an Aspire `ParameterResource`. |
| `.WithReadOnly(readOnly = true)` | Make every connection read-only, enforced in the driver. |
| `.WithQueryTimeout(TimeSpan)` | Default statement timeout. |
| `.WithMaxRows(int)` | Default row cap per result. |
| `.WithSessionLimits(maxSessions?, idleTimeout?)` | Cap open sessions per connection and how long an idle one lives. |
| `.WithSecretKey(base64)` | Key for the secrets the studio stores; also takes a `ParameterResource`. |
| `.WithDataVolume(name?)` / `.WithDataBindMount(path)` | Put the studio's own data somewhere else. |
| `resource.WithWebDataStudio(configure?, studioName?, connectionName?, engine?)` | Attach from the database's side, creating or reusing the studio. |
| `resource.WithWebDataStudio(studio, …)` | Attach to a studio you built yourself. |

## Engines

The engine is read from the resource type, so `AddPostgres`, `AddSqlServer`, `AddMySql`,
`AddOracle`, `AddMongoDB`, `AddRedis`, `AddValkey` and `AddGarnet` need no help. Anything else —
a container you wired up yourself, a connection string from configuration — takes an explicit
`engine:` argument, or the studio guesses from the connection string and skips the connection if
it cannot tell.

```csharp
studio.WithReference(clickhouse, engine: WebDataStudioEngine.ClickHouse);
```

## Notes

- Connection names become environment variables, so `shop-db` shows up as `SHOP_DB`. Pass
  `connectionName` for something nicer. Names ending in `_ENGINE`, `_READONLY`, `_GROUP` or
  `_COLOR` are rejected: the studio reads those as settings for another connection.
- Without `WithLogin` there is no login screen. That is the right default while the studio only
  listens on your machine — put a login on it before you expose the endpoint.
- Each studio gets its own named volume, so two studios in one stack never share saved connections.
- The studio image is `ghcr.io/fgilde/webdatastudio` and is always re-pulled, because the default
  tag is a rolling `latest`.

## Links

- [WebDataStudio](https://fgilde.github.io/WebDataStudio/) — the studio itself, and its documentation
- [Environment variables](https://fgilde.github.io/WebDataStudio/guide/#/environment) — everything this package writes
- [Sample AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/WebDataStudio.AppHost)
