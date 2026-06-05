using Nextended.Aspire.Hosting.N8n.Builders;

// Test/demo AppHost for the Nextended.Aspire.Hosting.N8n integration.
// Run with `dotnet run` (Docker required) or deploy with `azd up`.
var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment for `azd up` deployment (only materialized in publish mode).
if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");

var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    .WithBasicAuth("admin", "n8n-dev-password")
    .WithQueueMode(workers: 1);

builder.Build().Run();
