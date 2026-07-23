# Nextended.Aspire.Hosting.Php

Run PHP endpoints inside your .NET Aspire stack — a folder or a single `.php` file, served by
PHP's built-in web server in the official `php:cli` container — and call them from your .NET
services like any other referenced resource.

## Quick start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Folder mode: ./php is the docroot, every .php file inside becomes an endpoint.
var php = builder.AddPhp("php", "./php")
    .WithPhpIni("memory_limit", "256M")
    .WithPhpIni("display_errors", "1");

builder.AddProject<Projects.Api>("api")
    .WithReference(php); // service discovery: http://php resolves inside "api"

builder.Build().Run();
```

Calling it from .NET:

```csharp
// with Microsoft.Extensions.ServiceDiscovery (standard Aspire service defaults):
var client = new HttpClient { BaseAddress = new Uri("http://php") };
var json = await client.GetStringAsync("/index.php?who=aspire");
await client.PostAsJsonAsync("/send-mail.php", new { to = "x@y.z", subject = "hi" });
```

### Single file mode

Pass a `.php` file instead of a folder and it becomes the router script — every request,
regardless of path, is handled by that one file:

```csharp
builder.AddPhp("mailer", "./php/send-mail.php");
```

## API

| Method | What it does |
| ------ | ------------ |
| `AddPhp(name, path, port?, image?, tag?)` | Adds the container. `path` = folder (docroot) or `.php` file (router script); relative to the AppHost directory. |
| `WithPhpIni(key, value)` | One php.ini directive, passed as `php -d key=value`. |
| `WithPhpIni(dictionary)` | Several directives at once. |
| `WithPhpIniFile(path)` | Mounts a complete ini file into PHP's `conf.d` scan dir (overrides the base php.ini). |

## Notes

- **Concurrency:** `PHP_CLI_SERVER_WORKERS` defaults to `8`; override with
  `.WithEnvironment("PHP_CLI_SERVER_WORKERS", "16")`.
- **Dev server:** PHP's built-in server is a development server — a fine match for Aspire's
  local orchestration. For production deploys use a proper PHP image (fpm + nginx / apache).
- **Extensions:** the stock `php:cli` image ships without extras like `pdo_mysql`. Bake a small
  custom image and pass it via `AddPhp(..., image: "my/php", tag: "dev")`.
- **`mail()`:** the container has no MTA, so PHP's `mail()` silently no-ops. Send mail via SMTP
  (e.g. PHPMailer against your mail resource) or a custom image with msmtp.
