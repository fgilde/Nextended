---
title: Nextended.Cache
description: Ausdrucksbasiertes Caching — Cache-Keys entstehen aus dem Aufrufausdruck, CacheProvider mit bedingter Invalidierung, thread-sicheres AddOrGetExisting.
---

# Nextended.Cache

📚 **[Vollständige API-Referenz](/de/projects/cache-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/cache)

Ausdrucksbasiertes Caching: Der Cache-Key entsteht aus dem Aufruf selbst, nicht aus einer von Hand
getippten Zeichenkette.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Cache.svg)](https://www.nuget.org/packages/Nextended.Cache/)

## Installation

```bash
dotnet add package Nextended.Cache
```

## Der Gedanke

Cache-Code besteht meistens aus denselben vier Zeilen: Key bauen, nachsehen, teure Operation
ausführen, ablegen. Der Key ist die Stelle, an der es schiefgeht — er wird von Hand geschrieben,
vergisst einen Parameter, und zwei Aufrufstellen sind sich uneinig.

`Nextended.Cache` leitet den Key stattdessen aus dem **Aufrufausdruck** ab. Sie übergeben den
Aufruf, den Sie sonst gemacht hätten; der Key entsteht aus deklarierendem Typ, Ergebnistyp,
Methodenname und den tatsächlichen Argumentwerten.

## Einen Methodenaufruf cachen

```csharp
using Nextended.Cache;

public class UserService
{
    private readonly CacheProvider _cache = new();

    public User GetUser(int userId)
        // Das Lambda erhält die Instanz, damit der Ausdruck auswertbar ist.
        => _cache.ExecuteWithCache(this, self => self.LoadUserFromDb(userId));

    private User LoadUserFromDb(int userId) => /* der teure Aufruf */;
}
```

Der erzeugte Key sieht so aus:

```
MyApp.UserService_MyApp.User=UserService.LoadUserFromDb(userId=42)
```

Eine andere `userId` ist damit automatisch ein anderer Eintrag.

::: warning Der Lambda-Rumpf muss ein Methodenaufruf sein
`self => self.Property` oder ein Closure ohne Aufruf (`() => Load(id)`) lässt sich nicht in einen
Key übersetzen und wirft eine `InvalidCastException`.
:::

## Direkt auf `IMemoryCache`

```csharp
using Nextended.Cache.Extensions;

var info = this.ExecuteWithCache(memoryCache, self => self.LoadReport(reportId));

info.Result;      // der Wert
info.Key;         // der erzeugte Key
info.IsNewEntry;  // false, wenn er aus dem Cache kam
```

`ExecuteWithCache` nimmt auch einen expliziten Key, wenn Sie einen wollen, sowie
`MemoryCacheEntryOptions` für Ablauf und Priorität. `cache.MemoryCacheEntryOptions(token)` baut
einen sinnvollen Standard: normale Priorität, eine Stunde absoluter Ablauf, gebunden an ein
Cancellation-Token.

## Bedingte Invalidierung

`CacheProvider` kann sich selbst beobachten und alles verwerfen, sobald eine Bedingung zutrifft.
Die Bedingungen werden in einem Hintergrund-Task alle `ClearCheckInterval` ausgewertet
(Standard: 10 Minuten).

```csharp
var cache = new CacheProvider();
cache.ClearCheckInterval = TimeSpan.FromMinutes(1);

cache.ClearWhen(c => (DateTime.Now - c.LastWriteTime).TotalHours > 1)
     .ClearWhen(c => c.Count() > 10_000);

cache.Cleared += (_, _) => logger.LogInformation("Cache verworfen");
```

`Clear()` arbeitet über ein Ablauf-Token, gegen das jeder Eintrag registriert ist. Ein Verwerfen
ist damit O(1) und nicht eine Enumeration über alle Einträge.

## Thread-sichere Initialisierung für `ObjectCache`

Beim klassischen `AddOrGetExisting` von `System.Runtime.Caching` läuft Ihre Factory **bereits**,
bevor Sie den vorhandenen Eintrag zurückbekommen. Die Variante hier verpackt die Factory in ein
`Lazy<T>`, sodass der teure Aufruf auch unter gleichzeitigem Zugriff genau einmal erfolgt.

```csharp
using System.Runtime.Caching;
using Nextended.Cache.Extensions;

var rates = MemoryCache.Default.AddOrGetExisting(
    "exchange-rates",
    () => LoadExchangeRates(),
    DateTimeOffset.Now.AddMinutes(10));
```

## Konfiguration

```csharp
var cache = new CacheProvider(
    memoryCache,
    new MemoryCacheEntryOptions()
        .SetPriority(CacheItemPriority.High)
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15)));
```

Alternativ mit `IOptions<MemoryCacheOptions>`; ohne Argument legt der Provider einen eigenen
`MemoryCache` an.

| Member | Zweck |
| --- | --- |
| `CacheEntryOptions` | Standardoptionen für alles, was dieser Provider ablegt |
| `ClearCheckInterval` | Wie oft die `ClearWhen`-Bedingungen geprüft werden |
| `LastWriteTime` | Zeitstempel des letzten neuen Eintrags — die übliche Grundlage für eine `ClearWhen`-Bedingung |
| `Count()` | Aktuelle Anzahl der Einträge |
| `Clear()` | Alles verwerfen und `Cleared` auslösen |
| `Cleared` | Ereignis nach dem Verwerfen |

## Praxishinweise

**Nur cachen, was teuer ist.** Der Key-Aufbau kostet selbst etwas Reflection über den Ausdruck; für
einen Aufruf im Mikrosekundenbereich lohnt das nicht.

**Ablaufzeit zur Datenlage passend wählen.** Der Standard von einer Stunde ist für Stammdaten
sinnvoll, für einen Warenkorb nicht.

**Keine veränderlichen Objekte teilen.** Was im Cache liegt, wird von allen Aufrufern geteilt.
Wenn Aufrufer das Ergebnis verändern, legen Sie eine Kopie ab — etwa mit
`CloneDeep()` aus [Nextended.Core](/de/projects/core).

**Bedingungen billig halten.** `ClearWhen`-Prädikate laufen periodisch im Hintergrund; sie sollten
keine Datenbank befragen.

## Zusammenspiel

[Nextended.Imaging](/de/projects/imaging) hängt von diesem Paket ab, sodass sich
Thumbnail-Erzeugung direkt cachen lässt:

```csharp
var bytes = _cache.ExecuteWithCache(this, self => self.BuildThumbnail(imageId, 200));
```

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Plattform

Plattformübergreifend.

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- `Microsoft.Extensions.Caching.Abstractions`
- `Microsoft.Extensions.Caching.Memory`
- `System.Runtime.Caching`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Cache/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Cache)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.Cache/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
