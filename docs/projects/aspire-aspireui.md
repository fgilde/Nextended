---
title: Nextended.Aspire.Hosting.AspireUI
---
# Nextended.Aspire.Hosting.AspireUI

📚 **[Full API reference](/projects/aspire-aspireui-api)** — every public type and member, generated from the compiled assembly.

AspireUI — the visual AppHost builder — as a resource inside your own Aspire stack, with an optional pre-seeded admin user and a starter stack built from your project paths.
[![NuGet](https://img.shields.io/nuget/v/Nextended.Aspire.Hosting.AspireUI.svg)](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)

🇩🇪 [Diese Seite auf Deutsch](/de/projects/aspire-aspireui)

---

## Installation

```bash
dotnet add package Nextended.Aspire.Hosting.AspireUI
```

## Runnable sample

A complete AppHost you can start is checked into the repository:

**[AspireUI.AppHost](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/AspireUI.AppHost)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/AspireUI.AppHost
dotnet run
```

Run [AspireUI](https://github.com/fgilde/AspireUI) — the visual .NET Aspire AppHost builder — as a
resource inside your own Aspire stack.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithSeedStack("My App", builder.AppHostDirectory);

builder.Build().Run();
```

This adds the `ghcr.io/fgilde/aspireui` container with:

- an **HTTP endpoint** for the web UI,
- the host **Docker socket** mounted, so stacks you build in AspireUI can run,
- a **named volume** for AspireUI's data (stacks, settings, users).

## API

### The container

| Call | Effect |
|------|--------|
| `AddAspireUI(name = "aspireui", port?, image?, tag?)` | Add the AspireUI container. |
| `.WithDataBindMount(hostPath)` | Keep AspireUI's data in a host folder instead of a named volume. |
| `.WithoutDockerSocket()` | Run without the host's docker socket: builds and remote deploys still work, hosting on this machine does not. |
| `.WithDockerHost(dockerHost)` | Point AspireUI's docker client somewhere else (`DOCKER_HOST`); implies `WithoutDockerSocket()`. |
| `.WithSourceMount(hostPath, containerPath?)` | Bind-mount source into the container so a seeded stack can also run there. |

### Accounts and tokens

| Call | Effect |
|------|--------|
| `.WithAdminUser(username, password)` | Seed the admin on first run (idempotent; password stored hashed). Also accepts an Aspire `ParameterResource`. |
| `.WithUser(username, password, permissions?, viewModes?, mustChangePassword?)` | Create an account. `permissions` is a preset from `AspireUIPermissions` or a comma-separated list of ids. Also accepts a `ParameterResource` password. |
| `.WithUsers(params AspireUIUser[])` | Create several accounts at once, admins included. |
| `.WithAppUser(username, password)` | An account that installs, configures and browses files — no builder. |
| `.WithViewer(username, password)` | An account that may look and change nothing. |
| `.WithApiToken(name, username, token)` | A bearer token for automation, with a value you already know. Also accepts a `ParameterResource`. |

### Deploy targets

| Call | Effect |
|------|--------|
| `.WithSshTarget(name, host, user, port, keyFile?, key?, passphrase?, publicHost?, isDefault?)` | Another machine's docker daemon over SSH. `keyFile` is mounted read-only, never put in an environment variable. |
| `.WithDockerTcpTarget(name, host, port, caFile?, certFile?, keyFile?, publicHost?, isDefault?)` | A docker daemon over TCP with mTLS; the three certificates are mounted read-only. |
| `.WithKubernetesTarget(name, context?, kubeconfigFile?, namespace?, expose?, ingressHost?, storageClass?, isDefault?)` | A Kubernetes cluster, deployed with Helm. |

### Apps and stacks

| Call | Effect |
|------|--------|
| `.WithApps(params catalogIds)` | Install apps from AspireUI's own catalog by id (`vaultwarden`, `gitea`, …). |
| `.WithApp(catalogId, name?, deploy?)` | Install one catalog app under a name of your choosing. |
| `.WithAppSource(name, url)` | Register an app manifest url as a store source. |
| `.WithSeedStack(name, params projectPaths)` | Seed a stack with one `AddProject` node per path. |
| `.WithProjectStack(name, params projects)` | Same, from the `ProjectResource`s in this AppHost — and their folders are mounted so the stack can also run. |
| `.WithSeedFromDirectory(hostPath, name?, mode?, deploy?)` | Import a folder as a stack: manifest, compose file or AppHost, whichever it holds. Mounted read-only, imported as a copy. |
| `.WithSeedFromCompose(hostPath, name?, deploy?)` | Import a single docker-compose file as a stack. |
| `.WithSeedFromGit(url, branch?, subdir?, name?, mode?, deploy?)` | Clone a repository inside the container and import it. |
| `.WithSeedFile(hostPath)` | Mount a seed document (or a folder with `aspireui.seed.json`) and point AspireUI at it. |
| `.WithAutoDeploy(deploy = true)` | Deploy the seeded stacks and apps once hosting is up. |

### Settings

| Call | Effect |
|------|--------|
| `.WithAssistant(endpoint, model, apiKey?, label?)` | The assistant's backend: an OpenAI-compatible url. Also takes a `ReferenceExpression` for an endpoint only known at start, and an Aspire `ParameterResource` for the key. |
| `.WithAssistant(server, model, apiKey?, apiPath?, endpointName?, label?)` | A model server in this stack — Ollama, LocalAI, vLLM, llama.cpp. AspireUI waits for it and talks to it over the container network. |
| `.WithOllamaAssistant(ollama, model = "llama3.2")` / `.WithLocalAiAssistant(localAi, model)` | The same, named for the two servers people reach for first. |
| `.WithCliAssistant(tool, model?)` | An agent CLI on the AspireUI host (claude, gemini, ollama, llm, codex). Answers questions; cannot call tools. |
| `.WithAi(baseUrl, model, apiKey?)` / `.WithAi(backend, model, …)` | The older name for the first two. |
| `.WithPublicHost(host)` | The host name app urls are built from. |
| `.WithNginxProxyManager(baseUrl, email, password, forwardHost?)` | Let a hosted app be given a domain and a certificate from its own menu. Also takes an NPM resource in the same stack. |
| `.WithNotifications(webhookUrl?, telegramToken?, telegramChat?)` | Where a deployment that came up, went down or started failing is reported. |
| `.WithBackupSchedule(intervalHours = 24, retain = 7)` | Back up every hosted app's volumes on a schedule. |
| `.WithHostedDashboards(browserToken?)` | Host an Aspire dashboard next to every deployed app. |
| `.WithSingleSignOn(authority, clientId, clientSecret?, label?, …)` | Sign-in through an OpenID Connect provider. Only the authority and client id are needed — the endpoints come from the provider's discovery document. The secret also takes an Aspire `ParameterResource`. |
| `.WithS3Backups(bucket, accessKey, secretKey, endpoint?, region?, pathStyle?)` | Copy every backup to an S3-compatible bucket. |
| `.WithWebDavBackups(baseUrl, user, password)` | Copy every backup to a WebDAV share. |
| `.WithSshBackups(host, user, path, keyFile?, port?)` | Copy every backup to a directory on another machine over scp; the key file is mounted read-only. |
| `.WithAuditRetention(days)` | How long the activity log keeps an entry (0 = keep everything). |
| `.WithSettings(s => { … })` | Every setting under AspireUI's own settings, typed: public host, bundled dashboards, proxy, notifications, backup schedule, import limits, activity-log retention and the assistant's backend. Anything left null is not sent. |
| `.WithSetting(key, parameter)` | One setting whose value is a secret, from an Aspire `ParameterResource`. |
| `.WithSetting(key, value)` | Any AspireUI setting by key. |
| `.WithForcedSettings(force = true)` | Apply those settings on every start instead of only filling in what is empty. |

Everything is **idempotent by name**: restarting with the same AppHost changes nothing, adding one
entry adds exactly that one, and anything changed in the UI stays changed. `WithAdminUser` is the
one exception — like AspireUI's own first-run seed, it is skipped once any account exists.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opsPassword = builder.AddParameter("ops-password", secret: true);

builder.AddAspireUI()
    .WithAdminUser("admin", "change-me-please")
    .WithUser("ops", opsPassword, AspireUIPermissions.Operator)
    .WithViewer("guest", "guest-password-1")
    .WithApiToken("pipeline", "ops", builder.AddParameter("ci-token", secret: true))
    .WithSshTarget("nas", "nas.local", "deploy", keyFile: "./keys/id_ed25519")
    .WithApps("vaultwarden", "gitea")
    .WithSeedFromDirectory("./seed/edge", "Edge")
    .WithOllamaAssistant(ollama, "llama3.2")
    .WithAutoDeploy();

builder.Build().Run();
```

> The Docker-socket mount gives the container control over the host Docker daemon — run it only on a
> trusted host. Passwords and tokens belong in Aspire parameters, and key material in the files the
> target methods mount, so neither ends up in the manifest.

## Supported frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Dependencies

- Aspire.Hosting.AppHost

## Links

- 📦 [NuGet package](https://www.nuget.org/packages/Nextended.Aspire.Hosting.AspireUI/)
- 🧑‍💻 [Source code](https://github.com/fgilde/Nextended/tree/main/Nextended.Aspire.Hosting.AspireUI)
- 📄 [Package README](https://github.com/fgilde/Nextended/blob/main/Nextended.Aspire.Hosting.AspireUI/README.md)
- 🐛 [Report an issue](https://github.com/fgilde/Nextended/issues)