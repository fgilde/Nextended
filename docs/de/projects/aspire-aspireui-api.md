---
title: Nextended.Aspire.Hosting.AspireUI — API-Referenz
---

# Nextended.Aspire.Hosting.AspireUI — API-Referenz

🇬🇧 [This page in English](/projects/aspire-aspireui-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.AspireUI`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-aspireui)

## Nextended.Aspire.Hosting.AspireUI

### `AspireUIBuilderExtensions`

`static class`

Fluent API for running AspireUI inside your Aspire stack. Start with `AddAspireUI`, then optionally `WithAdminUser` and `WithSeedStack` to have it come up pre-configured.

### `AspireUIResource`

`class`

AspireUI — the visual .NET Aspire AppHost builder — running as a container resource. Exposes an HTTP endpoint for the web UI, and can be pre-seeded (admin user + a starter stack) so it comes up ready without the manual first-run wizard.

**Konstruktoren**

- `AspireUIResource(string name)`
  <br>AspireUI — the visual .NET Aspire AppHost builder — running as a container resource. Exposes an HTTP endpoint for the web UI, and can be pre-seeded (admin user + a starter stack) so it comes up ready without the manual first-run wizard.

**Eigenschaften**

- `AdminUsername : string { get; }`
  <br>Admin username seeded on first run, if configured via `WithAdminUser`.
- `SeedProjects : IList<string> { get; }`
  <br>Project paths seeded into the starter stack (one `AddProject` node each).
- `SeedStackName : string { get; }`
  <br>Name of the starter stack seeded on first run, if configured via `WithSeedStack`.

**Felder**

- `DefaultImage : string`
  <br>Container image (without registry-less shorthand): the published AspireUI image.
- `DefaultTag : string`
  <br>Default image tag.
- `DefaultTargetPort : int`
  <br>Internal port the AspireUI server listens on inside the container.
- `HttpEndpointName : string`
  <br>Name of the primary HTTP endpoint (the web UI).

## Projects

### `Nextended_Aspire_Hosting_AspireUI`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-aspireui)
