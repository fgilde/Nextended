---
title: Typische Anwendungsfälle
---
# Typische Anwendungsfälle

🇬🇧 [This page in English](/examples/common-use-cases.md)

Aufgabenorientierte Beispiele quer über die Pakete. Alle gezeigten Methodennamen sind gegen den
Quellcode geprüft.


## Objekte abbilden, ohne einen Mapper einzurichten

```csharp
using Nextended.Core.Extensions;

// Konventionsbasiert, ohne Konfiguration
var dto = user.MapTo<UserDto>();

// Ganze Sequenzen
var dtos = users.MapElementsTo<UserDto>();

// Asynchron
var async = await user.MapToAsync<UserDto>();
```

Wenn die Namen nicht übereinstimmen oder Felder ausgelassen werden sollen:

```csharp
var settings = ClassMappingSettings.Default
    .AddAssignment<User, UserDto>(u => u.EmailAddress, d => d.Mail)
    .IgnoreProperties<User>(u => u.Password)
    .AddConverter<string, DateTime>(DateTime.Parse);

var mapped = user.MapTo<UserDto>(settings);

// Einzelne Einstellungen inline setzen
var lenient = ClassMappingSettings.Default.Set(s => s.IgnoreExceptions = true);
```

## Ein Objekt tief kopieren

```csharp
using Nextended.Core.DeepClone;

var copy = order.CloneDeep();
```

Referenzen bleiben erhalten, Zyklen sind kein Problem. Was nie mitkopiert werden soll, markieren Sie
mit `[ClonerIgnore]`.

> Die Methode heißt `CloneDeep()`, nicht `DeepClone()`. Ältere Fassungen dieser Dokumentation nannten
> Methoden, die es nicht gibt — siehe die Hinweise weiter unten.

## Mit Geld und reinen Datumswerten arbeiten

```csharp
using Nextended.Core.Types;

var price = new Money(99.99m, Currency.USD);
var due   = Date.Today.AddDays(30);   // Datum ohne Zeitanteil
var span  = new Range<int>(1, 10);
```

`Money` hält Betrag und Währung zusammen und behält die Dezimalgenauigkeit. `Date` beseitigt die
ganze Fehlerklasse „welche Mitternacht in welcher Zeitzone" bei reinen Datumswerten.

## Zeichenketten und Datumsangaben umformen

```csharp
using Nextended.Core.Extensions;

"hello world".ToPascalCase();        // "HelloWorld"
"hello world".ToCamel();             // "helloWorld"
"MyClassName".SplitByUpperCase();    // "My Class Name"
"ein langer Satz".ToEllipsis(8);     // "ein lang…"
"user@example.com".IsEmailAddress(); // true
"abc".EnsureEndsWith("/");           // "abc/"

DateTime.Today.AddWeekDays(5);       // überspringt Wochenenden
DateTime.Today.IsWeekend();
DateTime.Today.FirstDayOfMonth();
DateTime.Today.LastDayOfMonth();
DateTime.UtcNow.ToISOz();
DateTime.UtcNow.ToUnixTimeStamp();
```

> **Nicht vorhanden:** `ToCamelCase()`, `ToSnakeCase()`, `ToKebabCase()`, `DeepClone()`,
> `AddBusinessDays()`, `IsBusinessDay()`. Diese Namen tauchten in älteren Dokumentationsfassungen auf,
> existieren im Code aber nicht. Richtig sind `ToCamel()`, `CloneDeep()`, `AddWeekDays()` und
> `IsWeekend()` / `IsWeekday()`.

## Einen teuren Methodenaufruf zwischenspeichern

```csharp
using Nextended.Cache;

public class UserService
{
    private readonly CacheProvider _cache = new();

    public User GetUser(int userId)
        => _cache.ExecuteWithCache(this, self => self.LoadUserFromDb(userId));
}
```

Der Cache-Key entsteht aus dem Aufrufausdruck — Typ, Methodenname und die tatsächlichen
Argumentwerte. Ein anderer `userId` ist automatisch ein anderer Eintrag. Der Lambda-Rumpf **muss** ein
Methodenaufruf sein.

Invalidierung nach Bedingung:

```csharp
_cache.ClearWhen(c => (DateTime.Now - c.LastWriteTime).TotalHours > 1)
      .ClearWhen(c => c.Count() > 10_000);
```

## Einen EF-Core-Graphen laden, ohne Include-Ketten

```csharp
using Nextended.EF;

// Navigationen ab einer geladenen Entität, tiefenbegrenzt und zyklensicher
var user = await db.Users.FindAsync(id);
await db.LoadGraphAsync(user, maxDepth: 2);

// Oder gleich alles, abzüglich einzelner Pfade
var products = await db.Products
    .IncludeAll(db, new[] { "Orders.OrderItems" })
    .ToListAsync();
```

## Blättern und nach einem Client-String sortieren

```csharp
var page = await db.Products
    .WhereIf(!string.IsNullOrEmpty(search), p => p.Name.Contains(search))
    .OrderByMembers("Category.Name asc", "Price desc")
    .ToPagedResultAsync(pageIndex: 2, pageSize: 25);

page.Items;
page.TotalCount;
```

## Eine Abfrage bedingt zusammenbauen

```csharp
var query = db.Orders
    .AsNoTrackingIf(readOnly)
    .IncludeIf(withCustomer, o => o.Customer)
    .WhereIf(from.HasValue, o => o.Created >= from!.Value)
    .WhereIn(o => o.Status, allowedStates)
    .WhereBetween(o => o.Total, min, max);
```

Jeder `*If`-Helfer gibt die Abfrage unverändert zurück, wenn die Bedingung nicht zutrifft — keine
`if`-Kaskade, kein doppelter Abfrageaufbau.

## Antwortfelder pro Benutzer verbergen

```csharp
public class OrderFilter : ResponseFilter<OrderDto>
{
    public OrderFilter()
    {
        Nullify(x => x.TotalCost).When(NotInRole("Finance"));
        Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).When(NotInRole("Admin"));
        Remove(x => x.InternalRef).When(NotInRole("Internal"));
        Truncate(x => x.Notes).After(200, "…").Always();
    }

    private static SyncPredicate<OrderDto> NotInRole(string role) =>
        (_, ctx) => !ctx.Services.GetRequiredService<ICurrentUser>().IsInRole(role);
}
```

Registrierung:

```csharp
builder.Services.AddNextendedResponseFilters(assemblies: [typeof(OrderFilter).Assembly]);
```

Die vollständige Referenz: [Nextended.ResponseFilters](../projects/responsefilters.md).

## OData anbieten, ohne ein EDM-Modell zu schreiben

```csharp
[ProvideAsEdm("Products")]
public class Product { public Guid Id { get; set; } public string Name { get; set; } = ""; }
```

```csharp
builder.Services.AddODataAuto();
builder.Services.AddControllers().AddODataAuto(ProvidedAsEdm.GetEdmModel(), routePrefix: "odata");
```

```csharp
public class ProductsController(AppDbContext db) : GenericODataController<Product>
{
    protected override IQueryable<Product> Queryable() => db.Products;
}
```

Damit stehen `$filter`, `$orderby`, `$top`, `$skip`, `$expand` und `$search` bereit.

## Arbeit ausführen, nachdem die Antwort draußen ist

```csharp
await executor.ExecuteDetachedWithCapturedRequestAsync(
    timeout: TimeSpan.FromMinutes(10),
    action: async (services, ct) =>
    {
        var importer = services.GetRequiredService<IImporter>();
        await importer.RunAsync(ct);
    });
```

Die Arbeit läuft in einem **neuen** DI-Scope. Dass der `DbContext` des Requests verworfen wird, kann
sie also nicht mehr zerreißen.

## Einen Upload prüfen, statt der Dateiendung zu glauben

```csharp
using Nextended.Imaging;

string mime = Miscellaneous.GetMimeFromBytes(uploadedBytes);   // liest die Magic Bytes
bool ok     = ImageHelper.IsValidImage(uploadedBytes);
```

## Ein Bild seitenverhältnistreu einpassen

```csharp
using var image = ImageHelper.FromFile(path);

var target = ImageHelper.CalculateResize(image.Size, new Size(800, 600));
using var resized = image.ResizeImage(target);
```

## DTOs zur Kompilierzeit erzeugen

```csharp
using Nextended.Core.Attributes;

[AutoGenerateDto(Namespace = "MyApp.Contracts")]
public class Address : EntityBase
{
    public string Street { get; set; } = "";

    [IgnoreOnGeneration]
    public string InternalNote { get; set; } = "";
}
```

```csharp
AddressDto dto = address.ToDto();
Address back = dto.ToSource();
```

Ebenso lassen sich Klassen aus JSON, XML und Excel erzeugen sowie Dokumentation aus Quelldateien.
Siehe [Nextended.CodeGen](../projects/codegen.md) und das
[Beispielprojekt](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/CodeGenSample).

## Einen Aspire-AppHost ohne Verzweigungen schreiben

```csharp
var isLocal = builder.Environment.IsDevelopment();
var cache   = isLocal ? builder.AddRedis("cache") : null;

var api = builder.AddProject<Projects.Api>("api")
    .WithReferenceIf(cache)              // wirkungslos, wenn cache null ist
    .WaitForIf(isLocal, database)
    .WithEnvironments(smtpOptions)       // wird zu SmtpOptions__Host, … aufgefaltet
    .WithExplicitStartIf(!isLocal);
```

## Eine Laufzeit messen

```csharp
using Nextended.Core.Measurement;

var measured = await Measure.RunAsync(() => repository.LoadAllAsync());
Console.WriteLine($"{measured.Elapsed} für {measured.Result.Count} Zeilen");
```

## Links

- [Alle Pakete](/projects/)
- [Installation](../guides/installation.md)
- [API-Referenz](/api/extensions.md) *(englisch)*
