using Nextended.Aspire.Hosting.Php;

// Test/demo AppHost for the Nextended.Aspire.Hosting.Php integration.
// Run with `dotnet run` (Docker required). Resources:
//   mysql + phpmyadmin — MySQL server with phpMyAdmin UI (fixed port so PHP can link to it)
//   mailpit            — dev SMTP sink with web UI (open its http endpoint to see received mail)
//   php                — ./www served by PHP's built-in server; index.php is a small dashboard
//                        showing the live MySQL connection; send-mail.php delivers via mailpit
//   webdemo            — .NET app with a form that posts to send-mail.php and shows PHP's response
var builder = DistributedApplication.CreateBuilder(args);

const int PhpMyAdminPort = 8081;
// Precomputed: an interpolated string here would bind to the ReferenceExpression
// WithEnvironment overload, which can't format an int.
var phpMyAdminUrl = "http://localhost:" + PhpMyAdminPort;

var mysqlPassword = builder.AddParameter("mysql-password", "demo-password-123", secret: true);
var mysql = builder.AddMySql("mysql", password: mysqlPassword)
    .WithPhpMyAdmin(c => c.WithHostPort(PhpMyAdminPort));

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp");

var php = builder.AddPhp("php", "./www")
    .WithPhpExtensions("mysqli") // stock php:cli has no MySQL driver; compiled at container start
    .WithPhpIniConfiguration(a =>
    {
        a.DisplayErrors = true;
        a.MemoryLimit = "256M";
        a.DateTimezone = "Europe/Berlin";
    })
    // Endpoint references resolve to container-network addresses (tcp://mailpit:1025, tcp://mysql:3306).
    .WithEnvironment("SMTP_URL", mailpit.GetEndpoint("smtp"))
    .WithEnvironment("MYSQL_URL", mysql.GetEndpoint("tcp"))
    .WithEnvironment("MYSQL_USER", "root")
    .WithEnvironment("MYSQL_PASSWORD", mysqlPassword)
    // Host-facing URL (opened by the user's browser, not by the container) — hence the fixed port.
    .WithEnvironment("PHPMYADMIN_URL", phpMyAdminUrl)
    .WaitFor(mysql);

builder.AddProject<Projects.Php_WebDemo>("webdemo")
    .WithReference(php)
    .WaitFor(php)
    .WithExternalHttpEndpoints();

// Single-file variant — one script answers every request, regardless of path:
// builder.AddPhp("mailer", "./www/send-mail.php");
// Composer dependencies (needs composer.json in the folder; vendor/ appears on the host):
// builder.AddPhp("app", "./www").WithComposer();

builder.Build().Run();
