---
title: Nextended.Aspire.Hosting.DbTools — API-Referenz
---

# Nextended.Aspire.Hosting.DbTools — API-Referenz

🇬🇧 [This page in English](/projects/aspire-dbtools-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.DbTools`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-dbtools)

## Nextended.Aspire.Hosting.DbTools

### `DbCloneOptions`

`class`

What a clone copies, and what it is allowed to overwrite.

**Konstruktoren**

- `DbCloneOptions()`

**Eigenschaften**

- `DataOnly : bool { get; set; }`
  <br>The rows into a schema that is already there.
- `Image : string { get; set; }`
  <br>The image the dump and restore run in.
- `Name : string { get; set; }`
  <br>The name of the resource the clone appears as, so the dashboard says which one it is when a stack has several.
- `OnlyWhenEmpty : bool { get; set; }`
  <br>Leave a target that already has something in it alone.
- `Overwrite : bool { get; set; }`
  <br>Replace whatever is in the target.
- `SchemaOnly : bool { get; set; }`
  <br>The shape without the rows.
- `TimeoutSeconds : int { get; set; }`
  <br>How long the clone may take before it is a failure. A first clone of a large database is minutes, not seconds.

### `DbEndpoint`

`class`

Where a database is and how to get in.

**Methoden**

- `Of(string host, string port, string user, string password, string database) : DbEndpoint`
  <br>Everything as literal text — what a test reads back, and what a typed source with no password looks like.
- `Parse(string connectionString, int defaultPort) : DbEndpoint`
  <br>A connection string as the parts its engine's command line wants.

**Eigenschaften**

- `IsWhole : bool { get; }`
  <br>True when this is one whole connection string for the script to take apart, false when the parts are already known here.

### `MongoDBCloneExtensions`

`static class`

Fills a MongoDB database with the contents of another one.

### `MySqlCloneExtensions`

`static class`

Fills a MySQL or MariaDB database with the contents of another one.

### `PostgresCloneExtensions`

`static class`

Fills a PostgreSQL database with the contents of another one.

### `RedisCloneExtensions`

`static class`

Fills a Redis server with the contents of another one.

### `SqlServerCloneExtensions`

`static class`

Fills a SQL Server database with the contents of another one.

## Projects

### `Nextended_Aspire_Hosting_DbTools`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-dbtools)
