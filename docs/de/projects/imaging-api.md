---
title: Nextended.Imaging — API-Referenz
---

# Nextended.Imaging — API-Referenz

🇬🇧 [This page in English](/projects/imaging-api)

Die vollständige öffentliche Oberfläche von `Nextended.Imaging`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/imaging)

## Nextended.Imaging

### `ImageHelper`

`static class`

Hilfklasse für Bildveraerbeitung

**Extension Methods**

- `StartsWith(this byte[] thisBytes, byte[] thatBytes) : bool`

**Methoden**

- `CalculateResize(Size imageSize, Size boxSize) : Size`
  <br>Calculates the resize.
- `ConvertImageToByteArray(string fileName) : byte[]`
  <br>Converts the image to a byte array.
- `GetDimensions(BinaryReader binaryReader) : Size`
  <br>Gets the dimensions of an image.
- `GetDimensions(string path) : Size`
  <br>Gets the dimensions of an image.
- `IsValidImage(BinaryReader binaryReader) : bool`
  <br>Gets the dimensions of an image.
- `IsValidImage(byte[] bytes) : bool`
  <br>IsValid Image
- `IsValidImage(string path) : bool`
  <br>Gets the dimensions of an image.
- `ReadImageColor(string imagePath) : Color`
  <br>Farbe eines Bildes ermitteln

### `Miscellaneous`

`static class`

Miscellaneous

**Extension Methods**

- `GetMimeType(this FileInfo fileInfo) : string`
  <br>Gibt den Mime type der Datei zurück
- `GetWeightedBrightness(this Color color) : int`
  <br>returns a value for the decision whether the text should be black or white depending on the human eye's sensitivity to the underlying colour
- `ToDelphiColor(this Color foreColor) : int`
  <br>Konvertiert die Farbe zum Integer-Code, gemäß dem Delphi-Farben z.B. in Reportdefinitionen hinterlegt sind.
- `ToHtml(this Color color) : string`
  <br>Color to Hex

**Methoden**

- `FromDelphiColor(int color) : Color`
  <br>Konvertiert Delphi-Farbcode in WinForms-Color.
- `GetColor(string htmlColor) : Color`
  <br>Farbe zurückgeben
- `GetMimeFromBytes(byte[] data) : string`
  <br>Gibt den Mime type der eines byte Arrays zurück
- `GetMimeType(string fileName) : string`
  <br>/// Gibt den MimeType der Datei zurück
- `GetOptimalForegroundColor(Color backgroundColor) : Color`
  <br>Gibt die je nach hintergrundfarbe schwarz oder weiß zurück

**Felder**

- `DefaultMimeType : string`
  <br>DefaultMimeType
- `MimeSampleSize : int`
  <br>MimeSampleSize

### `ResizeMode`

`enum`

ResizeMode

**Werte**

- `KeepScale`
  <br>KeepScale
- `KeepScaleAndCut`
  <br>KeepScaleAndCut
- `Stretch`
  <br>StretchMode
- `value__`

↩ [Zurück zur Paketseite](/de/projects/imaging)
