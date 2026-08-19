---
title: Nextended.Imaging
description: Bildverarbeitung — seitenverhältnistreues Skalieren, Zuschneiden, Farbersetzung, Thumbnails, Byte- und Data-URL-Konvertierung und MIME-Erkennung über Magic Bytes.
---

# Nextended.Imaging

📚 **[Vollständige API-Referenz](/de/projects/imaging-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/imaging)

Bildverarbeitung: seitenverhältnistreues Skalieren, Zuschneiden, Farbersetzung,
helligkeitsbasierte Vordergrundfarbwahl, Thumbnails, Konvertierung nach Bytes und Data-URL sowie
MIME-Erkennung über die Magic Bytes.

[![NuGet](https://img.shields.io/nuget/v/Nextended.Imaging.svg)](https://www.nuget.org/packages/Nextended.Imaging/)

## Installation

```bash
dotnet add package Nextended.Imaging
```

::: warning Nur Windows zur Laufzeit
Das Paket baut auf `System.Drawing.Common` auf. Microsoft unterstützt das ab .NET 7 **nur unter
Windows**; unter Linux und macOS werfen die `System.Drawing`-Typen eine
`PlatformNotSupportedException`. Für plattformübergreifende Bildverarbeitung nehmen Sie ImageSharp
oder SkiaSharp. Zielgruppe dieses Pakets sind Windows-Dienste, Desktop-Anwendungen und unter
Windows gehostete Web-Anwendungen.
:::

## Übersicht

| Bereich | API |
| --- | --- |
| **Laden** | `FromFile`, `FromFileAsync`, `FromUrl`, `FromByteArray`, `FromHtmlImageString`, `LoadCloneFromFile`, `CloneImage` |
| **Skalieren** | `ResizeImage(size)`, `ResizeImage(size, ResizeMode)`, `ResizeImage(size, useSizeAsHeight)`, `CalculateResize(imageSize, boxSize)` |
| **Zuschneiden & Farbe** | `CropBitmap`, `ReplaceColor`, `ChangeColor`, `ReadImageColor`, `GetColor` |
| **Kontrast** (`Miscellaneous`) | `GetWeightedBrightness`, `GetOptimalForegroundColor` |
| **Thumbnails** | `GetImageThumbnailData(image, size, useSizeAsHeight)`, `GetMinSizedImageThumbnailData(image, size, …)` |
| **Konvertierung** | `ConvertImageToByteArray`, `ToByteArray`, `ToHtml`, `ToHtmlImageString` |
| **Prüfen** | `GetDimensions`, `IsValidImage` (Bytes, Pfad oder `BinaryReader`), `GetMimeType`, `GetContentType`, `ParseSize`, `ImageSize` |
| **MIME-Erkennung** (`Miscellaneous`) | `GetMimeFromBytes` — liest die Magic Bytes statt der Dateiendung zu vertrauen |
| **Interop** (`Miscellaneous`) | `FromDelphiColor`, `ToDelphiColor` für alte Delphi-Farbwerte |

## In einen Rahmen einpassen, ohne zu verzerren

```csharp
using Nextended.Imaging;

using var image = ImageHelper.FromFile(@"C:\uploads\foto.jpg");

// CalculateResize ermittelt die größte Größe, die verzerrungsfrei in den Rahmen passt.
var target = ImageHelper.CalculateResize(image.Size, new Size(800, 600));
using var resized = image.ResizeImage(target);

resized.Save(@"C:\out\foto-800.jpg", ImageFormat.Jpeg);
```

Mit einem `ResizeMode` steuern Sie Zuschneiden gegenüber Letterboxing. Für eine Dimension gibt es
die Kurzform:

```csharp
using var thumb = ImageHelper.ResizeImage(image, 400);                        // 400 px breit
using var tall  = ImageHelper.ResizeImage(image, 400, useSizeAsHeight: true); // 400 px hoch
```

## Thumbnails als Bytes

```csharp
byte[] thumbnail = ImageHelper.GetImageThumbnailData(image, 200);

// Oder: in keiner Dimension unter ein Minimum gehen
byte[] minSized = ImageHelper.GetMinSizedImageThumbnailData(image, new Size(120, 120));
```

## Den echten Typ erkennen, nicht die Dateiendung glauben

```csharp
string mime = Miscellaneous.GetMimeFromBytes(uploadedBytes);   // liest die Magic Bytes
bool ok     = ImageHelper.IsValidImage(uploadedBytes);
```

Eine in `.png` umbenannte ausführbare Datei täuscht `GetMimeFromBytes` nicht — die Methode prüft
die Signatur im Dateikopf. Als Upload-Schranke gut geeignet.

`GetDimensions` liest Breite und Höhe aus dem Header, ohne das ganze Bild zu dekodieren; es gibt
eine Überladung für einen `BinaryReader`, sodass Sie einen Stream nur bis zum Header lesen müssen.

## Lesbare Schrift über einem beliebigen Bild

```csharp
var background = ImageHelper.FromFile(pfad).ReadImageColor();          // dominante Farbe
var foreground = Miscellaneous.GetOptimalForegroundColor(background);  // Schwarz oder Weiß

int brightness = background.GetWeightedBrightness();  // perzeptuell, kein einfacher Mittelwert
```

`GetWeightedBrightness` gewichtet die Kanäle nach ihrem Beitrag zur wahrgenommenen Helligkeit —
Grün zählt deutlich mehr als Blau. Ein reiner Mittelwert würde bei gesättigten Farben die falsche
Schriftfarbe wählen.

## Farben ersetzen

```csharp
using var recoloured = ImageHelper.ReplaceColor(bitmap, Color.Magenta, Color.Transparent);
```

Das klassische „Magenta bedeutet transparent" bei Sprite-Sheets, ohne Pixelschleife von Hand. Es
gibt Überladungen für einen Dateipfad, ein `Image` und eine `Bitmap`; die Pfad-Variante nimmt
optional einen Cache-Namen.

## In HTML einbetten

```csharp
string tag = ImageHelper.ToHtmlImageString(image);   // <img src="data:image/png;base64,…">
```

`FromHtmlImageString` ist die Umkehrung, ein Data-URL-Rundlauf braucht also keine
Base64-Handarbeit.

## Thumbnails cachen

Das Paket hängt von [Nextended.Cache](/de/projects/cache) ab, die Erzeugung lässt sich also direkt
mit ausdrucksbasiertem Caching kombinieren:

```csharp
var bytes = _cache.ExecuteWithCache(this, self => self.BuildThumbnail(imageId, 200));
```

Der Cache-Key enthält die Bild-ID und die Größe, unterschiedliche Größen kollidieren also nicht.

## Ressourcen freigeben

`Image` und `Bitmap` sind `IDisposable` und halten nicht verwalteten Speicher. Jedes von einer
`ImageHelper`-Methode zurückgegebene Bild gehört Ihnen — verwenden Sie `using`. In einer
Web-Anwendung, die viele Uploads verarbeitet, ist ein vergessenes `Dispose` die häufigste Ursache
für stetig wachsenden Speicherverbrauch.

## Unterstützte Frameworks

- `net8.0`
- `net9.0`
- `net10.0`

## Plattform

Windows (siehe Warnung oben).

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- [Nextended.Cache](/de/projects/cache)
- `System.Drawing.Common`
- `MediaTypeMap.Core`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.Imaging/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.Imaging)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.Imaging/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
