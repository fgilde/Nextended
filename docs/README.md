# Nextended documentation

The published site: **<https://fgilde.github.io/Nextended/>**

- 🇬🇧 English — [`index.md`](index.md)
- 🇩🇪 Deutsch — [`de/index.md`](de/index.md)

## Structure

```
docs/
├─ index.md                  English home
├─ _config.yml               just-the-docs configuration, logo, language switcher
├─ _data/packages.json       SINGLE SOURCE OF TRUTH for every package listing
├─ _includes/head-custom.html favicon / social preview, both from assets/icon.png
├─ assets/icon.png           generated copy of the repository-root icon.png
├─ guides/                   installation, architecture, migration (English)
├─ api/                      extension, type, mapping, helper, encryption reference (English)
├─ examples/                 task-oriented samples (English)
├─ projects/                 one page per package (English)
└─ de/                       German mirror
   ├─ index.md
   ├─ guides/                installation, architecture
   ├─ examples/              typische Anwendungsfälle
   └─ projects/              one page per package
```

## What is generated, and what is written by hand

**Generated — do not edit by hand:**

| Target | Produced by | From |
| --- | --- | --- |
| Package tables in `index.md`, `projects/README.md` and their German counterparts | Jekyll / Liquid at build time | `_data/packages.json` |
| `assets/icon.png` | `tools/Update-PackageDocs.ps1` | root `icon.png` |
| The `NEXTENDED:HEADER` / `NEXTENDED:PACKAGES` / `NEXTENDED:FOOTER` blocks in every README | `tools/Update-PackageDocs.ps1` | `_data/packages.json` |

Everything else is hand-written prose.

## Adding or changing a package

1. Add or edit the entry in [`_data/packages.json`](_data/packages.json).
2. Create `projects/<slug>.md` and `de/projects/<slug>.md`.
3. Run the generator:

   ```bash
   pwsh tools/Update-PackageDocs.ps1
   ```

The generator refuses to finish quietly when a packable project is missing from `packages.json`, when
`packages.json` names a project that does not exist, or when a package points at a documentation page
that is not there. In CI, use:

```bash
pwsh tools/Update-PackageDocs.ps1 -Check
```

## Replacing the icon

`icon.png` in the **repository root** is the only copy that matters. Replace it and run the generator:
the NuGet package icon (via `Package.props`), the logo in every README (via a raw GitHub URL) and the
site's logo, favicon and social preview (via `assets/icon.png`) all follow.

## Running the site locally

```bash
cd docs
bundle exec jekyll serve
```

The theme is [just-the-docs](https://just-the-docs.github.io/just-the-docs/), pulled in as a remote
theme, so GitHub Pages builds the site without a committed `Gemfile.lock`.
