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

// One password for everything in the demo. A parameter rather than a literal, because that is where
// a credential belongs even in a sample.
var password = builder.AddParameter("demo-password", "change-me-please", secret: true);

// --- 1. from a server this stack does not model --------------------------------------------------

// The stand-in for that server: a plain container rather than a database resource, because the
// server that lives somewhere else is not one either. Northwind goes into it on its first start, by
// the image itself.
builder.AddContainer("northwind-legacy", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", password)
    .WithEnvironment("POSTGRES_DB", "northwind")
    .WithBindMount("northwind", "/docker-entrypoint-initdb.d")
    .WithEndpoint(targetPort: 5432, name: "pg");

// And all this stack has of it: a connection string. It is not known until the stack runs — the
// password is a parameter and the host is only a container name by then — so it travels to the clone
// container whole and is taken apart there.
var externalNorthwind = builder.AddConnectionString("northwind-source",
    ReferenceExpression.Create(
        $"Host=northwind-legacy;Port=5432;Username=postgres;Password={password};Database=northwind"));

// A data volume so a second start of this stack finds the copy where it left it — which is how
// the "only when empty" default becomes visible: the clone then says so and copies nothing.
var postgres = builder.AddPostgres("pg", password: password)
    .WithDataVolume("dbtools-demo-pg");

// The copy this stack owns: eight tables, their keys, twelve indexes and a view, none of them
// written down here. pg_dump and psql run in a container of their own, and only into a database that
// has nothing in it yet.
var northwind = postgres.AddDatabase("northwind")
    .WithCloneFrom(externalNorthwind);

// --- 2. from another resource in this stack ------------------------------------------------------

var mysql = builder.AddMySql("mysql", password: password)
    // Suppliers, parts, a view and a function — the database the copy below is made of. Loaded
    // by the image on the first start, so it survives in the volume with everything else.
    .WithInitFiles("mysql-init")
    .WithDataVolume("dbtools-demo-mysql");

var legacy = mysql.AddDatabase("legacy");

// The typed overload: source and target are both MySQL resources, so two engines cannot be mixed up
// by accident, and the clone waits for the source as well — nothing can be dumped out of a server
// that has not started.
var parts = mysql.AddDatabase("parts")
    .WithCloneFrom(legacy);

// --- and something to look at ---------------------------------------------------------------------

// Not part of the package, only the fastest way to see that the rows are really there: one studio
// with both copies in it. Nextended.Aspire.Hosting.WebDataStudio.
northwind.WithWebDataStudio();
parts.WithWebDataStudio();

builder.Build().Run();
