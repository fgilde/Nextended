// A stack whose databases are filled from databases that already exist.
//
// Nothing in this file describes a table. Both connections that come up in the studio arrive by
// clone: one out of a server this stack does not model, one out of another resource in it. That is
// the whole point of Nextended.Aspire.Hosting.DbTools, and this app host is the shortest honest
// demonstration of it.
//
//   dotnet run
//
// The first start pulls two images and copies both databases, so give it a minute. Every clone
// appears in the dashboard as a resource of its own — its log is the dump and restore output.

using Nextended.Aspire.Hosting.DbTools;
using Nextended.Aspire.Hosting.WebDataStudio;

var builder = DistributedApplication.CreateBuilder(args);
var password = builder.AddParameter("demo-password", "change-me-please", secret: true);


builder.AddContainer("northwind-legacy", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", password)
    .WithEnvironment("POSTGRES_DB", "northwind")
    .WithBindMount("northwind", "/docker-entrypoint-initdb.d")
    .WithEndpoint(targetPort: 5432, name: "pg");


var externalNorthwind = builder.AddConnectionString("northwind-source",
    ReferenceExpression.Create(
        $"Host=northwind-legacy;Port=5432;Username=postgres;Password={password};Database=northwind"));


var postgres = builder.AddPostgres("pg", password: password)
    .WithDataVolume("dbtools-demo-pg");

var northwind = postgres.AddDatabase("northwind")
    .WithCloneFrom(externalNorthwind)
    .WithWebDataStudio();

// --- 2. from another resource in this stack ------------------------------------------------------

var mysql = builder.AddMySql("mysql", password: password)
    .WithInitFiles("mysql-init")
    .WithDataVolume("dbtools-demo-mysql");

var legacy = mysql.AddDatabase("legacy");

var parts = mysql.AddDatabase("parts")
    .WithCloneFrom(legacy)
    .WithWebDataStudio();





builder.Build().Run();
