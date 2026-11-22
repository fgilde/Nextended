using Nextended.Aspire.Hosting.SigNoz;

// Test/demo AppHost for Nextended.Aspire.Hosting.SigNoz: SigNoz with its dashboards and alert rules, and a sample
// service (HTTP, Postgres, logs, runtime metrics) that sends its telemetry there.
var builder = DistributedApplication.CreateBuilder(args);

var signoz = builder.AddSigNoz("signoz")
    .WithClickHouse(c => c.WithDataVolume())
    .WithDataVolume();

var db = builder.AddPostgres("pg").AddDatabase("db");
builder.AddProject("sample-api", Path.Combine(builder.AppHostDirectory, "..", "SigNoz.SampleApi", "SigNoz.SampleApi.csproj"))
    .WithReference(db)
    .WaitFor(db)
    // the dashboards rate over a minute; the SDK's default of one export a minute would leave them sparse
    .WithEnvironment("OTEL_METRIC_EXPORT_INTERVAL", "10000")
    .WithSigNozTelemetry(signoz);

// `dotnet run --launch-profile keycloak`: sign-in through Keycloak in front of SigNoz. Realm and users come from
// keycloak/nextended-realm.json: ada (client role signoz:admin, gets in) and nora (no role, turned away) — each password is the user name.
// `dotnet run --launch-profile anonymous`: no sign-in at all. Otherwise SigNoz's own login with the admin below.
switch (builder.Configuration["SAMPLE_SIGN_IN"])
{
    case "keycloak":
        const int keycloakPort = 8181;
        var keycloakUrl = $"http://localhost:{keycloakPort}";
        builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
            .WithArgs("start-dev", "--import-realm")
            .WithBindMount(Path.Combine(builder.AppHostDirectory, "keycloak"), "/opt/keycloak/data/import", isReadOnly: true)
            .WithEnvironment("KC_HOSTNAME", keycloakUrl)
            .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
            .WithHttpEndpoint(port: keycloakPort, targetPort: 8080, name: "http");

        signoz.WithOidcLogin(SigNozOidcOptions.Keycloak(
            realmUrl: $"{keycloakUrl}/realms/nextended",
            clientId: "signoz",
            clientSecret: ReferenceExpression.Create($"nextended-sample-secret"),
            backchannelRealmUrl: "http://keycloak:8080/realms/nextended"));
        break;
    case "anonymous":
        signoz.WithAnonymousAdmin();
        break;
    default:
        signoz.WithAdminUser("admin@example.com", "Sample-Passw0rd!");
        break;
}

builder.Build().Run();
