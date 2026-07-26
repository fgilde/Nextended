using Nextended.Aspire.Hosting.AspireUI;

// Test/demo AppHost for the Nextended.Aspire.Hosting.AspireUI 
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithSeedStack("Demo", builder.AppHostDirectory);

builder.Build().Run();
