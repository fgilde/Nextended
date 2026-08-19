---
title: Nextended.Aspire.Hosting.Php — API-Referenz
---

# Nextended.Aspire.Hosting.Php — API-Referenz

🇬🇧 [This page in English](/projects/aspire-php-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.Php`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-php)

## Nextended.Aspire.Hosting.Php

### `PhpBuilderExtensions`

`static class`

Fluent API for running PHP endpoints inside your Aspire stack. Start with `AddPhp`, then optionally tune php.ini via `WithPhpIni` or `WithPhpIniFile`.

### `PhpIniConfiguration`

`class`

Typed php.ini settings for `WithPhpIniConfiguration`. Only assigned (non-null) properties are applied. Property names map to directives by convention (`DisplayErrors` → `display_errors`); `PhpIniKeyAttribute` overrides where the convention doesn't fit. Booleans become `1`/`0`. The mapping is purely reflective — subclass this and add your own properties for any directive not listed here.

**Konstruktoren**

- `PhpIniConfiguration()`

**Eigenschaften**

- `DateTimezone : string { get; set; }`
  <br>`date.timezone` — e.g. `"Europe/Berlin"`.
- `DefaultCharset : string { get; set; }`
  <br>`default_charset` — e.g. `"UTF-8"`.
- `DisplayErrors : bool? { get; set; }`
  <br>`display_errors` — print errors as part of the output (dev setting).
- `DisplayStartupErrors : bool? { get; set; }`
  <br>`display_startup_errors` — also show errors from PHP's startup sequence.
- `ErrorReporting : string { get; set; }`
  <br>`error_reporting` — passed verbatim (e.g. `"E_ALL"` or a numeric value like `"32767"`).
- `FileUploads : bool? { get; set; }`
  <br>`file_uploads` — allow HTTP file uploads at all.
- `LogErrors : bool? { get; set; }`
  <br>`log_errors` — log errors to the server log (visible in the Aspire console).
- `MaxExecutionTime : int? { get; set; }`
  <br>`max_execution_time` — script timeout in seconds.
- `MaxFileUploads : int? { get; set; }`
  <br>`max_file_uploads` — maximum simultaneous file uploads.
- `MaxInputTime : int? { get; set; }`
  <br>`max_input_time` — input parsing timeout in seconds.
- `MaxInputVars : int? { get; set; }`
  <br>`max_input_vars` — maximum number of accepted input variables.
- `MemoryLimit : string { get; set; }`
  <br>`memory_limit` — e.g. `"256M"`.
- `PostMaxSize : string { get; set; }`
  <br>`post_max_size` — e.g. `"64M"`.
- `SessionSavePath : string { get; set; }`
  <br>`session.save_path` — where session files are stored inside the container.
- `ShortOpenTag : bool? { get; set; }`
  <br>`short_open_tag` — allow `<?` as PHP open tag.
- `UploadMaxFilesize : string { get; set; }`
  <br>`upload_max_filesize` — e.g. `"32M"`.

### `PhpIniKeyAttribute`

`class`

Names the php.ini directive a `PhpIniConfiguration` property maps to, for directives the PascalCase→snake_case convention can't produce (e.g. `date.timezone`).

**Konstruktoren**

- `PhpIniKeyAttribute(string key)`
  <br>Names the php.ini directive a `PhpIniConfiguration` property maps to, for directives the PascalCase→snake_case convention can't produce (e.g. `date.timezone`).

**Eigenschaften**

- `Key : string { get; }`
  <br>The php.ini directive name.

### `PhpResource`

`class`

A PHP app served by PHP's built-in web server (`php -S`) inside the official `php:cli` container. The source is a bind-mounted host folder (docroot) or a single `.php` file (router script — every request is handed to it). Exposes one HTTP endpoint and supports service discovery, so .NET services can call the PHP endpoints via `WithReference`.

**Konstruktoren**

- `PhpResource(string name)`
  <br>A PHP app served by PHP's built-in web server (`php -S`) inside the official `php:cli` container. The source is a bind-mounted host folder (docroot) or a single `.php` file (router script — every request is handed to it). Exposes one HTTP endpoint and supports service discovery, so .NET services can call the PHP endpoints via `WithReference`.

**Eigenschaften**

- `Extensions : IList<string> { get; }`
  <br>PHP extensions installed at container start (see `WithPhpExtensions`).
- `IniSettings : IDictionary<string, string> { get; }`
  <br>php.ini directives applied as `-d key=value` (see `WithPhpIni`).
- `RouterScript : string { get; }`
  <br>File name of the router script when a single `.php` file was added; `null` in folder (docroot) mode.
- `SourcePath : string { get; }`
  <br>Resolved host path (folder or file) that is mounted into the container.

**Felder**

- `AppDirectory : string`
  <br>Mount point of the app source inside the container.
- `DefaultImage : string`
  <br>Container image: the official PHP image.
- `DefaultTag : string`
  <br>Default image tag (CLI variant — ships the built-in web server).
- `DefaultTargetPort : int`
  <br>Port the built-in server listens on inside the container.
- `HttpEndpointName : string`
  <br>Name of the HTTP endpoint.

## Projects

### `Nextended_Aspire_Hosting_Php`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-php)
