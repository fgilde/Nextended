using Nextended.Aspire.Hosting.N8n.Builders;

// Test/demo AppHost for the Nextended.Aspire.Hosting.N8n integration.
// Run with `dotnet run` (Docker required) or deploy with `azd up`.
var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment for `azd up` deployment (only materialized in publish mode).
if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");

// Runs with zero configuration; everything below is optional and overridable.
// Secrets can be passed as Aspire parameters (user secrets locally, Key Vault on deploy), e.g.:
//   var redisPassword  = builder.AddParameter("n8n-redis-password",  secret: true);
//   var encryptionKey  = builder.AddParameter("n8n-encryption-key", secret: true);
//   ... .WithEncryptionKey(encryptionKey).WithQueueMode(workers: 1, redisPassword: redisPassword);
var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    .WithQueueMode(workers: 1)
    // Seed n8n with example workflows on startup (local development).
    .WithWorkflowsFromDirectory(Path.Combine(builder.AppHostDirectory, "workflows"));

builder.Build().Run();
