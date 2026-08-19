---
layout: home
hero:
  name: Nextended
  text: .NET libraries that stay out of your way
  tagline: >
    Extension methods and custom types, expression-based caching, EF Core graph loading,
    permission-aware response shaping, a Roslyn source generator and eight .NET Aspire hosting
    integrations. Independent packages on one small foundation.
  image:
    src: /icon.png
    alt: Nextended
  actions:
    - theme: brand
      text: Get started
      link: /guides/installation
    - theme: alt
      text: All packages
      link: /projects/
    - theme: alt
      text: Source
      link: https://github.com/fgilde/Nextended
features:
  - title: Map objects without a mapper
    details: >
      Convention-based mapping needs no profile registration. Explicit assignments,
      ignored members and type converters compose on a settings object when the conventions are
      not enough.
    link: /api/class-mapping
    linkText: Class mapping
  - title: Types that carry their meaning
    details: >
      Money keeps an amount and its currency together at decimal precision. Date is a date with no
      time component, which removes the "which midnight in which zone" class of bug outright.
    link: /api/types
    linkText: Custom types
  - title: Cache keys you never write
    details: >
      Hand over the call you would have made and the key is derived from the declaring type, the
      method name and the actual argument values. Two call sites can no longer disagree.
    link: /projects/cache
    linkText: Caching
  - title: EF Core without Include chains
    details: >
      Walk navigations from a loaded entity, include everything minus named paths, or declare a
      reusable include definition once and hand it to every query that needs it.
    link: /projects/ef
    linkText: Entity Framework
  - title: One response, many audiences
    details: >
      A declarative filter per DTO redacts, masks, rounds, truncates, hashes, prunes and even
      renames keys before serialization — per request, per user, per permission.
    link: /projects/responsefilters
    linkText: Response filters
  - title: Four generators, one config file
    details: >
      DTOs and mapping extensions from your entities, typed classes from JSON and XML, lookup
      tables from Excel, and documentation from source files. The sample project checks its own
      generated output in.
    link: /projects/codegen
    linkText: Source generation
  - title: An AppHost without if-cascades
    details: >
      Every conditional builder call applies its step when the condition holds and returns the
      builder untouched when it does not, so the chain stays one chain instead of branching into
      duplicates.
    link: /projects/aspire
    linkText: .NET Aspire
  - title: Whole stacks as one resource
    details: >
      Supabase, n8n, Grafana with its observability stack, a browser database studio, the visual
      AppHost builder, self-hosted multimodal AI and PHP endpoints — each with a runnable sample
      AppHost.
    link: /projects/aspire-supabase
    linkText: Hosting integrations
  - title: Nothing when nothing applies
    details: >
      The response pipeline analyses a response's type graph once and skips the entire walk when no
      registered filter can reach it. Metadata-driven property selection resolves at build time,
      not per request.
    link: /projects/responsefilters#performance
    linkText: How it stays cheap
---

<script setup>
import data from '@data/packages.json'

const cats = data.categories
const pkgs = data.packages
const repo = data.meta.repo
const base = '/Nextended/'

const byCat = (id) => pkgs.filter((p) => p.category === id)
const leaf = (path) => path.split('/').pop()
const samples = pkgs.filter((p) => p.sample)
</script>

## The packages

::: info Generated from one file
This listing, the sidebar and the tables in every README all come from
[`docs/data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/data/packages.json).
Adding a package there updates every one of them, so they cannot drift apart.
:::

<div v-for="cat in cats" :key="cat.id">
  <h3>{{ cat.en }}</h3>
  <table>
    <thead>
      <tr><th>Package</th><th>Description</th><th>Sample</th></tr>
    </thead>
    <tbody>
      <tr v-for="p in byCat(cat.id)" :key="p.id">
        <td><a :href="base + 'projects/' + p.slug"><strong>{{ p.name }}</strong></a></td>
        <td>{{ p.summary.en }}</td>
        <td>
          <a v-if="p.sample" :href="repo + '/tree/main/' + p.sample">{{ leaf(p.sample) }}</a>
          <span v-else>—</span>
        </td>
      </tr>
    </tbody>
  </table>
</div>

The [package overview](/projects/) adds target frameworks, platforms, dependencies and the
dependency graph.

## Quick start

```bash
dotnet add package Nextended.Core
```

```csharp
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Nextended.Core.DeepClone;

// Object mapping — no profiles, no configuration
var userDto = user.MapTo<UserDto>();
var userDtos = users.MapElementsTo<UserDto>();

// Deep clone, references preserved
var copy = order.CloneDeep();

// Types that carry their meaning
var price = new Money(99.99m, Currency.USD);
var due   = Date.Today.AddDays(30);

// Extension methods
"hello world".ToPascalCase();     // "HelloWorld"
"hello world".ToCamel();          // "helloWorld"
"MyClassName".SplitByUpperCase(); // "My Class Name"
DateTime.Today.AddWeekDays(5);    // skips weekends
```

## Runnable samples

Every Aspire integration and the source generator ship with a project you can start:

<ul>
  <li v-for="p in samples" :key="p.id">
    <a :href="repo + '/tree/main/' + p.sample"><code>{{ leaf(p.sample) }}</code></a> — {{ p.name }}
  </li>
</ul>

## Migrating from nExt

This suite was previously published as **nExt**. Namespaces moved from `nExt.*` to `Nextended.*`;
the API is otherwise source-compatible. See the [migration guide](/guides/migration). The legacy
[nExt.Core](https://www.nuget.org/packages/nExt.Core/) package is no longer maintained.
