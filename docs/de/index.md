---
layout: home
hero:
  name: Nextended
  text: .NET-Bibliotheken, die nicht im Weg stehen
  tagline: >
    Extension Methods und eigene Typen, ausdrucksbasiertes Caching, Graph-Laden für EF Core,
    berechtigungsabhängiges Response-Shaping, ein Roslyn-Source-Generator und acht
    Hosting-Integrationen für .NET Aspire. Unabhängige Pakete auf einer kleinen Basis.
  image:
    src: /icon.png
    alt: Nextended
  actions:
    - theme: brand
      text: Einstieg
      link: /de/guides/installation
    - theme: alt
      text: Alle Pakete
      link: /de/projects/
    - theme: alt
      text: Quellcode
      link: https://github.com/fgilde/Nextended
features:
  - title: Objekte abbilden ohne Mapper
    details: >
      Konventionsbasiertes Mapping braucht keine Profilregistrierung. Explizite
      Zuordnungen, ausgelassene Member und Typkonverter lassen sich auf einem Settings-Objekt
      kombinieren, wenn die Konventionen nicht reichen.
    link: /api/class-mapping
    linkText: Class Mapping (en)
  - title: Typen, die ihre Bedeutung mitbringen
    details: >
      Money hält Betrag und Währung in Dezimalgenauigkeit zusammen. Date ist ein Datum ohne
      Zeitanteil und beseitigt damit die ganze Fehlerklasse „welche Mitternacht in welcher
      Zeitzone".
    link: /api/types
    linkText: Eigene Typen (en)
  - title: Cache-Keys, die niemand schreibt
    details: >
      Sie übergeben den Aufruf, den Sie sonst gemacht hätten; der Key entsteht aus Typ,
      Methodenname und den tatsächlichen Argumentwerten. Zwei Aufrufstellen können sich nicht
      mehr widersprechen.
    link: /de/projects/cache
    linkText: Caching
  - title: EF Core ohne Include-Ketten
    details: >
      Navigationen ab einer geladenen Entität durchlaufen, alles außer benannten Pfaden einbinden,
      oder eine wiederverwendbare Include-Definition einmal deklarieren und jeder Abfrage
      mitgeben.
    link: /de/projects/ef
    linkText: Entity Framework
  - title: Eine Antwort, viele Zielgruppen
    details: >
      Ein deklarativer Filter pro DTO schwärzt, maskiert, rundet, kürzt, hasht, dünnt aus und
      benennt sogar Schlüssel um — vor der Serialisierung, pro Request, pro Benutzer, pro
      Berechtigung.
    link: /de/projects/responsefilters
    linkText: Response-Filter
  - title: Vier Generatoren, eine Konfigurationsdatei
    details: >
      DTOs und Mapping-Erweiterungen aus Ihren Entities, typisierte Klassen aus JSON und XML,
      Lookup-Tabellen aus Excel und Dokumentation aus Quelldateien. Das Beispielprojekt checkt
      seine erzeugte Ausgabe mit ein.
    link: /de/projects/codegen
    linkText: Codegenerierung
  - title: Ein AppHost ohne if-Kaskaden
    details: >
      Jeder konditionale Builder-Aufruf führt seinen Schritt aus, wenn die Bedingung greift, und
      gibt den Builder sonst unverändert zurück. Die Kette bleibt eine Kette statt in Duplikate
      zu zerfallen.
    link: /de/projects/aspire
    linkText: .NET Aspire
  - title: Ganze Stacks als eine Ressource
    details: >
      Supabase, n8n, Grafana mit seinem Observability-Stack, ein Browser-Datenbankstudio, der
      visuelle AppHost-Builder, selbst gehostete multimodale KI und PHP-Endpunkte — jeweils mit
      ausführbarem Beispiel-AppHost.
    link: /de/projects/aspire-supabase
    linkText: Hosting-Integrationen
  - title: Nichts, wenn nichts zutrifft
    details: >
      Die Response-Pipeline analysiert den Typgraphen einer Antwort einmal und überspringt den
      gesamten Durchlauf, wenn kein registrierter Filter sie erreichen kann. Metadatenbasierte
      Auswahl löst zur Bauzeit auf, nicht pro Request.
    link: /de/projects/responsefilters#laufzeitverhalten
    linkText: Warum es günstig bleibt
---

<script setup>
import data from '@data/packages.json'

const cats = data.categories
const pkgs = data.packages
const repo = data.meta.repo
const base = '/Nextended/de/'

const byCat = (id) => pkgs.filter((p) => p.category === id)
const leaf = (path) => path.split('/').pop()
const samples = pkgs.filter((p) => p.sample)
</script>

## Die Pakete

::: info Aus einer Datei erzeugt
Diese Liste, die Seitenleiste und die Tabellen in jedem README stammen aus
[`docs/data/packages.json`](https://github.com/fgilde/Nextended/blob/main/docs/data/packages.json).
Ein dort ergänztes Paket aktualisiert alle davon — sie können also nicht auseinanderlaufen.
:::

<div v-for="cat in cats" :key="cat.id">
  <h3>{{ cat.de }}</h3>
  <table>
    <thead>
      <tr><th>Paket</th><th>Beschreibung</th><th>Beispiel</th></tr>
    </thead>
    <tbody>
      <tr v-for="p in byCat(cat.id)" :key="p.id">
        <td><a :href="base + 'projects/' + p.slug"><strong>{{ p.name }}</strong></a></td>
        <td>{{ p.summary.de }}</td>
        <td>
          <a v-if="p.sample" :href="repo + '/tree/main/' + p.sample">{{ leaf(p.sample) }}</a>
          <span v-else>—</span>
        </td>
      </tr>
    </tbody>
  </table>
</div>

Die [Projektübersicht](/de/projects/) enthält zusätzlich Zielframeworks, Plattformen,
Abhängigkeiten und den Abhängigkeitsgraphen.

## Schnellstart

```bash
dotnet add package Nextended.Core
```

```csharp
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Nextended.Core.DeepClone;

// Objekt-Mapping — ohne Profile, ohne Konfiguration
var userDto = user.MapTo<UserDto>();
var userDtos = users.MapElementsTo<UserDto>();

// Deep Clone, Referenzen bleiben erhalten
var copy = order.CloneDeep();

// Typen, die ihre Bedeutung mitbringen
var price = new Money(99.99m, Currency.USD);
var due   = Date.Today.AddDays(30);   // ein Datum, ohne Zeitanteil

// Extension Methods
"hello world".ToPascalCase();     // "HelloWorld"
"hello world".ToCamel();          // "helloWorld"
"MyClassName".SplitByUpperCase(); // "My Class Name"
DateTime.Today.AddWeekDays(5);    // überspringt Wochenenden
```

## Ausführbare Beispiele

<ul>
  <li v-for="p in samples" :key="p.id">
    <a :href="repo + '/tree/main/' + p.sample"><code>{{ leaf(p.sample) }}</code></a> — {{ p.name }}
  </li>
</ul>

## Sprachstand

Vollständig auf Deutsch: Startseite, Projektübersicht, Installation, Architektur, typische
Anwendungsfälle und die Referenz zu Response-Filtern. Die übrigen Paketseiten sind deutsche Seiten
mit Beschreibung, Installation, Frameworks, Plattform, Abhängigkeiten und Beispiel; für die
Tiefenreferenz verweisen sie auf die englische Fassung. Die API-Referenz ist noch englisch.

## Migration von nExt

Die Sammlung erschien früher als **nExt**. Die Namespaces heißen jetzt `Nextended.*`, die API ist
ansonsten quellkompatibel. Siehe den [Migrationsleitfaden](/guides/migration) *(englisch)*. Das
Altpaket [nExt.Core](https://www.nuget.org/packages/nExt.Core/) wird nicht mehr gepflegt.
