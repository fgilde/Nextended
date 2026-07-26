using Nextended.Aspire;
using Nextended.Aspire.Hosting.N8n.Builders;

// Test/demo AppHost for the Nextended.Aspire.Hosting.N8n 
var builder = DistributedApplication.CreateBuilder(args);

if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");


var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    .WithQueueMode(workers: 1)
    .WithWorkflowsFromDirectory(Path.Combine(builder.AppHostDirectory, "workflows"));

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
