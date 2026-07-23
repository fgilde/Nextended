using Nextended.Aspire.Hosting.Php;

// Test/demo AppHost for the Nextended.Aspire.Hosting.Php integration.
// Run with `dotnet run` (Docker required). Serves ./www with PHP's built-in web server;
// open the http endpoint to hit index.php, or POST JSON to /send-mail.php.
var builder = DistributedApplication.CreateBuilder(args);

builder.AddPhp("php", "./www")
    .WithPhpIni("display_errors", "1")
    .WithPhpIni("memory_limit", "256M");

// Single-file variant — one script answers every request, regardless of path:
// builder.AddPhp("mailer", "./www/send-mail.php");

builder.Build().Run();
