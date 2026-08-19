---
layout: default
title: Projects
nav_order: 2
has_children: true
permalink: /projects
---

# Nextended packages
{: .no_toc }

🇩🇪 [Diese Seite auf Deutsch](https://github.com/fgilde/Nextended/blob/main/docs/de/projects/README.md)

Every package in the Nextended suite, its purpose, its runnable sample and its documentation page.
{: .fs-5 .fw-300 }

{: .note }
> This overview is generated at build time from
> [`docs/_data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/_data/packages.json),
> the single source of truth for every package listing in this repository. Adding a package there
> updates this page, the German mirror, the root README and all package READMEs — the last via
> `pwsh tools/Update-PackageDocs.ps1`.

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

{% assign pkgs = site.data.packages.packages %}
{% assign cats = site.data.packages.categories %}
{% assign repo = site.data.packages.meta.repo %}

{% for cat in cats %}
## {{ cat.en }}

{% for pkg in pkgs %}{% if pkg.category == cat.id %}
### [{{ pkg.name }}]({{ pkg.slug }}.html)

{{ pkg.summary.en }}

| | |
| --- | --- |
| **NuGet** | [{{ pkg.name }}](https://www.nuget.org/packages/{{ pkg.name }}/) |
| **Install** | `dotnet add package {{ pkg.name }}` |
| **Frameworks** | {% for fw in pkg.frameworks %}`{{ fw }}`{% unless forloop.last %}, {% endunless %}{% endfor %} |
| **Platform** | {{ pkg.platform }} |
| **Dependencies** | {% if pkg.dependencies.size == 0 %}none{% else %}{% for dep in pkg.dependencies %}{{ dep }}{% unless forloop.last %}, {% endunless %}{% endfor %}{% endif %} |
| **Source** | [{{ pkg.name }}]({{ repo }}/tree/main/{{ pkg.name }}) |{% if pkg.sample %}
| **Runnable sample** | [{{ pkg.sample | split: "/" | last }}]({{ repo }}/tree/main/{{ pkg.sample }}) |{% endif %}
| **Documentation** | [English]({{ pkg.slug }}.html) · [Deutsch]({{ repo }}/blob/main/docs/de/projects/{{ pkg.slug }}.md) |

{% endif %}{% endfor %}
{% endfor %}

---

## Quick reference matrix

| Package | Frameworks | Platform | Dependencies |
| --- | --- | --- | --- |
{% for pkg in pkgs %}| [{{ pkg.name }}]({{ pkg.slug }}.html) | {% for fw in pkg.frameworks %}`{{ fw }}`{% unless forloop.last %}, {% endunless %}{% endfor %} | {{ pkg.platform }} | {% if pkg.dependencies.size == 0 %}none{% else %}{% for dep in pkg.dependencies %}{{ dep }}{% unless forloop.last %}, {% endunless %}{% endfor %}{% endif %} |
{% endfor %}

## Dependency graph

```
Nextended.Core ─┬─ Nextended.Cache ─── Nextended.Imaging
                ├─ Nextended.EF ────── Nextended.Web
                ├─ Nextended.Blazor
                ├─ Nextended.UI
                ├─ Nextended.ResponseFilters ─── Nextended.ResponseFilters.AspNetCore
                └─ Nextended.Aspire ─┬─ Nextended.Aspire.Hosting.N8n
                                     ├─ Nextended.Aspire.Hosting.LocalAI
                                     └─ Nextended.Aspire.Hosting.Supabase ─┐
                                                                           │
Nextended.Aspire.Hosting.Grafana ──────────────────────────────────────────┘

standalone (Aspire.Hosting.AppHost only):
  Nextended.Aspire.Hosting.WebDataStudio
  Nextended.Aspire.Hosting.AspireUI
  Nextended.Aspire.Hosting.Php

build-time only:
  Nextended.CodeGen  (consumes the attributes from Nextended.Core)
```

## Which package do I need?

| I want to … | Package |
| --- | --- |
| Map objects without a mapper library | [Nextended.Core](core.html) |
| Work with money, date-only values or typed ids | [Nextended.Core](core.html) |
| Build a faceted search over `IQueryable<T>` | [Nextended.Core](core.html) |
| Cache a method call without inventing a cache key | [Nextended.Cache](cache.html) |
| Load an EF Core graph without writing `Include` chains | [Nextended.EF](ef.html) |
| Page and sort by a client-supplied string | [Nextended.EF](ef.html) |
| Expose OData without writing an EDM model | [Nextended.Web](web.html) |
| Run work after the response has been sent | [Nextended.Web](web.html) |
| Hide, mask or rename response fields per user | [Nextended.ResponseFilters](responsefilters.html) |
| Do that in an MVC app | [Nextended.ResponseFilters.AspNetCore](responsefilters-aspnetcore.html) |
| Let users browse inside an uploaded zip in Blazor | [Nextended.Blazor](blazor.html) |
| Global shortcuts and gamepads in a WPF app | [Nextended.UI](ui.html) |
| Resize, crop or sniff images on Windows | [Nextended.Imaging](imaging.html) |
| Generate DTOs, or classes from JSON/XML/Excel | [Nextended.CodeGen](codegen.html) |
| Remove `if` branches from my Aspire AppHost | [Nextended.Aspire](aspire.html) |
| Run Supabase, n8n, Grafana, PHP or LocalAI in Aspire | the matching `Nextended.Aspire.Hosting.*` package |

## Getting help

- [Examples](../examples/common-use-cases.md) — task-oriented code samples
- [API reference](../api/extensions.md) — the detailed extension and type reference
- [Installation guide](../guides/installation.md)
- [GitHub issues](https://github.com/fgilde/Nextended/issues)
