---
title: Nextended.Aspire — API-Referenz
---

# Nextended.Aspire — API-Referenz

🇬🇧 [This page in English](/projects/aspire-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire)

## Nextended.Aspire

### `DevCertHostingExtensions`

`static class`

_Keine Beschreibung._

### `DistributedApplicationBuilderExtensions`

`static class`

Extension methods for `IResourceBuilder`1` that provide conditional operations such as waiting for dependencies and adding references or environment variables to a resource.

### `DistributedApplicationExtensions`

`static class`

_Keine Beschreibung._

### `DockerHelper`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DockerHelper()`

**Methoden**

- `EnsureDockerIsRunning() : void`
- `IsDockerInstalled() : bool`
- `IsDockerRunning() : bool`
- `StartDocker() : void`

### `GithubRepositoryExtensions`

`static class`

Runs any GitHub (or other git) repository as an Aspire container resource: the repo is cloned/updated on the host at build time and built via its own — or a generated — Dockerfile.

**Methoden**

- `EnsureGitCheckout(string repository, string gitRef, string dir) : void`
  <br>Clones `repository`@`gitRef` into `dir` (shallow), or refreshes an existing checkout to that ref. A failed refresh (offline) keeps the existing checkout; the initial clone is required and throws with a clear message.

### `GithubRepositoryOptions`

`class`

Options for `AddGithubRepository` / `WithGithubSource``1`.

**Konstruktoren**

- `GithubRepositoryOptions()`

**Eigenschaften**

- `CheckoutDirectory : string { get; set; }`
  <br>Directory the repository is cloned into. Default `{AppHostDirectory}/obj/github/{resourceName}`. The checkout itself lives in a `src` subfolder; a generated Dockerfile is written next to it.
- `ContextSubPath : string { get; set; }`
  <br>Subdirectory of the checkout to use as the docker build context. Default: repo root.
- `DockerfileContent : string { get; set; }`
  <br>Content of a Dockerfile to generate next to the checkout (for repos that ship none). Line endings are normalized to LF. When `null`, the repo's own Dockerfile is used (see `DockerfilePath`).
- `DockerfilePath : string { get; set; }`
  <br>Path of the Dockerfile inside the checkout, relative to the repo root — only used when `DockerfileContent` is `null`. Default: the builder's own default (`Dockerfile` in the context).
- `GitRef : string { get; set; }`
  <br>Git branch/tag/commit-ish to check out. Default `main`. Pin a tag for reproducible builds.

## Projects

### `Nextended_Aspire`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire)
