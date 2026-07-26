using Nextended.Aspire.Hosting.Grafana;

// Test/demo AppHost for the Nextended.Aspire.Hosting.Grafana
var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("pg");

builder.AddGrafana("grafana")
    .WithAnonymousAdmin()
    .WithPrometheus(configure: p => p.WithRetention("7d"))
    .WithLoki(configure: l => l.WithPromtail())
    .WithTempo()
    .WithOtelCollector()
    .WithCAdvisor()
    .WithPostgresDatasource(pg)
    .WithPostgresExporter(pg)
    .WithDashboards(Path.Combine(builder.AppHostDirectory, "dashboards"), "Demo")
    .WithDataVolume();

builder.Build().Run();
