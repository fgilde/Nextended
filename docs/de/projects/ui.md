---
title: Nextended.UI
description: WPF- und Windows-Desktop-Helfer — globaler Input-Binding-Manager, Gamepad-Reader, Steuerelemente zum Erfassen von Tastenkombinationen, Converter, Behaviours und Laufzeittypen für den PropertyGrid.
---

# Nextended.UI

📚 **[Vollständige API-Referenz](/de/projects/ui-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/ui)

Helfer für WPF und Windows-Desktop: ein globaler Input-Binding-Manager mit Hold- und
Sequenz-Erkennung, DirectInput- und XInput-Gamepads, Steuerelemente zum Erfassen von
Tastenkombinationen, Converter, Behaviours, Markup-Extensions und zur Laufzeit definierte Typen für
den `PropertyGrid`.

[![NuGet](https://img.shields.io/nuget/v/Nextended.UI.svg)](https://www.nuget.org/packages/Nextended.UI/)

## Installation

```bash
dotnet add package Nextended.UI
```

::: warning Nur Windows
Die Zielframeworks tragen den `-windows`-Suffix, das Paket referenziert WPF,
`Microsoft.Xaml.Behaviors.Wpf`, `MouseKeyHook` und SharpDX. Außerhalb von Windows lässt es sich
nicht kompilieren.
:::

## Übersicht

| Bereich | API |
| --- | --- |
| **Input-Bindings** | `InputBindingManager`, `StoredInputBinding`, `InputSequenceMatcher`, `KeyDisplayName`, `KeyLocalizer` |
| **Gamepads** | `IGamepadReader`, `XInputGamepadReader`, `DirectInputGamepadReader`, `GamepadEventArgs` |
| **Erfassungs-Steuerelemente** | `KeyBindChanger`, `KeyBindSequenceBox`, `KeyBindChangedEventArgs` |
| **WPF-Infrastruktur** | `ViewUtility`, Behaviours, Converter (`UIElementToImageConverter`, …), Markup-Extensions (`StaticImage`, …) |
| **Laufzeittypen** | `CustomClass`, `DynamicProperty` — einen Typ zur Laufzeit für den `PropertyGrid` bauen |
| **PropertyGrid** | `PropertyGridSearchExtenter`, `PropertyGridTypeEditor` |
| **ViewModels** | `ItemFilterModel` |
| **Theming** | Mitgelieferte WPF-Theme-Dictionaries unter `Theming/Themes` |
| **Shell** | `FileDescription`, `ExtractIconFromFile`, `Margins` |

## Globale Tastenkürzel, auch Halten und Sequenzen

Bindings werden unter einer **String-ID** registriert, und Sie reagieren auf diese ID. Damit lässt
sich das Kürzel zur Laufzeit neu belegen, ohne den Handler anzufassen.

```csharp
using System.Windows.Forms;   // Keys
using Nextended.UI.Input;

using var manager = new InputBindingManager();

// Einzelne Taste oder Maustaste — implizite Konvertierungen aus Keys, MouseButtons,
// GamepadButton und GamepadSlider machen einen Wrapper-Aufruf überflüssig.
manager.RegisterBinding("emergency-stop", Keys.Escape);

// Eine Kombination
manager.RegisterBinding("command-palette",
    StoredInputBinding.Combo([Keys.ControlKey, Keys.ShiftKey, Keys.P]));

// Muss gehalten werden, bevor es zählt — MinTime in Millisekunden
manager.RegisterBinding("force-quit",
    StoredInputBinding.Combo([Keys.Alt, Keys.F4]).SetMinTime(750));

manager.OnBindingPressed  += id => Handle(id);
manager.OnBindingReleased += id => Release(id);
manager.OnKeyPressed      += binding => Log(binding);
manager.OnMouseMove       += e => Track(e);
```

Der Manager sitzt auf einem Low-Level-Hook, die Bindings greifen also auch, wenn Ihr Fenster nicht
den Fokus hat. `InputBindingManager.Instance` gibt die zuletzt erzeugte Instanz heraus,
`Bindings` das aktuelle Wörterbuch.

Zustand direkt abfragen statt auf ein Ereignis zu reagieren:

```csharp
if (InputBindingManager.IsHoldingBinding("emergency-stop")) { … }

TimeSpan held = InputBindingManager.GetHoldingTime("emergency-stop");

if (InputBindingManager.IsHoldingBindingFor("force-quit", TimeSpan.FromSeconds(2))) { … }
```

`binding.Flatten()` zerlegt eine Kombination in ihre Bestandteile — praktisch, um ein Kürzel in der
Oberfläche darzustellen. `WithoutMinTime()` liefert eine Kopie ohne Halte-Anforderung.
`InputSequenceMatcher` erkennt geordnete Sequenzen (die Konami-Code-Form) statt gleichzeitiger
Kombinationen.

## Den Benutzer ein Kürzel neu belegen lassen

`StartListeningForBinding` versetzt den Manager in den Aufnahmemodus: Die nächste Eingabe wird zur
neuen Belegung dieser ID und über `OnBindingSet` gemeldet.

```csharp
manager.OnBindingSet += (id, binding) => settings.Save(id, binding);
manager.StartListeningForBinding("command-palette");
manager.StopListening();
```

Die mitgelieferten Steuerelemente kapseln diesen Ablauf:

```xml
<nx:KeyBindChanger x:Name="Changer" />
<nx:KeyBindSequenceBox x:Name="SequenceBox" />
```

```csharp
Changer.KeyBindChanged += (_, e) => settings.Save(e.NewValue);   // e.OldValue gibt es auch
Changer.KeyDeleted     += (_, _) => settings.Clear();
Changer.BindingPressed += (_, _) => Flash();
SequenceBox.Changed    += (_, sequence) => settings.SaveSequence(sequence);
```

`KeyDisplayName` und `KeyLocalizer` übersetzen eine Taste in die Beschriftung, die der Benutzer
erwartet — `OemQuestion` erscheint also als die Taste, die auf seinem Tastaturlayout wirklich
aufgedruckt ist.

## Gamepads

```csharp
using Nextended.UI.Input.Gamepad;

using IGamepadReader pad = new XInputGamepadReader();   // oder DirectInputGamepadReader

pad.ButtonEvent += (_, e) =>
{
    e.Button;         // Rohcode
    e.GamepadButton;  // geparstes Enum, null wenn es kein Tastenereignis ist
    e.GamepadSlider;
    e.GamepadAxis;
    e.IsPressed;      // null bei Achsen- und Slider-Ereignissen
    e.Value;          // Analogwert bei Slidern und Sticks
    e.IsStickEvent;
};
```

`XInputGamepadReader` deckt Controller im Xbox-Stil ab, `DirectInputGamepadReader` die übrigen
Geräte, die XInput nicht aufzählt. Übergeben Sie einen Reader mit
`manager.AttachGamepadReader(pad)`, dann werden Gamepad-Tasten zu Bindings wie jede andere Taste.
Über `new InputBindingManager(enableGamepad: false)` schalten Sie das ab.

## Ein PropertyGrid über Daten ohne Klasse

```csharp
using Nextended.UI.Classes;

var custom = new CustomClass();
custom.AddProperty<string>("Host");
custom.AddProperty<int>("Port");

propertyGrid.SelectedObject = custom;
```

`CustomClass` implementiert `ICustomTypeDescriptor`. Der `PropertyGrid` sieht damit echte
Eigenschaften, obwohl die Form erst zur Laufzeit feststand — die übliche Antwort auf „die
Einstellungen kommen aus einer Konfigurationsdatei und ich will dafür keine Klasse generieren".

`PropertyGridSearchExtenter` ergänzt eine Suche über die angezeigten Eigenschaften,
`PropertyGridTypeEditor` eigene Editoren pro Typ.

## Ein Steuerelement als Bild

```csharp
var converter = new UIElementToImageConverter();
var bitmap = converter.Convert(myControl, typeof(BitmapSource), null, CultureInfo.CurrentCulture);
```

## Shell-Informationen

```csharp
var icon = FileDescription.ExtractIconFromFile(@"C:\Windows\explorer.exe");
var info = new FileDescription(path);   // Anzeigename, Typbeschreibung, Icon
```

## Unterstützte Frameworks

- `net8.0-windows`
- `net9.0-windows`
- `net10.0-windows`

## Plattform

Windows.

## Abhängigkeiten

- [Nextended.Core](/de/projects/core)
- `Microsoft.Xaml.Behaviors.Wpf`
- `MouseKeyHook`
- `SharpDX`, `SharpDX.DirectInput`, `SharpDX.XInput`

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.UI/)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.UI)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.UI/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)
