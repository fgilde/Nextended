using Nextended.Aspire;
using Nextended.Aspire.Hosting.Observability;
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

// Observability stack (Grafana/Prometheus/Loki/…) — same signature as before the
// extraction into Nextended.Aspire.Hosting.Grafana; exercises the compat surface.
var observabilityRoot = Path.Combine(builder.AppHostDirectory, "observability");
builder.AddObservabilityStack(supabase, opts =>
{
    opts.ConfigRootPath = observabilityRoot;
    opts.DashboardsPath = Path.Combine(observabilityRoot, "grafana", "dashboards");
    opts.GrafanaDashboardsFolder = "Supabase"; // sidebar grouping in Grafana UI
    opts.GrafanaAdminUser = "admin";
    opts.GrafanaAdminPassword = "dev1234";
    opts.GrafanaAnonymousAdmin = false;
    opts.IncludeTempo = true;
    opts.IncludeOtelCollector = true;
});

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
