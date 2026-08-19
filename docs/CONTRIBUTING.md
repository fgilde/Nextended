---
title: How the docs are generated
description: The structure of the documentation site, which parts are generated, and how to add or change a package.
---

# How the docs are generated

The published site: **<https://fgilde.github.io/Nextended/>**

- 🇬🇧 English — the site root
- 🇩🇪 Deutsch — [`/de/`](/de/)

The site is [VitePress](https://vitepress.dev) with two locales in one build. The language switch
in the navigation bar is part of the theme, so it needs no per-page wiring.

## Structure

```
docs/
├─ package.json              vitepress dev / build / preview
├─ .vitepress/config.mts     locales, navigation, sidebar, search, theme
├─ data/packages.json        SINGLE SOURCE OF TRUTH for every package listing
├─ public/icon.png           generated copy of the repository-root icon.png
├─ index.md                  English home (hero layout)
├─ guides/                   installation, architecture, migration (English)
├─ api/                      extension, type, mapping, helper, encryption reference (English)
├─ examples/                 task-oriented samples (English)
├─ projects/                 one page per package (English) + index.md overview
└─ de/                       German locale, same shape
   ├─ index.md
   ├─ guides/                Installation, Architektur
   ├─ examples/              typische Anwendungsfälle
   └─ projects/              one page per package + index.md overview
```

## Running it locally

```bash
cd docs
npm install
npm run dev
```

`npm run build` produces `docs/.vitepress/dist`. The build **fails on a dead internal link**
(`ignoreDeadLinks` is off), which is how a broken cross-reference gets caught before it ships.

## What is generated, and what is written by hand

| Target | Produced by | From |
| --- | --- | --- |
| Package tables and the matrix on `index.md`, `/projects/`, and their German counterparts | VitePress at build time — the pages import the JSON and render it with Vue | `data/packages.json` |
| The package groups in the sidebar | `.vitepress/config.mts` reads the same JSON | `data/packages.json` |
| `public/icon.png` | `tools/Update-PackageDocs.ps1` | root `icon.png` |
| The `NEXTENDED:HEADER` / `NEXTENDED:PACKAGES` / `NEXTENDED:FOOTER` blocks in every README | `tools/Update-PackageDocs.ps1` | `data/packages.json` |

Everything else is hand-written prose.

## Adding or changing a package

1. Add or edit the entry in [`data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/data/packages.json).
2. Create `projects/<slug>.md` and `de/projects/<slug>.md`.
3. Nothing else — the local build regenerates the README blocks, and CI does it on `main`.

To regenerate by hand:

```bash
pwsh tools/Update-PackageDocs.ps1
```

The generator refuses to finish quietly when a packable project is missing from `packages.json`,
when `packages.json` names a project that does not exist, or when a package points at a
documentation page that is not there.

## How it stays current without anyone remembering

| Layer | What it does |
| --- | --- |
| `Directory.Build.targets` | Runs the generator with `-IfNeeded` after any build that touches `Nextended.Core`. A stamp file and a named mutex make it once-per-build and incremental. |
| `.github/workflows/docs-sync.yml` | On a pull request it verifies with `-Check`; on `main` it regenerates and commits. |
| `.github/workflows/pages.yml` | Builds and deploys the VitePress site when `docs/**` or `icon.png` changes. |

## Replacing the icon

`icon.png` in the **repository root** is the only copy that matters. Replace it and build:

- the NuGet package icon for all 18 packages follows via `Package.props`
- the logo in every README follows via a raw GitHub URL (no copy)
- the site logo, favicon and social preview follow via `public/icon.png`

Keep it square and under 1 MB — NuGet rejects a `PackageIcon` above that, and renders icons
square.

## Writing pages

VitePress compiles markdown as a Vue template, which has two consequences worth knowing:

- A bare generic in prose (`Range<T>`, `IResourceBuilder<T>`) is parsed as an unclosed HTML tag
  and breaks the build. Put it in backticks.
- A page can import data and render it, which is how the package listings work:

  ```md
  <script setup>
  import data from '@data/packages.json'
  </script>

  <ul>
    <li v-for="p in data.packages" :key="p.id">{{ p.name }}</li>
  </ul>
  ```

  The `@data` alias is defined in `.vitepress/config.mts`, so no page has to count `../` levels.

Callouts use the VitePress container syntax:

```md
::: info Heading
Body text.
:::
```

`::: tip`, `::: warning` and `::: danger` also exist.
