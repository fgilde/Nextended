using Nextended.Aspire.Hosting.AspireUI;

// Test/demo AppHost for the Nextended.Aspire.Hosting.AspireUI integration.
// Run with `dotnet run` (Docker required). AspireUI comes up with a seeded admin and a starter
// stack pointing at this AppHost's own directory.
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithSeedStack("Demo", builder.AppHostDirectory);

builder.Build().Run();
