---
title: Nextended.Blazor — API-Referenz
---

# Nextended.Blazor — API-Referenz

🇬🇧 [This page in English](/projects/blazor-api)

Die vollständige öffentliche Oberfläche von `Nextended.Blazor`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/blazor)

## Nextended.Blazor

### `_Imports`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `_Imports()`

### `Component1`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `Component1()`

## Nextended.Blazor.Extensions

### `BrowserFileExtensions`

`static class`

_Keine Beschreibung._

### `TypeExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `IsEventCallback(this Type type) : bool`
- `IsRenderFragment(this Type type) : bool`

## Nextended.Blazor.Helper

### `ComponentRenderHelper`

`static class`

_Keine Beschreibung._

**Methoden**

- `GetCompatibleParameters<T>(T instance, Type targetCompatibleType) : IDictionary<string, object>`
- `GetCompatibleProperties<T>(T instance, Type targetCompatibleType) : IDictionary<string, object>`
- `IsValidParameter(Type componentType, string key, object value) : bool`
- `IsValidProperty(Type componentType, string key, object value, Type[] requiredAttributeTypes) : bool`
- `IsValidPropertyWithAttribute<TAttribute>(Type componentType, string key, object value) : bool`

### `MimeTypeHelper`

`static class`

_Keine Beschreibung._

**Methoden**

- `IsZip(string contentType) : bool`
- `Matches(string mimeType, string[] mimeTypes) : bool`

## Nextended.Blazor.Models

### `ArchiveStructure`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ArchiveStructure()`

### `ArchiveStructureBase<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ArchiveStructureBase()`
- `ArchiveStructureBase(IArchivedBrowserFile browserFile)`
- `ArchiveStructureBase(string name)`

**Methoden**

- `CreateStructure(IList<IArchivedBrowserFile> archiveEntries, string rootFolderName) : T`
- `ToArchiveBytesAsync(CancellationToken cancellationToken = null) : Task<byte[]>`

**Eigenschaften**

- `BrowserFile : IArchivedBrowserFile { get; set; }`
- `ContainingFiles : IEnumerable<IArchivedBrowserFile> { get; }`
- `IsDirectory : bool { get; }`
- `IsDownloading : bool { get; set; }`
- `Name : string { get; set; }`
- `Size : long { get; }`

### `DataUrl`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DataUrl(byte[] bytes, ContentType mimeType)`
- `DataUrl(byte[] bytes, string mimeType = null)`
- `DataUrl(string url)`

### `IArchivedBrowserFile`

`interface`

_Keine Beschreibung._

**Eigenschaften**

- `FileBytes : byte[] { get; }`
- `FullName : string { get; }`
- `IsDirectory : bool { get; }`
- `ParentDirectoryName : string { get; }`
- `Path : string { get; }`
- `PathArray : string[] { get; }`

### `ZipBrowserFile`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ZipBrowserFile(ZipArchiveEntry entry, bool load = true)`

**Methoden**

- `OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = null) : Stream`

**Eigenschaften**

- `ContentType : string { get; }`
- `Entry : ZipArchiveEntry { get; }`
- `FileBytes : byte[] { get; }`
- `FullName : string { get; }`
- `IsDirectory : bool { get; }`
- `LastModified : DateTimeOffset { get; }`
- `Name : string { get; set; }`
- `ParentDirectoryName : string { get; }`
- `Path : string { get; }`
- `PathArray : string[] { get; }`
- `Size : long { get; }`

### `ZipStructure`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ZipStructure()`

↩ [Zurück zur Paketseite](/de/projects/blazor)
