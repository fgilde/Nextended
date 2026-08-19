---
layout: default
title: Projekte
parent: Deutsch
nav_order: 1
has_children: true
permalink: /de/projects
---

# Nextended-Pakete
{: .no_toc }

🇬🇧 [This page in English](../../projects/README.md)

Alle Pakete der Nextended-Sammlung mit Zweck, ausführbarem Beispiel und Dokumentationsseite.
{: .fs-5 .fw-300 }

{: .note }
> Diese Übersicht wird beim Build aus
> [`docs/_data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/_data/packages.json)
> erzeugt — der einzigen Quelle der Wahrheit für jede Paketliste in diesem Repository. Wer dort ein
> Paket ergänzt, aktualisiert damit diese Seite, das englische Gegenstück, das Root-README und alle
> Paket-READMEs (letztere über `pwsh tools/Update-PackageDocs.ps1`).

## Inhalt
{: .no_toc .text-delta }

1. TOC
{:toc}

---

{% assign pkgs = site.data.packages.packages %}
{% assign cats = site.data.packages.categories %}
{% assign repo = site.data.packages.meta.repo %}

{% for cat in cats %}
## {{ cat.de }}

{% for pkg in pkgs %}{% if pkg.category == cat.id %}
### [{{ pkg.name }}]({{ pkg.slug }}.html)

{{ pkg.summary.de }}

| | |
| --- | --- |
| **NuGet** | [{{ pkg.name }}](https://www.nuget.org/packages/{{ pkg.name }}/) |
| **Installation** | `dotnet add package {{ pkg.name }}` |
| **Frameworks** | {% for fw in pkg.frameworks %}`{{ fw }}`{% unless forloop.last %}, {% endunless %}{% endfor %} |
| **Plattform** | {{ pkg.platform }} |
| **Abhängigkeiten** | {% if pkg.dependencies.size == 0 %}keine{% else %}{% for dep in pkg.dependencies %}{{ dep }}{% unless forloop.last %}, {% endunless %}{% endfor %}{% endif %} |
| **Quellcode** | [{{ pkg.name }}]({{ repo }}/tree/main/{{ pkg.name }}) |{% if pkg.sample %}
| **Ausführbares Beispiel** | [{{ pkg.sample | split: "/" | last }}]({{ repo }}/tree/main/{{ pkg.sample }}) |{% endif %}
| **Dokumentation** | [Deutsch]({{ pkg.slug }}.html) · [English]({{ repo }}/blob/main/docs/projects/{{ pkg.slug }}.md) |

{% endif %}{% endfor %}
{% endfor %}

---

## Referenzmatrix

| Paket | Frameworks | Plattform | Abhängigkeiten |
| --- | --- | --- | --- |
{% for pkg in pkgs %}| [{{ pkg.name }}]({{ pkg.slug }}.html) | {% for fw in pkg.frameworks %}`{{ fw }}`{% unless forloop.last %}, {% endunless %}{% endfor %} | {{ pkg.platform }} | {% if pkg.dependencies.size == 0 %}keine{% else %}{% for dep in pkg.dependencies %}{{ dep }}{% unless forloop.last %}, {% endunless %}{% endfor %}{% endif %} |
{% endfor %}

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
| Objekte ohne Mapper-Bibliothek abbilden | [Nextended.Core](core.html) |
| Mit Geldbeträgen, reinen Datumswerten oder typisierten IDs arbeiten | [Nextended.Core](core.html) |
| Eine facettierte Suche über `IQueryable<T>` bauen | [Nextended.Core](core.html) |
| Einen Methodenaufruf cachen, ohne einen Cache-Key zu erfinden | [Nextended.Cache](cache.html) |
| Einen EF-Core-Graphen laden, ohne `Include`-Ketten zu schreiben | [Nextended.EF](ef.html) |
| Nach einem vom Client gelieferten String sortieren und blättern | [Nextended.EF](ef.html) |
| OData anbieten, ohne ein EDM-Modell zu schreiben | [Nextended.Web](web.html) |
| Arbeit ausführen, nachdem die Antwort schon draußen ist | [Nextended.Web](web.html) |
| Antwortfelder pro Benutzer verbergen, maskieren oder umbenennen | [Nextended.ResponseFilters](responsefilters.html) |
| Genau das in einer MVC-Anwendung | [Nextended.ResponseFilters.AspNetCore](responsefilters-aspnetcore.html) |
| Benutzer in einem hochgeladenen ZIP navigieren lassen (Blazor) | [Nextended.Blazor](blazor.html) |
| Globale Tastenkürzel und Gamepads in einer WPF-Anwendung | [Nextended.UI](ui.html) |
| Bilder unter Windows skalieren, zuschneiden oder prüfen | [Nextended.Imaging](imaging.html) |
| DTOs erzeugen oder Klassen aus JSON/XML/Excel | [Nextended.CodeGen](codegen.html) |
| `if`-Verzweigungen aus meinem Aspire-AppHost entfernen | [Nextended.Aspire](aspire.html) |
| Supabase, n8n, Grafana, PHP oder LocalAI in Aspire betreiben | das passende `Nextended.Aspire.Hosting.*`-Paket |

## Hilfe

- [Typische Anwendungsfälle](../examples/common-use-cases.md)
- [API-Referenz](../../api/extensions.md) *(englisch)*
- [Installationsleitfaden](../guides/installation.md)
- [GitHub Issues](https://github.com/fgilde/Nextended/issues)
