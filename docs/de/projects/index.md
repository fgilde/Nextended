---
title: Alle Pakete
description: Jedes Paket der Nextended-Sammlung mit Frameworks, Plattform, Abhängigkeiten, ausführbarem Beispiel und Dokumentationsseite.
---

<script setup>
import data from '@data/packages.json'

const cats = data.categories
const pkgs = data.packages
const repo = data.meta.repo
const base = '/Nextended/'

const byCat = (id) => pkgs.filter((p) => p.category === id)
const leaf = (path) => path.split('/').pop()
const deps = (p) => (p.dependencies.length ? p.dependencies.join(', ') : 'keine')
const fws = (p) => p.frameworks.join(', ')
</script>

# Alle Pakete

Jedes Paket der Sammlung, wofür es da ist und wo sein ausführbares Beispiel liegt.

::: info Generiert
Diese Seite wird beim Build aus
[`docs/data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/data/packages.json)
gerendert — derselben Datei, die auch der README-Generator liest.
:::

<div v-for="cat in cats" :key="cat.id">
  <h2>{{ cat.de }}</h2>
  <div v-for="p in byCat(cat.id)" :key="p.id">
    <h3><a :href="base + 'de/projects/' + p.slug">{{ p.name }}</a></h3>
    <p>{{ p.summary.de }}</p>
    <table>
      <tbody>
        <tr>
          <td><strong>NuGet</strong></td>
          <td><a :href="'https://www.nuget.org/packages/' + p.name + '/'">{{ p.name }}</a></td>
        </tr>
        <tr>
          <td><strong>Installation</strong></td>
          <td><code>dotnet add package {{ p.name }}</code></td>
        </tr>
        <tr><td><strong>Frameworks</strong></td><td><code>{{ fws(p) }}</code></td></tr>
        <tr><td><strong>Plattform</strong></td><td>{{ p.platform }}</td></tr>
        <tr><td><strong>Abhängigkeiten</strong></td><td>{{ deps(p) }}</td></tr>
        <tr>
          <td><strong>Quellcode</strong></td>
          <td><a :href="repo + '/tree/main/' + p.name">{{ p.name }}</a></td>
        </tr>
        <tr v-if="p.sample">
          <td><strong>Ausführbares Beispiel</strong></td>
          <td><a :href="repo + '/tree/main/' + p.sample">{{ leaf(p.sample) }}</a></td>
        </tr>
        <tr>
          <td><strong>Dokumentation</strong></td>
          <td>
            <a :href="base + 'de/projects/' + p.slug">Deutsch</a> ·
            <a :href="base + 'projects/' + p.slug">English</a>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</div>

## Referenzmatrix

<table>
  <thead>
    <tr><th>Paket</th><th>Frameworks</th><th>Plattform</th><th>Abhängigkeiten</th></tr>
  </thead>
  <tbody>
    <tr v-for="p in pkgs" :key="p.id">
      <td><a :href="base + 'de/projects/' + p.slug">{{ p.name }}</a></td>
      <td><code>{{ fws(p) }}</code></td>
      <td>{{ p.platform }}</td>
      <td>{{ deps(p) }}</td>
    </tr>
  </tbody>
</table>

## Abhängigkeitsgraph

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

eigenständig (nur Aspire.Hosting.AppHost):
  Nextended.Aspire.Hosting.WebDataStudio
  Nextended.Aspire.Hosting.AspireUI
  Nextended.Aspire.Hosting.Php

nur zur Buildzeit:
  Nextended.CodeGen  (verwendet die Attribute aus Nextended.Core)
```

## Welches Paket brauche ich?

| Ich möchte … | Paket |
| --- | --- |
| Objekte ohne Mapper-Bibliothek abbilden | [Nextended.Core](/de/projects/core) |
| Mit Geldbeträgen, reinen Datumswerten oder typisierten IDs arbeiten | [Nextended.Core](/de/projects/core) |
| Eine facettierte Suche über `IQueryable<T>` bauen | [Nextended.Core](/de/projects/core) |
| Einen Methodenaufruf cachen, ohne einen Cache-Key zu erfinden | [Nextended.Cache](/de/projects/cache) |
| Einen EF-Core-Graphen laden, ohne `Include`-Ketten zu schreiben | [Nextended.EF](/de/projects/ef) |
| Nach einem vom Client gelieferten String sortieren und blättern | [Nextended.EF](/de/projects/ef) |
| OData anbieten, ohne ein EDM-Modell zu schreiben | [Nextended.Web](/de/projects/web) |
| Arbeit ausführen, nachdem die Antwort schon draußen ist | [Nextended.Web](/de/projects/web) |
| Antwortfelder pro Benutzer verbergen, maskieren oder umbenennen | [Nextended.ResponseFilters](/de/projects/responsefilters) |
| Genau das in einer MVC-Anwendung | [Nextended.ResponseFilters.AspNetCore](/de/projects/responsefilters-aspnetcore) |
| Benutzer in einem hochgeladenen ZIP navigieren lassen (Blazor) | [Nextended.Blazor](/de/projects/blazor) |
| Globale Tastenkürzel und Gamepads in einer WPF-Anwendung | [Nextended.UI](/de/projects/ui) |
| Bilder unter Windows skalieren, zuschneiden oder prüfen | [Nextended.Imaging](/de/projects/imaging) |
| DTOs erzeugen oder Klassen aus JSON/XML/Excel | [Nextended.CodeGen](/de/projects/codegen) |
| `if`-Verzweigungen aus meinem Aspire-AppHost entfernen | [Nextended.Aspire](/de/projects/aspire) |
| Supabase, n8n, Grafana, PHP oder LocalAI in Aspire betreiben | das passende `Nextended.Aspire.Hosting.*`-Paket |

## Hilfe

- [Typische Anwendungsfälle](/de/examples/common-use-cases)
- [API-Referenz](/api/extensions) *(englisch)*
- [Installationsleitfaden](/de/guides/installation)
- [GitHub Issues](https://github.com/fgilde/Nextended/issues)
