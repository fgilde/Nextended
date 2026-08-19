---
title: All packages
description: Every package in the Nextended suite, with frameworks, platform, dependencies, runnable sample and documentation page.
---

<script setup>
import data from '@data/packages.json'

const cats = data.categories
const pkgs = data.packages
const repo = data.meta.repo
const base = '/Nextended/'

const byCat = (id) => pkgs.filter((p) => p.category === id)
const leaf = (path) => path.split('/').pop()
const deps = (p) => (p.dependencies.length ? p.dependencies.join(', ') : 'none')
const fws = (p) => p.frameworks.join(', ')
</script>

# All packages

Every package in the suite, what it is for, and where its runnable sample lives.

::: info Generated
This page is rendered from
[`docs/data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/data/packages.json)
at build time — the same file the README generator reads. See
[how the docs are generated](/CONTRIBUTING).
:::

<div v-for="cat in cats" :key="cat.id">
  <h2>{{ cat.en }}</h2>
  <div v-for="p in byCat(cat.id)" :key="p.id">
    <h3><a :href="base + 'projects/' + p.slug">{{ p.name }}</a></h3>
    <p>{{ p.summary.en }}</p>
    <table>
      <tbody>
        <tr>
          <td><strong>NuGet</strong></td>
          <td><a :href="'https://www.nuget.org/packages/' + p.name + '/'">{{ p.name }}</a></td>
        </tr>
        <tr>
          <td><strong>Install</strong></td>
          <td><code>dotnet add package {{ p.name }}</code></td>
        </tr>
        <tr><td><strong>Frameworks</strong></td><td><code>{{ fws(p) }}</code></td></tr>
        <tr><td><strong>Platform</strong></td><td>{{ p.platform }}</td></tr>
        <tr><td><strong>Dependencies</strong></td><td>{{ deps(p) }}</td></tr>
        <tr>
          <td><strong>Source</strong></td>
          <td><a :href="repo + '/tree/main/' + p.name">{{ p.name }}</a></td>
        </tr>
        <tr v-if="p.sample">
          <td><strong>Runnable sample</strong></td>
          <td><a :href="repo + '/tree/main/' + p.sample">{{ leaf(p.sample) }}</a></td>
        </tr>
        <tr>
          <td><strong>Documentation</strong></td>
          <td>
            <a :href="base + 'projects/' + p.slug">English</a> ·
            <a :href="base + 'de/projects/' + p.slug">Deutsch</a>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</div>

## Quick reference matrix

<table>
  <thead>
    <tr><th>Package</th><th>Frameworks</th><th>Platform</th><th>Dependencies</th></tr>
  </thead>
  <tbody>
    <tr v-for="p in pkgs" :key="p.id">
      <td><a :href="base + 'projects/' + p.slug">{{ p.name }}</a></td>
      <td><code>{{ fws(p) }}</code></td>
      <td>{{ p.platform }}</td>
      <td>{{ deps(p) }}</td>
    </tr>
  </tbody>
</table>

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
| Map objects without a mapper library | [Nextended.Core](/projects/core) |
| Work with money, date-only values or typed ids | [Nextended.Core](/projects/core) |
| Build a faceted search over `IQueryable<T>` | [Nextended.Core](/projects/core) |
| Cache a method call without inventing a cache key | [Nextended.Cache](/projects/cache) |
| Load an EF Core graph without writing `Include` chains | [Nextended.EF](/projects/ef) |
| Page and sort by a client-supplied string | [Nextended.EF](/projects/ef) |
| Expose OData without writing an EDM model | [Nextended.Web](/projects/web) |
| Run work after the response has been sent | [Nextended.Web](/projects/web) |
| Hide, mask or rename response fields per user | [Nextended.ResponseFilters](/projects/responsefilters) |
| Do that in an MVC app | [Nextended.ResponseFilters.AspNetCore](/projects/responsefilters-aspnetcore) |
| Let users browse inside an uploaded zip in Blazor | [Nextended.Blazor](/projects/blazor) |
| Global shortcuts and gamepads in a WPF app | [Nextended.UI](/projects/ui) |
| Resize, crop or sniff images on Windows | [Nextended.Imaging](/projects/imaging) |
| Generate DTOs, or classes from JSON/XML/Excel | [Nextended.CodeGen](/projects/codegen) |
| Remove `if` branches from my Aspire AppHost | [Nextended.Aspire](/projects/aspire) |
| Run Supabase, n8n, Grafana, PHP or LocalAI in Aspire | the matching `Nextended.Aspire.Hosting.*` package |

## Getting help

- [Common use cases](/examples/common-use-cases) — task-oriented code samples
- [API reference](/api/extensions) — the detailed extension and type reference
- [Installation guide](/guides/installation)
- [GitHub issues](https://github.com/fgilde/Nextended/issues)
