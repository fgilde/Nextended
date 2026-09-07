using Nextended.Aspire.Hosting.AspireUI;

// Test/demo AppHost for Nextended.Aspire.Hosting.AspireUI. It exercises the whole fluent surface, so
// what comes up here is what a real AppHost gets: accounts with different rights, a deploy target,
// an api token, apps from the store and a stack seeded from this repository.
var builder = DistributedApplication.CreateBuilder(args);

// Secrets belong in parameters, not in the AppHost source.
var opsPassword = builder.AddParameter("ops-password", secret: true, value: "ops-password-please-change");
var ciToken = builder.AddParameter("ci-token", secret: true, value: "aspireui_demo_ci_token_value");
var ssoSecret = builder.AddParameter("sso-client-secret", secret: true, value: "demo-client-secret");

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")

    // Three accounts that show what the permission model does: an operator who may run and repair
    // apps, an app user who only installs and configures, and a viewer who may only look.
    .WithUser("ops", opsPassword, AspireUIPermissions.Operator)
    .WithAppUser("kim", "kim-password-1")
    .WithViewer("guest", "guest-password-1")
    .WithUsers(new AspireUIUser("ci", "ci-password-1", $"{AspireUIPermissions.Deploy},{AspireUIPermissions.Configure}"),
               new AspireUIUser("second-admin", "admin-password-1", Admin: true, MustChangePassword: true))
    .WithApiToken("pipeline", "ci", ciToken)

    // A deploy target the demo can point at. Nothing is contacted until somebody deploys to it.
    .WithSshTarget("demo-nas", "nas.local", "deploy", publicHost: "apps.example.com")

    // Two apps from AspireUI's own catalog, and this AppHost as a stack on the canvas.
    .WithApps("vaultwarden", "gitea")
    .WithSeedStack("Demo", builder.AppHostDirectory)

    // Settings that would otherwise need a trip through the UI.
    .WithPublicHost("localhost")
    .WithBackupSchedule(intervalHours: 24, retain: 7)
    .WithAuditRetention(days: 30)
    .WithNotifications(webhookUrl: "https://example.invalid/hooks/aspireui")

    // Off-site copies of every backup, and sign-in through an identity provider. Both point at
    // addresses that do not exist: this AppHost is here to show the shape, not to reach a bucket.
    .WithS3Backups(bucket: "aspireui-backups", accessKey: "demo", secretKey: "demo-secret",
        endpoint: "https://minio.example.invalid", region: "eu-central-1")
    .WithSingleSignOn(authority: "https://id.example.invalid/realms/main", clientId: "aspireui",
        clientSecret: ssoSecret, label: "Keycloak", groupsClaim: "groups", adminGroup: "aspireui-admins",
        defaultPermissions: AspireUIPermissions.AppUser);

builder.Build().Run();
