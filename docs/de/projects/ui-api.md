---
title: Nextended.UI — API-Referenz
---

# Nextended.UI — API-Referenz

🇬🇧 [This page in English](/projects/ui-api)

Die vollständige öffentliche Oberfläche von `Nextended.UI`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/ui)

## Nextended.UI

### `GradientColorOption`

`enum`

GradientColorOption für GetColor

**Werte**

- `First`
  <br>Die erste Farbe im Gradient
- `Last`
  <br>Die letzte Farbe im Gradient
- `LeastBrightness`
  <br>Die dunkelste Farbe im Gradient
- `MostBrightness`
  <br>Die hellste Farbe im Gradient
- `value__`

### `ViewUtility`

`static class`

Zusammenfassung mehrfach verwendeter Methoden für's GUI.

**Extension Methods**

- `GetWeightedBrightness(this Color color) : int`
  <br>returns a value for the decision whether the text should be black or white depending on the human eye's sensitivity to the underlying colour
- `Invert(this Color color) : Color`
  <br>Inverts the specified color.

**Methoden**

- `FindResource(object key) : object`
  <br>Finds the resource.
- `GetDC(IntPtr hWnd) : IntPtr`
- `GetGradients(Color start, Color end, int steps) : IEnumerable<Color>`
  <br>Gibt eine liste der gradients von start nach end zurück
- `GetLeastBrightnessColor(Color[] colors) : Color`
  <br>Gibt die dunkelste farbe zurück
- `GetMostBrightnessColor(Color[] colors) : Color`
  <br>Gibt die hellste farbe zurück
- `GetOptimalForegroundColor(Color backgroundColor) : Color`
  <br>Gibt die je nach hintergrundfarbe schwarz oder weiß zurück
- `GetPixel(IntPtr hdc, int nXPos, int nYPos) : int`
  <br>GetPixel
- `PixelsToPoints(double pixels) : float`
  <br>Konvertiert Punkte zu Pixeln
- `PointsToPixels(float points) : double`
  <br>Konvertiert Punkte zu Pixeln
- `ReleaseDC(IntPtr dc) : void`

**Eigenschaften**

- `InDesignWPF : bool { get; }`
  <br>Es wird in VS oder Blend Designer editiert

## Nextended.UI.Classes

### `CustomClass`

`class`

CustomClass

**Konstruktoren**

- `CustomClass()`
- `CustomClass(object obj)`
  <br>Constructor of CustomClass which initializes the new PropertyDescriptorCollection.

**Methoden**

- `AddProperty(string propName, object propValue, string propDesc, string propCat, Type propType) : PropertyDescriptor`
  <br>Adds the property.
- `AddProperty(string propName, object propValue, string propDesc, string propCat, Type propType, bool isReadOnly, bool isExpandable) : PropertyDescriptor`
  <br>Adds a property into the CustomClass.
- `AddProperty<T>(string propName, object propValue, string propDesc, string propCat, Type propType) : PropertyDescriptor`
  <br>Adds the property.
- `GetAttributes() : AttributeCollection`
- `GetClassName() : string`
- `GetComponentName() : string`
- `GetConverter() : TypeConverter`
- `GetDefaultEvent() : EventDescriptor`
- `GetDefaultProperty() : PropertyDescriptor`
- `GetEditor(Type editorBaseType) : object`
- `GetEvents() : EventDescriptorCollection`
- `GetEvents(Attribute[] attributes) : EventDescriptorCollection`
- `GetProperties() : PropertyDescriptorCollection`
- `GetProperties(Attribute[] attributes) : PropertyDescriptorCollection`
- `GetPropertyOwner(PropertyDescriptor pd) : object`
- `NotifyPropertyChanged(string propertyName) : void`
  <br>Notifies the property changed.
- `RemoveProperty(DynamicProperty prop) : void`
- `RemoveProperty(string propName) : void`
  <br>Adds the property.

**Eigenschaften**

- `Item : DynamicProperty { get; }`
- `Item : DynamicProperty { get; }`
- `MaxLength : int { get; set; }`
  <br>MaxLength

**Ereignisse**

- `PropertyChanged : PropertyChangedEventHandler`
  <br>Occurs when [property changed].

### `DynamicProperty`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DynamicProperty(string pName, object pValue, string pDesc, string pCat, Type pType, bool readOnly, bool expandable, bool isBrowsable, Attribute[] attrs)`

**Eigenschaften**

- `Category : string { get; }`
- `ComponentType : Type { get; }`
- `Description : string { get; }`
- `IsBrowsable : bool { get; }`
- `IsExpandable : bool { get; }`
- `IsReadOnly : bool { get; }`
- `Name : string { get; }`
- `PropertyName : string { get; }`
- `PropertyType : Type { get; }`

### `FileDescription`

`class`

Small file description

**Konstruktoren**

- `FileDescription(string fileExtension)`
  <br>Initializes a new instance of the `FileDescription` class.

**Methoden**

- `GetDialogFilter() : string`

**Eigenschaften**

- `ExtensionName : string { get; }`
  <br>Gets or sets the name of the extension.
- `Extensions : List<string> { get; set; }`
  <br>Gets or sets the extensions.
- `MimeType : string { get; }`
  <br>MimeType
- `Name : string { get; }`
  <br>Name

### `FileDescriptionHelper`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `GetDialogFilter(this IEnumerable<FileDescription> fileDescriptions) : string`

### `PropertyGridTypeEditor`

`class`

PropertyGridTypeEditor

**Konstruktoren**

- `PropertyGridTypeEditor()`

## Nextended.UI.Helper

### `DebugHelper`

`static class`

Debug ausgaben in winform

**Methoden**

- `DebugOut(object obj, string title = "", bool condition = true) : void`
  <br>Zeigt den Inhalt eines Objektes an
- `DebugOutIfDebug(object obj, string title = "") : void`
  <br>Zeigt den Inhalt eines Objektes an (nur wenn Anwendung im Debugger Läuft (Debugger Attached oder #DEBUG))

**Eigenschaften**

- `IsDebug : bool { get; }`
  <br>Ist Debug

### `DispatcherHelper`

`static class`

DispatcherHelper

**Extension Methods**

- `EnsureAccess<T>(this T dependencyObject, Action<T> action) : void`

### `FileHelper`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `FileHelper()`

**Methoden**

- `BrowseDirectory(string path, string description, bool canCreateFolder) : string`
  <br>Ordnerauswahl
- `BrowseFile(string path = "", string description = "", string defaultExt = "", string filter = "", bool useSaveDialog = false) : string`
  <br>Dateiauswahl
- `BrowseFile(string[] allowedExtensions) : string`
  <br>Dateiauswahl
- `BrowseFiles(string path = "", string description = "", string defaultExt = "", string filter = "") : IEnumerable<string>`
  <br>Dateiauswahl
- `BrowseFiles(string[] allowedExtensions) : IEnumerable<string>`
  <br>Dateiauswahl
- `GetFilterString(IEnumerable<string> extensions) : string`

### `KeyGestureConvertHelper`

`static class`

Statische Hilfklasse, um ein KeyGesture zu konvertieren

### `PropertyGridSearcher`

`static class`

PropertyGridSearcher

### `ToolStripSpringTextBox`

`class`

ToolStripSpringTextBox Diese Klasse überschreibt die GetPreferredSize-Methode, um die verfügbare Breite des übergeordneten ToolStrip-Steuerelements zu berechnen, nachdem die Gesamtbreite aller anderen Elemente subtrahiert wurde.

**Konstruktoren**

- `ToolStripSpringTextBox()`

### `WindowsSecurityHelper`

`static class`

WindowsSecurityHelper

**Methoden**

- `CoTaskMemFree(IntPtr ptr) : void`
  <br>CoTaskMemFree
- `GetUserAccountImagePath() : string`
- `GetUserAccountImagePath(string username) : string`
  <br>Gibt den Pfad des Userimages zurück
- `GetUserDomainName() : string`
- `RestartElevated() : void`
- `SendMessage(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam) : UInt32`
  <br>SendMessage
- `ShowCredentialDialog(string caption, string message) : NetworkCredential`
  <br>GetCredentialsVistaAndUp

**Eigenschaften**

- `IsAdmin : bool { get; }`
  <br>Checks if the process is elevated
- `IsVistaOrHigher : bool { get; }`
  <br>Gibt an ob des System Vista oder höher ist
- `IsWin7OrHigher : bool { get; }`
  <br>Gibt an ob des System Windows 7 oder höher ist

## Nextended.UI.Input

### `InputBindingManager`

`class`

Global input listener + binding matcher. Installs a low-level keyboard/mouse hook (via MouseKeyHook) and optionally polls a gamepad; matches registered `StoredInputBinding`s (singles AND chords like Ctrl+X or LT+A) against the live held-input set and raises `OnBindingPressed`/`OnBindingReleased`. Also records new bindings (commit-on-release) for the key-changer controls.

**Konstruktoren**

- `InputBindingManager(bool enableGamepad = true)`

**Methoden**

- `AttachGamepadReader(IGamepadReader reader) : void`
  <br>Attach a custom gamepad reader (e.g. a `DirectInputGamepadReader` for a raw DualSense) instead of the default XInput reader. Disposes/replaces a previous reader.
- `Dispose() : void`
- `EnsureHookEvents() : void`
- `GetHoldingTime(string bindingId) : TimeSpan`
- `IsHoldingBinding(StoredInputBinding binding) : bool`
- `IsHoldingBinding(string bindingId) : bool`
- `IsHoldingBindingFor(StoredInputBinding binding, TimeSpan? duration) : bool`
- `IsHoldingBindingFor(string bindingId, TimeSpan? duration) : bool`
- `RegisterBinding(string bindingId, StoredInputBinding bindingValue) : void`
  <br>Register (or replace) a binding under an id and start listening.
- `RemoveBinding(string bindingId) : void`
  <br>Remove a registered binding by id.
- `StartListeningForBinding(string bindingId) : void`
  <br>Arm recording for the given id: the next inputs pressed together become the new binding (committed on first release, 1 input → single, ≥2 → chord), raised via `OnBindingSet`.
- `StopListening() : void`

**Eigenschaften**

- `Bindings : IReadOnlyDictionary<string, StoredInputBinding> { get; }`
  <br>All currently registered bindings by id.
- `EnableGamepad : bool { get; set; }`
  <br>When true (default), an XInput gamepad reader is created lazily on first use.
- `GamepadReader : IGamepadReader { get; }`
  <br>The gamepad reader currently feeding events (null until listening starts).
- `Instance : InputBindingManager { get; }`
  <br>Last created instance (the typical app has exactly one).

**Ereignisse**

- `OnBindingPressed : Action<string>`
- `OnBindingReleased : Action<string>`
- `OnBindingSet : Action<string, StoredInputBinding>`
- `OnKeyPressed : Action<StoredInputBinding>`
- `OnKeyReleased : Action<StoredInputBinding>`

### `InputDeviceType`

`enum`

Source device of a single input binding.

**Werte**

- `Gamepad`
- `Keyboard`
- `Mouse`
- `None`
- `value__`

### `InputSequenceMatcher`

`class`

Matches an ORDERED sequence of bindings (each step may itself be a chord), e.g. Ctrl+X → X → J. Registers each distinct step with the given `InputBindingManager` and raises `SequenceCompleted` when the steps are pressed in order, each within `StepTimeout` of the previous one.

**Konstruktoren**

- `InputSequenceMatcher(InputBindingManager manager, IEnumerable<StoredInputBinding> sequence)`

**Methoden**

- `Dispose() : void`

**Eigenschaften**

- `Sequence : IReadOnlyList<StoredInputBinding> { get; }`
  <br>The (valid) steps this matcher watches.
- `StepTimeout : TimeSpan { get; set; }`
  <br>Maximum gap between two steps before the sequence resets.

**Ereignisse**

- `SequenceCompleted : Action`

### `KeyDisplayName`

`static class`

Maps raw key names ("D1", "LMenu", "Oem5") to friendly display names ("1", "Left Alt", "Backslash (\\)").

**Methoden**

- `For(string keyName) : string`

**Eigenschaften**

- `Overrides : Dictionary<string, string> { get; }`

### `StoredInputBinding`

`class`

One stored input binding: EITHER a single input (`Key` + `Device`) OR a chord/combo (`Components` — every child must be held simultaneously). Children of a combo are always plain singles (never nested). Serializes as a plain POCO: Key, Device, MinTime, Components.

**Konstruktoren**

- `StoredInputBinding()`
- `StoredInputBinding(GamepadButton button)`
- `StoredInputBinding(GamepadEventArgs data)`
- `StoredInputBinding(GamepadSlider slider)`

**Methoden**

- `Combo(IEnumerable<StoredInputBinding> parts) : StoredInputBinding`
- `Equals(StoredInputBinding other) : bool`
- `Flatten() : IEnumerable<StoredInputBinding>`
- `IsHolding() : bool`
- `IsHoldingFor(TimeSpan? timeSpan = null) : bool`
- `SetMinTime(double value) : StoredInputBinding`
- `WaitHoldingFor() : Task<bool>`
- `WithoutMinTime() : StoredInputBinding`

**Eigenschaften**

- `Components : List<StoredInputBinding> { get; set; }`
  <br>Component inputs of a CHORD/COMBO binding (e.g. Ctrl+Shift+X, LT+A). `null` = a plain single binding. When set, every child must be held simultaneously to match.
- `Device : InputDeviceType { get; set; }`
- `DeviceName : string { get; }`
- `Empty : StoredInputBinding { get; }`
- `IsCombo : bool { get; }`
  <br>True when this binding is a multi-input chord rather than a single input.
- `IsValid : bool { get; }`
- `Key : string { get; set; }`
- `MinTime : double { get; set; }`
  <br>Hold-to-trigger time in seconds (0 = fire immediately).

## Nextended.UI.Input.Gamepad

### `DirectInputGamepadReader`

`class`

Reads a NON-XInput pad — e.g. a PS5 DualSense connected raw over USB/Bluetooth, which XInput never sees — through SharpDX.DirectInput, and exposes it through the same contract as the XInput `XInputGamepadReader`: normalized string vocabulary ("A"/"RSX"/"LT"…) via `ButtonEvent`. The axis/button/POV mapping follows the common DualShock 4 / DualSense DirectInput layout.

**Konstruktoren**

- `DirectInputGamepadReader(Guid instanceGuid)`

**Methoden**

- `Dispose() : void`
- `EnumerateDevices() : List<ValueTuple<Guid, string>>`
- `FindFirstDevice() : Guid?`
- `IsPressed(string button) : bool`

**Eigenschaften**

- `InstanceGuid : Guid { get; }`
  <br>The DirectInput device this reader is bound to.
- `IsConnected : bool { get; }`
- `LeftTriggerThreshold : float { get; set; }`
  <br>Trigger value (0..1) above which LT counts as pressed.
- `RightTriggerThreshold : float { get; set; }`
  <br>Trigger value (0..1) above which RT counts as pressed.

**Ereignisse**

- `ButtonEvent : EventHandler<GamepadEventArgs>`

### `GamepadAxis`

`enum`

_Keine Beschreibung._

**Werte**

- `LeftThumbX`
- `LeftThumbY`
- `RightThumbX`
- `RightThumbY`
- `value__`

### `GamepadButton`

`enum`

_Keine Beschreibung._

**Werte**

- `A`
- `B`
- `Back`
- `Down`
- `Left`
- `LeftShoulder`
- `LeftThumb`
- `Right`
- `RightShoulder`
- `RightThumb`
- `Start`
- `Up`
- `X`
- `Y`
- `value__`

### `GamepadEventArgs`

`class`

One normalized gamepad input event. `Button` is the transport-neutral string vocabulary ("A", "LT", "RSX", …) shared by all readers.

**Konstruktoren**

- `GamepadEventArgs()`
- `GamepadEventArgs(GamepadAxis axis, float? value = null)`
- `GamepadEventArgs(GamepadButton button, bool? pressed = null)`
- `GamepadEventArgs(GamepadSlider slider, float? value = null)`

**Eigenschaften**

- `Button : string { get; set; }`
- `Code : string { get; }`
- `GamepadAxis : GamepadAxis? { get; }`
- `GamepadButton : GamepadButton? { get; }`
- `GamepadSlider : GamepadSlider? { get; }`
- `IsPressed : bool? { get; set; }`
- `IsStickEvent : bool { get; set; }`
- `Value : float? { get; set; }`

### `GamepadSlider`

`enum`

_Keine Beschreibung._

**Werte**

- `LeftTrigger`
- `RightTrigger`
- `value__`

### `IGamepadReader`

`interface`

Transport-neutral gamepad reader: turns a physical pad's input into the normalized string vocabulary ("A"/"B"/…/"RSX"/"LT") via `ButtonEvent`. Implemented by the XInput reader (`IXInputGamepadReader`) and by a DirectInput/HID reader (e.g. for a raw PS5 DualSense), so consumers never need to know the transport.

**Methoden**

- `IsPressed(string button) : bool`

**Eigenschaften**

- `IsConnected : bool { get; }`

**Ereignisse**

- `ButtonEvent : EventHandler<GamepadEventArgs>`

### `IXInputGamepadReader`

`interface`

A reader backed by an XInput slot. Only this variant exposes the XInput `Controller` and slot switching.

### `XInputGamepadReader`

`class`

XInput polling reader (10 ms loop). Edge-detects buttons/triggers/sticks against the previous state and raises `ButtonEvent` with the normalized string vocabulary.

**Methoden**

- `Dispose() : void`
- `IsPressed(string button) : bool`

**Eigenschaften**

- `IsConnected : bool { get; }`
- `LeftTriggerThreshold : float { get; set; }`
  <br>Trigger value (0..1) above which LT counts as pressed.
- `RightTriggerThreshold : float { get; set; }`
  <br>Trigger value (0..1) above which RT counts as pressed.

**Ereignisse**

- `ButtonEvent : EventHandler<GamepadEventArgs>`

## Nextended.UI.ViewModels

### `ItemFilterModel`

`class`

Model für Filter

**Eigenschaften**

- `Caption : string { get; set; }`
  <br>Caption
- `Description : string { get; set; }`
  <br>Description

### `ItemFilterModel<T>`

`class`

Model für ItemFilter eines bestimmten Item-Typs.

**Eigenschaften**

- `Expression : Func<T, bool> { get; set; }`
  <br>Expression

## Nextended.UI.WPF

### `KeyLocalizer`

`static class`

KeyLocalizer

**Methoden**

- `TranslateGesture(string gesture) : string`
  <br>Translates the gesture.
- `TranslateKey(string key) : string`
  <br>Übersetzen

## Nextended.UI.WPF.Behaviors

### `CommandBehavior`

`static class`

Das CommandBehavior kann benutzt werden um jedem Element die möglichkeit zu geben ein Command zu binden local:CommandBehavior.RoutedEventName="MouseLeftButtonUp" local:CommandBehavior.Command="{Binding Command}"

### `ItemSourceFilterBehavior`

`class`

ItemSourceFilterBehavior ist ein behavoir um ein ItemsControl zu Filtern, und funtkiniert automatisch bei ItemsControl und ItemsPresentern für templates

**Konstruktoren**

- `ItemSourceFilterBehavior()`

**Eigenschaften**

- `AdditionalItemFilters : ObservableCollection<ItemFilterModel<object>> { get; set; }`
  <br>Zusätzliche Filter die dann ausgewählt werden können
- `CurrentFilter : ItemFilterModel<object> { get; set; }`
  <br>Aktueller filter
- `HasAdditionalFilters : bool { get; set; }`
  <br>Text der als Platzhalter benutzt wird
- `IsCaseSensitive : bool { get; set; }`
  <br>Gibt an, ob der filter groß und klein schreibung beachtet
- `IsFilterEnabled : bool { get; set; }`
  <br>Gibt an ob der filter aktiv ist
- `PropertyNameToFilter : string { get; set; }`
  <br>Eigenschaft, auf die der Filter greift
- `PropertyNamesToFilter : ObservableCollection<string> { get; set; }`
  <br>Eigenschaft, auf die der Filter greift
- `SearchInAllProperties : bool { get; set; }`
  <br>Gibt an ob in allen Properties gesucht werden soll
- `WaterMarkText : string { get; set; }`
  <br>Text der als Platzhalter benutzt wird

### `OptimalForegroundBehavoir`

`class`

OptimalForegroundBehavoir passt den Foreground entsprechend zum Background an

**Konstruktoren**

- `OptimalForegroundBehavoir()`

## Nextended.UI.WPF.Controls

### `KeyBindChangedEventArgs`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `KeyBindChangedEventArgs(StoredInputBinding oldValue, StoredInputBinding newValue)`

**Eigenschaften**

- `NewValue : StoredInputBinding { get; }`
- `OldValue : StoredInputBinding { get; }`

### `KeyBindChanger`

`class`

A single key-bind editor: shows the current `KeyBind` (keyboard, mouse or gamepad — singles and chords like Ctrl+X or LT+A), records a new one on click (commit-on-release via the `BindingManager`), supports a per-binding min-hold-time and context-menu delete. Persistence is the consumer's job — listen to `KeyBindChanged`.

**Konstruktoren**

- `KeyBindChanger()`

**Methoden**

- `Dispose() : void`
- `InitializeComponent() : void`
- `RecordNewBinding() : void`

**Eigenschaften**

- `BindingManager : InputBindingManager { get; set; }`
- `CanEditMinTime : bool { get; set; }`
- `CanRemoveBinding : bool { get; set; }`
- `HasKeySet : bool { get; }`
- `HasTimeValue : bool { get; }`
- `InUpdateMode : bool { get; }`
- `InvalidText : string { get; set; }`
  <br>Shown on the bind button while no binding is set.
- `KeyBind : StoredInputBinding { get; set; }`
- `KeyConfigName : string { get; set; }`
  <br>Registration id used with the `BindingManager`. Auto-generated when unset.
- `ShowTimeEdit : bool { get; }`
- `Text : string { get; set; }`
  <br>Optional title shown left of the bind button.
- `WithBorder : bool { get; set; }`

**Ereignisse**

- `BindingPressed : EventHandler`
  <br>Raised when the registered binding is pressed globally (outside record mode).
- `KeyBindChanged : EventHandler<KeyBindChangedEventArgs>`
- `KeyDeleted : EventHandler`
- `PropertyChanged : PropertyChangedEventHandler`

### `KeyBindSequenceBox`

`class`

Editor for an ORDERED sequence of key binds (each slot may be a single input or a chord): a wrap-list of `KeyBindChanger`s plus a trailing "+" placeholder to append, and an optional "record sequence" mode that captures presses in order (stamping the gap to the previous press into each binding's MinTime). Use with `InputSequenceMatcher` to react to the sequence at runtime.

**Konstruktoren**

- `KeyBindSequenceBox()`

**Methoden**

- `InitializeComponent() : void`

**Eigenschaften**

- `AllowDuplicates : bool { get; set; }`
  <br>Sequences repeat steps by nature (X, X), so duplicates default to allowed.
- `BindingManager : InputBindingManager { get; set; }`
- `CanRecordSequence : bool { get; set; }`
- `ErrorMessage : string { get; set; }`
- `HasError : bool { get; }`
- `Keys : ObservableCollection<StoredInputBinding> { get; set; }`
- `RecordButtonText : string { get; set; }`
- `StopRecordButtonText : string { get; set; }`
- `ValidKeys : IReadOnlyList<StoredInputBinding> { get; }`
  <br>The current sequence WITHOUT the trailing placeholder slot.

**Ereignisse**

- `Changed : EventHandler<IReadOnlyList<StoredInputBinding>>`
- `PropertyChanged : PropertyChangedEventHandler`

## Nextended.UI.WPF.Controls.Wizard

### `WizardPageState`

`enum`

_Keine Beschreibung._

**Werte**

- `InProgress`
- `InValid`
- `None`
- `Valid`
- `value__`

## Nextended.UI.WPF.Converters

### `BooleanToSelectionModeConverter`

`class`

Konvertiert einen Boolean (AllowMultiSelect) zu dem SelectionMode für ListViews

**Konstruktoren**

- `BooleanToSelectionModeConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert einen Boolean (AllowMultiSelect) zu dem SelectionMode für ListViews
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Gibt zurück, ob der übergebene SelectionMode MultiSelect ist

### `BoolToBrushConverter`

`class`

Konvertiert einen Bool zu einer farbe

**Konstruktoren**

- `BoolToBrushConverter()`

### `BoolToColorConverter`

`class`

Konvertiert einen Bool zu einer farbe

**Konstruktoren**

- `BoolToColorConverter()`

### `BoolToOppositeConverter`

`class`

BoolToOppositeConverter

**Konstruktoren**

- `BoolToOppositeConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `BoolToStringConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `BoolToStringConverter()`

### `BoolToVisibilityConverter`

`class`

g

**Konstruktoren**

- `BoolToVisibilityConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `BrushToColorConverter`

`class`

Konvertiert einen Brush zu einer Farbe

**Konstruktoren**

- `BrushToColorConverter()`

### `CenterConverter`

`class`

Einfacher Konverter, der einfach den wetrt durch 2 teilt, um positionen zu zentrieren

**Konstruktoren**

- `CenterConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `CountToBooleanConverter`

`class`

Count &gt; 0 = true

**Konstruktoren**

- `CountToBooleanConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Gibt true zurück, wenn der value null ist
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`

### `CountToVisibilityConverter`

`class`

Count to visibility

**Konstruktoren**

- `CountToVisibilityConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Gibt true zurück, wenn der value null ist
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`

### `DoubleTypeConverter`

`class`

DoubleTypeConverter

**Konstruktoren**

- `DoubleTypeConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `DummyConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DummyConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `EnumerableTakeConverter`

`class`

Gibt von einer Enumeration die ersten x items zurück (x muss als int als ConvertParam kommen)

**Konstruktoren**

- `EnumerableTakeConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `EnumTypeConverter`

`class`

EnumTypeConverter

**Konstruktoren**

- `EnumTypeConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `EqualToColorConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `EqualToColorConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `GenericValueConverter<TValue, TResult>`

`class`

GenericValueConverter

**Methoden**

- `Convert(TValue value, Type targetType, object parameter, CultureInfo culture) : TResult`
- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(TResult value, Type targetType, object parameter, CultureInfo culture) : TValue`
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value back.

### `HeightConverter`

`class`

Höhen Konverter

**Konstruktoren**

- `HeightConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `ImageGrayscaleConverter`

`class`

Konvertiert ein Bild zu einem Bild mit Graustufen (ohne Farbe)

**Konstruktoren**

- `ImageGrayscaleConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `ImageToImageSourceConverter`

`class`

Class ImageToImageSourceConverter

**Konstruktoren**

- `ImageToImageSourceConverter()`

### `InvertBooleanConverter`

`class`

Invertiert einen boolschen Wert

**Konstruktoren**

- `InvertBooleanConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value. The data binding engine calls this method when it propagates a value from the binding source to the binding target.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value. The data binding engine calls this method when it propagates a value from the binding target to the binding source.

### `IsNullConverter`

`class`

Gibt true zurück, wenn der value null ist

**Konstruktoren**

- `IsNullConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Gibt true zurück, wenn der value null ist
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`

### `ItemToCollectionConverter`

`class`

Konvertiert ein Element in eine Auflistung

**Konstruktoren**

- `ItemToCollectionConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `KeyGestureToStringConverter`

`class`

Convertiert einen KeyGesture zu einem String

**Konstruktoren**

- `KeyGestureToStringConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Convertiert einen KeyGesture zu einem String
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>string to gesure

### `KeyToStringConverter`

`class`

KeyToStringConverter

**Konstruktoren**

- `KeyToStringConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Convert
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>ConvertBack

### `MethodToValueConverter`

`class`

Konvertiert eine Methode eines Objektes zu einem Wert

**Konstruktoren**

- `MethodToValueConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `MultiBooleanConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `MultiBooleanConverter()`

**Methoden**

- `Convert(object[] values, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
- `ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) : object[]`
  <br>Converts a binding target value to the source binding values.

**Eigenschaften**

- `ReturnVisibility : bool { get; set; }`
  <br>Wenn true, dann wird Visibility zurückgegeben

### `NullToVisibilityConverter`

`class`

Null to visibility

**Konstruktoren**

- `NullToVisibilityConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `OptimalBrushConverter`

`class`

Konvertiert ein Binding an einem Brush zu dem passenden lesbaren Brush Foreground="{Binding ElementName=grid, Path=Background, Converter={StaticResource OptimalBrushConverter}}"

**Konstruktoren**

- `OptimalBrushConverter()`

### `OptimalColorConverter`

`class`

Konvertiert ein Binding an einer Farbe zu der passenden lesbaren farbe

**Konstruktoren**

- `OptimalColorConverter()`

### `ProgressStateToBrushConverter`

`class`

ProgressState zu brush

**Konstruktoren**

- `ProgressStateToBrushConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert einen ProgressState zu einem Brush
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Macht nix

### `ProgressStateToIsIndeterminateConverter`

`class`

ProgressState zu boolean IsIndeterminate

**Konstruktoren**

- `ProgressStateToIsIndeterminateConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert einen ProgressState zu einem boolean
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Macht nix

### `ProgressStateToVisibilityConverter`

`class`

ProgressState zu Visibility

**Konstruktoren**

- `ProgressStateToVisibilityConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert einen Progressstate zu einem Sichtbarkeitsstatus
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Macht nix

### `RemoveAccessKeyConverter`

`class`

RemoveAccessKeyConverter

**Konstruktoren**

- `RemoveAccessKeyConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `ReplaceWithParamConverter`

`class`

Entfernt den angegebenen parameter aus dem string

**Konstruktoren**

- `ReplaceWithParamConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `StaticMethodToValueConverter`

`class`

StaticMethodToValueConverter

**Konstruktoren**

- `StaticMethodToValueConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert eine Statische Methode der übergebenen Klasse zu einem Wert, um extension methods für ein objekt zu binden Text="{Binding ConverterParameter=Namespace.MyStaticExtensions.GetVersion, Converter={StaticResource callMethodMethodConverter}}" /&gt;
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `StringPrefixConverter`

`class`

StringSuffixConverter

**Konstruktoren**

- `StringPrefixConverter()`

### `StringSuffixConverter`

`class`

StringSuffixConverter

**Konstruktoren**

- `StringSuffixConverter()`

### `StringToBooleanConverter`

`class`

StringToVisibilityConverter

**Konstruktoren**

- `StringToBooleanConverter()`

### `StringToKeyConverter`

`class`

StringToKeyConverter

**Konstruktoren**

- `StringToKeyConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert ein String zu einem Key (für AccessKey)
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `StringToVisibilityConverter`

`class`

StringToVisibilityConverter

**Konstruktoren**

- `StringToVisibilityConverter()`

### `ThemeAlternationConverter`

`class`

AlternationConverter der den Theme berücksichtigt

**Konstruktoren**

- `ThemeAlternationConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Konvertiert den AlternationIndex zu einem Brush

### `ToStringConverter`

`class`

ToString

**Konstruktoren**

- `ToStringConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `TruncateStringConverter`

`class`

TruncateStringConverter

**Konstruktoren**

- `TruncateStringConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Convert
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Not implemented

### `TypeNameConverter`

`class`

TypeName Converter

**Konstruktoren**

- `TypeNameConverter()`

**Methoden**

- `Convert(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.
- `ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) : object`
  <br>Converts a value.

### `UIElementToImageConverter`

`class`

Konvertiert ein UIElement/Control zu einem Bild

**Konstruktoren**

- `UIElementToImageConverter()`

**Methoden**

- `ClearCache() : void`

**Eigenschaften**

- `UseCache : bool { get; set; }`
  <br>Gibt an, ob ein UIElement nur einmal als Bild erzeugt werden soll

### `VisibilityToBooleanConverter`

`class`

VisibilityToBooleanConverter

**Konstruktoren**

- `VisibilityToBooleanConverter()`

### `WidthConverter`

`class`

WidthConverter

**Konstruktoren**

- `WidthConverter()`

### `WizardPageStateToBooleanConverter`

`class`

Konvertiert den Status einer Wizard Seite zu einem Bild

**Konstruktoren**

- `WizardPageStateToBooleanConverter()`

### `WizardPageStateToImageConverter`

`class`

Konvertiert den Status einer Wizard Seite zu einem Bild

**Konstruktoren**

- `WizardPageStateToImageConverter()`

## Nextended.UI.WPF.Extensions

### `AnimationExtensions`

`static class`

Erweiterungen für Storyboards und animations

### `WindowHelper`

`static class`

Helper für Glass

**Methoden**

- `IsAnyModalDialogOpen() : bool`

**Eigenschaften**

- `IsGlassAvailable : bool { get; }`
  <br>Gibt zurück ob Glass möglich

**Felder**

- `Contexthelp : int`
- `ExContexthelp : int`
- `ExtStyle : int`
- `Maximizebox : int`
- `Minimizebox : int`
- `Style : int`
- `Syscommand : int`

**Ereignisse**

- `IsGlassAvailableChanged : EventHandler<EventArgs>`
  <br>Wird ausgelöst wenn sich `IsGlassAvailable` ändert

## Nextended.UI.WPF.MarkupExtensions

### `StaticImage`

`class`

Static Image extension für wpf

**Konstruktoren**

- `StaticImage(string member)`
  <br>Initializes a new instance of the `StaticImage` class.

## XamlGeneratedNamespace

### `GeneratedInternalTypeHelper`

`class`

GeneratedInternalTypeHelper

**Konstruktoren**

- `GeneratedInternalTypeHelper()`

↩ [Zurück zur Paketseite](/de/projects/ui)
