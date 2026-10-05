using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.Observability;

// Test/demo AppHost for the Nextended.Aspire.Hosting.Grafana
var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("pg");

var grafana = builder.AddGrafana("grafana")
    .WithPrometheus(configure: p => p.WithRetention("7d"))
    .WithLoki(configure: l => l.WithPromtail())
    .WithTempo()
    .WithOtelCollector()
    .WithCAdvisor()
    .WithPostgresDatasource(pg)
    .WithPostgresExporter(pg)
    .WithDashboards(Path.Combine(builder.AppHostDirectory, "dashboards"), "Demo")
    .WithDataVolume();

// `dotnet run --launch-profile keycloak`: sign-in through Keycloak instead of anonymous access.
// Realm and users come from keycloak/nextended-realm.json: ada (server admin), eve (editor),
// vic (viewer) and nora (no role, turned away) — each password is the user name.
if (builder.Configuration["SAMPLE_SIGN_IN"] == "keycloak")
{
    const int keycloakPort = 8180;
    var keycloakUrl = $"http://localhost:{keycloakPort}";
    builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
        .WithArgs("start-dev", "--import-realm")
        .WithBindMount(Path.Combine(builder.AppHostDirectory, "keycloak"), "/opt/keycloak/data/import", isReadOnly: true)
        // The browser opens the login page on localhost; Grafana reaches keycloak:8080 directly.
        .WithEnvironment("KC_HOSTNAME", keycloakUrl)
        .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
        .WithHttpEndpoint(port: keycloakPort, targetPort: 8080, name: "http");

    grafana.WithOAuthLogin(GrafanaOAuthOptions.Keycloak(
        realmUrl: $"{keycloakUrl}/realms/nextended",
        clientId: "grafana",
        clientSecret: ReferenceExpression.Create($"nextended-sample-secret"),
        backchannelRealmUrl: "http://keycloak:8080/realms/nextended"));
}
else
{
    grafana.WithAnonymousAdmin();
}

builder.Build().Run();
