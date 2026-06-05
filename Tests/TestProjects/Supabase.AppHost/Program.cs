using Nextended.Aspire.Hosting.Supabase.Builders;

// Test/demo AppHost for the Nextended.Aspire.Hosting.Supabase integration.
// Run with `dotnet run` (Docker required) or deploy with `azd up`.
var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment for `azd up` deployment (only materialized in publish mode).
if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");

var supabase = builder.AddSupabase("supabase")
    .WithRegisteredUser("dev@example.com", "dev1234", "Developer")
    .WithClearCommand();

builder.Build().Run();
