---
title: Nextended.Core — API-Referenz
---

# Nextended.Core — API-Referenz

🇬🇧 [This page in English](/projects/core-api)

Die vollständige öffentliche Oberfläche von `Nextended.Core`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/core)

## 

### `NewtonJsonMoneyConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `NewtonJsonMoneyConverter()`

### `RijndaelEncryption`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `RijndaelEncryption()`

**Methoden**

- `Decrypt(string str, string key) : string`
- `Encrypt(string str, string key) : string`

**Eigenschaften**

- `Iterations : int { get; set; }`

### `SystemJsonMoneyConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `SystemJsonMoneyConverter()`

## Nextended.Core

### `AutoEditableNotificationObject`

`abstract class`

Class that implements automatic INotifyPropertyCHanged an INotifyPropertyChanging with default {get; set} properties

**Ereignisse**

- `PropertyChangedDetailed : EventHandler<PropertyChangedEventArgs<object>>`
  <br>Occurs when a property changes, providing detailed information about the change

### `Bindable<TValue>`

`class`

Represents a bindable value that supports property change notification

**Konstruktoren**

- `Bindable()`
- `Bindable(TValue value)`

**Eigenschaften**

- `Value : TValue { get; set; }`
  <br>Gets or sets the value

**Ereignisse**

- `Changed : EventHandler`
  <br>Occurs when the value changes

### `BindableKeyValuePair<TKey, TValue>`

`class`

Represents a bindable key-value pair that supports property change notification

**Konstruktoren**

- `BindableKeyValuePair()`
- `BindableKeyValuePair(TKey key, TValue value)`

**Eigenschaften**

- `Key : TKey { get; set; }`
  <br>Gets or sets the key
- `Value : TValue { get; set; }`
  <br>Gets or sets the value

**Ereignisse**

- `Changed : EventHandler`
  <br>Occurs when the key or value changes

### `Check`

`static class`

Static class to check certain preconditions

**Extension Methods**

- `ThrowIfNull<T>(this T input, string paramName) : T`
- `ThrowIfNull<TException, T>(this T input, string message) : T`

**Methoden**

- `IsVistaOrHigher() : void`
- `NotNull(Expression<Func<object>> expression1, Expression<Func<object>> expression2, Expression<Func<object>>[] parameters) : void`
- `NotNull(Guid parameter, string parameterName) : void`
  <br>Checks if a `Guid` is empty
- `NotNull(object parameter, string parameterName) : void`
  <br>Checks if a `parameter` is null.
- `NotNull<T>(Expression<Func<T>> parameter) : T`
- `NotNullOrEmpty(string parameter, string parameterName) : void`
  <br>Checks if a `parameter` is null or empty.
- `Requires(bool condition, Func<Exception> exceptionCreateFactory) : void`
- `Requires<TException>(bool condition) : void`
  <br>Checks if the condition `condition` is met. If not, an exception of type `TException` is thrown.
- `Requires<TException>(bool condition, string message) : void`
  <br>Checks if the condition `condition` is met. If not, an exception of type `TException` is thrown.
- `TryCatch<TException>(Action block, Func<TException, Exception> onException = null) : void`
- `TryCatch<TResult, TException>(Func<TResult> block, Func<TException, Exception> onException = null) : TResult`
- `TryCatchAsync<TException>(Action block, Func<TException, Exception> onException = null) : Task`
- `TryCatchAsync<TException>(Task block, Func<TException, Exception> onException = null) : Task`
- `TryCatchAsync<TResult, TException>(Func<TResult> block, Func<TException, Exception> onException = null) : Task`
- `TryCatchAsync<TResult, TException>(Task<TResult> task, Func<TException, Exception> onException = null, CancellationToken cancellation = null) : Task<TResult>`

### `DeterministicGuid`

`static class`

_Keine Beschreibung._

**Methoden**

- `Create(string key = "") : Guid`

### `EditableNotificationObject`

`abstract class`

Base Notification Object, that automatic correct implements IEditable

**Methoden**

- `BeginEdit() : void`
- `CancelEdit() : void`
- `EndEdit() : void`

### `ExposedClass`

`class`

Provides access to static members of a class including private members using a dynamic object

**Methoden**

- `From(Type type) : object`

### `ExposedObject`

`class`

ExposedObject easy to use private members in dynamic object

**Methoden**

- `Cast<T>(ExposedObject t) : T`
  <br>Cast
- `From(object obj) : object`
  <br>Exposed object aus einer objekt instanz erstellen
- `GetConstructorInfo(Type type, object[] parameters) : ConstructorInfo`
  <br>GetConstructorInfo
- `New(Type type, object[] parameters) : object`
  <br>Neues exposed objekt erstellen
- `New<T>(object[] parameters) : object`
  <br>Creates a new exposed object

**Eigenschaften**

- `Object : object { get; }`
  <br>Gets the actual internal object

### `GeneratedLocalizationBase`

`abstract class`

_Keine Beschreibung._

**Methoden**

- `GetAll(CultureInfo cultureInfo = null) : IDictionary<string, string>`
- `GetAll(bool includeParents, CultureInfo cultureInfo = null) : IDictionary<string, string>`
- `GetString(string key, CultureInfo cultureInfo = null) : string`
- `GetString(string key, bool includeParents, CultureInfo cultureInfo = null) : string`

### `GeneratedLocalizationBase<T>`

`abstract class`

_Keine Beschreibung._

**Methoden**

- `GetAll(CultureInfo cultureInfo = null) : IDictionary<string, string>`
- `GetAll(bool includeParents, CultureInfo cultureInfo = null) : IDictionary<string, string>`
- `GetInstance() : T`
- `GetString(string key, CultureInfo cultureInfo = null) : string`
- `GetString(string key, bool includeParents, CultureInfo cultureInfo = null) : string`

### `IGeneratedLocalization`

`interface`

Interface for generated localization resources that provides access to localized strings and dictionaries.

**Methoden**

- `GetAll(CultureInfo cultureInfo = null) : IDictionary<string, string>`
  <br>Gets all localized strings as a dictionary for the specified culture.
- `GetAll(bool includeParents, CultureInfo cultureInfo = null) : IDictionary<string, string>`
  <br>Gets all localized strings as a dictionary for the specified culture, optionally including parent culture strings.
- `GetString(string key, CultureInfo cultureInfo = null) : string`
  <br>Gets a localized string for the specified key and culture.
- `GetString(string key, bool includeParents, CultureInfo cultureInfo = null) : string`
  <br>Gets a localized string for the specified key and culture, optionally including parent culture strings.

### `KeyValuePairExtensions`

`static class`

Extension methods for key-value pairs

**Extension Methods**

- `AsBindable<TKey, TValue>(this KeyValuePair<TKey, TValue> pair) : BindableKeyValuePair<TKey, TValue>`
- `ToBindableList<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) : ObservableCollection<BindableKeyValuePair<TKey, TValue>>`

### `MimeType`

`static class`

Provides MIME type mappings and utilities for file extensions and content types

**Methoden**

- `AddOrUpdate(string mime, string extension) : void`
  <br>Adds or updates a MIME type mapping for the specified extension
- `GetExtension(string mime) : string`
  <br>Gets the file extension for the specified MIME type
- `GetMimeType(string fileName) : string`
  <br>Gets the MIME type for the specified file name
- `Is7Zip(string contentType) : bool`
  <br>Determines whether the specified content type is a 7-Zip archive
- `IsArchive(string contentType) : bool`
  <br>Determines whether the specified content type is an archive
- `IsAudio(string contentType) : bool`
  <br>Determines whether the specified content type is an audio file
- `IsExcel(string contentType) : bool`
  <br>Determines whether the specified content type is an Excel file
- `IsImage(string contentType) : bool`
  <br>Determines whether the specified content type is an image
- `IsPdf(string contentType) : bool`
  <br>Determines whether the specified content type is PDF
- `IsRar(string contentType) : bool`
  <br>Determines whether the specified content type is a RAR archive
- `IsTar(string contentType) : bool`
  <br>Determines whether the specified content type is a TAR archive
- `IsVideo(string contentType) : bool`
  <br>Determines whether the specified content type is a video
- `IsWord(string contentType) : bool`
  <br>Determines whether the specified content type is a Word document
- `IsZip(string contentType) : bool`
  <br>Determines whether the specified content type is a ZIP archive
- `Matches(string mimeType, string[] mimeTypes) : bool`
  <br>Returns true if the given MIME type matches any of the given MIME types
- `ReadMimeTypeFromUrlAsync(string url, CancellationToken cancellationToken = null) : Task<string>`
  <br>Reads the MIME type from the specified URL asynchronously
- `ReadMimeTypeFromUrlAsync(string url, HttpClient client, CancellationToken cancellationToken = null) : Task<string>`
  <br>Reads the MIME type from the specified URL asynchronously

**Eigenschaften**

- `Aab : string { get; }`
- `Aac : string { get; }`
- `Aam : string { get; }`
- `Aas : string { get; }`
- `Abw : string { get; }`
- `Ac : string { get; }`
- `Acc : string { get; }`
- `Ace : string { get; }`
- `Acu : string { get; }`
- `Acutc : string { get; }`
- `Adp : string { get; }`
- `Aep : string { get; }`
- `Afm : string { get; }`
- `Afp : string { get; }`
- `Ahead : string { get; }`
- `Ai : string { get; }`
- `Aif : string { get; }`
- `Aifc : string { get; }`
- `Aiff : string { get; }`
- `Air : string { get; }`
- `Ait : string { get; }`
- `All : Dictionary<string, string> { get; }`
  <br>Gets all MIME type mappings
- `AllTypes : string[] { get; }`
  <br>Gets all available MIME types
- `Ami : string { get; }`
- `Apk : string { get; }`
- `Appcache : string { get; }`
- `Application : string { get; }`
- `Apr : string { get; }`
- `Arc : string { get; }`
- `ArchiveTypes : string[] { get; }`
  <br>Gets all archive MIME types
- `Asc : string { get; }`
- `Asf : string { get; }`
- `Asm : string { get; }`
- `Aso : string { get; }`
- `Asx : string { get; }`
- `Atc : string { get; }`
- `Atom : string { get; }`
- `Atomcat : string { get; }`
- `Atomsvc : string { get; }`
- `Atx : string { get; }`
- `Au : string { get; }`
- `AudioTypes : string[] { get; }`
  <br>Gets all audio MIME types
- `Avi : string { get; }`
- `Aw : string { get; }`
- `Azf : string { get; }`
- `Azs : string { get; }`
- `Azw : string { get; }`
- `Bat : string { get; }`
- `Bcpio : string { get; }`
- `Bdf : string { get; }`
- `Bdm : string { get; }`
- `Bed : string { get; }`
- `Bh2 : string { get; }`
- `BinExt : string { get; }`
- `Blb : string { get; }`
- `Blorb : string { get; }`
- `Bmi : string { get; }`
- `Bmp : string { get; }`
- `Book : string { get; }`
- `Box : string { get; }`
- `Boz : string { get; }`
- `Bpk : string { get; }`
- `Btif : string { get; }`
- `Bz : string { get; }`
- `Bz2 : string { get; }`
- `C : string { get; }`
- `C11amc : string { get; }`
- `C11amz : string { get; }`
- `C4d : string { get; }`
- `C4f : string { get; }`
- `C4g : string { get; }`
- `C4p : string { get; }`
- `C4u : string { get; }`
- `Cab : string { get; }`
- `Caf : string { get; }`
- `Cap : string { get; }`
- `Car : string { get; }`
- `Cat : string { get; }`
- `Cb7 : string { get; }`
- `Cba : string { get; }`
- `Cbr : string { get; }`
- `Cbt : string { get; }`
- `Cbz : string { get; }`
- `Cc : string { get; }`
- `Cct : string { get; }`
- `Ccxml : string { get; }`
- `Cdbcmsg : string { get; }`
- `Cdf : string { get; }`
- `Cdkey : string { get; }`
- `Cdmia : string { get; }`
- `Cdmic : string { get; }`
- `Cdmid : string { get; }`
- `Cdmio : string { get; }`
- `Cdmiq : string { get; }`
- `Cdx : string { get; }`
- `Cdxml : string { get; }`
- `Cdy : string { get; }`
- `Cer : string { get; }`
- `Cfs : string { get; }`
- `Cgm : string { get; }`
- `Chat : string { get; }`
- `Chm : string { get; }`
- `Chrt : string { get; }`
- `Cif : string { get; }`
- `Cii : string { get; }`
- `Cil : string { get; }`
- `Cla : string { get; }`
- `Class : string { get; }`
- `Clkk : string { get; }`
- `Clkp : string { get; }`
- `Clkt : string { get; }`
- `Clkw : string { get; }`
- `Clkx : string { get; }`
- `Clp : string { get; }`
- `Cmc : string { get; }`
- `Cmdf : string { get; }`
- `Cml : string { get; }`
- `Cmp : string { get; }`
- `Cmx : string { get; }`
- `Cod : string { get; }`
- `Com : string { get; }`
- `Conf : string { get; }`
- `Cpio : string { get; }`
- `Cpp : string { get; }`
- `Cpt : string { get; }`
- `Crd : string { get; }`
- `Crl : string { get; }`
- `Crt : string { get; }`
- `Cryptonote : string { get; }`
- `Cs : string { get; }`
- `Csh : string { get; }`
- `Csml : string { get; }`
- `Csp : string { get; }`
- `Css : string { get; }`
- `Cst : string { get; }`
- `Csv : string { get; }`
- `Cu : string { get; }`
- `Curl : string { get; }`
- `Cww : string { get; }`
- `Cxt : string { get; }`
- `Cxx : string { get; }`
- `Dae : string { get; }`
- `Daf : string { get; }`
- `Dart : string { get; }`
- `Dataless : string { get; }`
- `Davmount : string { get; }`
- `Dbk : string { get; }`
- `Dcr : string { get; }`
- `Dcurl : string { get; }`
- `Dd2 : string { get; }`
- `Ddd : string { get; }`
- `Deb : string { get; }`
- `Def : string { get; }`
- `Deploy : string { get; }`
- `Der : string { get; }`
- `Dfac : string { get; }`
- `Dgc : string { get; }`
- `Dic : string { get; }`
- `Dir : string { get; }`
- `Dis : string { get; }`
- `Dist : string { get; }`
- `Distz : string { get; }`
- `Djv : string { get; }`
- `Djvu : string { get; }`
- `Dll : string { get; }`
- `Dmg : string { get; }`
- `Dmp : string { get; }`
- `Dms : string { get; }`
- `Dna : string { get; }`
- `Doc : string { get; }`
- `Docm : string { get; }`
- `DocumentTypes : string[] { get; }`
  <br>Gets all document MIME types
- `Docx : string { get; }`
- `Dot : string { get; }`
- `Dotm : string { get; }`
- `Dotx : string { get; }`
- `Dp : string { get; }`
- `Dpg : string { get; }`
- `Dra : string { get; }`
- `Dsc : string { get; }`
- `Dssc : string { get; }`
- `Dtb : string { get; }`
- `Dtd : string { get; }`
- `Dts : string { get; }`
- `Dtshd : string { get; }`
- `Dump : string { get; }`
- `Dvb : string { get; }`
- `Dvi : string { get; }`
- `Dwf : string { get; }`
- `Dwg : string { get; }`
- `Dxf : string { get; }`
- `Dxp : string { get; }`
- `Dxr : string { get; }`
- `Ecelp4800 : string { get; }`
- `Ecelp7470 : string { get; }`
- `Ecelp9600 : string { get; }`
- `Ecma : string { get; }`
- `Edm : string { get; }`
- `Edx : string { get; }`
- `Efif : string { get; }`
- `Ei6 : string { get; }`
- `Elc : string { get; }`
- `Emf : string { get; }`
- `Eml : string { get; }`
- `Emma : string { get; }`
- `Emz : string { get; }`
- `Eol : string { get; }`
- `Eot : string { get; }`
- `Eps : string { get; }`
- `Epub : string { get; }`
- `Es3 : string { get; }`
- `Esa : string { get; }`
- `Esf : string { get; }`
- `Et3 : string { get; }`
- `Etx : string { get; }`
- `Eva : string { get; }`
- `Evy : string { get; }`
- `Exe : string { get; }`
- `Exi : string { get; }`
- `Ext : string { get; }`
- `Ez : string { get; }`
- `Ez2 : string { get; }`
- `Ez3 : string { get; }`
- `F : string { get; }`
- `F4v : string { get; }`
- `F77 : string { get; }`
- `F90 : string { get; }`
- `Fbs : string { get; }`
- `Fcdt : string { get; }`
- `Fcs : string { get; }`
- `Fdf : string { get; }`
- `Fe_launch : string { get; }`
- `Fg5 : string { get; }`
- `Fgd : string { get; }`
- `Fh : string { get; }`
- `Fh4 : string { get; }`
- `Fh5 : string { get; }`
- `Fh7 : string { get; }`
- `Fhc : string { get; }`
- `Fig : string { get; }`
- `Flac : string { get; }`
- `Fli : string { get; }`
- `Flo : string { get; }`
- `Flv : string { get; }`
- `Flw : string { get; }`
- `Flx : string { get; }`
- `Fly : string { get; }`
- `Fm : string { get; }`
- `Fnc : string { get; }`
- `For : string { get; }`
- `Fpx : string { get; }`
- `Frame : string { get; }`
- `Fsc : string { get; }`
- `Fst : string { get; }`
- `Ftc : string { get; }`
- `Fti : string { get; }`
- `Fvt : string { get; }`
- `Fxp : string { get; }`
- `Fxpl : string { get; }`
- `Fzs : string { get; }`
- `G2w : string { get; }`
- `G3 : string { get; }`
- `G3w : string { get; }`
- `Gac : string { get; }`
- `Gam : string { get; }`
- `Gbr : string { get; }`
- `Gca : string { get; }`
- `Gdl : string { get; }`
- `Geo : string { get; }`
- `Gex : string { get; }`
- `Ggb : string { get; }`
- `Ggt : string { get; }`
- `Ghf : string { get; }`
- `Gif : string { get; }`
- `Gim : string { get; }`
- `Gml : string { get; }`
- `Gmx : string { get; }`
- `Gnumeric : string { get; }`
- `Gph : string { get; }`
- `Gpx : string { get; }`
- `Gqf : string { get; }`
- `Gqs : string { get; }`
- `Gram : string { get; }`
- `Gramps : string { get; }`
- `Gre : string { get; }`
- `Grv : string { get; }`
- `Grxml : string { get; }`
- `Gsf : string { get; }`
- `Gtar : string { get; }`
- `Gtm : string { get; }`
- `Gtw : string { get; }`
- `Gv : string { get; }`
- `Gxf : string { get; }`
- `Gxt : string { get; }`
- `H : string { get; }`
- `H261 : string { get; }`
- `H263 : string { get; }`
- `H264 : string { get; }`
- `Hal : string { get; }`
- `Hbci : string { get; }`
- `Hdf : string { get; }`
- `Hh : string { get; }`
- `Hlp : string { get; }`
- `Hpgl : string { get; }`
- `Hpid : string { get; }`
- `Hps : string { get; }`
- `Hqx : string { get; }`
- `Htke : string { get; }`
- `Htm : string { get; }`
- `Html : string { get; }`
- `Hvd : string { get; }`
- `Hvp : string { get; }`
- `Hvs : string { get; }`
- `I2g : string { get; }`
- `Icc : string { get; }`
- `Ice : string { get; }`
- `Icm : string { get; }`
- `Ico : string { get; }`
- `Ics : string { get; }`
- `Ief : string { get; }`
- `Ifb : string { get; }`
- `Ifm : string { get; }`
- `Iges : string { get; }`
- `Igl : string { get; }`
- `Igm : string { get; }`
- `Igs : string { get; }`
- `Igx : string { get; }`
- `Iif : string { get; }`
- `ImageTypes : string[] { get; }`
  <br>Gets all image MIME types
- `Imp : string { get; }`
- `Ims : string { get; }`
- `InExt : string { get; }`
- `Ink : string { get; }`
- `Inkml : string { get; }`
- `Install : string { get; }`
- `Iota : string { get; }`
- `Ipfix : string { get; }`
- `Ipk : string { get; }`
- `Irm : string { get; }`
- `Irp : string { get; }`
- `Iso : string { get; }`
- `Itp : string { get; }`
- `Ivp : string { get; }`
- `Ivu : string { get; }`
- `Jad : string { get; }`
- `Jam : string { get; }`
- `Jar : string { get; }`
- `Java : string { get; }`
- `Jisp : string { get; }`
- `Jlt : string { get; }`
- `Jnlp : string { get; }`
- `Joda : string { get; }`
- `Jpe : string { get; }`
- `Jpeg : string { get; }`
- `Jpg : string { get; }`
- `Jpgm : string { get; }`
- `Jpgv : string { get; }`
- `Jpm : string { get; }`
- `Js : string { get; }`
- `Json : string { get; }`
- `Jsonml : string { get; }`
- `Kar : string { get; }`
- `Karbon : string { get; }`
- `Kfo : string { get; }`
- `Kia : string { get; }`
- `Kml : string { get; }`
- `Kmz : string { get; }`
- `Kne : string { get; }`
- `Knp : string { get; }`
- `Kon : string { get; }`
- `Kpr : string { get; }`
- `Kpt : string { get; }`
- `Kpxx : string { get; }`
- `Ksp : string { get; }`
- `Ktr : string { get; }`
- `Ktx : string { get; }`
- `Ktz : string { get; }`
- `Kwd : string { get; }`
- `Kwt : string { get; }`
- `Lasxml : string { get; }`
- `Latex : string { get; }`
- `Lbd : string { get; }`
- `Lbe : string { get; }`
- `Les : string { get; }`
- `Lha : string { get; }`
- `Link66 : string { get; }`
- `List : string { get; }`
- `List3820 : string { get; }`
- `Listafp : string { get; }`
- `Lnk : string { get; }`
- `Log : string { get; }`
- `Lostxml : string { get; }`
- `Lrf : string { get; }`
- `Lrm : string { get; }`
- `Ltf : string { get; }`
- `Lvp : string { get; }`
- `Lwp : string { get; }`
- `Lzh : string { get; }`
- `M13 : string { get; }`
- `M14 : string { get; }`
- `M1v : string { get; }`
- `M21 : string { get; }`
- `M2a : string { get; }`
- `M2v : string { get; }`
- `M3a : string { get; }`
- `M3u : string { get; }`
- `M3u8 : string { get; }`
- `M4a : string { get; }`
- `M4u : string { get; }`
- `M4v : string { get; }`
- `Ma : string { get; }`
- `Mads : string { get; }`
- `Mag : string { get; }`
- `Maker : string { get; }`
- `Man : string { get; }`
- `Mar : string { get; }`
- `Mathml : string { get; }`
- `Mb : string { get; }`
- `Mbk : string { get; }`
- `Mbox : string { get; }`
- `Mc1 : string { get; }`
- `Mcd : string { get; }`
- `Mcurl : string { get; }`
- `Md : string { get; }`
- `Mdb : string { get; }`
- `Mdi : string { get; }`
- `Me : string { get; }`
- `Mesh : string { get; }`
- `Meta4 : string { get; }`
- `Metalink : string { get; }`
- `Mets : string { get; }`
- `Mfm : string { get; }`
- `Mft : string { get; }`
- `Mgp : string { get; }`
- `Mgz : string { get; }`
- `Mid : string { get; }`
- `Midi : string { get; }`
- `Mie : string { get; }`
- `Mif : string { get; }`
- `Mime : string { get; }`
- `Mj2 : string { get; }`
- `Mjp2 : string { get; }`
- `Mk3d : string { get; }`
- `Mka : string { get; }`
- `Mks : string { get; }`
- `Mkv : string { get; }`
- `Mlp : string { get; }`
- `Mmd : string { get; }`
- `Mmf : string { get; }`
- `Mmr : string { get; }`
- `Mng : string { get; }`
- `Mny : string { get; }`
- `Mobi : string { get; }`
- `Mods : string { get; }`
- `Mov : string { get; }`
- `Movie : string { get; }`
- `Mp2 : string { get; }`
- `Mp21 : string { get; }`
- `Mp2a : string { get; }`
- `Mp3 : string { get; }`
- `Mp4 : string { get; }`
- `Mp4a : string { get; }`
- `Mp4s : string { get; }`
- `Mp4v : string { get; }`
- `Mpc : string { get; }`
- `Mpe : string { get; }`
- `Mpeg : string { get; }`
- `Mpg : string { get; }`
- `Mpg4 : string { get; }`
- `Mpga : string { get; }`
- `Mpkg : string { get; }`
- `Mpm : string { get; }`
- `Mpn : string { get; }`
- `Mpp : string { get; }`
- `Mpt : string { get; }`
- `Mpy : string { get; }`
- `Mqy : string { get; }`
- `Mrc : string { get; }`
- `Mrcx : string { get; }`
- `Ms : string { get; }`
- `Mscml : string { get; }`
- `Mseed : string { get; }`
- `Mseq : string { get; }`
- `Msf : string { get; }`
- `Msh : string { get; }`
- `Msi : string { get; }`
- `Msl : string { get; }`
- `Msty : string { get; }`
- `Mts : string { get; }`
- `Mus : string { get; }`
- `Musicxml : string { get; }`
- `Mvb : string { get; }`
- `Mwf : string { get; }`
- `Mxf : string { get; }`
- `Mxl : string { get; }`
- `Mxml : string { get; }`
- `Mxs : string { get; }`
- `Mxu : string { get; }`
- `N3 : string { get; }`
- `N_gage : string { get; }`
- `Nb : string { get; }`
- `Nbp : string { get; }`
- `Nc : string { get; }`
- `Ncx : string { get; }`
- `Nfo : string { get; }`
- `Ngdat : string { get; }`
- `Nitf : string { get; }`
- `Nlu : string { get; }`
- `Nml : string { get; }`
- `Nnd : string { get; }`
- `Nns : string { get; }`
- `Nnw : string { get; }`
- `Npx : string { get; }`
- `Nsc : string { get; }`
- `Nsf : string { get; }`
- `Ntf : string { get; }`
- `Nupkg : string { get; }`
- `Nzb : string { get; }`
- `Oa2 : string { get; }`
- `Oa3 : string { get; }`
- `Oas : string { get; }`
- `Obd : string { get; }`
- `Obj : string { get; }`
- `Oda : string { get; }`
- `Odb : string { get; }`
- `Odc : string { get; }`
- `Odf : string { get; }`
- `Odft : string { get; }`
- `Odg : string { get; }`
- `Odi : string { get; }`
- `Odm : string { get; }`
- `Odp : string { get; }`
- `Ods : string { get; }`
- `Odt : string { get; }`
- `OfficeTypes : string[] { get; }`
  <br>Gets all office document MIME types
- `Oga : string { get; }`
- `Ogg : string { get; }`
- `Ogv : string { get; }`
- `Ogx : string { get; }`
- `Omdoc : string { get; }`
- `Onepkg : string { get; }`
- `Onetmp : string { get; }`
- `Onetoc : string { get; }`
- `Onetoc2 : string { get; }`
- `Opf : string { get; }`
- `Opml : string { get; }`
- `Oprc : string { get; }`
- `OrgExt : string { get; }`
- `Osf : string { get; }`
- `Osfpvg : string { get; }`
- `Otc : string { get; }`
- `Otf : string { get; }`
- `Otg : string { get; }`
- `Oth : string { get; }`
- `Oti : string { get; }`
- `Otp : string { get; }`
- `Ots : string { get; }`
- `Ott : string { get; }`
- `Oxps : string { get; }`
- `Oxt : string { get; }`
- `P : string { get; }`
- `P10 : string { get; }`
- `P12 : string { get; }`
- `P7b : string { get; }`
- `P7c : string { get; }`
- `P7m : string { get; }`
- `P7r : string { get; }`
- `P7s : string { get; }`
- `P8 : string { get; }`
- `Pas : string { get; }`
- `Paw : string { get; }`
- `Pbd : string { get; }`
- `Pbm : string { get; }`
- `Pcap : string { get; }`
- `Pcf : string { get; }`
- `Pcl : string { get; }`
- `Pclxl : string { get; }`
- `Pct : string { get; }`
- `Pcurl : string { get; }`
- `Pcx : string { get; }`
- `Pdb : string { get; }`
- `Pdf : string { get; }`
- `Pfa : string { get; }`
- `Pfb : string { get; }`
- `Pfm : string { get; }`
- `Pfr : string { get; }`
- `Pfx : string { get; }`
- `Pgm : string { get; }`
- `Pgn : string { get; }`
- `Pgp : string { get; }`
- `Pic : string { get; }`
- `Pkg : string { get; }`
- `Pki : string { get; }`
- `Pkipath : string { get; }`
- `Plb : string { get; }`
- `Plc : string { get; }`
- `Plf : string { get; }`
- `Pls : string { get; }`
- `Pml : string { get; }`
- `Png : string { get; }`
- `Pnm : string { get; }`
- `Portpkg : string { get; }`
- `Pot : string { get; }`
- `Potm : string { get; }`
- `Potx : string { get; }`
- `Ppam : string { get; }`
- `Ppd : string { get; }`
- `Ppm : string { get; }`
- `Pps : string { get; }`
- `Ppsm : string { get; }`
- `Ppsx : string { get; }`
- `Ppt : string { get; }`
- `Pptm : string { get; }`
- `Pptx : string { get; }`
- `Pqa : string { get; }`
- `Prc : string { get; }`
- `Pre : string { get; }`
- `Prf : string { get; }`
- `Ps : string { get; }`
- `Psb : string { get; }`
- `Psd : string { get; }`
- `Psf : string { get; }`
- `Pskcxml : string { get; }`
- `Ptid : string { get; }`
- `Pub : string { get; }`
- `Pvb : string { get; }`
- `Pwn : string { get; }`
- `Pya : string { get; }`
- `Pyv : string { get; }`
- `Qam : string { get; }`
- `Qbo : string { get; }`
- `Qfx : string { get; }`
- `Qps : string { get; }`
- `Qt : string { get; }`
- `Qwd : string { get; }`
- `Qwt : string { get; }`
- `Qxb : string { get; }`
- `Qxd : string { get; }`
- `Qxl : string { get; }`
- `Qxt : string { get; }`
- `Ra : string { get; }`
- `Ram : string { get; }`
- `Rar : string { get; }`
- `Ras : string { get; }`
- `Razor : string { get; }`
- `Rcprofile : string { get; }`
- `Rdf : string { get; }`
- `Rdz : string { get; }`
- `Rep : string { get; }`
- `Res : string { get; }`
- `Rgb : string { get; }`
- `Rif : string { get; }`
- `Rip : string { get; }`
- `Ris : string { get; }`
- `Rl : string { get; }`
- `Rlc : string { get; }`
- `Rld : string { get; }`
- `Rm : string { get; }`
- `Rmi : string { get; }`
- `Rmp : string { get; }`
- `Rms : string { get; }`
- `Rmvb : string { get; }`
- `Rnc : string { get; }`
- `Roa : string { get; }`
- `Roff : string { get; }`
- `Rp9 : string { get; }`
- `Rpss : string { get; }`
- `Rpst : string { get; }`
- `Rq : string { get; }`
- `Rs : string { get; }`
- `Rsd : string { get; }`
- `Rss : string { get; }`
- `RtfExt : string { get; }`
- `Rtx : string { get; }`
- `S : string { get; }`
- `S3m : string { get; }`
- `Saf : string { get; }`
- `Sbml : string { get; }`
- `Sc : string { get; }`
- `Scd : string { get; }`
- `Scm : string { get; }`
- `Scq : string { get; }`
- `Scs : string { get; }`
- `Scurl : string { get; }`
- `Sda : string { get; }`
- `Sdc : string { get; }`
- `Sdd : string { get; }`
- `Sdkd : string { get; }`
- `Sdkm : string { get; }`
- `Sdp : string { get; }`
- `Sdw : string { get; }`
- `See : string { get; }`
- `Seed : string { get; }`
- `Sema : string { get; }`
- `Semd : string { get; }`
- `Semf : string { get; }`
- `Ser : string { get; }`
- `Setpay : string { get; }`
- `Setreg : string { get; }`
- `Sfd_hdstx : string { get; }`
- `Sfs : string { get; }`
- `Sfv : string { get; }`
- `Sgi : string { get; }`
- `Sgl : string { get; }`
- `Sgm : string { get; }`
- `Sgml : string { get; }`
- `Sh : string { get; }`
- `Shar : string { get; }`
- `Shf : string { get; }`
- `Sid : string { get; }`
- `Sig : string { get; }`
- `Sil : string { get; }`
- `Silo : string { get; }`
- `Sis : string { get; }`
- `Sisx : string { get; }`
- `Sit : string { get; }`
- `Sitx : string { get; }`
- `Skd : string { get; }`
- `Skm : string { get; }`
- `Skp : string { get; }`
- `Skt : string { get; }`
- `Sldm : string { get; }`
- `Sldx : string { get; }`
- `Slt : string { get; }`
- `Sm : string { get; }`
- `Smf : string { get; }`
- `Smi : string { get; }`
- `Smil : string { get; }`
- `Smv : string { get; }`
- `Smzip : string { get; }`
- `Snd : string { get; }`
- `Snf : string { get; }`
- `So : string { get; }`
- `Spc : string { get; }`
- `Spf : string { get; }`
- `Spl : string { get; }`
- `Spot : string { get; }`
- `Spp : string { get; }`
- `Spq : string { get; }`
- `Spx : string { get; }`
- `Sql : string { get; }`
- `Src : string { get; }`
- `Srt : string { get; }`
- `Sru : string { get; }`
- `Srx : string { get; }`
- `Ssdl : string { get; }`
- `Sse : string { get; }`
- `Ssf : string { get; }`
- `Ssml : string { get; }`
- `St : string { get; }`
- `Stc : string { get; }`
- `Std : string { get; }`
- `Stf : string { get; }`
- `Sti : string { get; }`
- `Stk : string { get; }`
- `Stl : string { get; }`
- `Str : string { get; }`
- `Stw : string { get; }`
- `Sub : string { get; }`
- `Sus : string { get; }`
- `Susp : string { get; }`
- `Sv4cpio : string { get; }`
- `Sv4crc : string { get; }`
- `Svc : string { get; }`
- `Svd : string { get; }`
- `Svg : string { get; }`
- `Svgz : string { get; }`
- `Swa : string { get; }`
- `Swf : string { get; }`
- `Swi : string { get; }`
- `Sxc : string { get; }`
- `Sxd : string { get; }`
- `Sxg : string { get; }`
- `Sxi : string { get; }`
- `Sxm : string { get; }`
- `Sxw : string { get; }`
- `T : string { get; }`
- `T3 : string { get; }`
- `Taglet : string { get; }`
- `Tao : string { get; }`
- `Tar : string { get; }`
- `Tar_gz : string { get; }`
- `Tcap : string { get; }`
- `Tcl : string { get; }`
- `Teacher : string { get; }`
- `Tei : string { get; }`
- `Teicorpus : string { get; }`
- `Tex : string { get; }`
- `Texi : string { get; }`
- `Texinfo : string { get; }`
- `Text : string { get; }`
- `Tfi : string { get; }`
- `Tfm : string { get; }`
- `Tga : string { get; }`
- `Tgz : string { get; }`
- `Thmx : string { get; }`
- `Tif : string { get; }`
- `Tiff : string { get; }`
- `Tmo : string { get; }`
- `Torrent : string { get; }`
- `Tpl : string { get; }`
- `Tpt : string { get; }`
- `Tr : string { get; }`
- `Tra : string { get; }`
- `Trm : string { get; }`
- `Tsd : string { get; }`
- `Tsv : string { get; }`
- `Ttc : string { get; }`
- `Ttf : string { get; }`
- `Ttl : string { get; }`
- `Twd : string { get; }`
- `Twds : string { get; }`
- `Txd : string { get; }`
- `Txf : string { get; }`
- `Txt : string { get; }`
- `U32 : string { get; }`
- `Udeb : string { get; }`
- `Ufd : string { get; }`
- `UfdlExt : string { get; }`
- `Ulx : string { get; }`
- `Umj : string { get; }`
- `Unityweb : string { get; }`
- `Uoml : string { get; }`
- `Uri : string { get; }`
- `Uris : string { get; }`
- `Urls : string { get; }`
- `Ustar : string { get; }`
- `Utz : string { get; }`
- `Uu : string { get; }`
- `Uva : string { get; }`
- `Uvd : string { get; }`
- `Uvf : string { get; }`
- `Uvg : string { get; }`
- `Uvh : string { get; }`
- `Uvi : string { get; }`
- `Uvm : string { get; }`
- `Uvp : string { get; }`
- `Uvs : string { get; }`
- `Uvt : string { get; }`
- `Uvu : string { get; }`
- `Uvv : string { get; }`
- `Uvva : string { get; }`
- `Uvvd : string { get; }`
- `Uvvf : string { get; }`
- `Uvvg : string { get; }`
- `Uvvh : string { get; }`
- `Uvvi : string { get; }`
- `Uvvm : string { get; }`
- `Uvvp : string { get; }`
- `Uvvs : string { get; }`
- `Uvvt : string { get; }`
- `Uvvu : string { get; }`
- `Uvvv : string { get; }`
- `Uvvx : string { get; }`
- `Uvvz : string { get; }`
- `Uvx : string { get; }`
- `Uvz : string { get; }`
- `Vcard : string { get; }`
- `Vcd : string { get; }`
- `Vcf : string { get; }`
- `Vcg : string { get; }`
- `Vcs : string { get; }`
- `Vcx : string { get; }`
- `VideoTypes : string[] { get; }`
  <br>Gets all video MIME types
- `Vis : string { get; }`
- `Viv : string { get; }`
- `Vob : string { get; }`
- `Vor : string { get; }`
- `Vox : string { get; }`
- `Vrml : string { get; }`
- `Vsd : string { get; }`
- `Vsf : string { get; }`
- `Vss : string { get; }`
- `Vst : string { get; }`
- `Vsw : string { get; }`
- `Vtu : string { get; }`
- `Vxml : string { get; }`
- `W3d : string { get; }`
- `Wad : string { get; }`
- `Wav : string { get; }`
- `Wax : string { get; }`
- `Wbmp : string { get; }`
- `Wbs : string { get; }`
- `Wbxml : string { get; }`
- `Wcm : string { get; }`
- `Wdb : string { get; }`
- `Wdp : string { get; }`
- `Weba : string { get; }`
- `Webm : string { get; }`
- `Webp : string { get; }`
- `Wg : string { get; }`
- `Wgt : string { get; }`
- `Wks : string { get; }`
- `Wm : string { get; }`
- `Wma : string { get; }`
- `Wmd : string { get; }`
- `Wmf : string { get; }`
- `Wml : string { get; }`
- `Wmlc : string { get; }`
- `Wmls : string { get; }`
- `Wmlsc : string { get; }`
- `Wmv : string { get; }`
- `Wmx : string { get; }`
- `Wmz : string { get; }`
- `Woff : string { get; }`
- `Woff2 : string { get; }`
- `Wpd : string { get; }`
- `Wpl : string { get; }`
- `Wps : string { get; }`
- `Wqd : string { get; }`
- `Wri : string { get; }`
- `Wrl : string { get; }`
- `Wsdl : string { get; }`
- `Wspolicy : string { get; }`
- `Wtb : string { get; }`
- `Wvx : string { get; }`
- `X32 : string { get; }`
- `X3d : string { get; }`
- `X3db : string { get; }`
- `X3dbz : string { get; }`
- `X3dv : string { get; }`
- `X3dvz : string { get; }`
- `X3dz : string { get; }`
- `Xaml : string { get; }`
- `Xap : string { get; }`
- `Xar : string { get; }`
- `Xbap : string { get; }`
- `Xbd : string { get; }`
- `Xbm : string { get; }`
- `Xdf : string { get; }`
- `Xdm : string { get; }`
- `Xdp : string { get; }`
- `Xdssc : string { get; }`
- `Xdw : string { get; }`
- `Xenc : string { get; }`
- `Xer : string { get; }`
- `Xfdf : string { get; }`
- `Xfdl : string { get; }`
- `Xht : string { get; }`
- `Xhtml : string { get; }`
- `Xhvml : string { get; }`
- `Xif : string { get; }`
- `Xla : string { get; }`
- `Xlam : string { get; }`
- `Xlc : string { get; }`
- `Xlf : string { get; }`
- `Xlm : string { get; }`
- `Xls : string { get; }`
- `Xlsb : string { get; }`
- `Xlsm : string { get; }`
- `Xlsx : string { get; }`
- `Xlt : string { get; }`
- `Xltm : string { get; }`
- `Xltx : string { get; }`
- `Xlw : string { get; }`
- `Xm : string { get; }`
- `Xml : string { get; }`
- `Xo : string { get; }`
- `Xop : string { get; }`
- `Xpi : string { get; }`
- `Xpl : string { get; }`
- `Xpm : string { get; }`
- `Xpr : string { get; }`
- `Xps : string { get; }`
- `Xpw : string { get; }`
- `Xpx : string { get; }`
- `Xsl : string { get; }`
- `Xslt : string { get; }`
- `Xsm : string { get; }`
- `Xspf : string { get; }`
- `Xul : string { get; }`
- `Xvm : string { get; }`
- `Xvml : string { get; }`
- `Xwd : string { get; }`
- `Xyz : string { get; }`
- `Xz : string { get; }`
- `Yang : string { get; }`
- `Yin : string { get; }`
- `Z1 : string { get; }`
- `Z2 : string { get; }`
- `Z3 : string { get; }`
- `Z4 : string { get; }`
- `Z5 : string { get; }`
- `Z6 : string { get; }`
- `Z7 : string { get; }`
- `Z8 : string { get; }`
- `Zaz : string { get; }`
- `Zip : string { get; }`
- `Zir : string { get; }`
- `Zirz : string { get; }`
- `Zmm : string { get; }`
- `_123 : string { get; }`
- `_3dml : string { get; }`
- `_3ds : string { get; }`
- `_3g2 : string { get; }`
- `_3gp : string { get; }`
- `_7z : string { get; }`

**Felder**

- `OpenXml : string`
  <br>MIME type for OpenXML spreadsheet files

### `NotificationObject`

`abstract class`

Implementation of `INotifyPropertyChanged` and `INotifyPropertyChanging`

**Methoden**

- `Clone() : object`

**Eigenschaften**

- `IsNotifying : bool { get; set; }`
  <br>Enables/Disables property change notification.

**Ereignisse**

- `IsNotifyingChanged : EventHandler`
  <br>Occurs when [IsNotifying changed].
- `PropertyChanged : PropertyChangedEventHandler`
  <br>Occurs when a property value changes.
- `PropertyChanging : PropertyChangingEventHandler`
  <br>Occurs when a property value is changing.

### `PausableCancellationToken`

`static class`

Provides extension methods for pausable cancellation tokens

**Extension Methods**

- `IsPaused(this CancellationToken token) : bool`
  <br>Determines whether the cancellation token is currently paused
- `RegisterPaused(this CancellationToken token, Action<CancellationToken, bool> pausedChangedAction) : void`
- `WaitWhenPaused(this CancellationToken token) : Task`
  <br>Waits asynchronously while the cancellation token is paused

### `PausableCancellationTokenSource`

`class`

A cancellation token source that can be paused and resumed

**Konstruktoren**

- `PausableCancellationTokenSource()`

**Methoden**

- `CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2) : PausableCancellationTokenSource`
  <br>Creates a pausable cancellation token source that is linked to the specified two tokens
- `CreateLinkedTokenSource(CancellationToken[] tokens) : PausableCancellationTokenSource`
  <br>Creates a pausable cancellation token source that is linked to the specified tokens
- `Pause() : void`
- `PauseAfter(TimeSpan delay) : void`
  <br>Pauses the cancellation token source after the specified delay
- `Resume() : void`
- `ResumeAfter(TimeSpan delay) : void`
  <br>Resumes the cancellation token source after the specified delay

**Eigenschaften**

- `IsPaused : bool { get; }`
  <br>Gets a value indicating whether the token source is currently paused

### `SingletonBase<T>`

`abstract class`

Base class for singleton pattern (everything that inherits from this class can be accessed via Type.Instance)

**Eigenschaften**

- `Instance : T { get; }`
  <br>Gets the current instance

### `Waiter`

`static class`

Provides utility methods for asynchronously waiting for conditions or results

**Extension Methods**

- `WaitForResultAsync<T>(this Func<T> expression, CancellationToken cancellationToken = null) : Task<T>`
- `WaitForTrueAsync(this Func<bool> expression, CancellationToken cancellationToken = null) : Task`

## Nextended.Core.Attributes

### `AutoGenerateComAttribute`

`class`

Attribute to automatically generate a COM interface and a COM class for the class or enum it is applied to.

**Konstruktoren**

- `AutoGenerateComAttribute()`

### `AutoGenerateDtoAttribute`

`class`

Attribute to automatically generate a COM interface and a COM class for the class or enum it is applied to. (The classes are generated at compile-time using a T4 template.)

**Konstruktoren**

- `AutoGenerateDtoAttribute()`

**Eigenschaften**

- `AddContainingNamespaceUsings : bool { get; set; }`
- `AddReferencedNamespacesUsings : bool { get; set; }`
- `AutoGenerateDerived : bool { get; set; }`
  <br>Gets or sets a value indicating whether derived types should be automatically generated.
- `BaseType : string { get; set; }`
  <br>Sets the base type for the generated DTO class.
- `ClassModifier : Modifier { get; set; }`
  <br>Modifier for generated DTO classes.
- `DefaultPropertyInterfaceAccess : InterfaceProperty { get; set; }`
- `GenerateBeforeAndAfterAssignPartialsInMapping : bool { get; set; }`
  <br>If this is true partials methods are generated that can be overwritten to do custom assignments
- `GenerateMapping : bool { get; set; }`
  <br>Indicates whether the "ToNetMappingAttribute" should be generated for automatic COM to .NET conversion.
- `GeneratedClassName : string { get; set; }`
  <br>Allows you to override the generated class name. If this is set, prefix and suffix logic will not be applied.
- `InterfaceModifier : Modifier { get; set; }`
  <br>Modifier for generated DTO interfaces.
- `Interfaces : string[] { get; set; }`
  <br>Adds interfaces to the generated DTO class and generated interface.
- `IsComCompatible : bool { get; set; }`
  <br>If set to true, the generated classes are COM visible and fully COM compatible.
- `KeepAttributesOnGeneratedClass : bool { get; set; }`
  <br>If set to true, the generated classes will keep the attributes from the original class.
- `KeepAttributesOnGeneratedInterface : bool { get; set; }`
  <br>If set to true, the generated interfaces will keep the attributes from the original class.
- `KeepPropertyAttributesOnGeneratedClass : bool { get; set; }`
  <br>If set to true, the generated property on classes will keep the attributes from the original class.
- `KeepPropertyAttributesOnGeneratedInterface : bool { get; set; }`
  <br>If set to true, the generated property on interfaces will keep the attributes from the original class.
- `Namespace : string { get; set; }`
  <br>Gets or sets the namespace used for the generated classes and interfaces.
- `PreClassString : string { get; set; }`
  <br>A string that will be added before the generated class, useful for adding attributes or something.
- `PreInterfaceString : string { get; set; }`
  <br>A string that will be added before the generated interface, useful for adding attributes or something.
- `Prefix : string { get; set; }`
  <br>Gets or sets the prefix for the generated class and interface names (e.g., IComMyType).
- `PropertiesToIgnore : string[] { get; set; }`
  <br>set properties name you dont want to generate inside the dto. If possible you should use the attribute `IgnoreOnGenerationAttribute` but if you cant control the source code this is an alternative.
- `Suffix : string { get; set; }`
  <br>Gets or sets the suffix for the generated class and interface names (e.g., IComMyTypeSuffix).
- `ToDtoMethodName : string { get; set; }`
  <br>Gets or sets the name of the generated mapping method.
- `ToSourceMethodName : string { get; set; }`
  <br>Gets or sets the name of the generated mapping method that is called by the DTO class to convert the DTO back to the source class.
- `Usings : string[] { get; set; }`

### `ConfirmationMode`

`enum`

Art der Nachfrage

**Werte**

- `ConfirmAlways`
  <br>Ask always for a confirmation
- `ConfirmWithOptionDontShowAgain`
  <br>Default question with option for do not ask again
- `NoConfirmation`
  <br>NoConfirmation
- `value__`

### `EditableList`

`class`

Editierbare liste

**Konstruktoren**

- `EditableList()`

**Eigenschaften**

- `CanAddAndRemoveEntries : bool { get; set; }`
  <br>Set to true to allow add and remove objects
- `ConfirmRemoveObject : ConfirmationMode { get; set; }`
  <br>if true user must confirm the deletion
- `ObjectType : Type { get; set; }`
  <br>typeof object in list

### `GenerateBeforeAndAfterAssignPartialsInMappingAttribute`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `GenerateBeforeAndAfterAssignPartialsInMappingAttribute()`

### `GenerationPropertySettingAttribute`

`class`

Can be applied to a property or field of a class that uses the AutoGenerateComAttribute to specify the type to be used for the COM interface and the COM class, or to provide custom names.

**Konstruktoren**

- `GenerationPropertySettingAttribute()`
- `GenerationPropertySettingAttribute(string comPropertyName)`
  <br>Initializes a new instance of the `GenerationPropertySettingAttribute` class with the specified property name for the COM interface/class.

**Eigenschaften**

- `InterfaceAccess : InterfaceProperty { get; set; }`
- `KeepAttributesOnGeneratedClass : bool { get; set; }`
  <br>If set to true, the generated classes will keep the attributes from the original class.
- `KeepAttributesOnGeneratedInterface : bool { get; set; }`
  <br>If set to true, the generated interfaces will keep the attributes from the original class.
- `MapWithClassMapper : bool { get; set; }`
  <br>If this property is set to true, the property will use the class mapper when automatic .NET mapping is generated for the class.
- `PreClassString : string { get; set; }`
  <br>A string that will be added before the generated property on the generated class, useful for adding attributes or something.
- `PreInterfaceString : string { get; set; }`
  <br>A string that will be added before the generated property on the interface, useful for adding attributes or something.
- `PropertyName : string { get; set; }`
  <br>The name to be used for the property in the COM interface and COM class.

### `IgnoreOnGenerationAttribute`

`class`

Set this attribute on a property or field to ignore it during the generation of DTOs or COM interfaces.

**Konstruktoren**

- `IgnoreOnGenerationAttribute()`

### `IncludeInDetailsAttribute`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `IncludeInDetailsAttribute(string group = null, int maxDepth = 6)`

**Eigenschaften**

- `Group : string { get; }`
- `MaxDepth : int { get; }`

### `ProvideAsEdmAttribute`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ProvideAsEdmAttribute()`
- `ProvideAsEdmAttribute(string name)`

**Eigenschaften**

- `Name : string { get; }`
- `ProvideInherits : bool { get; set; }`

### `RegisterAsAttribute`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `RegisterAsAttribute(Type registerAsType, int order = 99)`
- `RegisterAsAttribute(Type registerAsType, object serviceKey, int order = 99)`

**Methoden**

- `GetServiceDescriptor() : IEnumerable<ServiceDescriptor>`

**Eigenschaften**

- `Enabled : bool { get; set; }`
- `Order : int { get; set; }`
- `RegisterAsImplementation : bool { get; set; }`
- `RegisterAsType : Type { get; }`
- `ReplaceServices : bool { get; set; }`
- `ServiceKey : object { get; set; }`
- `ServiceLifetime : ServiceLifetime { get; set; }`

### `SelectableItemAttribute`

`class`

Attribute for Properties in Auto Object Editor Template with selectable items

**Konstruktoren**

- `SelectableItemAttribute(string possibleValuesPropertyName)`
  <br>Initializes a new instance of the `SelectableItemAttribute` class.

**Eigenschaften**

- `IsEditable : bool { get; set; }`
  <br>Gets or sets a value indicating whether [is editable].
- `IsFilterable : bool { get; set; }`
  <br>Set to true if you want to have possibility to filter possible values
- `IsReadOnly : bool { get; set; }`
  <br>Gets or sets a value indicating whether [is read only].
- `PossibleValues : ObservableCollection<object> { get; set; }`
  <br>List of Possible Values
- `PossibleValuesPropertyName : string { get; set; }`
  <br>Name of the Property that returns a list with possible values
- `SelectedIndex : int { get; set; }`
  <br>SelectedIndex
- `UseListBox : bool { get; set; }`
  <br>If true template will use a listbox otherwise its a combo box

### `SettingsPropertyAttribute`

`class`

Attribute für Eigenschaften, die innerhalb einer BaseOptionPage automatisch geladen und gespeichert werden sollen

**Konstruktoren**

- `SettingsPropertyAttribute(string settingsKey, object defaultValue)`
  <br>Initializes a new instance of the `SettingsPropertyAttribute` class.

**Eigenschaften**

- `AllowResetValue : bool { get; set; }`
  <br>Gibt an ob der wert zurückgesetzt werden kann
- `DefaultValue : object { get; set; }`
  <br>Der defaultwert
- `ResetValueConverterFunc : string { get; set; }`
  <br>Funktion, die beim zurücksetzen aufgerufen werden soll (wenn keine vorhanden wird nur der wert auf defaultvalue gesetzt)
- `SettingsKey : string { get; set; }`
  <br>Der Key

## Nextended.Core.COM

### `BaseComList<T>`

`abstract class`

Basis Callback Klasse

**Methoden**

- `Add(object aValue) : void`
  <br>Fügt Werte in die Liste
- `Count() : int`
- `Get(int index) : object`
  <br>Gets the specified index.
- `GetEnumerator() : IEnumerator`
- `Items() : IEnumerable<T>`

### `ComId`

`struct`

COM Id für die Kommunikation zwischen CP-Server .NET und CP-Server Delphi. Wird im Delphi cpserverembedded.dll genutzt.

**Konstruktoren**

- `ComId(int intvalue, Guid guid)`
  <br>Setzt

**Felder**

- `Guid : Guid`
  <br>Guid ID
- `Int : int`
  <br>Int ID

### `ComList`

`class`

Allgemeine Liste für COM Objekte

**Konstruktoren**

- `ComList()`

**Methoden**

- `Add(object aValue) : void`
  <br>Fügt Elemente in die Liste
- `Count() : int`
- `Get(int index) : object`
  <br>Liefert das Element an der Stelle index.
- `GetEnumerator() : IEnumerator`

### `ComList<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ComList()`
- `ComList(IList<T> list)`

### `ComToNetList<TCom, TNet>`

`class`

Allgemeine Liste für COM Objekte

**Konstruktoren**

- `ComToNetList(Func<TCom, TNet> comToNetConverter = null)`
- `ComToNetList(IList<TNet> list, Func<TCom, TNet> comToNetConverter = null)`

### `IComList`

`interface`

IComList

**Methoden**

- `Add(object aValue) : void`
  <br>Element hinzufügen
- `Count() : int`
- `Get(int index) : object`
  <br>Element aus der Liste holen

### `NetToComList<TNet, TCom>`

`class`

COM Liste für die Übergabe von .NET an COM

**Konstruktoren**

- `NetToComList(Func<TNet, TCom> netToComConverter = null)`
- `NetToComList(IList<TCom> list, Func<TNet, TCom> netToComConverter = null)`
- `NetToComList(IList<TNet> collection, Func<TNet, TCom> netToComConverter = null)`

## Nextended.Core.Contracts

### `IIncludePathDefinition`

`interface`

_Keine Beschreibung._

**Methoden**

- `GetPaths() : IEnumerable<string>`

### `IRange<T>`

`interface`

Defines a generic interface for a range.

**Methoden**

- `ClampLength(RangeLength<T> min, RangeLength<T> max) : RangeOf<T>`
- `Contains(T value) : bool`
- `Intersection(IRange<T> other) : IRange<T>`
- `Intersects(IRange<T> other) : bool`
- `IsAdjacent(IRange<T> other, double tolerance = 0) : bool`
- `IsInRange(T value) : bool`
- `Union(IRange<T> other) : IRange<T>`

**Eigenschaften**

- `End : T { get; }`
  <br>The end value of the range.
- `Length : RangeLength<T> { get; }`
  <br>Length
- `Start : T { get; }`
  <br>The start value of the range.

### `ISearchable<TSelf>`

`interface`

_Keine Beschreibung._

**Methoden**

- `AllOfString() : Expression<Func<TSelf, string>>[]`
- `AllWithAttribute<TAttribute>() : Expression<Func<TSelf, string>>[]`
- `Combine(Expression<Func<TSelf, string>>[][] arrays) : Expression<Func<TSelf, string>>[]`
- `GetSearchProperties() : Expression<Func<TSelf, string>>[]`

**Eigenschaften**

- `SearchProperties : Expression<Func<TSelf, string>>[] { get; }`

### `IStringEncoding`

`interface`

Defines the contract for string encoding and decoding operations.

**Methoden**

- `Decode(string str) : string`
  <br>Decodes a string using the specific encoding algorithm.
- `Encode(string str) : string`
  <br>Encodes a string using the specific encoding algorithm.

### `IStringEncodingExt`

`interface`

Extends `IStringEncoding` with hooks for pre- and post-processing during encoding and decoding operations.

**Methoden**

- `AfterDecode(Func<string, string> onAfterDecode) : IStringEncodingExt`
- `AfterEncode(Func<string, string> onAfterEncode) : IStringEncodingExt`
- `BeforeDecode(Func<string, string> onBeforeDecode) : IStringEncodingExt`
- `BeforeEncode(Func<string, string> onBeforeEncode) : IStringEncodingExt`

### `IStringEncryption`

`interface`

Interface for string encryption and decryption operations.

**Methoden**

- `Decrypt(string str, string key) : string`
  <br>Decrypts the specified encrypted string using the provided key.
- `Encrypt(string str, string key) : string`
  <br>Encrypts the specified string using the provided key.

### `IStringHashing`

`interface`

Interface for string hashing operations with optional salt support.

**Methoden**

- `Hash(string input, string salt = null) : string`
  <br>Computes a hash of the input string, optionally using a salt for additional security.

### `IUploadableFile`

`interface`

Interface representing a file that can be uploaded, supporting both local file paths and remote URLs.

**Methoden**

- `EnsureDataLoadedAsync(HttpClient client = null) : Task`
  <br>Ensures that the file data is loaded. If the data is not yet loaded and a URL is available, downloads the file from the URL.

**Eigenschaften**

- `ContentType : string { get; set; }`
  <br>Gets or sets the MIME content type of the file (e.g., "text/plain", "image/jpeg").
- `Data : byte[] { get; set; }`
  <br>Gets or sets the binary data of the file.
- `Extension : string { get; set; }`
  <br>Gets or sets the file extension (e.g., ".txt", ".jpg").
- `FileName : string { get; set; }`
  <br>Gets or sets the name of the file.
- `Path : string { get; set; }`
  <br>Gets or sets the local file system path to the file.
- `Size : long { get; set; }`
  <br>Gets or sets the size of the file in bytes.
- `Url : string { get; set; }`
  <br>Gets or sets the URL from which the file can be downloaded.

## Nextended.Core.DeepClone

### `AttributesCollections`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `AttributesCollections(List<Attribute> attrs)`

**Methoden**

- `Add(Attribute attr) : void`
- `Remove(Attribute attr) : void`

### `CloneConfigrationManager`

`static class`

globa ConfigrationManager, for managing the library settings

**Felder**

- `OnAttributeCollectionChanged : Action<IClonerProperty>`
- `OnPropertTypeSet : Func<IClonerProperty, Type>`

### `CloneLevel`

`enum`

CloneLevel FirstLevelOnly = Only InternalTypes Hierarki = All types Hierarki

**Werte**

- `FirstLevelOnly`
  <br>Only InternalTypes
- `Hierarchical`
  <br>All types
- `value__`

### `Cloner`

`static class`

Cloner

**Extension Methods**

- `ActAsInterface<T>(this object o) : T`
  <br>Convert an object to an interface The object dose not have to inherit from the interface as the library will handle the job of doing it
- `CloneDeep(this object objectToBeCloned, FieldType fieldType = 0) : object`
  <br>DeepClone object
- `CloneDeep<T>(this T objectToBeCloned, ClonerSettings settings) : T`
- `CloneDeep<T>(this T objectToBeCloned, FieldType fieldType = 0) : T`
- `CloneDynamic(this object objectToBeCloned) : object`
  <br>DeepClone dynamic(AnonymousType) object
- `CloneTo(this object itemToClone, object CloneToItem) : void`
  <br>This will handle only internal types and noneinternal type must be of the same type to be cloned
- `CreateInstance(this Type type, object[] args) : object`
  <br>Create CreateInstance() this will use ILGenerator to create new object from the cached ILGenerator This is alot faster then using Activator or GetUninitializedObject. The library will be using ILGenerator or Expression depending on the platform and then cach both the contructorinfo and the type, so it can be reused later on
- `CreateProxyInstance(this Type type) : INotifyPropertyChanged`
  <br>Create a type that implement PropertyChanged. Note it will only include properties that are virtual If type containe PropertyChanged(object sender, PropertyChangedEventArgs e) it will be bound automatically otherwise you will have to add it manually
- `GetFastDeepClonerFields(this Type type) : List<IClonerProperty>`
  <br>will return fieldInfo from the cached fieldinfo. Get and set value is much faster.
- `GetFastDeepClonerProperties(this Type type) : List<IClonerProperty>`
  <br>will return propertyInfo from the cached propertyInfo. Get and set value is much faster.
- `GetField(this Type type, string name) : IClonerProperty`
  <br>Get field by Name
- `GetListItemType(this Type listType) : Type`
  <br>Get the internal item type of the List or ObservableCollection types
- `GetObjectType(this string typePath, string assembly) : Type`
  <br>This will try and load the assembly and cached then from that assembly it will load typePath and also cach it, so it will load much faster next time
- `GetProperty(this Type type, string name) : IClonerProperty`
  <br>Get Property by name
- `ValueByType(this Type propertyType, object defaultValue = null) : object`
  <br>Get DefaultValue by type
- `ValueConverter(this object value, Type datatype, object defaultValue = null) : object`
  <br>Convert Value from Type to Type when fail a default value will be loaded. can handle all known types like datetime, time span, string, long etc ex "1115rd" to int? will return null "152" to int? 152
- `ValueConverter<T>(this object value, object defaultValue = null) : T`
  <br>Convert Value from Type to Type when fail a default value will be loaded. can handle all known types like datetime, time span, string, long etc ex "1115rd" to int? will return null "152" to int? 152

**Methoden**

- `ActAsInterface(Type interfaceType, object o) : object`
  <br>Convert an object to an interface The object dose not have to inherit from the interface as the library will handle the job of doing it
- `CleanCachedItems() : void`
- `CreateInstance<T>(object[] args) : T`
  <br>Create CreateInstance() this will use ILGenerator to create new object from the cached ILGenerator This is alot faster then using Activator or GetUninitializedObject. TThe library will be using ILGenerator or Expression depending on the platform and then cach both the contructorinfo and the type, so it can be reused later on
- `CreateProxyInstance<T>() : T`

### `ClonerColumn`

`class`

This attribute is used be CloneTo Method Add this attribute to map property from one class to another class

**Konstruktoren**

- `ClonerColumn(string columnName)`
  <br>map property from one class to another class

**Eigenschaften**

- `ColumnName : string { get; }`
  <br>ColumnName

### `ClonerIgnoreAttribute`

`class`

Ignore Properties or Field that containe this attribute

**Konstruktoren**

- `ClonerIgnoreAttribute()`

### `ClonerPrimaryIdentifire`

`class`

Incase you have circular references in some object You could mark an identifier or a primary key property so that fastDeepcloner could identify them

**Konstruktoren**

- `ClonerPrimaryIdentifire()`

### `ClonerProperty`

`class`

_Keine Beschreibung._

**Methoden**

- `Add(Attribute attr) : void`
- `ContainAttribute(Type type) : bool`
- `ContainAttribute<T>() : bool`
- `GetCustomAttribute(Type type) : Attribute`
- `GetCustomAttribute<T>() : T`
- `GetCustomAttributes(Type type) : IEnumerable<Attribute>`
- `GetCustomAttributes<T>() : IEnumerable<T>`
- `GetValue(object o) : object`
- `SetValue(object o, object value) : void`

**Eigenschaften**

- `Attributes : AttributesCollections { get; set; }`
- `CanRead : bool { get; }`
- `CanWrite : bool { get; }`
- `FastDeepClonerIgnore : bool { get; }`
- `FastDeepClonerPrimaryIdentifire : bool { get; }`
- `FullName : string { get; }`
- `GetMethod : Func<object, object> { get; set; }`
- `IsInternalType : bool { get; }`
- `IsVirtual : bool? { get; }`
- `Name : string { get; }`
- `NoneCloneable : bool { get; }`
- `PropertyGetValue : MethodInfo { get; }`
- `PropertySetValue : MethodInfo { get; }`
- `PropertyType : Type { get; set; }`
- `ReadAble : bool { get; }`
- `SetMethod : Action<object, object> { get; set; }`

### `ClonerSettings`

`class`

FastDeepClonerSettings

**Konstruktoren**

- `ClonerSettings()`

**Eigenschaften**

- `CloneLevel : CloneLevel { get; set; }`
  <br>CloneDeep Level
- `FieldType : FieldType { get; set; }`
  <br>Field type
- `OnCreateInstance : CreateInstance { get; set; }`
  <br>override Activator CreateInstance and use your own method

### `CreateInstance`

`delegate`

_Keine Beschreibung._

**Konstruktoren**

- `CreateInstance(object object, IntPtr method)`

**Methoden**

- `BeginInvoke(Type type, AsyncCallback callback, object object) : IAsyncResult`
- `EndInvoke(IAsyncResult result) : object`
- `Invoke(Type type) : object`

### `Extensions`

`static class`

FastDeepCloner Extensions

**Extension Methods**

- `IsAnonymousType(this Type type) : bool`
  <br>Validate if type is AnonymousType This is the very basic validation
- `IsInternalType(this Type underlyingSystemType) : bool`
  <br>Determines if the specified type is a Class type.

### `FieldType`

`enum`

PropertyField for property FielInfo for property

**Werte**

- `Both`
- `FieldInfo`
- `PropertyInfo`
- `value__`

### `IClonerProperty`

`interface`

Interface for FastDeepClonerProperty

**Methoden**

- `Add(Attribute attr) : void`
  <br>Using this method will trigger ConfigrationManager.OnAttributeCollectionChanged
- `ContainAttribute(Type type) : bool`
  <br>Validate if attribute type exist
- `ContainAttribute<T>() : bool`
- `GetCustomAttribute(Type type) : Attribute`
  <br>Get first found attribute type
- `GetCustomAttribute<T>() : T`
- `GetCustomAttributes(Type type) : IEnumerable<Attribute>`
  <br>Get a collection of attributes
- `GetCustomAttributes<T>() : IEnumerable<T>`
- `GetValue(object o) : object`
  <br>Get Value
- `SetValue(object o, object value) : void`
  <br>Set Value

**Eigenschaften**

- `Attributes : AttributesCollections { get; set; }`
  <br>All available attributes
- `CanRead : bool { get; }`
  <br>CanRead= !(field.IsInitOnly || field.FieldType == typeof(IntPtr) || field.IsLiteral); or for PropertyInfo CanRead= !(!property.CanWrite || !property.CanRead || property.PropertyType == typeof(IntPtr) || property.GetIndexParameters().Length &gt; 0);
- `CanWrite : bool { get; }`
  <br>If you could write to the propertyInfo
- `FastDeepClonerIgnore : bool { get; }`
  <br>Ignored
- `FastDeepClonerPrimaryIdentifire : bool { get; }`
  <br>Incase you have circular references in some object You could mark an identifier or a primary key property so that fastDeepcloner could identify them
- `FullName : string { get; }`
  <br>Property FullName
- `GetMethod : Func<object, object> { get; set; }`
  <br>Get Method for GetValue()
- `IsInternalType : bool { get; }`
  <br>Is a reference type eg not GetTypeInfo().IsClass
- `IsVirtual : bool? { get; }`
  <br>IsVirtual
- `Name : string { get; }`
  <br>PropertyName
- `NoneCloneable : bool { get; }`
  <br>Apply this to properties that cant be cloned, eg ImageSource and other controls. Those property will still be copied insted of cloning
- `PropertyGetValue : MethodInfo { get; }`
  <br>Exist only for PropertyInfo
- `PropertySetValue : MethodInfo { get; }`
  <br>Exist only for PropertyInfo
- `PropertyType : Type { get; set; }`
  <br>Type
- `ReadAble : bool { get; }`
  <br>Simple can read. this should have been called CanRead to bad we alread have CanRead Property, its a pain to change it now.
- `SetMethod : Action<object, object> { get; set; }`
  <br>Set Method for SetValue()

### `NoneCloneable`

`class`

Apply this to properties that cant be cloned, eg ImageSource and other controls. Those property will still be copied insted of cloning

**Konstruktoren**

- `NoneCloneable()`

### `SafeValueType<T, P>`

`class`

CustomValueType Dictionary

**Konstruktoren**

- `SafeValueType(ConcurrentDictionary<T, P> dictionary = null)`

**Methoden**

- `Get(T key) : P`
- `GetOrAdd(T key, P item, bool overwrite = false) : P`
- `TryAdd(T key, P item, bool overwrite = false) : bool`

## Nextended.Core.Encode

### `Base64Encoding`

`class`

Provides Base64 encoding and decoding functionality for strings.

**Konstruktoren**

- `Base64Encoding()`

### `EncodeAction`

`class`

Represents an encoding action that can encode or decode a string using a specific encoding algorithm.

**Methoden**

- `Decode() : string`
- `Encode() : string`

### `EncodingActions`

`class`

Provides access to different encoding actions (Base64, Hex) for a string.

**Eigenschaften**

- `Base64 : EncodeAction { get; }`
  <br>Gets the Base64 encoding action for the string.
- `Hex : EncodeAction { get; }`
  <br>Gets the Hex encoding action for the string.

### `EncodingExtensions`

`static class`

Provides extension methods for encoding and decoding strings.

**Extension Methods**

- `EncodeDecode(this string str) : EncodingActions`
  <br>Creates an encoding actions object that provides access to various encoding/decoding operations for the string.

### `HexEncoding`

`class`

Provides hexadecimal encoding and decoding functionality for strings. Converts strings to their hexadecimal representation and vice versa.

**Konstruktoren**

- `HexEncoding()`

### `StringEncodingBase<T>`

`abstract class`

_Keine Beschreibung._

**Methoden**

- `AddReplacements(KeyValuePair<string, string>[] pairs) : T`
- `AddReplacements(string key, string value) : T`
- `AfterDecode(Func<string, string> onAfterDecode) : T`
- `AfterEncode(Func<string, string> onAfterEncode) : T`
- `BeforeDecode(Func<string, string> onBeforeDecode) : T`
- `BeforeEncode(Func<string, string> onBeforeEncode) : T`
- `ClearReplacements() : T`
- `Decode(string encodedText) : string`
- `Encode(string plainText) : string`
- `SetEncoding(Encoding value) : T`
- `SetReplacements(IDictionary<string, string> replacements) : T`

**Eigenschaften**

- `Encoding : Encoding { get; set; }`
- `Replacements : IDictionary<string, string> { get; set; }`

## Nextended.Core.Encryption

### `AesEncryption`

`class`

Provides string encryption and decryption using the AES algorithm with PBKDF2-SHA512 key derivation. This implementation uses modern .NET cryptographic APIs.

**Konstruktoren**

- `AesEncryption()`

**Methoden**

- `Decrypt(string cipherText, string key) : string`
- `Encrypt(string clearText, string key) : string`
  <br>Encrypts the specified clear text using AES encryption with the provided key.

**Eigenschaften**

- `Iterations : int { get; set; }`
  <br>Gets or sets the number of iterations for PBKDF2 key derivation. Default is 1223. Higher values increase security but reduce performance.

### `EncryptionExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `Decrypt(this string str, string key = null) : string`
- `Encrypt(this string str, string key = null) : string`

## Nextended.Core.Enums

### `GeneratedModelType`

`enum`

Specifies the type of model to generate during code generation (class, struct, record, or record struct).

**Werte**

- `Class`
- `Record`
- `RecordStruct`
- `Struct`
- `Unset`
- `value__`

### `GeneratedModelTypeExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToCSharpKeyword(this GeneratedModelType modifier) : string`
- `ToCSharpKeyword(this GeneratedModelType? modifier) : string`

### `InterfaceProperty`

`enum`

Specifies the accessor types for interface properties (get, set, or both).

**Werte**

- `Get`
- `GetAndSet`
- `Set`
- `Unset`
- `value__`

### `InterfacePropertyExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToCSharpKeyword(this InterfaceProperty property) : string`

### `JsonArrayGeneration`

`enum`

Specifies how JSON arrays should be generated in code generation scenarios.

**Werte**

- `Array`
  <br>Generate as T[] array.
- `List`
  <br>Generate as List&lt;T&gt; collection.
- `value__`

### `Modifier`

`enum`

Specifies the access modifier for generated code (public, private, protected, or internal).

**Werte**

- `Internal`
- `Private`
- `Protected`
- `Public`
- `Unset`
- `value__`

### `ModifierExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToCSharpKeyword(this Modifier modifier) : string`
- `ToCSharpKeyword(this Modifier? modifier) : string`

## Nextended.Core.Extensions

### `AssemblyExtensions`

`static class`

Extensions for Assembly

**Extension Methods**

- `AsyncVoidMethods(this Assembly assembly) : IEnumerable<MethodInfo>`
  <br>Returns Async and void Methods
- `GetLoadableTypes(this Assembly assembly) : IEnumerable<Type>`
  <br>Returns loaded types in Assembly

### `ClassMappingExtensions`

`static class`

Extension methods for object-to-object mapping. Provides fluent API for converting objects between different types with support for custom converters, property assignments, and asynchronous operations.

**Extension Methods**

- `And<TResult>(this Tuple<ClassMappingSettings, MemberInfo, MemberInfo> tpl, Expression<Func<TResult, object>> outProp) : Tuple<ClassMappingSettings, MemberInfo, MemberInfo>`
- `Assign<TInput>(this ClassMappingSettings settings, Expression<Func<TInput, object>> inProp) : Tuple<ClassMappingSettings, MemberInfo>`
- `MapElementsTo<T>(this IEnumerable enumerable, ClassMappingSettings settings = null) : IEnumerable<T>`
  <br>Maps each element in a collection to the specified target type. Useful for converting entire collections like List&lt;SourceDto&gt; to List&lt;TargetEntity&gt;.
- `MapTo<TInput, TResult>(this TInput input, Action<TResult, TInput>[] differentMappingAssignments) : TResult`
- `MapTo<TInput, TResult>(this TInput input, ClassMappingSettings settings, Action<TResult, TInput>[] differentMappingAssignments) : TResult`
- `MapTo<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null) : object`
- `MapTo<TResult>(this object input, ClassMappingSettings settings) : TResult`
  <br>Maps an object to a target type with specified settings.
- `MapTo<TResult>(this object input, TypeConverter[] specificConverters) : TResult`
  <br>Maps an object to a target type using specific type converters for this operation only.
- `MapToAsync<TInput, TResult>(this TInput input, Action<TResult, TInput>[] differentMappingAssignments) : Task<TResult>`
- `MapToAsync<TInput, TResult>(this TInput input, ClassMappingSettings settings, Action<TResult, TInput>[] differentMappingAssignments) : Task<TResult>`
- `MapToAsync<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null) : Task<object>`
- `MapToAsync<TResult>(this object input, ClassMappingSettings settings) : Task<TResult>`
  <br>Map class to another
- `MapToAsync<TResult>(this object input, TypeConverter[] specificConverters) : Task<TResult>`
  <br>Map class to another
- `Set(this ClassMappingSettings settings, Action<ClassMappingSettings>[] o) : ClassMappingSettings`
- `Set<T>(this ClassMappingSettings settings, Expression<Func<ClassMappingSettings, T>> memberExpression, T value) : ClassMappingSettings`
- `Settings(this Tuple<ClassMappingSettings, MemberInfo, MemberInfo> tpl) : ClassMappingSettings`
- `Settings(this Tuple<ClassMappingSettings, MemberInfo> tpl) : ClassMappingSettings`
- `To<TResult>(this Tuple<ClassMappingSettings, MemberInfo> tpl, Expression<Func<TResult, object>> outProp) : Tuple<ClassMappingSettings, MemberInfo, MemberInfo>`

### `CompositeStream`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `CompositeStream(IEnumerable<Stream> streams)`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanSeek : bool { get; }`
- `CanWrite : bool { get; }`
- `Length : long { get; }`
- `Position : long { get; set; }`

### `ContainsType`

`enum`

Specifies how characters should be matched when checking if a string contains certain characters.

**Werte**

- `All`
  <br>Alle zeichen m�ssen enthalten sein
- `Any`
  <br>Ein zeichen muss enthalten sein
- `value__`

### `DateTimeExtensions`

`static class`

DateTimeExtensions

**Extension Methods**

- `AddDaysSafe(this DateTime date, double value) : DateTime`
- `AddWeekDays(this DateTime date, int weekDays) : DateTime`
- `Between(this DateTime dateTime, DateTime startDate, DateTime endDate) : bool`
- `DividedBy(this TimeSpan t1, int divider) : TimeSpan`
- `FindWeekNumber(this DateTime d) : int`
- `FirstDayOfMonth(this DateTime input) : DateTime`
- `FirstOfMonth(this DateTime d) : DateTime`
- `FromUnixTimeStamp(this int timestamp) : DateTime`
- `IsFirstDayOfMonth(this DateTime input) : bool`
- `IsFriday(this DateTime input) : bool`
- `IsLastDayOfMonth(this DateTime input) : bool`
- `IsMonday(this DateTime input) : bool`
- `IsNegative(this TimeSpan t1) : bool`
- `IsNegativeOrZero(this TimeSpan t1) : bool`
- `IsPositive(this TimeSpan t1) : bool`
- `IsPositiveOrZero(this TimeSpan t1) : bool`
- `IsSaturday(this DateTime input) : bool`
- `IsSunday(this DateTime input) : bool`
- `IsThursday(this DateTime input) : bool`
- `IsTuesday(this DateTime input) : bool`
- `IsWednesday(this DateTime input) : bool`
- `IsWeekday(this DateTime input) : bool`
- `IsWeekend(this DateTime input) : bool`
- `LastDayOfMonth(this DateTime input) : DateTime`
- `LastOfMonth(this DateTime d) : DateTime`
- `Max(this DateTime a, DateTime b) : DateTime`
- `Min(this DateTime a, DateTime b) : DateTime`
- `Minutes(this DateTime d) : int`
- `Multiply(this TimeSpan t1, decimal factor) : TimeSpan`
- `Multiply(this TimeSpan t1, long factor) : TimeSpan`
- `Next(this DateTime d, DayOfWeek day) : DateTime`
- `NextOrCurrent(this DateTime d, DayOfWeek day) : DateTime`
- `Previous(this DateTime d, DayOfWeek day) : DateTime`
- `PreviousOrCurrent(this DateTime d, DayOfWeek day) : DateTime`
- `Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector) : TimeSpan`
- `ToHttpDate(this DateTime d) : string`
- `ToISO(this DateTime d) : string`
- `ToISOz(this DateTime d) : string`
- `ToShortDateTimeString(this DateTime d, CultureInfo cultureInfo) : string`
- `ToShortISO(this DateTime d) : string`
- `ToUnixTimeStamp(this DateTime dateTime) : int`

**Methoden**

- `FromUnixTimeStampInMilliseconds(double timestamp) : DateTime`
- `IsFirstOfMonth(DateTime d) : bool`
- `IsLastOfMonth(DateTime d) : bool`
- `Max(TimeSpan t1, TimeSpan t2) : TimeSpan`
- `Min(TimeSpan t1, TimeSpan t2) : TimeSpan`

### `DisposableExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AsAsyncDisposable(this IDisposable d) : IAsyncDisposable`
  <br>Wraps an `IDisposable` so it can be used as `IAsyncDisposable`.
- `AsDisposable(this IAsyncDisposable d) : IDisposable`
  <br>Wraps an `IAsyncDisposable` so it can be used as `IDisposable`. The async DisposeAsync is run synchronously – use with care (no deadlocks on thread-pool!).
- `CombineWith(this IAsyncDisposable first, IAsyncDisposable[] others) : IAsyncDisposable`
  <br>Combines this `IAsyncDisposable` with one or more others into a single async disposable.
- `CombineWith(this IDisposable first, IDisposable[] others) : IDisposable`
  <br>Combines this `IDisposable` with one or more others into a single disposable.
- `DisposeIfNotNull(this IDisposable d) : void`
  <br>Disposes `d` if it is not null.
- `DisposeIfNotNullAsync(this IAsyncDisposable d) : ValueTask`
  <br>Disposes `d` if it is not null.
- `ToAsyncDisposable(this Func<ValueTask> onDispose) : IAsyncDisposable`
- `ToDisposable(this Action onDispose) : IDisposable`
  <br>Creates an `IDisposable` that runs `onDispose` exactly once.

**Methoden**

- `Combine(IDisposable[] disposables) : IDisposable`
  <br>Combines multiple `IDisposable` into one. All are disposed in reverse order; all exceptions are collected and re-thrown as `AggregateException`.
- `Combine(IEnumerable<IDisposable> disposables) : IDisposable`
- `CombineAsync(IAsyncDisposable[] disposables) : IAsyncDisposable`
  <br>Combines multiple `IAsyncDisposable` into one async disposable. All are disposed in reverse order; all exceptions are collected and re-thrown as `AggregateException`.
- `CombineAsync(IEnumerable<IAsyncDisposable> disposables) : IAsyncDisposable`
- `Defer<T>(Func<T> factory) : IDisposable`
- `Swap<T>(ref T current, T next) : T`

### `EnumerableExtensions`

`static class`

Extends IEnumerable

**Extension Methods**

- `AddNonNull<T>(this ICollection<T> set, T item) : void`
- `AddOrUpdate<TKey, TSource>(this IDictionary<TKey, TSource> collection, TKey key, TSource value) : IDictionary<TKey, TSource>`
- `AddRange<TKey, TSource>(this IDictionary<TKey, TSource> source, IDictionary<TKey, TSource> collection) : IDictionary<TKey, TSource>`
- `AddRange<TSource>(this ConcurrentBag<TSource> source, IEnumerable<TSource> itemsToAdd) : ConcurrentBag<TSource>`
- `AddRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> itemsToAdd) : ICollection<TSource>`
- `Apply(this IEnumerable enumerable, Action<int, object> action) : IEnumerable`
- `Apply(this IEnumerable enumerable, Action<object> action) : IEnumerable`
- `Apply<T>(this IEnumerable<T> enumerable, Action<T> action) : IEnumerable<T>`
- `Apply<T>(this IEnumerable<T> enumerable, Action<int, T> action) : IEnumerable<T>`
- `AsDataTable<T>(this IEnumerable<T> source) : DataTable`
- `AsEnumerable<T>(this T item) : IEnumerable<T>`
- `AsList<T>(this T item) : IList<T>`
- `AsListOf<TResult>(this IEnumerable source) : IList<TResult>`
  <br>Casts the elements of an `IEnumerable``source` to the specified type `TResult` and creates a list.
- `AsSet<T>(this T item) : ISet<T>`
- `AsSet<T>(this T item, IEqualityComparer<T> comparer) : ISet<T>`
- `ChunkBy<T>(this IEnumerable<T> collection, int chunkSize) : IEnumerable<List<T>>`
- `ContainsAll<T>(this IEnumerable<T> collection, IEnumerable<T> otherCollection) : bool`
- `ContainsAll<T>(this IEnumerable<T> collection, T[] otherCollection) : bool`
- `ContainsDuplicate<T>(this IEnumerable<T> items) : bool`
- `ContainsDuplicate<T>(this IEnumerable<T> items, IEqualityComparer<T> equalityComparer) : bool`
- `ContainsNone<T>(this IEnumerable<T> collection, IEnumerable<T> otherCollection) : bool`
- `ContainsNone<T>(this IEnumerable<T> collection, T[] otherCollection) : bool`
- `DistinctByObsolete<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) : IEnumerable<TSource>`
- `DistinctByObsolete<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer) : IEnumerable<TSource>`
- `EmptyIfNull<T>(this IEnumerable<T> collection) : IEnumerable<T>`
- `FullGroupJoin<TFirst, TSecond, TKey, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector, Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector, IEqualityComparer<TKey> comparer = null) : IEnumerable<TResult>`
- `Get<T, TU>(this Dictionary<T, TU> dict, T key) : TU`
- `GetDuplicates<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector) : IEnumerable<IGrouping<TKey, T>>`
- `GetDuplicates<T>(this IEnumerable<T> collection) : IEnumerable<T>`
- `HasValues<T>(this IEnumerable<T> enumerable) : bool`
- `IndexOf<T>(this T[] array, T obj) : int`
- `IsEmpty<T>(this IEnumerable<T> source) : bool`
- `IsNullOrEmpty<T>(this IEnumerable<T> collection) : bool`
- `MergeWith<TKey, TValue>(this IDictionary<TKey, TValue> collection, IDictionary<TKey, TValue>[] collections) : IDictionary<TKey, TValue>`
- `None<T>(this IEnumerable<T> enumerable) : bool`
- `None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) : bool`
- `NullIfEmpty<T>(this ICollection<T> collection) : ICollection<T>`
- `NullIfEmpty<T>(this IList<T> collection) : IList<T>`
- `NullIfEmpty<TK, TV>(this IDictionary<TK, TV> dictionary) : IDictionary<TK, TV>`
- `Order<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, ListSortDirection direction) : IOrderedEnumerable<TSource>`
- `Recursive<TSource>(this IEnumerable<TSource> children, Func<TSource, IEnumerable<TSource>> childDelegate) : IEnumerable<TSource>`
- `Recursive<TSource>(this IEnumerable<TSource> children, Func<TSource, IEnumerable<TSource>> childDelegate, Func<TSource, bool> predicate) : IEnumerable<TSource>`
- `Remove<T>(this ConcurrentBag<T> bag, T value) : bool`
- `RemoveAll<TSource>(this ICollection<TSource> source, Func<TSource, bool> predicate = null) : ICollection<TSource>`
- `RemoveRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> itemsToRemove) : ICollection<TSource>`
- `ThrowIfNullOrEmpty<T>(this IEnumerable<T> items, string paramName) : IEnumerable<T>`
- `ToConcatenatedString(this IEnumerable<string> list, string separator = ",") : string`
- `ToConcatenatedString<T>(this IEnumerable<T> list, Func<T, string> keySelector, string separator = ",") : string`
- `ToEnumerable<T>(this T item) : IEnumerable<T>`
- `ToObservableCollection<TSource>(this IEnumerable<TSource> source) : ObservableCollection<TSource>`
- `ToSafeEnumeration<T>(this IEnumerable<T> enumerable) : T[]`
- `WithIndex<T>(this IEnumerable<T> source) : IEnumerable<ValueTuple<T, int>>`

### `ExceptionExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ExtractDescription(this Exception ex) : string`
- `ExtractFriendlyMessage(this Exception ex) : string`
- `ExtractMessage(this AggregateException aggregateException) : string`
- `ExtractMessage(this Exception ex) : string`
- `ExtractMessages(this AggregateException aggregateException) : IEnumerable<string>`
- `FindBaseException(this Exception ex) : Exception`
- `Unwrap(this AggregateException aggregateException) : IEnumerable<Exception>`

### `ExposedObjectExtensions`

`static class`

Extensions for easy expose of an object

**Extension Methods**

- `SetExposed<T>(this T instance, Action<object>[] setterActions) : T`

### `ExpressionExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `GetMemberExpression(this Expression expression) : MemberExpression`
- `GetMemberExpression<T>(this Expression<Func<T, object>> expression) : MemberExpression`
- `GetMemberInfo(this Expression expression) : MemberInfo`
  <br>Gets the member info represented by an expression.
- `GetMemberInfosPaths<T, TResult>(this Expression<Func<T, TResult>> expr) : IReadOnlyList<MemberInfo>`
- `GetMemberName<T, TResult>(this Expression<Func<T, TResult>> expr) : string`
- `GetMemberName<T>(this Expression<Func<T>> expr) : string`
- `GetPropertyInfo(this Expression<Func<object>> expr) : PropertyInfo`
- `GetPropertyPath(this Expression expression) : string`
- `GetPropertyPath(this LambdaExpression expr) : string`
- `GetPropertyPath(this MemberExpression member) : string`
- `Not<T>(this Expression<Func<T, bool>> expr) : Expression<Func<T, bool>>`
- `ReadParameters(this MethodCallExpression methodCallExpr) : IDictionary<string, object>`

### `FileInfoExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `CopyTo(this DirectoryInfo directoryInfo, DirectoryInfo destinationFolder, bool overwriteExisting) : DirectoryInfo`
- `CopyTo(this DirectoryInfo directoryInfo, string destinationFolder, bool overwriteExisting = false) : DirectoryInfo`
- `CopyToAsync(this DirectoryInfo directoryInfo, DirectoryInfo destination, bool overwriteExisting = false, CancellationToken cancellationToken = null) : Task<DirectoryInfo>`
- `CopyToAsync(this DirectoryInfo directoryInfo, string destinationFolder, bool overwriteExisting = false, CancellationToken cancellationToken = null) : Task<DirectoryInfo>`
- `Execute(this FileInfo fileInfo, string arguments = "", ScriptExecutionSettings executionSettings = null, CancellationToken cancellationToken = null) : Process`
- `ExtensionFileName(this FileInfo fileInfo) : string`
- `FileTypeDescription(this FileInfo fileInfo) : string`
- `FindLockingProcesses(this FileInfo fileInfo) : IList<Process>`
- `GetReadableFileSize(this FileInfo fileInfo, bool fullName = false) : string`
  <br>Returns a readable filesize string
- `GetRelativePathTo(this FileSystemInfo fileInfo, FileSystemInfo other) : string`
- `GetRelativePathTo(this FileSystemInfo fileInfo, string referencePath) : string`
- `GetShortPath(this FileSystemInfo fileSystemInfo, int length = 30) : string`
- `IsExecutable(this FileInfo fileInfo) : bool`
- `IsLockedByProcess(this FileInfo fileInfo) : bool`
- `MimeType(this FileInfo fileInfo) : string`
- `MoveToRecycleBin(this FileSystemInfo fileInfo) : void`
- `ShowProperties(this FileInfo fileInfo) : void`

### `FuncExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TParam16>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TParam15>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TParam14>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TParam13>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TParam12>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TParam11>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TParam5>(this Action<TParam1, TParam2, TParam3, TParam4, TParam5> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4, TParam5>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4, TResult>(this Func<TParam1, TParam2, TParam3, TParam4, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TParam4, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3, TParam4>(this Action<TParam1, TParam2, TParam3, TParam4> f) : Expression<Action<TParam1, TParam2, TParam3, TParam4>>`
- `ToExpression<TParam1, TParam2, TParam3, TResult>(this Func<TParam1, TParam2, TParam3, TResult> f) : Expression<Func<TParam1, TParam2, TParam3, TResult>>`
- `ToExpression<TParam1, TParam2, TParam3>(this Action<TParam1, TParam2, TParam3> f) : Expression<Action<TParam1, TParam2, TParam3>>`
- `ToExpression<TParam1, TParam2, TResult>(this Func<TParam1, TParam2, TResult> f) : Expression<Func<TParam1, TParam2, TResult>>`
- `ToExpression<TParam1, TParam2>(this Action<TParam1, TParam2> f) : Expression<Action<TParam1, TParam2>>`
- `ToExpression<TParam1, TResult>(this Func<TParam1, TResult> f) : Expression<Func<TParam1, TResult>>`
- `ToExpression<TParam1>(this Action<TParam1> f) : Expression<Action<TParam1>>`

### `Guider`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToFormattedId(this Guid id) : string`
- `ToInt(this Guid value) : int`
- `ToInt64(this Guid value) : long`

**Methoden**

- `FromFormattedId(ReadOnlySpan<Char> id) : Guid`

### `HttpClientExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `GetStreamInChunksAsync(this HttpClient httpClient, string url, long chunkSizeInBytes, CancellationToken cancellationToken = null) : Task<Stream>`
- `GetStreamInTaskChunksAsync(this HttpClient httpClient, string url, int taskCount, CancellationToken cancellationToken = null) : Task<Stream>`

### `IoCExtensions`

`static class`

IoC Erweiterungen

### `JObjectExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `CaseSelectPropertyValues(this IEnumerable<JToken> tokens, string name) : IEnumerable<JToken>`
- `CaseSelectPropertyValues(this JToken token, string name) : IEnumerable<JToken>`
- `ToDictionary(this JObject jObject) : IDictionary<string, object>`
- `ToFlatDictionary(this JObject jObject) : IDictionary<string, string>`

### `MemberInfoExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `GetAttributes<T>(this MemberInfo member, bool inherit) : IEnumerable<T>`
- `HasAttribute<TAttribute>(this MethodInfo method) : bool`
- `IsEqual<T>(this PropertyInfo prop, Expression<Func<T, object>> propertyExpression) : bool`
- `ReadFromAttribute<TResult, TAttribute>(this MemberInfo info, Func<TAttribute, TResult> readerFunc, TResult fallbackValue = null) : TResult`

### `NotificationExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `OnChange<T>(this INotifyPropertyChanged propertyChangedObject, Expression<Func<T>> action, Action<T> callback, bool ignoreExceptions = false) : PropertyChangedEventHandler`
- `OnChange<TNotificationObject>(this TNotificationObject propertyChangedObject, Action<TNotificationObject> callback) : TNotificationObject`
- `OnChange<TPropertyType>(this INotifyPropertyChanged propertyChangedObject, Expression<Func<TPropertyType>> action, Action<TPropertyType> callback) : void`

**Methoden**

- `GetPropertyChangedEventHandler<T>(Expression<Func<T>> action, Action<T> callback, bool ignoreExceptions) : PropertyChangedEventHandler`

### `NumericExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `Absolute(this decimal input) : decimal`
- `Absolute(this int input) : int`
- `Between(this int value, int left, int right) : bool`
- `Ceiling(this double d, int precision) : double`
- `Floor(this double d, int precision) : double`
- `Round(this decimal d, int precision) : decimal`
- `Round(this double d, int precision) : double`
- `RoundTo(this decimal val, int place) : decimal`
- `RoundToMoney(this decimal val) : decimal`
- `ToGuid(this int value) : Guid`
- `ToGuid(this long value) : Guid`

### `ObjectExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AllOf<T>(this object instance, ReflectReadSettings settings = null) : T[]`
- `As<T>(this object obj) : T`
  <br>Used to simplify and beautify casting an object to a type.
- `AsLazy<T>(this T t) : Lazy<T>`
- `Clone<T>(this T source, ClonerSettings settings) : T`
- `Clone<T>(this T source, FieldType fieldType) : T`
- `Clone<T>(this T source, bool useFastDeepClone = true) : T`
- `ExposeField<T>(this object instance, string fieldName) : T`
- `GetProperties(this object input, BindingFlags bindingAttr = 0) : IEnumerable<PropertyInfo>`
- `If<T>(this T obj, Func<bool> condition, Action<T> action) : T`
- `If<T>(this T obj, Func<bool> condition, Func<T, T> func) : T`
- `If<T>(this T obj, bool condition, Action<T> action) : T`
- `If<T>(this T obj, bool condition, Func<T, T> func) : T`
- `IsIn<T>(this T item, IEnumerable<T> items) : bool`
- `IsIn<T>(this T item, T[] list) : bool`
- `IsNull(this object input) : bool`
- `NotNull(this object input) : bool`
- `SetProperties<T>(this T instance, Action<T>[] actions) : T`
- `ToDictionary(this object obj) : IDictionary<string, object>`
- `ToFlatDictionary(this object obj, string separator = ".") : IDictionary<string, string>`
- `ToNullSafeString(this object input, string defaultIfNull = "") : string`
- `ToTask<T>(this T input) : Task<T>`
- `ToUrlQueryString(this object obj, string firstDelimiter = "") : string`

### `RangeExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToRange(this RangeOf<int> range, int? length = null) : Range`
- `ToRange(this SimpleRange<int> range) : Range`
- `ToRangeOfInt(this Range range) : RangeOf<int>`

### `RangeMathExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AddSteps<T>(this IRangeMath<T> math, T v, RangeLength<T> step, int steps) : T`
- `Clamp<T>(this IRangeMath<T> math, T v, IRange<T> bounds) : T`
- `Lerp<T>(this IRangeMath<T> math, IRange<T> size, double pct) : T`
- `Normalize<T>(this IRange<T> r) : IRange<T>`
- `Percent<T>(this IRangeMath<T> math, T v, IRange<T> size) : double`
- `SnapRange<T>(this IRangeMath<T> math, IRange<T> r, IRange<T> size, RangeLength<T> step) : IRange<T>`
- `SnapToStep<T>(this IRangeMath<T> math, T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = 0) : T`
- `Span<T>(this IRange<T> r, IRangeMath<T> math) : double`
- `Span<T>(this IRangeMath<T> math, IRange<T> r) : double`
- `Span<T>(this IRangeMath<T> math, IRange<T> r, SnapPolicy policy) : double`
- `Span<T>(this IRangeMath<T> math, IRange<T> r, bool absolute) : double`

### `SerializationHelper`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `JsonSerialize<T>(this T obj, string fileName = "") : string`
- `TryJsonSerialize<T>(this T obj, out string s) : bool`
- `TryXmlSerialize<T>(this T content, Stream stream) : bool`
- `TryXmlSerialize<T>(this T content, string filename) : bool`
- `XmlSerialize<T>(this T content, Stream stream = null) : Stream`
- `XmlSerialize<T>(this T content, string filename) : bool`

**Methoden**

- `JsonDeserialize<T>(string json) : T`
- `TryJsonDeserialize<T>(string json, out T obj) : bool`
- `TryXmlDeserialize<T>(Stream stream, out T result) : bool`
- `TryXmlDeserialize<T>(string filename, out T result) : bool`
- `XmlDeserialize<T>(Stream stream) : T`
- `XmlDeserialize<T>(string filename) : T`

### `ServiceCollectionExtensions`

`static class`

Extension methods for `IServiceCollection`

**Extension Methods**

- `RegisterAllImplementationsOf(this IServiceCollection services, Type[] interfacesToSearchImplementationsFor, Assembly[] assembliesToSearchImplementationsIn = null, ServiceLifetime lifeTime = 2, Action<ServiceDescriptor> onRegistered = null) : IServiceCollection`
- `RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3, TInterface4>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, ServiceLifetime lifeTime = 2, Action<ServiceDescriptor> onRegistered = null) : IServiceCollection`
- `RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, ServiceLifetime lifeTime = 2, Action<ServiceDescriptor> onRegistered = null) : IServiceCollection`
- `RegisterAllImplementationsOf<TInterface1, TInterface2>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, ServiceLifetime lifeTime = 2, Action<ServiceDescriptor> onRegistered = null) : IServiceCollection`
- `RegisterAllImplementationsOf<TInterface>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, ServiceLifetime lifeTime = 2, Action<ServiceDescriptor> onRegistered = null) : IServiceCollection`
- `RegisterAllWithRegisterAsAttribute(this IServiceCollection services, Action<ServiceDescriptor> onRegistered, Assembly[] assemblies) : IServiceCollection`
- `RegisterAllWithRegisterAsAttribute(this IServiceCollection services, Assembly[] assemblies) : IServiceCollection`
  <br>Registers all types marked with `RegisterAsAttribute` from the specified assemblies

### `StreamExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToByteArray(this Stream input) : byte[]`

### `StringBuilderExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AppendLineIfNotEmpty(this StringBuilder builder, string s) : StringBuilder`
- `AppendLineWhen(this StringBuilder builder, string s, Func<string, bool> predicate) : StringBuilder`
- `AppendLines(this StringBuilder builder, IEnumerable<string> lines) : StringBuilder`
- `AppendLinesIfNotEmpty(this StringBuilder builder, string[] s) : StringBuilder`
- `AppendLinesWhen(this StringBuilder builder, IEnumerable<string> s, Func<string, bool> predicate) : StringBuilder`

### `StringExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `Capitalize(this string input) : string`
- `Contains(this string s, Char[] chars) : bool`
- `Contains(this string s, ContainsType type, Char[] chars) : bool`
- `Contains(this string s, ContainsType type, string[] values) : bool`
- `Contains(this string s, string[] values) : bool`
- `ContainsAll(this string input, string[] contains) : bool`
- `ContainsAny(this string input, string[] contains) : bool`
- `EnsureEndsWith(this string str, Char toEndWith) : string`
- `EnsureEndsWith(this string str, string ending) : string`
- `EnsureStartsWith(this string str, Char toStartWith) : string`
- `EnsureStartsWith(this string str, string prefix) : string`
- `FormatWith(this string format, object[] args) : string`
- `GetLines(this string s) : IEnumerable<string>`
- `HasValue(this string input) : bool`
- `IsEmailAddress(this string input) : bool`
- `IsGuid(this string input) : bool`
- `IsNullOrEmpty(this string value) : bool`
- `IsNullOrWhiteSpace(this string value) : bool`
- `JoinWith<T>(this IEnumerable<T> values, string separator) : string`
- `Replace(this string s, IDictionary<Char, Char> dictionary) : string`
- `Replace(this string s, IDictionary<string, string> dictionary) : string`
- `Replace(this string s, string[] valuesToReplace, string newValue) : string`
- `SkipChars(this string str, Char[] chars) : string`
- `SplitByUpperCase(this string str) : string[]`
- `TakeCharacters(this string input, int count) : string`
- `ThrowIfNullOrEmpty(this string input, string paramName) : void`
- `ToCamel(this string input) : string`
- `ToEllipsis(this string input, int maxChars, Char ellipseChar = ., bool keepLength = false) : string`
- `ToLower(this string input, bool firstCharOnly) : string`
- `ToPascalCase(this string input) : string`
- `ToUpper(this string input, bool firstCharOnly) : string`
- `Uncapitalize(this string input) : string`

### `TaskExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `IgnoreCancellation<T>(this Task<T> task, CancellationToken cancellationToken = null) : Task<T>`
- `RetryOnException<T, TException>(this Task<T> task, int retryCount, TimeSpan retryDelay, CancellationToken cancellationToken = null) : Task<T>`
- `RetryOnException<T>(this Task<T> task, int retryCount, TimeSpan retryDelay) : Task<T>`
- `RetryOnException<T>(this Task<T> task, int retryCount, int retryDelayMilliseconds) : Task<T>`
- `TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken = null) : Task<TResult>`
- `TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, Func<TResult> onTimeoutReached, CancellationToken cancellationToken = null) : Task<TResult>`

### `TypeExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `CreateInstance(this Type input) : object`
- `CreateInstance<T>(this Type input, bool checkCyclingDependencies = true) : T`
- `CreateInstance<T>(this Type input, object[] args) : T`
- `GetAllImplementations(this Type type, Assembly[] serviceImplementationAssemblies) : IEnumerable<Type>`
- `GetBaseIdBaseType(this Type modelType) : Type`
  <br>Gibt an ob der Typ eine BaseId ist
- `GetPropertyIgnoreCase(this Type type, string propertyName) : PropertyInfo`
- `IsAction(this Type type) : bool`
- `IsBaseId(this Type modelType) : bool`
  <br>Gibt an ob der Typ eine BaseId ist
- `IsBool(this Type input) : bool`
- `IsCollection(this Type type) : bool`
- `IsDateTime(this Type input) : bool`
- `IsDecimal(this Type input) : bool`
- `IsEnumerable(this Type type) : bool`
- `IsEnumerableOrArray(this Type type) : bool`
- `IsExpression(this Type type) : bool`
- `IsFunc(this Type type) : bool`
- `IsIEnumerable(this Type input) : bool`
- `IsIList(this Type input) : bool`
- `IsInstanceOfGenericTypeDefinition(this Type type, Type genericTypeDefinition) : bool`
- `IsInstanceOfGenericTypeDefinition(this Type type, Type genericTypeDefinition, Type otherGenericTypeDefinition, Type[] otherGenericTypeDefinitions) : bool`
- `IsInstanceOfGenericTypeDefinition(this Type type, Type[] genericTypeDefinitions) : bool`
- `IsInstanceOfOpenGenericType(this Type type, Type[] openGenericTypes) : bool`
- `IsInt(this Type input) : bool`
- `IsKeyValuePair(this Type t) : bool`
- `IsNotBool(this Type input) : bool`
- `IsNotDateTime(this Type input) : bool`
- `IsNotDecimal(this Type input) : bool`
- `IsNotIEnumerable(this Type input) : bool`
- `IsNotIList(this Type input) : bool`
- `IsNotInt(this Type input) : bool`
- `IsNotNullableBool(this Type input) : bool`
- `IsNotNullableDateTime(this Type input) : bool`
- `IsNotNullableDecimal(this Type input) : bool`
- `IsNotNullableInt(this Type input) : bool`
- `IsNotString(this Type input) : bool`
- `IsNotType(this Type input) : bool`
- `IsNullable(this Type type) : bool`
- `IsNullableAction(this Type type) : bool`
- `IsNullableBool(this Type input) : bool`
- `IsNullableDateTime(this Type input) : bool`
- `IsNullableDecimal(this Type input) : bool`
- `IsNullableEnum(this Type t) : bool`
- `IsNullableFunc(this Type type) : bool`
- `IsNullableGenericOf(this Type type, Type[] openGenericTypes) : bool`
- `IsNullableGenericOfGenericTypeDefinition(this Type type, Type genericTypeDefinition) : bool`
- `IsNullableInt(this Type input) : bool`
- `IsNullableOf<T>(this Type type) : bool`
- `IsNullableType(this Type type) : bool`
- `IsReadOnlyStruct(this Type t) : bool`
- `IsScalar(this Type t) : bool`
- `IsString(this Type input) : bool`
- `IsStruct(this Type t) : bool`
- `IsSubclassOfInterfaceOf(this Type toCheck, Type interfaceType) : bool`
- `IsSubclassOfInterfaceOf<TInterface>(this Type toCheck) : bool`
- `IsTuple(this Type t) : bool`
- `IsTupleOrValueTuple(this Type t) : bool`
- `IsType(this Type input) : bool`
- `IsValueTuple(this Type t) : bool`

### `UriExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `AddParameter(this Uri uri, IDictionary<string, string> parameters) : Uri`
- `AddParameter(this Uri uri, string key, string value) : Uri`
- `Append(this Uri uri, bool shouldEndWithSlash, string[] segments) : Uri`
- `Append(this Uri uri, string[] segments) : Uri`
- `SetQuery(this Uri uri, object getParams) : Uri`
- `SetQuery(this UriBuilder builder, object getParams) : UriBuilder`

**Methoden**

- `AddParameterToUrl(string url, string key, string value) : string`
- `Combine(string path, string[] segments) : string`
- `Combine(string[] segments) : string`

### `Validation`

`static class`

Hilfsklasse für Validierung

**Extension Methods**

- `Validate(this object obj) : IEnumerable<ValidationResult>`
- `ValidateProperty(this object obj, string propertyName) : IEnumerable<ValidationResult>`

## Nextended.Core.Facets

### `AppliedFacet`

`class`

Represents a facet filter that has been applied to a search or query. Contains the group key, display label, selected values, and the OData filter expression.

**Konstruktoren**

- `AppliedFacet()`

**Eigenschaften**

- `GroupKey : string { get; set; }`
  <br>GroupKey for the filter group (e.g. "status").
- `Label : string { get; set; }`
  <br>Label for the chip (e.g. "Booked").
- `OData : string { get; set; }`
  <br>OData-Fragment for this specific filter (e.g. "status eq 'Booked' or status eq 'InProgress'").
- `Values : List<string> { get; set; }`
  <br>Values for the chip (e.g. ["Booked", "InProgress"]).

### `DefaultODataLiteralFormatter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DefaultODataLiteralFormatter()`

**Methoden**

- `RangeClause(string field, object fromInclusive, object toExclusive, Type type) : string`
- `ToLiteral(object value, Type type) : string`
- `ToSimpleLiteral(object value, Type type) : string`
  <br>Simple literal builder without OData type prefixes (for BuildLiterals=false). - strings are single-quoted - booleans are true/false - DateTime/DateTimeOffset are ISO8601 and quoted - enums use their name as quoted string - numerics are culture-invariant without quotes

### `FacetBuilder`

`class`

Builds facet definitions (groups/options/ranges) for a given IQueryable. - Reflection metadata is cached per type (ConcurrentDictionary) - All list facets are fetched in a single combined DB query - Range preset counts use a single-roundtrip IIF/GroupBy query per group - Parallel range computation available via FacetBuilderOptions.ParallelizeRanges - Disjunctive faceting controlled by options and GroupOperator

**Konstruktoren**

- `FacetBuilder(IServiceScopeFactory scopeFactory, FacetBuilderOptions options = null, IODataLiteralFormatter literal = null)`
  <br>Builds facet definitions (groups/options/ranges) for a given IQueryable. - Reflection metadata is cached per type (ConcurrentDictionary) - All list facets are fetched in a single combined DB query - Range preset counts use a single-roundtrip IIF/GroupBy query per group - Parallel range computation available via FacetBuilderOptions.ParallelizeRanges - Disjunctive faceting controlled by options and GroupOperator

**Methoden**

- `BuildAsync<T>(Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `BuildAsync<T>(Func<Task<IQueryable<T>>> baseQueryFactory, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `BuildAsync<T>(IQueryable<T> baseQuery, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `WithLocalizationFunc(Func<string, Type, string> localizerFn) : IFacetBuilder`
- `WithOptions(FacetBuilderOptions options) : IFacetBuilder`

### `FacetBuilderOptions`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `FacetBuilderOptions()`

**Eigenschaften**

- `BuildFacets : bool { get; set; }`
  <br>If true, facets will be built. If false, only applied filters will be extracted.
- `BuildLiterals : bool { get; set; }`
  <br>Specifies whether to build literals should be false for Edm provided models.
- `ComputeDynamicLists : bool { get; set; }`
  <br>If true, the list of available values for properties with `Type``CheckboxList`, `TokenList` or `Radio` will be calculated
- `DefaultTopDistinct : int? { get; set; }`
  <br>Default top limit for discrete facets if not set on attribute.
- `DisjunctiveByGroupOperator : bool { get; set; }`
  <br>If true, group operator (AND/OR) will be considered when computing disjunctive facets. Only relevant if DisjunctiveFacets is false.
- `DisjunctiveFacets : bool { get; set; }`
  <br>If true, compute counts per group on a query with all other groups' filters applied (disjunctive faceting).
- `IsDisjunctiveBuild : bool { get; }`
- `MaxDbParallelism : int { get; set; }`
  <br>Maximum concurrent DB connections when ParallelizeRanges is true. Default: 4.
- `ParallelizeRanges : bool { get; set; }`
  <br>Computes range preset counts in parallel (one DB scope per group). Only effective when using the factory-overload of BuildAsync.

### `FacetDependency`

`class`

Represents a dependency relationship between facet groups, where one group's availability depends on selections in another group.

**Konstruktoren**

- `FacetDependency()`

**Eigenschaften**

- `ParentKey : string { get; set; }`
  <br>Key of the parent group (e.g. "transportMode").
- `RequiredValues : List<string> { get; set; }`
  <br>Values that must be set in the parent group for this group to be active.

### `FacetGroup`

`class`

Represents a group of facet options for filtering and searching. A facet group defines the structure, behavior, and options for a specific filter category.

**Konstruktoren**

- `FacetGroup()`

**Eigenschaften**

- `BuildDuration : TimeSpan { get; set; }`
  <br>Gets or sets the time taken to build this facet group's options.
- `DependsOn : List<FacetDependency> { get; set; }`
  <br>Gets or sets the list of dependencies that control when this facet is available.
- `Enabled : bool { get; set; }`
  <br>Gets or sets a value indicating whether this facet group is currently enabled.
- `Field : string { get; set; }`
  <br>Gets or sets the field name in the data source that this facet operates on.
- `GroupOperator : FacetGroupOperator { get; set; }`
  <br>Gets or sets the logical operator for combining multiple selections (OR/AND).
- `Key : string { get; set; }`
  <br>Gets or sets the unique key identifying this facet group.
- `Label : string { get; set; }`
  <br>Gets or sets the display label for this facet group.
- `LabelPath : string { get; set; }`
  <br>Gets or sets the path to the label property when working with complex objects.
- `MultiSelect : bool { get; set; }`
  <br>Gets or sets a value indicating whether multiple options can be selected simultaneously.
- `Options : List<FacetOption> { get; set; }`
  <br>Gets or sets the list of available options for this facet group.
- `Order : int { get; set; }`
  <br>Gets or sets the display order for this facet group relative to others.
- `Range : FacetRangeDefinition { get; set; }`
  <br>Gets or sets the range definition if this is a range-based facet.
- `Type : FacetType { get; set; }`
  <br>Gets or sets the type of facet UI control (checkbox list, range, etc.).
- `ValueClrType : string { get; set; }`
  <br>Gets or sets the CLR type name for the value (used for type conversion).
- `ValuePath : string { get; set; }`
  <br>Gets or sets the path to the value property when working with complex objects.

### `FacetGroupOperator`

`enum`

Specifies the logical operator used to combine multiple selected options within a facet group.

**Werte**

- `And`
  <br>Combines filter options using AND logic (all conditions must be met).
- `Or`
  <br>Combines filter options using OR logic (any condition must be met).
- `value__`

### `FacetOption`

`class`

Represents a single option within a faceted search/filter system. Contains the value, label, selection state, and associated metadata for filtering operations.

**Konstruktoren**

- `FacetOption()`

**Eigenschaften**

- `Count : long? { get; set; }`
  <br>Number of results if this option were applied alone (Facet Count).
- `Enabled : bool { get; set; }`
  <br>Whether currently applicable (e.g., 0 results -&gt; disabled).
- `Hint : string { get; set; }`
  <br>Optional: Tooltip / description.
- `Label : string { get; set; }`
  <br>Label in the UI.
- `Meta : Dictionary<string, string> { get; set; }`
  <br>Arbitrary additional data (e.g., icons, badges).
- `OData : string { get; set; }`
  <br>Prepared OData fragment for this single option (e.g., "Status eq 'Booked'").
- `Selected : bool { get; set; }`
  <br>Whether currently selected.
- `Value : string { get; set; }`
  <br>Stable value (e.g., "Booked").

### `FacetRangeBucket`

`class`

Represents a single bucket in a range-based facet (e.g., date ranges, price ranges). Contains the range definition, label, and count of items within this bucket.

**Konstruktoren**

- `FacetRangeBucket()`

**Eigenschaften**

- `Count : long? { get; set; }`
  <br>Gets or sets the number of items that fall within this range bucket.
- `Key : string { get; set; }`
  <br>Gets or sets the unique key for this range bucket (e.g., "last7d").
- `Label : string { get; set; }`
  <br>Gets or sets the display label for this range bucket (e.g., "Last 7 days").
- `Value : FacetRangeValue { get; set; }`
  <br>Gets or sets the range value definition (from/to bounds).

### `FacetRangeDataType`

`enum`

Specifies the data type for range-based facet filters.

**Werte**

- `Date`
  <br>Date-only range (without time component).
- `DateTime`
  <br>Date and time range.
- `Decimal`
  <br>Decimal or floating-point number range.
- `Number`
  <br>Integer or whole number range.
- `value__`

### `FacetRangeDefinition`

`class`

Defines the configuration for a range-based facet filter, including data type, selected range, and preset range options.

**Konstruktoren**

- `FacetRangeDefinition()`

**Eigenschaften**

- `DataType : FacetRangeDataType { get; set; }`
  <br>Range type (Number, Decimal, Date, DateTime).
- `Presets : List<FacetRangeBucket> { get; set; }`
  <br>Predefined buckets (e.g. "Last 7 days").
- `Selected : FacetRangeValue { get; set; }`
  <br>Current selected range.

### `FacetRangeValue`

`class`

Represents the selected value range for a range-based facet filter. Contains the from/to bounds and the corresponding OData filter expression.

**Konstruktoren**

- `FacetRangeValue()`

**Eigenschaften**

- `From : string { get; set; }`
  <br>Gets or sets the lower bound of the range.
- `OData : string { get; set; }`
  <br>OData-Fragmment for this range, e.g. "(Eta ge 2025-01-01 and Eta lt 2025-02-01)".
- `To : string { get; set; }`
  <br>Gets or sets the upper bound of the range.

### `FacetType`

`enum`

Specifies the type of UI control and behavior for a facet filter group.

**Werte**

- `CheckboxList`
  <br>Represents a filter type that allows users to select multiple options from a list of checkboxes.
- `DateRange`
  <br>Represents a filter type that allows users to select a range of dates.
- `Radio`
  <br>Represents a filter type that allows users to select a single option from a list of radio buttons.
- `Range`
  <br>Represents a filter type that allows users to select a range of numeric values.
- `Search`
  <br>Represents a search filter type that allows free text input for searching.
- `TokenList`
  <br>Represents a filter type that allows selection from a list of tokens.
- `value__`

### `IFacetBuilder`

`interface`

Provides methods for building facet groups from queryable data sources. Analyzes data to generate filter options and counts for faceted search functionality.

**Methoden**

- `BuildAsync<T>(Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `BuildAsync<T>(IQueryable<T> baseQuery, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = null) : Task<List<FacetGroup>>`
- `WithLocalizationFunc(Func<string, Type, string> localizerFn) : IFacetBuilder`
- `WithOptions(FacetBuilderOptions options) : IFacetBuilder`
  <br>Configures the facet builder with custom options.

### `IFacetResponse`

`interface`

Represents the response from a faceted search operation, containing filter groups, applied filters, and query metadata.

**Eigenschaften**

- `Filters : List<FacetGroup> { get; set; }`
  <br>All applicable filter groups (for UI rendering).
- `ODataFilter : string { get; set; }`
  <br>Complete OData filter that combines all applied filters.
- `Query : ODataQueryModel { get; set; }`
  <br>Additional information about the current query (e.g. $orderby, $top, $skip).

### `IODataLiteralFormatter`

`interface`

Provides formatting of CLR values to OData v4 literal representations for use in $filter expressions.

**Methoden**

- `RangeClause(string field, object fromInclusive, object toExclusive, Type type) : string`
  <br>Builds a (field ge from and field lt to) clause for range-like filters.
- `ToLiteral(object value, Type type) : string`
  <br>Formats a CLR value to an OData literal (v4) suitable for $filter.

### `ProvideFacetAttribute`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ProvideFacetAttribute()`

**Eigenschaften**

- `GroupOperator : FacetGroupOperator { get; set; }`
- `Label : string { get; set; }`
- `LabelPath : string { get; set; }`
  <br>Optional label path to show a human-friendly value (e.g., "TransportMode/Name"). If not set, the builder will use the value or value.ToString().
- `MultiSelect : bool { get; set; }`
- `Order : int { get; set; }`
- `TopDistinct : int { get; set; }`
- `Type : FacetType { get; set; }`
- `ValuePath : string { get; set; }`
  <br>Optional value path for navigation properties (e.g., "TransportMode/Id" or "TransportModeId"). If not set, the builder uses the property itself (for scalar fields).
- `ValueType : Type { get; set; }`
  <br>The CLR type of the value path, required when ValuePath is set (e.g., typeof(Guid)).

### `QueryMetaDto`

`class`

Data transfer object containing OData query metadata such as sorting, pagination, and raw query information.

**Konstruktoren**

- `QueryMetaDto()`

**Eigenschaften**

- `OrderBy : string { get; set; }`
  <br>Gets or sets the OData $orderby clause for sorting results.
- `RawODataQuery : string { get; set; }`
  <br>Gets or sets the raw OData query string as received from the client.
- `Skip : int? { get; set; }`
  <br>Gets or sets the OData $skip value for pagination (number of records to skip).
- `Top : int? { get; set; }`
  <br>Gets or sets the OData $top value for pagination (maximum number of records to return).

## Nextended.Core.Hashing

### `HashExtensions`

`static class`

Provides extension methods for hashing strings using various algorithms.

**Extension Methods**

- `Hash(this string str, string salt = null) : HashingActions`
  <br>Creates a hashing actions object that provides access to various hashing operations for the string.

### `HashHelper`

`static class`

_Keine Beschreibung._

**Methoden**

- `MD5(Stream stream) : string`
- `MD5(byte[] input) : string`
- `MD5(string text, string salt = null) : string`
- `MD5FileHash(string fileName) : string`
- `Sha256(byte[] input) : byte[]`
  <br>Creates a SHA256 hash of the specified input.
- `Sha256(string input, string salt = null) : string`
- `Sha512(string input, string salt = null) : string`
  <br>Creates a SHA512 hash of the specified input.

### `HashingActions`

`class`

Provides access to different hashing algorithms (MD5, SHA-256, SHA-512) for a string.

**Eigenschaften**

- `MD5 : Func<string> { get; }`
  <br>Gets a function that computes the MD5 hash of the string.
- `Sha265 : Func<string> { get; }`
  <br>Gets a function that computes the SHA-256 hash of the string.
- `Sha512 : Func<string> { get; }`
  <br>Gets a function that computes the SHA-512 hash of the string.

### `StringHasher`

`class`

_Keine Beschreibung._

**Methoden**

- `Hash(string input, string salt = null) : string`

**Eigenschaften**

- `MD5 : IStringHashing { get; }`
- `Sha265 : IStringHashing { get; }`
- `Sha512 : IStringHashing { get; }`

## Nextended.Core.Helper

### `ClassMapper`

`class`

Provides object-to-object mapping functionality with support for type conversion, custom converters, and complex mapping scenarios. The ClassMapper can automatically map properties between objects of different types, handling type conversions, nested objects, collections, and more.

**Konstruktoren**

- `ClassMapper()`
- `ClassMapper(ClassMappingSettings settings)`
  <br>Initializes a new instance of the `Object` class.

**Methoden**

- `Dispose() : void`
- `Map<TInput, TResult>(TInput input, Action<TResult, TInput>[] differentMappingAssignments) : TResult`
- `Map<TInput>(TInput input, Type tResult) : object`
- `MapAsync<TInput, TResult>(TInput input, Action<TResult, TInput>[] differentMappingAssignments) : Task<TResult>`
- `MapAsync<TInput>(TInput input, Type tResult) : Task<object>`
- `SetAsDefaultMapper() : ClassMapper`
- `SetSettings(Action<ClassMappingSettings>[] o) : ClassMapper`
- `SetSettings(ClassMappingSettings settings) : ClassMapper`
  <br>Set Settings for mapping behavior

### `ClassMappingSettings`

`class`

Configuration settings for class mapping operations that control how objects are mapped between different types. Includes options for exception handling, type conversion, async processing, custom converters, and property mapping behavior.

**Konstruktoren**

- `ClassMappingSettings(bool shouldEnumerateListsAsync = false, TypeConverter[] specificConverters)`
  <br>Initializes a new instance of the `Object` class.

**Methoden**

- `AddAllLoadedTypeConverters(Assembly[] assemblies) : ClassMappingSettings`
  <br>Fügt alle möglichen Converter der liste der zu benutzenden Konverter hinzu
- `AddAssignment(MemberInfo inputProperty, MemberInfo outputProperty) : ClassMappingSettings`
  <br>Property assignment hinzufügen z.B falls properties unterschiedliche Namen haben
- `AddAssignment<TInput, TResult>(Expression<Func<TInput, object>> inProp, Expression<Func<TResult, object>> outProp) : ClassMappingSettings`
- `AddAssignment<TResult>(MemberInfo inputProperty, Expression<Func<TResult, object>> outProp) : ClassMappingSettings`
- `AddConverter(Type tIn, Type tOut, Func<object, object> fn = null, bool allowAssignableInputs = false) : ClassMappingSettings`
- `AddConverter(TypeConverter converter) : ClassMappingSettings`
  <br>Typeconverter zu den einstellungen hinzufügen
- `AddConverter<TIn, TOut>(Func<TIn, TOut> fn = null, bool allowAssignableInputs = false) : ClassMappingSettings`
- `AddConverters(TypeConverter[] converters) : ClassMappingSettings`
  <br>Type converter zu den einstellungen hinzufügen
- `AddGlobalConverter(Type tIn, Type tOut, Func<object, object> fn = null, bool allowAssignableInputs = false) : TypeConverter`
- `AddGlobalConverter<TIn, TOut>(Func<TIn, TOut> fn = null, bool allowAssignableInputs = false) : TypeConverter`
- `AddGlobalConverters(TypeConverter[] converters) : void`
  <br>Adds one or more type converters to the global converters collection. Global converters are applied to all mapping operations unless explicitly ignored by setting IgnoreGlobalConverters to true.
- `AddTypeMapping<TIn, TOut>(bool allowAssignableInputs = false) : ClassMappingSettings`
  <br>Einfaches type mapping hinzufügen bei dem dann automatisch wieder mapTo greift
- `ClearGlobalConverters() : void`
- `IgnoreProperties(MemberInfo[] toIgnore) : ClassMappingSettings`
  <br>Die hier angegebenen Properties werden beim Mapping ignoriert
- `IgnoreProperties<TInput>(Expression<Func<TInput, object>>[] toIgnore) : ClassMappingSettings`
- `RemoveConverter(TypeConverter converter) : ClassMappingSettings`
  <br>Typeconverter zu den einstellungen hinzufügen
- `RemoveConverters(TypeConverter[] converters) : ClassMappingSettings`
- `RemoveGlobalConverter(TypeConverter converter) : ClassMappingSettings`
  <br>Removes a type converter from the global converters collection. Global converters are applied to all mapping operations unless explicitly ignored.
- `RetrieveLoadedAssembliesTypeConverters(Assembly[] assemblies) : IList<TypeConverter>`
  <br>Liefert Alle TypeConverter
- `SetAsDefault() : ClassMappingSettings`

**Eigenschaften**

- `AllowGuidConversion : bool { get; set; }`
  <br>Bei true, können int,long und string immer zu oder von Guids konvetiert werden
- `AutoCheckForDataContractJsonSerializer : bool { get; set; }`
  <br>Wenn diese Option an ist wird geprüft ob bei der JSON Konvertierung ggf der DataContractJsonSerializer benutzt werden kann (dauert etwas länger)
- `CanConvertFromJSON : bool { get; set; }`
  <br>Wenn diese Option an kann man aus einem JSON string per mapto das entsprechende Opjekt desirialisieren
- `CheckCyclicDependencies : bool { get; set; }`
  <br>Check cyclic dependencies
- `CoverUpAbstractMembers : bool { get; set; }`
  <br>Wenn true werden Abstrakte basis properties überdeckt
- `Default : ClassMappingSettings { get; }`
  <br>Gets the default mapping settings instance. If no custom default has been set, creates a new instance with standard configuration.
- `DefaultValueTypeValuesAsNullForNonValueTypes : bool { get; set; }`
  <br>Wenn diese Option true ist und der aktuelle wert einem default wert des valuetypes entspricht wird wenn der target type kein ValueType ist immer null zurückgeben, ansonsten wird der result type erzeugt und wenn möglich konvetiert oder zugewiesen.
- `Fast : ClassMappingSettings { get; }`
  <br>Gets optimized settings for fast mapping operations. This configuration ignores exceptions, skips DataContract checks, enables async processing, and disables container resolution for improved performance.
- `FormatProvider : IFormatProvider { get; set; }`
- `HasAssignments : bool { get; }`
  <br>Gibt an ob assignments vorhanden sind
- `IgnoreExceptions : bool { get; set; }`
  <br>Gibt an, ob exceptions weitergeworfen werden sollen oder nicht
- `IgnoreGlobalConverters : bool { get; set; }`
  <br>Wenn diese Option auf true steht werden die Global Konverter nicht berücksichtigt
- `IncludePrivateFields : bool { get; set; }`
  <br>Wenn true, dann werden auch private Member gemapped. (wenn eine Klasse z.B für NotifyPropertyChanged viele backing fields hat, sollte diese option auf false (default) stehen)
- `MatchCaseForEnumNameConversion : bool { get; set; }`
  <br>Wenn diese Option true ist muss bei string to enum Groß und kleinschreibung stimmen
- `MinListCountToEnumerateAsync : int { get; set; }`
  <br>Eine liste muss min so viele einträge haben wie `MinListCountToEnumerateAsync` damit diese asynchron enumeriert wird
- `ObjectToStringWithJSON : bool { get; set; }`
  <br>Wenn diese Option true ist wird ein Objekt per mapTo string zu einem JSON String
- `SearchForTryParseInTargetTypes : bool { get; set; }`
  <br>Wenn diese Option true ist wird automatisch beim target type nach einer methode TryParse gesucht und ggf zum Konvertieren benutzt
- `ServiceProvider : IServiceProvider { get; set; }`
  <br>ServiceProvider wird benutzt wenn `TryContainerResolve` auf true steht um das erste resolve des target typen / interface zu machen
- `ShouldEnumerateListsAsync : bool { get; set; }`
  <br>Wenn auf true, werden listen asyncron befüllt, dieses ist zwar schneller, doch ist dann die Reihenfolge der liste ggf nicht die gleiche wie bei dem input objekt
- `ShouldEnumeratePropertiesAsync : bool { get; set; }`
  <br>Gibt an ob Properties asncron befüllt werden sollen
- `TryContainerResolve : bool { get; set; }`
  <br>Gibt an ob zum erzeugen eines Typs versucht werden soll diesen mit Unity zu resolven (schneller wenn nicht)

### `CollectionWatcher<T>`

`class`

Watch Collection for Changes

**Konstruktoren**

- `CollectionWatcher(ICollection collection)`
  <br>Initializes a new instance of the `CollectionWatcher`1` class.
- `CollectionWatcher(ICollection collection, bool autoStartWatching)`
  <br>Initializes a new instance of the `CollectionWatcher`1` class.

**Methoden**

- `Dispose() : void`
- `StartWatching() : void`
- `StopWatching() : void`

**Eigenschaften**

- `IsWatching : bool { get; }`
  <br>Determines if active or not

**Ereignisse**

- `CountChanged : EventHandler<EventArgs<ICollection>>`
  <br>Event is raised when count changed
- `ItemAdded : EventHandler<EventArgs<T>>`
  <br>Event is raised after item has been added
- `ItemRemoved : EventHandler<EventArgs<ICollection>>`
  <br>WEvent is raised after item has been removed

### `CurrencyExchangeRateImporter`

`static class`

_Keine Beschreibung._

**Methoden**

- `GetCurrencyExchangeRateData(DateTime fromDate, DateTime toDate, Currency sourceRateCurrency = null, bool returnAverageRate = false) : IEnumerable<CurrencyImportInformation>`

### `CurrencyImportInformation`

`class`

Währungsimport information einer Währung

**Konstruktoren**

- `CurrencyImportInformation(DateRange range, decimal rate, Currency currency, Currency sourceCurrency)`
  <br>Initializes a new instance of the `CurrencyImportInformation` class.
- `CurrencyImportInformation(DateTime dateTime, decimal rate, Currency currency, Currency sourceCurrency)`
  <br>Initializes a new instance of the `CurrencyImportInformation` class.

**Eigenschaften**

- `Currency : Currency { get; }`
  <br>Die Währung, um die es sich handelt
- `Date : DateOnly { get; }`
  <br>Datum, zu dem dieser Kurs galt
- `DateRange : DateRange { get; }`
  <br>Datum, zu dem dieser Kurs galt
- `Rate : decimal { get; set; }`
  <br>Der Kurs der Währung (Multiplizierung mit diesem Kurs ergibt Currency als ergebnis)
- `SourceCurrency : Currency { get; }`
  <br>Die Währung, auf die sich der Kurs bezieht
- `SourceRate : decimal { get; }`
  <br>Der Kurs der Währung (Multiplizierung mit diesem Kurs ergibt SourceCurrency als ergebnis)

### `DictionaryHelper`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToJObject(this IDictionary<string, object> dictionary) : JObject`
- `ToObject(this IDictionary<string, object> dictionary, Type objectType) : object`
- `ToObject<T>(this IDictionary<string, object> dictionary) : T`

**Methoden**

- `GetValuesDictionary<T>(Action<T> options, bool removeDefaults, BindingFlags flags = 20) : IDictionary<string, object>`
- `GetValuesDictionary<T>(T o, bool removeDefaults, BindingFlags flags = 20) : IDictionary<string, object>`
- `GetValuesDictionary<T>(bool removeDefaults, Action<T>[] options) : IDictionary<string, object>`
- `GetValuesFunc<T>(BindingFlags flags = 20) : Func<T, Dictionary<string, object>>`

### `DumpHelper`

`static class`

Helper for Dumpfiles

**Methoden**

- `CreateDump(string fileName, MinidumpType type) : void`
  <br>Create MiniDump
- `MiniDumpWriteDump(IntPtr hProcess, int processId, IntPtr hFile, int dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callackParam) : bool`
  <br>Create MiniDump

### `EmbeddedIconInfo`

`struct`

_Keine Beschreibung._

**Felder**

- `FileName : string`
- `IconIndex : int`

### `EncodingDetector`

`static class`

EncodingDetector

**Methoden**

- `DetectBOMBytes(byte[] bomBytes) : Encoding`
  <br>Get BOM Bytes
- `DetectTextByteArrayEncoding(byte[] textData) : Encoding`
  <br>Detects Encoding for byte array
- `DetectTextByteArrayEncoding(byte[] textData, out bool hasBom) : Encoding`
- `DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize) : Encoding`
  <br>Detects Encoding for file stream
- `DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize, out bool hasBom) : Encoding`
- `DetectTextFileEncoding(string inputFilename) : Encoding`
  <br>Detects Encoding for filename
- `DetectUnicodeInByteSampleByHeuristics(byte[] sampleBytes) : Encoding`
  <br>Detects the unicode in byte sample by heuristics.
- `GetStringFromByteArray(byte[] textData, Encoding defaultEncoding) : string`
  <br>/// string aus einem byte array im richtigem encoding liedern
- `GetStringFromByteArray(byte[] textData, Encoding defaultEncoding, long maxHeuristicSampleSize) : string`
  <br>string aus einem byte array im richtigem encoding liedern

### `Enum<T>`

`static class`

Enum extensions

**Methoden**

- `ConvertAll<TOutput>() : IEnumerable<TOutput>`
- `DescriptionFor(T value) : string`
- `Except(IEnumerable<T> sequence) : IEnumerable<T>`
- `Except(T[] sequence) : IEnumerable<T>`
- `GetAttributes<TAttribute>(T value) : IEnumerable<TAttribute>`
- `GetAttributes<TAttribute>(T value, bool inherit) : IEnumerable<TAttribute>`
- `GetDictionary() : IDictionary<T, string>`
- `GetDictionaryAttributes<TAttribute>(bool inherit = false) : Dictionary<T, IEnumerable<TAttribute>>`
- `GetName(T value) : string`
- `GetValues() : IEnumerable<T>`
- `Parse(string name, bool ignoreCase = false) : T`
  <br>Converts den `name` to enum
- `ToArray() : T[]`
- `TryGetEnumValue(int value, out T? enumValue) : bool`
- `TryParse(string name, bool ignoreCase = false) : T?`
  <br>Converts den `name` to enum
- `TryParse(string name, out T value, bool ignoreCase = false) : bool`

**Eigenschaften**

- `Values : IEnumerable<T> { get; }`
  <br>Retrieve all values of an enum as a `ISet`1`.

### `EnumExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `GetCustomAttributes<TAttribute>(this Enum val, bool inherit) : TAttribute[]`
- `ToDescriptionString(this Enum val) : string`
- `ToEnum<T>(this int intValue) : T`
- `ToEnum<T>(this string input) : T`

**Methoden**

- `ToDictionary(Type enumType, Func<Enum, object> valueConverterFunc = null, Func<object, string> nameConverterFunc = null) : IDictionary<string, object>`
- `ToDictionary<TEnum>() : IDictionary<string, object>`

### `EnvironmentHelper`

`static class`

_Keine Beschreibung._

**Methoden**

- `RemoveVariablesAsync(ICollection<string> keys, EnvironmentVariableTarget target = 0, CancellationToken cancellationToken = null) : Task`
- `RemoveVariablesAsync(IDictionary<string, string> dict, EnvironmentVariableTarget target = 0, CancellationToken cancellationToken = null) : Task`
- `ResolveFullValue(string value) : string`
- `SetEnvironmentVariableWithValueReplace(string key, string value, EnvironmentVariableTarget target = 0) : void`
- `SetEnvironmentVariableWithValueReplaceAsync(string key, string value, EnvironmentVariableTarget target = 0, CancellationToken cancellationToken = null) : Task`
- `SetEnvironmentVariables(IDictionary<string, string> vars, EnvironmentVariableTarget target = 0) : void`
- `SetEnvironmentVariablesIfNotExistsAsync(IDictionary<string, string> vars, EnvironmentVariableTarget target = 0, CancellationToken cancellationToken = null) : Task<Dictionary<string, string>>`
- `SetEnvironmentVariablesIfNotExistsAsync(string key, string value, EnvironmentVariableTarget target = 0, CancellationToken cancellationToken = null) : Task<Dictionary<string, string>>`

### `EnvironmentSetScope`

`class`

Provides a scope for temporarily setting environment variables that are automatically restored when disposed.

**Konstruktoren**

- `EnvironmentSetScope(IDictionary<string, string> varsToSet)`

**Methoden**

- `Dispose() : void`

### `FileHelper`

`class`

Provides utility methods for file and directory operations, including symbolic links, file locking detection, path manipulation, and file system operations with Windows shell integration.

**Konstruktoren**

- `FileHelper()`

**Methoden**

- `CopyFile(string source, string dest, bool confirmOverwrites) : bool`
  <br>Verschiebt eine Datei
- `CopyFolder(string sourceFolderPath, string destFolderPath, bool confirmOverwrites) : void`
  <br>Kopiert einen Ordner per API-Funktion
- `CopyFolderFast(string source, string target, bool recurive = false, bool copyOnlyIfNewer = false) : void`
- `CreateSymbolicLink(SymbolLinkInfo info) : void`
- `CreateSymbolicLink(string linkName, string target) : void`
- `DestroyIcon(IntPtr hIcon) : int`
- `DirectoryExists(string path, string relativeTo = null) : bool`
  <br>Gibt an ob ein Verzeichnis existiert, und kann dabei einen relativen bzug berücksichtigen
- `EnsureDirectory(string dir) : string`
- `ExtractIcon(int hInst, string lpszExeFileName, int nIconIndex) : IntPtr`
- `ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, UInt32 nIcons) : UInt32`
- `ExtractIconFromFile(string fileAndParam) : ValueTuple<EmbeddedIconInfo, IntPtr>`
  <br>Extract the icon from file.
- `FileExists(string file, string relativeTo = null) : bool`
  <br>Gibt an ob eine Datei existiert, und kann dabei einen relativen bzug berücksichtigen
- `FileIsExecutable(string path) : bool`
  <br>Prüft, ob eine Datei ausfürbar ist (.exe, .bat, etc.)
- `GetAbsolutePath(string path, string basePath = null) : string`
  <br>Ermittelt für einen gegebenen relativen einen absoluten Pfad
- `GetAllFiles(string path, Func<DirectoryInfo, bool> directoryConditionFn, Func<FileInfo, bool> fileConditionFn, string[] filters) : IEnumerable<FileInfo>`
- `GetAllFiles(string path, string[] filters) : IEnumerable<FileInfo>`
- `GetEmbeddedIconInfo(string fileAndParam) : EmbeddedIconInfo`
  <br>Parses the parameters string to the structure of EmbeddedIconInfo.
- `GetFileDescriptionByExtension(string sExtension, out string extensionFileName) : string`
- `GetFileTypeAndIcon() : Hashtable`
- `GetMimeType(string sExtension) : string`
  <br>returns Mimetype by given extension
- `GetReadableFileSize(double fileSize, bool fullName = false, Func<string, string> nameConverterFn = null) : string`
- `GetRelativePath(string absolutePath, bool absolutePathIsDirectory, string referencePath, bool referencePathIsDirectory) : string`
  <br>Ermittelt den relativen Pfad eines absoluten Pfades
- `IsPathValid(string path) : bool`
  <br>Überprüft, ob eine Pfadangabe gültig ist
- `IsSubPathOf(string path, string baseDirPath) : bool`
- `MoveFileOrFolder(string source, string dest, bool confirmOverwrites) : bool`
  <br>Verschiebt eine Datei
- `MoveToRecycleBin(string path) : bool`
  <br>Verschiebt eine Datei in den Papierkorb
- `NextAvailableFilename(string path, string numberPattern = " ({0})") : string`
- `OpenAs(string file) : void`
  <br>Datei öffnen als
- `RemoveSymbolicLink(string linkName) : void`
- `SHGetFileInfo(string pszPath, UInt32 dwFileAttributes, ref ShfileInfo psfi, UInt32 cbSizeFileInfo, UInt32 uFlags) : IntPtr`
- `SetReadOnlyAttribute(string fullName, bool readOnly) : void`
- `ShellExecuteEx(ref ShellExecuteInfo lpExecInfo) : bool`
- `ShowProperties(FileInfo fi) : void`
- `ShowProperties(string path) : void`
- `TaskCopyDirectory(string src, string dst, bool overwriteExisting = true, bool multiTask = true, bool useWindowsCopy = false, CancellationToken cancellationToken = null) : void`
- `ToShortPath(string path, int length) : string`
  <br>Diese Funktion kürzt einen Pfad ab so das aus "C:\Windows\System32\Test\Test.dll" dann "C:\Windows\...\Test.dll" wird.
- `WhoIsLocking(string path, bool ignoreExceptions) : List<Process>`
  <br>Find out what process(es) have a lock on the specified file.

### `IJObjectParser`

`interface`

Interface for parsing structured data content into JObject instances.

**Methoden**

- `Parse(string content) : JObject`
  <br>Parses the content string into a JObject.

### `IStructuredDataObject`

`interface`

Represents an object that can be converted to structured data formats.

**Methoden**

- `ToString(StructuredDataType dataType) : string`
  <br>Converts the object to a string representation of the specified structured data type.

### `JsonDictionaryConverter`

`class`

Provides static helpers to deal with flat dictionaries and works as official JsonConverter class to keep objects and arrays after converting

**Konstruktoren**

- `JsonDictionaryConverter()`

**Methoden**

- `ConvertToDictionary(JObject jObject) : IDictionary<string, object>`
  <br>Converts a jObject to a hashable dictionary
- `ConvertToUnflattenDictionary(IDictionary<string, string> flatDictionary) : IDictionary<string, object>`
- `DictionaryToJObject(IDictionary<string, object> dictionary) : JObject`
- `Flatten(JObject jsonObject, string separator = ".") : Dictionary<string, string>`
  <br>Creates a flat dictionary for a jObject
- `Flatten(object obj, string separator = ".") : Dictionary<string, string>`
  <br>Creates a flat dictionary for an object
- `SplitPath(string path) : IList<string>`
- `Unflatten(IDictionary<string, string> keyValues) : JObject`

### `JsonJObjectParser`

`class`

Parser for JSON content that converts it to JObject instances.

**Konstruktoren**

- `JsonJObjectParser()`

**Methoden**

- `Parse(string content) : JObject`
  <br>Parses JSON content into a JObject.

### `JsStringBuilder`

`class`

Kann JavaScript bauen

**Konstruktoren**

- `JsStringBuilder(bool minfiResult, string namespacePrefix = "")`
  <br>Initializes a new instance of the `Object` class.

**Methoden**

- `Append(Type type) : JsStringBuilder`
  <br>Appends the function.
- `Append(string functionName, IDictionary<string, object> dictionary) : JsStringBuilder`
- `Append(string functionName, object obj) : JsStringBuilder`
  <br>Appends the function.
- `AppendDictionary(string functionName, IDictionary<string, object> dictionary) : JsStringBuilder`
- `AppendFunction(Type type) : JsStringBuilder`
  <br>Appends the function.
- `AppendFunction(string functionName, IDictionary<string, object> dictionary) : JsStringBuilder`
- `AppendFunction(string functionName, object obj) : JsStringBuilder`
  <br>Appends the function.
- `AppendObjectMembers(object o, string name = "", Type memberType = null, Func<object, object> selector = null) : JsStringBuilder`
- `AppendStaticMembers(Type owerClassType, Type memberType = null, Func<object, object> selector = null) : JsStringBuilder`
- `AppendStaticMembers<T>(Func<T, object> selector = null) : JsStringBuilder`
- `AppendStaticMembers<TOwner, TMember>(Func<TMember, object> selector = null) : JsStringBuilder`
- `Ignore<T>(Expression<Func<T>>[] members) : JsStringBuilder`
- `ToJsonAsync(CancellationToken cancellationToken = null) : Task<string>`
  <br>Returns a string that represents the current object.

**Eigenschaften**

- `MinifyResult : bool { get; set; }`
  <br>Gibt an ob das Ergebnis minifiziert werden soll
- `NameSpacePrefix : string { get; set; }`
  <br>Prefix

### `MemberDistinct`

`enum`

Specifies how to handle duplicate members during reflection operations.

**Werte**

- `ByName`
  <br>Deduplicate members by name.
- `Default`
  <br>Use the default deduplication strategy.
- `None`
  <br>Do not deduplicate members.
- `value__`

### `MemberMethod`

`enum`

Specifies which member types to retrieve during reflection operations.

**Werte**

- `All`
  <br>Retrieve all member types (fields and properties).
- `GetFields`
  <br>Retrieve only fields.
- `GetProperty`
  <br>Retrieve only properties.
- `value__`

### `MinidumpType`

`enum`

MinidumpType

**Werte**

- `MiniDumpFilterMemory`
- `MiniDumpFilterModulePaths`
- `MiniDumpNormal`
- `MiniDumpScanMemory`
- `MiniDumpWithCodeSegs`
- `MiniDumpWithDataSegs`
- `MiniDumpWithFullMemory`
- `MiniDumpWithFullMemoryInfo`
- `MiniDumpWithHandleData`
- `MiniDumpWithIndirectlyReferencedMemory`
- `MiniDumpWithPrivateReadWriteMemory`
- `MiniDumpWithProcessThreadData`
- `MiniDumpWithThreadInfo`
- `MiniDumpWithUnloadedModules`
- `MiniDumpWithoutOptionalData`
- `value__`

### `ProcessHelper`

`class`

Provides utility methods for working with system processes, including retrieving process information such as executable paths and command-line arguments.

**Konstruktoren**

- `ProcessHelper()`

**Methoden**

- `GetProcesses() : IList<SmallProcessInfo>`

### `ProcessWatcher`

`class`

Monitors system processes and raises events when processes are started or stopped.

**Konstruktoren**

- `ProcessWatcher()`

**Methoden**

- `Dispose() : void`
- `Start() : void`
- `Stop() : void`

**Ereignisse**

- `NewProcessesStarted : EventHandler<List<SmallProcessInfo>>`
  <br>Occurs when new processes are started.
- `ProcessesStopped : EventHandler<List<SmallProcessInfo>>`
  <br>Occurs when processes are stopped.

### `PropertyChangedEventArgs<T>`

`class`

PropertyChangedEventArgs

**Konstruktoren**

- `PropertyChangedEventArgs(string propertyName, T oldValue, T newValue, PropertyInfo propertyInfo)`

**Eigenschaften**

- `NewValue : T { get; }`
  <br>Neuer Wert der Eigenschaft
- `OldValue : T { get; }`
  <br>Alter Wert der Eigenschaft
- `Property : PropertyInfo { get; }`
  <br>PropertyInfo

### `PropertyPath<TSource>`

`static class`

_Keine Beschreibung._

**Methoden**

- `Get<TResult>(Expression<Func<TSource, TResult>> expression) : IReadOnlyList<MemberInfo>`

### `PropertyWatcher`

`class`

PropertyWatcher

**Konstruktoren**

- `PropertyWatcher(object instance, Expression<Func<object>> prop)`
- `PropertyWatcher(object instance, string propertyName = "")`
  <br>Initializes a new instance of the `PropertyWatcher` class.

**Methoden**

- `AddAllProptertiesToWatch() : void`
- `AddPropertyToWatch(Expression<Func<object>> property) : void`
- `AddPropertyToWatch(string propertyName) : void`
  <br>Fügt eine weitere Property zur überwachung hinzu
- `Dispose() : void`
- `StartWatching() : void`
- `StopWatching() : void`

**Eigenschaften**

- `IsWatching : bool { get; }`
  <br>Gibt an ob die Eigenschaft überwacht wird

**Ereignisse**

- `PropertyChanged : EventHandler<PropertyChangedEventArgs<object>>`
  <br>Wird ausgelöst sobald sich die Eigenschaft ändert

### `ReflectionHelper`

`static class`

ReflectionHelper

**Extension Methods**

- `GetFieldsRecursive(this Type type, BindingFlags flags, Func<Type, bool> continueCondition = null) : FieldInfo[]`
- `GetPropertiesRecursive(this Type type, BindingFlags flags, Func<Type, bool> continueCondition = null) : PropertyInfo[]`
- `GetSignature(this MethodInfo method, bool callable = false) : string`
  <br>Gibt die signatur der methode zur�ck
- `ImplementsInterface(this Type type, Type interfaceType) : bool`
  <br>Pr�ft ob ein bestimmter Typ ein bestimmtes interface implementiert
- `ToDictionary(this Type type, Func<Type, MemberInfo, bool> condition = null) : IDictionary<string, object>`

**Methoden**

- `ClearTypeCache() : void`
- `CreateInstance(Type t, bool allowInterfacesAndAbstractClasses, bool coverUpAbstractMembers, bool checkCyclicDependencies = true, HashSet<Type> processedTypes = null) : object`
- `CreateInstance(Type t, bool allowInterfacesAndAbstractClasses, bool coverUpAbstractMembers, bool tryResolve, bool checkCyclicDependencies, IServiceProvider serviceProvider = null) : object`
  <br>Eine instance erzeugen
- `CreateInstance(Type t, bool checkCyclicDependencies = true) : object`
  <br>Eine instance erzeugen
- `CreateInstance<T>(bool allowInterfacesAndAbstractClasses, bool coverUpAbstractMembers, bool checkCyclicDependencies = true) : T`
  <br>Instanz erzeugen
- `CreateInstance<T>(bool checkCyclicDependencies = true) : T`
  <br>Instanz erzeugen
- `CreateInstanceFromInterfaceOrAbstractType(Type interfaceType, bool coverUpAbstractMembers, bool checkCyclicDependencies = true, HashSet<Type> processedTypes = null) : object`
- `CreateInstanceFromInterfaceOrAbstractType<TInterface>(bool coverUpAbstractMembers) : TInterface`
  <br>Instanz f�r interface erzeugen
- `CreateTypeAndDeserialize(IDictionary<string, object> contentDict, string typeName = "", bool cacheTypes = false) : IStructuredDataObject`
- `CreateTypeAndDeserialize(string content, StructuredDataType structuredDataType, string typeName = "", bool cacheTypes = false) : IStructuredDataObject`
- `CreateTypeAndDeserialize(string content, string typeName = "", bool cacheTypes = false) : IStructuredDataObject`
- `CreateTypeFor(IDictionary<string, object> contentDict, string typeName = "", bool cacheTypes = false) : Type`
- `CreateTypeFor(string content, StructuredDataType structuredDataType, string typeName = "", bool cacheTypes = false) : Type`
- `CreateTypeFor(string content, string typeName = "", bool cacheTypes = false) : Type`
- `FindAllValuesOf<T>(object instance, ReflectReadSettings settings = null) : T[]`
- `FindImplementingType(Type interfaceType) : Type`
  <br>Findet den typen, der das angegebene interface implementiert
- `GetCallingMethod(int skip = 0) : MethodBase`
  <br>Gibt die Methode zur�ck, von der der Aufruf der Methode, die "GetCallingMethod" aufgerufen hat kam
- `GetDeclaringTypes(Type t) : Type[]`
  <br>Enthaltene typen zur�ckgeben
- `GetDefaultValue(Type t) : object`
  <br>Returns the default Value
- `GetProperties(object objectValue, string[] except) : Dictionary<string, object>`
  <br>Gibt alle Properties mit wert zur�ck
- `GetValue(object obj, string property) : object`
  <br>Gibt den wert einer Property eines Objektes zur�ck
- `SimpleCast(object o, Type targetType) : object`
  <br>Simple Implicit or Explicit cast
- `SimpleCast<T>(object o) : T`
  <br>Simple Implicit or Explicit cast
- `TrySimpleCast(object o, Type targetType, out object result) : bool`
- `TypeName(Type type) : string`
  <br>Get full type name with full namespace names

**Felder**

- `PublicBindingFlags : BindingFlags`
  <br>PublicBindingFlags

### `ReflectReadSettings`

`class`

Configuration settings for reflection-based member reading operations.

**Konstruktoren**

- `ReflectReadSettings()`

**Eigenschaften**

- `All : ReflectReadSettings { get; }`
- `AllExactType : ReflectReadSettings { get; }`
- `AllIsAssignableFrom : ReflectReadSettings { get; }`
- `AllIsAssignableTo : ReflectReadSettings { get; }`
- `AllPublic : ReflectReadSettings { get; }`
- `AllPublicExactType : ReflectReadSettings { get; }`
- `AllPublicIsAssignableFrom : ReflectReadSettings { get; }`
- `AllPublicIsAssignableTo : ReflectReadSettings { get; }`
- `AllWithHierarchyTraversal : ReflectReadSettings { get; }`
- `BindingFlags : BindingFlags { get; set; }`
  <br>Gets or sets the binding flags used to control which members are reflected.
- `Default : ReflectReadSettings { get; }`
  <br>Gets the default reflection settings (public and non-public instance members).
- `MemberDistinct : MemberDistinct { get; set; }`
  <br>Gets or sets the strategy for removing duplicate members.
- `MemberMethod : MemberMethod { get; set; }`
  <br>Gets or sets which member types (fields, properties, or both) to retrieve.
- `TraverseHierarchy : bool { get; set; }`
  <br>Gets or sets a value indicating whether to traverse the type hierarchy when reading members.
- `TypeMatch : ReflectTypeMatch { get; set; }`
  <br>Gets or sets the type matching strategy to use when filtering members.

### `ReflectTypeMatch`

`enum`

Specifies the type matching strategy to use when filtering reflected members.

**Werte**

- `ExactType`
  <br>Match only the exact type.
- `IsAssignableFrom`
  <br>Match types that the target type is assignable from.
- `IsAssignableTo`
  <br>Match types that are assignable to the target type.
- `NoCheck`
  <br>Do not perform type checking.
- `value__`

### `ScriptExecutingResult`

`class`

Represents the result of a script execution, containing the process and execution status.

**Methoden**

- `False(Process process = null) : ScriptExecutingResult`
- `FromResult(bool result, Process process = null) : ScriptExecutingResult`
  <br>Creates a ScriptExecutingResult from the specified result and process.
- `True(Process process = null) : ScriptExecutingResult`

**Eigenschaften**

- `Process : Process { get; }`
  <br>Gets the Process object associated with the script execution.
- `ProcessResult : bool { get; }`
  <br>Gets a value indicating whether the process execution was successful.

### `ScriptExecutionSettings`

`class`

Configuration settings for script execution, controlling visibility, output tracking, and process behavior.

**Konstruktoren**

- `ScriptExecutionSettings()`
- `ScriptExecutionSettings(bool isHidden, bool trackLiveOutput, bool waitForProcessExit)`
  <br>Initializes a new instance of the `Object` class.

**Eigenschaften**

- `Default : ScriptExecutionSettings { get; }`
- `DefaultWithCmd : ScriptExecutionSettings { get; }`
- `ExecuteWithCmd : bool { get; set; }`
- `IgnoreExceptions : bool { get; set; }`
- `IsHidden : bool { get; set; }`
- `NormalProcess : ScriptExecutionSettings { get; }`
- `OneOutputStream : ScriptExecutionSettings { get; }`
- `OneSafeOutputStream : ScriptExecutionSettings { get; }`
- `RequiresAdminPrivileges : bool { get; set; }`
- `TrackLiveOutput : bool { get; set; }`
- `WaitForProcessExit : bool { get; set; }`
- `WorkingDirectory : string { get; set; }`

### `ScriptHelper`

`class`

Provides methods for executing scripts and command-line applications with customizable execution settings, output capture, and error handling.

**Konstruktoren**

- `ScriptHelper()`

**Methoden**

- `ExecuteScript(string fileName, string arguments, ScriptExecutionSettings settings, Action<string> onDataReceived = null, Action<string> onError = null, CancellationToken cancellationToken = null) : ScriptExecutingResult`
- `ExecuteScriptAsync(string fileName, string arguments, ScriptExecutionSettings settings, Action<string> onDataReceived = null, Action<string> onError = null, CancellationToken cancellationToken = null) : Task<ScriptExecutingResult>`
- `IsPowerShell(string filename) : bool`
  <br>Determines whether the specified file is a PowerShell script based on its extension.
- `PrepareScriptVars(string scriptContent, bool removeDuplicateLines) : string`

### `SecurityHelper`

`class`

Provides security-related utility methods for checking user privileges and permissions.

**Konstruktoren**

- `SecurityHelper()`

**Methoden**

- `IsCurrentProcessAdmin() : bool`

### `ShellExecuteInfo`

`struct`

_Keine Beschreibung._

**Felder**

- `Class : string`
- `Directory : string`
- `File : string`
- `HkeyClass : IntPtr`
- `HotKey : UInt32`
- `Hwnd : IntPtr`
- `IDList : IntPtr`
- `Icon : IntPtr`
- `InstApp : IntPtr`
- `Mask : UInt32`
- `Monitor : IntPtr`
- `Parameters : string`
- `Show : UInt32`
- `Size : int`
- `Verb : string`

### `ShfileInfo`

`struct`

_Keine Beschreibung._

**Felder**

- `dwAttributes : UInt32`
- `hIcon : IntPtr`
- `iIcon : IntPtr`
- `szDisplayName : string`
- `szTypeName : string`

### `Shfileopstruct`

`struct`

_Keine Beschreibung._

**Felder**

- `fAnyOperationsAborted : bool`
- `fFlags : Int16`
- `hNameMappings : IntPtr`
- `hwnd : IntPtr`
- `lpszProgressTitle : string`
- `pFrom : string`
- `pTo : string`
- `wFunc : int`

### `SimpleConvert`

`static class`

_Keine Beschreibung._

**Methoden**

- `ConvertDataStringTo(string dataString, StructuredDataType currentDataType, StructuredDataType targetDataType) : string`
- `ConvertDataStringTo(string dataString, StructuredDataType target) : string`
- `JsonToXml(string json, string rootObjectName = "") : string`
- `JsonToYaml(string json) : string`
- `XmlToJson(string xml) : string`
- `XmlToYaml(string xml) : string`
- `YamlToJson(string yaml) : string`
- `YamlToXml(string yaml, string rootObjectName = "") : string`

### `StructuredDataFormatConverter`

`static class`

_Keine Beschreibung._

**Methoden**

- `ConvertToString(object obj, StructuredDataType dataType) : string`

### `StructuredDataType`

`enum`

Enumeration of supported structured data types.

**Werte**

- `Json`
  <br>JavaScript Object Notation format.
- `Xml`
  <br>Extensible Markup Language format.
- `Yaml`
  <br>YAML Ain't Markup Language format.
- `value__`

### `StructuredDataTypeValidator`

`class`

Provides methods to validate and detect structured data formats (JSON, XML, YAML).

**Konstruktoren**

- `StructuredDataTypeValidator()`

**Methoden**

- `DetectInputType(string content) : StructuredDataType?`
  <br>Detects the structured data type of the provided content.
- `IsValidData(string data, StructuredDataType dataType) : bool`
- `IsValidJson(string data) : bool`
- `IsValidXml(string data) : bool`
- `IsValidYaml(string data) : bool`
- `TryDetectInputType(string content, out StructuredDataType detectedType) : bool`

### `SystemHelper`

`class`

Provides system-level utility methods for querying hardware and environment information.

**Konstruktoren**

- `SystemHelper()`

**Methoden**

- `IsVirtualMachine() : bool`

### `TypeExtender`

`class`

Provides functionality to dynamically create and extend types at runtime using System.Reflection.Emit. This class allows you to build new types with properties, fields, and attributes programmatically.

**Konstruktoren**

- `TypeExtender(Type type, Type baseType = null)`
  <br>Initializes a type extender object with the name of the derived class and the base class the new class should derive from.
- `TypeExtender(string className)`
  <br>Initializes a type extender object with the name of the derive class that will extend System.Object as the base class.
- `TypeExtender(string className, Type baseType)`
  <br>Initializes a type extender object with the name of the derived class and the base class the new class should derive from.

**Methoden**

- `AddAttribute(Type type, object[] attributeCtorParams = null) : void`
  <br>Adds an attribute to the derived class
- `AddAttribute<T>(object[] attributeCtorParams = null) : void`
  <br>Adds an attribute to the derived class
- `AddField(string fieldName, Type fieldType) : void`
  <br>Adds a field to the class being extended or created
- `AddField(string fieldName, Type fieldType, Dictionary<Type, List<object>> attributeTypesAndParameters) : void`
- `AddField(string fieldName, Type fieldType, Type attributeType, object[] attributeValues) : void`
  <br>Adds a field to the class being extended or created
- `AddField<T>(string fieldName) : void`
  <br>Adds a field to the class being extended or created
- `AddField<Tfield, Tattr>(string fieldName, object[] attributeValues) : void`
  <br>Adds a field to the class being extended or created
- `AddProperty(IEnumerable<string> properties, IEnumerable<Type> types, bool allReadOnly) : void`
- `AddProperty(IEnumerable<string> propertyNames, Type propertyType) : void`
- `AddProperty(string propertyName, Type propertyType, IEnumerable<Tuple<Type, object[]>> attributesWithValues, bool isReadOnly = false) : void`
- `AddProperty(string propertyName, Type propertyType, Type attributeType, object[] attributeValues, bool isReadOnly = false) : void`
  <br>Adds a property to the class being extended or created
- `AddProperty(string propertyName, Type propertyType, bool isReadOnly = false) : void`
  <br>Adds a property to the class being extended or created
- `AddProperty<T>(IEnumerable<string> propertyNames) : void`
- `AddProperty<T>(string propertyName, IEnumerable<Tuple<Type, object[]>> attributesWithValues, bool isReadOnly = false) : void`
- `AddProperty<T>(string propertyName, bool isReadOnly = false) : void`
  <br>Adds a property to the class being extended or created
- `AddProperty<Tproperty, Tattr>(string propertyName, object[] attributeValues, bool isReadOnly) : void`
  <br>Adds a property with a custom attribute to the class being extended or created
- `FetchType() : Type`
- `Refresh() : void`

**Eigenschaften**

- `BaseType : Type { get; }`
  <br>Gets the base class that the derived class extends
- `TypeName : string { get; }`
  <br>Gets the name of the derived class

### `XmlJObjectParser`

`class`

Parser for XML content that converts it to JObject instances.

**Konstruktoren**

- `XmlJObjectParser()`

**Methoden**

- `Parse(string content) : JObject`
  <br>Parses XML content into a JObject.

### `YamlJObjectParser`

`class`

Parser for YAML content that converts it to JObject instances.

**Konstruktoren**

- `YamlJObjectParser()`

**Methoden**

- `Parse(string content) : JObject`
  <br>Parses YAML content into a JObject.

## Nextended.Core.IncludeDefinitions

### `AttributeIncludePathDefinition<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `AttributeIncludePathDefinition(string group = null, int maxDepth = 6, bool includeCollections = true)`

**Methoden**

- `GetPaths() : IEnumerable<string>`

### `CompositeIncludePathDefinition`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `CompositeIncludePathDefinition(IIncludePathDefinition[] defs)`

**Methoden**

- `GetPaths() : IEnumerable<string>`

### `FilteredIncludePathDefinition`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `FilteredIncludePathDefinition(IIncludePathDefinition inner, IEnumerable<Func<string, bool>> predicates)`

**Methoden**

- `GetPaths() : IEnumerable<string>`

### `IncludeDefinitionFilterExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `Except(this IIncludePathDefinition def, IIncludePathDefinition remove) : IIncludePathDefinition`
- `Without(this IIncludePathDefinition def, string[] exactOrGlob) : IIncludePathDefinition`
- `Without<TEntity>(this IIncludePathDefinition def, Expression<Func<TEntity, object>>[] expressions) : IIncludePathDefinition`
- `WithoutPrefix(this IIncludePathDefinition def, string[] prefixes) : IIncludePathDefinition`
- `WithoutRegex(this IIncludePathDefinition def, string[] regexes) : IIncludePathDefinition`
- `WithoutWhere(this IIncludePathDefinition def, Func<string, bool> predicate) : IIncludePathDefinition`

### `IncludeDefinitionFor<TEntity>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `IncludeDefinitionFor()`

**Methoden**

- `Include(Expression<Func<TEntity, object>> expression) : IncludeDefinitionFor<TEntity>`
- `Include(Expression<Func<TEntity, object>>[] expressions) : IncludeDefinitionFor<TEntity>`
- `IncludeAllVirtual(Func<PropertyInfo, bool> condition = null, int maxDepth = 6, bool includeCollections = true) : IncludeDefinitionFor<TEntity>`
- `IncludeAllWhere(Func<PropertyInfo, bool> condition, int maxDepth = 6, bool includeCollections = true) : IncludeDefinitionFor<TEntity>`

### `IncludePathDefinition`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `IncludePathDefinition()`

### `IncludePathDefinitionBase<TIncludeDefinition>`

`abstract class`

_Keine Beschreibung._

**Methoden**

- `Exclude(IIncludePathDefinition other) : IncludePathDefinition`
- `GetPaths() : IEnumerable<string>`
- `Include(IIncludePathDefinition other) : TIncludeDefinition`
- `Include(IIncludePathDefinition[] other) : TIncludeDefinition`
- `Include(string propertyPath) : TIncludeDefinition`
- `Include(string[] paths) : TIncludeDefinition`
- `Include<T>(Expression<Func<T, object>> expression) : TIncludeDefinition`
- `Include<T>(Expression<Func<T, object>>[] expressions) : TIncludeDefinition`
- `IncludeAllVirtual(Type type, Func<PropertyInfo, bool> condition = null, int maxDepth = 6, bool includeCollections = true) : TIncludeDefinition`
- `IncludeAllVirtual<T>(Func<PropertyInfo, bool> condition = null, int maxDepth = 6, bool includeCollections = true) : TIncludeDefinition`
- `IncludeAllWhere(Type type, Func<PropertyInfo, bool> condition, int maxDepth = 6, bool includeCollections = true) : TIncludeDefinition`
- `IncludeAllWhere<T>(Func<PropertyInfo, bool> condition, int maxDepth = 6, bool includeCollections = true) : TIncludeDefinition`
- `IncludeWithPrefix(string prefix, IIncludePathDefinition other) : TIncludeDefinition`
- `IncludeWithPrefix<TEntity, TChild>(Expression<Func<TEntity, IEnumerable<TChild>>> navigation, IIncludePathDefinition def, Func<IIncludePathDefinition, IIncludePathDefinition> mutate) : TIncludeDefinition`
- `IncludeWithPrefix<TEntity, TChild>(Expression<Func<TEntity, IEnumerable<TChild>>> navigation, IIncludePathDefinition other) : TIncludeDefinition`
- `IncludeWithPrefix<TEntity, TChild>(Expression<Func<TEntity, TChild>> navigation, IIncludePathDefinition def, Func<IIncludePathDefinition, IIncludePathDefinition> mutate) : TIncludeDefinition`
- `IncludeWithPrefix<TEntity, TChild>(Expression<Func<TEntity, TChild>> navigation, IIncludePathDefinition other) : TIncludeDefinition`

### `PrefixedIncludePathDefinition`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `PrefixedIncludePathDefinition(string prefix, IIncludePathDefinition inner)`

**Methoden**

- `GetPaths() : IEnumerable<string>`

## Nextended.Core.Measurement

### `Measure`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToMeasureResult<T>(this Task<T> task, MemoryMetric memoryMetric = 1, bool precise = true) : Task<MeasureResult<T>>`

**Methoden**

- `Run<T>(Func<T> func, MemoryMetric memoryMetric = 1, bool precise = true) : MeasureResult<T>`
- `RunAsync<T>(Func<Task<T>> func, MemoryMetric memoryMetric = 1, bool precise = true) : Task<MeasureResult<T>>`

### `MeasureResult<T>`

`struct`

_Keine Beschreibung._

**Konstruktoren**

- `MeasureResult(T Result, TimeSpan Elapsed, long AllocatedBytes)`

**Eigenschaften**

- `AllocatedBytes : long { get; set; }`
- `Elapsed : TimeSpan { get; set; }`
- `Result : T { get; set; }`

### `MemoryMetric`

`enum`

_Keine Beschreibung._

**Werte**

- `None`
- `ProcessAllocatedBytes`
- `ThreadAllocatedBytes`
- `value__`

## Nextended.Core.OData

### `ODataExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `ToFilterString(this Expression expression) : string`
- `ToFilterString<TSource>(this Expression<Func<TSource, bool>> expression) : string`
- `ToFilterString<TSource>(this IQueryable<TSource> source) : string`
- `ToODataModel(this IQueryable source) : ODataQueryModel`
- `ToODataModel<TSource>(this IQueryable<TSource> source) : ODataQueryModel`
- `ToSelectString<TSource>(this IQueryable<TSource> source) : string`

### `ODataQueryModel`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ODataQueryModel()`

**Methoden**

- `Equals(ODataQueryModel other) : bool`
- `For<T>(Func<IQueryable<T>, IQueryable> action) : ODataQueryModel`
- `FromString(string query) : ODataQueryModel`
- `Parse(string s, IFormatProvider culture = null) : ODataQueryModel`
- `ToExpression<T>() : Expression<Func<T, bool>>`
- `ToQueryable<TSource, TResult>(IQueryable<TSource> source) : IQueryable<TResult>`
- `ToQueryable<TSource>(IQueryable<TSource> source) : IQueryable<TSource>`
- `ToQueryableWithSelect<TSource>(IQueryable<TSource> source) : IQueryable`
- `TryParse(string s, IFormatProvider culture, out ODataQueryModel res) : bool`
- `TryParse(string s, out ODataQueryModel res) : bool`

**Eigenschaften**

- `Filter : string { get; set; }`
- `FilterString : string { get; }`
- `FullString : string { get; }`
- `IsValid : bool { get; }`
- `OrderBy : string { get; set; }`
- `OrderByString : string { get; }`
- `Select : string { get; set; }`
- `SelectString : string { get; }`
- `Skip : string { get; set; }`
- `SkipString : string { get; }`
- `Take : string { get; set; }`
- `TakeString : string { get; }`

## Nextended.Core.Scopes

### `ActionScope`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `ActionScope(Action actionIn, Action actionOut)`
- `ActionScope(Action actionOut)`

**Methoden**

- `Dispose() : void`

### `PauseCheckedActionScope`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `PauseCheckedActionScope(Action actionIn, Action actionOut)`
  <br>Initializes a new instance.
- `PauseCheckedActionScope(Action actionIn, Action actionOut, CancellationToken cancellationToken)`

**Methoden**

- `Dispose() : void`

## Nextended.Core.Streams

### `HashCalculationStream`

`class`

Stream which calculates a hash value while the data is being read from the wrapped stream.

**Konstruktoren**

- `HashCalculationStream(Stream sourceStream, HashAlgorithmName hashAlgorithmName, bool disposeSourceStream = true)`
  <br>Creates a new instance of `HashCalculationStream`

**Methoden**

- `GetHash() : byte[]`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanSeek : bool { get; }`
- `CanWrite : bool { get; }`
- `Length : long { get; }`
- `Position : long { get; set; }`

### `HashCalculationStreamWrite`

`class`

Stream which calculates a hash value while the data is being written to it. Useful to calculate a hash for a stream but not actually persist it somewhere.

**Konstruktoren**

- `HashCalculationStreamWrite(HashAlgorithmName hashAlgorithmName)`
  <br>Creates a new instance of `HashCalculationStreamWrite`

**Methoden**

- `GetHash() : byte[]`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanSeek : bool { get; }`
- `CanWrite : bool { get; }`
- `Length : long { get; }`
- `Position : long { get; set; }`

### `MultiStream`

`class`

Stream which reads data from multiple streams in sequence until all are exhausted. Supports streams which are not seekable or have an undefined length, e.g HTTP streams.

**Konstruktoren**

- `MultiStream(IList<Stream> sourceStreams, bool disposeSourceStreams = true)`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanSeek : bool { get; }`
- `CanWrite : bool { get; }`
- `Length : long { get; }`
- `Position : long { get; set; }`

### `NonDisposableStream`

`class`

Stream to wrap another stream to prevent it from being disposed

**Konstruktoren**

- `NonDisposableStream(Stream sourceStream)`

**Methoden**

- `ForceDispose() : void`

**Eigenschaften**

- `CanRead : bool { get; }`
- `CanSeek : bool { get; }`
- `CanWrite : bool { get; }`
- `Length : long { get; }`
- `Position : long { get; set; }`

## Nextended.Core.TypeConverters

### `DateOnlyToDoubleConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DateOnlyToDoubleConverter(bool allowAssignableInputs)`

### `DateTimeToDoubleConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DateTimeToDoubleConverter(bool allowAssignableInputs)`

### `DoubleToDateOnlyConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DoubleToDateOnlyConverter(bool allowAssignableInputs)`

### `DoubleToDateTimeConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DoubleToDateTimeConverter(bool allowAssignableInputs)`

### `DoubleToTimeOnlyConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DoubleToTimeOnlyConverter(bool allowAssignableInputs)`

### `DoubleToTimeSpanConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DoubleToTimeSpanConverter(bool allowAssignableInputs)`

### `GenericTypeConverter<TIn, TOut>`

`class`

Konverter mit konverter func, kann z.B für serializer und classmapper benutzt werden

**Konstruktoren**

- `GenericTypeConverter(Func<TIn, TOut> converterFunc, bool allowAssignableInputs)`

**Methoden**

- `SetConverterFunc(Func<TIn, TOut> fn) : void`

### `SimpleFuncConverter`

`class`

Konverter mit konverter func, kann z.B für serializer und classmapper benutzt werden

**Konstruktoren**

- `SimpleFuncConverter(Type tIn, Type tOut, Func<object, object> converterFunc, bool allowAssignableInputs)`

**Methoden**

- `SetConverterFunc(Func<object, object> fn) : void`

### `TimeOnlyToDoubleConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeOnlyToDoubleConverter(bool allowAssignableInputs)`

### `TimeSpanToDoubleConverter`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeSpanToDoubleConverter(bool allowAssignableInputs)`

## Nextended.Core.Types

### `BaseId<T, TIdType>`

`abstract class`

Zusammenfassung für BaseId.

**Methoden**

- `Equals(T other) : bool`

**Eigenschaften**

- `Id : TIdType { get; }`
  <br>Identifier

### `Currency`

`class`

Währung

**Konstruktoren**

- `Currency()`
- `Currency(CultureInfo cultureInfo)`
  <br>Initializes a new instance of the `Currency` class.
- `Currency(RegionInfo regionInfo)`
  <br>Initializes a new instance of the `Currency` class.
- `Currency(string isoCode)`
  <br>Initializes a new instance of the `Currency` class.
- `Currency(string name, string isoCode)`
  <br>Initializes a new instance of the `Currency` class.

**Methoden**

- `ConvertAmount(decimal amount, Currency targetCurrency, DateTime? currencyRateTargetDate = null) : Money`
- `CreateMoney(decimal amount) : Money`
- `Equals(Currency other) : bool`
  <br>Equalses the specified other.
- `Find(string s) : Currency`
  <br>Versucht für den übergebenen string eine Currency zu finden
- `GetCulturesForCurrencyISOCode(string isoCode) : IEnumerable<CultureInfo>`
- `GetCurrencyNameForISOCode(string currencyIso) : string`

**Eigenschaften**

- `AED : Currency { get; }`
- `AFN : Currency { get; }`
- `ALL : Currency { get; }`
- `AMD : Currency { get; }`
- `ANG : Currency { get; }`
- `AOA : Currency { get; }`
- `ARS : Currency { get; }`
- `AUD : Currency { get; }`
- `AWG : Currency { get; }`
- `AZN : Currency { get; }`
- `All : IEnumerable<Currency> { get; }`
  <br>Gibt alle möglichen Währungen zurück
- `BAM : Currency { get; }`
- `BBD : Currency { get; }`
- `BDT : Currency { get; }`
- `BGN : Currency { get; }`
- `BHD : Currency { get; }`
- `BIF : Currency { get; }`
- `BMD : Currency { get; }`
- `BND : Currency { get; }`
- `BOB : Currency { get; }`
- `BRL : Currency { get; }`
- `BSD : Currency { get; }`
- `BTN : Currency { get; }`
- `BWP : Currency { get; }`
- `BYN : Currency { get; }`
- `BZD : Currency { get; }`
- `CAD : Currency { get; }`
- `CDF : Currency { get; }`
- `CHF : Currency { get; }`
- `CLP : Currency { get; }`
- `CNY : Currency { get; }`
- `COP : Currency { get; }`
- `CRC : Currency { get; }`
- `CUP : Currency { get; }`
- `CVE : Currency { get; }`
- `CZK : Currency { get; }`
- `Cultures : IEnumerable<CultureInfo> { get; }`
  <br>Gibt alle Cultures für diese Währung zurück
- `DJF : Currency { get; }`
- `DKK : Currency { get; }`
- `DOP : Currency { get; }`
- `DZD : Currency { get; }`
- `EGP : Currency { get; }`
- `ERN : Currency { get; }`
- `ETB : Currency { get; }`
- `Euro : Currency { get; }`
  <br>Euro
- `ExternalId : object { get; set; }`
  <br>ExternalId
- `FJD : Currency { get; }`
- `FKP : Currency { get; }`
- `GBP : Currency { get; }`
- `GEL : Currency { get; }`
- `GGP : Currency { get; }`
- `GHS : Currency { get; }`
- `GIP : Currency { get; }`
- `GMD : Currency { get; }`
- `GNF : Currency { get; }`
- `GTQ : Currency { get; }`
- `GYD : Currency { get; }`
- `HKD : Currency { get; }`
- `HNL : Currency { get; }`
- `HRK : Currency { get; }`
- `HTG : Currency { get; }`
- `HUF : Currency { get; }`
- `IDR : Currency { get; }`
- `ILS : Currency { get; }`
- `IMP : Currency { get; }`
- `INR : Currency { get; }`
- `IQD : Currency { get; }`
- `IRR : Currency { get; }`
- `ISK : Currency { get; }`
- `IsValid : bool { get; }`
  <br>Gibt an ob die Währung gültig ist
- `IsoCode : string { get; set; }`
  <br>ISOCurrencySymbol
- `JEP : Currency { get; }`
- `JMD : Currency { get; }`
- `JOD : Currency { get; }`
- `JPY : Currency { get; }`
- `KES : Currency { get; }`
- `KGS : Currency { get; }`
- `KHR : Currency { get; }`
- `KMF : Currency { get; }`
- `KPW : Currency { get; }`
- `KRW : Currency { get; }`
- `KWD : Currency { get; }`
- `KYD : Currency { get; }`
- `KZT : Currency { get; }`
- `LAK : Currency { get; }`
- `LBP : Currency { get; }`
- `LKR : Currency { get; }`
- `LRD : Currency { get; }`
- `LSL : Currency { get; }`
- `LYD : Currency { get; }`
- `MAD : Currency { get; }`
- `MDL : Currency { get; }`
- `MGA : Currency { get; }`
- `MKD : Currency { get; }`
- `MMK : Currency { get; }`
- `MNT : Currency { get; }`
- `MOP : Currency { get; }`
- `MRU : Currency { get; }`
- `MUR : Currency { get; }`
- `MVR : Currency { get; }`
- `MWK : Currency { get; }`
- `MXN : Currency { get; }`
- `MYR : Currency { get; }`
- `MZN : Currency { get; }`
- `NAD : Currency { get; }`
- `NGN : Currency { get; }`
- `NIO : Currency { get; }`
- `NOK : Currency { get; }`
- `NPR : Currency { get; }`
- `NZD : Currency { get; }`
- `Name : string { get; set; }`
  <br>Name der währung
- `NativeName : string { get; set; }`
  <br>Nativer Name der Währung
- `OMR : Currency { get; }`
- `PAB : Currency { get; }`
- `PEN : Currency { get; }`
- `PGK : Currency { get; }`
- `PHP : Currency { get; }`
- `PKR : Currency { get; }`
- `PLN : Currency { get; }`
- `PYG : Currency { get; }`
- `QAR : Currency { get; }`
- `RON : Currency { get; }`
- `RSD : Currency { get; }`
- `RUB : Currency { get; }`
- `RWF : Currency { get; }`
- `Regions : IEnumerable<RegionInfo> { get; }`
  <br>enthält alle Regionen für diese Währung
- `SAR : Currency { get; }`
- `SBD : Currency { get; }`
- `SCR : Currency { get; }`
- `SDG : Currency { get; }`
- `SEK : Currency { get; }`
- `SGD : Currency { get; }`
- `SHP : Currency { get; }`
- `SLL : Currency { get; }`
- `SOS : Currency { get; }`
- `SRD : Currency { get; }`
- `SSP : Currency { get; }`
- `STN : Currency { get; }`
- `SVC : Currency { get; }`
- `SYP : Currency { get; }`
- `SZL : Currency { get; }`
- `Symbol : string { get; set; }`
  <br>Symbol der Währung (z.b €)
- `THB : Currency { get; }`
- `TJS : Currency { get; }`
- `TMT : Currency { get; }`
- `TND : Currency { get; }`
- `TOP : Currency { get; }`
- `TRY : Currency { get; }`
- `TTD : Currency { get; }`
- `TVD : Currency { get; }`
- `TWD : Currency { get; }`
- `TZS : Currency { get; }`
- `UAH : Currency { get; }`
- `UGX : Currency { get; }`
- `USD : Currency { get; }`
  <br>US-Dollar
- `UYU : Currency { get; }`
- `UZS : Currency { get; }`
- `VED : Currency { get; }`
- `VES : Currency { get; }`
- `VND : Currency { get; }`
- `VUV : Currency { get; }`
- `WST : Currency { get; }`
- `XAF : Currency { get; }`
- `XCD : Currency { get; }`
- `XOF : Currency { get; }`
- `XPF : Currency { get; }`
- `YER : Currency { get; }`
- `ZAR : Currency { get; }`
- `ZMW : Currency { get; }`
- `ZWL : Currency { get; }`

### `DataUrl`

`class`

Represents a data URL (RFC 2397) that encodes binary data in a base64 string with an optional MIME type.

**Konstruktoren**

- `DataUrl(byte[] bytes, ContentType mimeType)`
  <br>Initializes a new instance of the DataUrl class with the specified bytes and MIME type.
- `DataUrl(byte[] bytes, string mimeType = null)`
  <br>Initializes a new instance of the DataUrl class with the specified bytes and optional MIME type.
- `DataUrl(string url)`
  <br>Initializes a new instance of the DataUrl class by parsing an existing data URL string.

**Methoden**

- `GetDataUrl(byte[] bytes, string mimeType = "application/octet-stream") : string`
- `GetDataUrlAsync(byte[] bytes, string mimeType = "application/octet-stream", CancellationToken ct = null) : Task<string>`
- `IsDataUrl(string url) : bool`
- `Parse(string url) : DataUrl`
- `TryParse(string url, out DataUrl dataUrl) : bool`

**Eigenschaften**

- `Bytes : byte[] { get; }`
  <br>Gets the binary data encoded in the data URL.
- `MimeType : string { get; }`
  <br>Gets the MIME type of the data.

### `Date`

`class`

Ein Datum (ohne Zeit)

**Konstruktoren**

- `Date(DateTime value)`
  <br>Konstruktor für Date mit DateTime
- `Date(int year, int month, int day)`
  <br>Konstruktor für Date mit Jahr, Monat und Tag.

**Methoden**

- `AddDays(int value) : Date`
  <br>Addiert die Anzahl Tage zu dem Datum
- `AddMonths(int value) : Date`
  <br>Addiert die Anzahl der Monate zu dem Datum
- `AddYears(int value) : Date`
  <br>Addiert die Anzahl der Jahre zu dem Datum
- `CompareTo(Date other) : int`
- `CompareTo(object obj) : int`
  <br>Compare-Methode
- `GetMonthBetweenDates(Date startDate, Date endDate) : int`
  <br>Berechnet die Anzahl Monate zwischen zwei Datumsangaben

**Eigenschaften**

- `DateTime : DateTime { get; }`
  <br>DateTime Zugriff
- `Day : int { get; }`
  <br>Liefert den Tag des Monats
- `Month : int { get; }`
  <br>Liefert den Monat
- `Today : Date { get; }`
  <br>Date now
- `Year : int { get; }`
  <br>Liefert das Jahr

### `EventArgs<T>`

`class`

Generic Eventargs

**Konstruktoren**

- `EventArgs(T value)`

**Eigenschaften**

- `Value : T { get; }`
  <br>Gets or sets the value.

### `Hierarchical<T>`

`abstract class`

_Keine Beschreibung._

**Methoden**

- `ContainsChild(T entry) : bool`
- `GetLoadingIndicatorItems() : HashSet<T>`
- `GetPathString(Func<T, string> toStringFn, string separator = "/") : string`
- `LoadChildren() : Task`

**Eigenschaften**

- `Children : HashSet<T> { get; set; }`
- `HasChildren : bool { get; }`
- `IsExpanded : bool { get; set; }`
- `IsLoading : bool { get; set; }`
- `LoadChildrenFunc : Func<T, CancellationToken, Task<HashSet<T>>> { get; set; }`
- `OnChildrenLoaded : Action<IHierarchical<T>, HashSet<T>> { get; set; }`
- `Parent : T { get; set; }`
- `Path : IEnumerable<T> { get; }`

### `HierarchicalExtensions`

`static class`

_Keine Beschreibung._

**Extension Methods**

- `Contains<T>(this T node, T entry) : bool`
- `Find<T>(this IEnumerable<T> entries, Func<T, bool> predicate) : IEnumerable<T>`
- `Find<T>(this T entry, Func<T, bool> predicate) : IEnumerable<T>`
- `GetLoadedChildren<T>(this T node) : IEnumerable<T>`
- `GetPathString<T>(this T node, Func<T, string> toStringFn, string separator = "/") : string`
- `HasChildren<T>(this T node) : bool`
- `IsInPathOf<T>(this T node, T entry) : bool`
- `NeedsLoadChildren<T>(this T node) : bool`
- `Path<T>(this T item) : IEnumerable<T>`
- `Siblings<T>(this T node) : IEnumerable<T>`

### `IAsyncHierarchical<T>`

`interface`

_Keine Beschreibung._

**Methoden**

- `GetLoadingIndicatorItems() : HashSet<T>`
- `LoadChildren() : Task`

**Eigenschaften**

- `IsLoading : bool { get; set; }`
- `LoadChildrenFunc : Func<T, CancellationToken, Task<HashSet<T>>> { get; }`
- `OnChildrenLoaded : Action<IHierarchical<T>, HashSet<T>> { get; set; }`

### `IChildInfo`

`interface`

_Keine Beschreibung._

**Eigenschaften**

- `HasChildren : bool { get; }`

### `IHierarchical<T>`

`interface`

_Keine Beschreibung._

**Eigenschaften**

- `Children : HashSet<T> { get; }`
- `Parent : T { get; }`

### `IRangeMath<T>`

`interface`

_Keine Beschreibung._

**Methoden**

- `Add(T value, double delta) : T`
- `Difference(T start, T end) : double`
- `FromDouble(double value) : T`
- `ToDouble(T value) : double`

### `ITypeBasedId`

`interface`

Interface für typisierte Ids

### `ITypeBasedId<TIdType>`

`interface`

Interface für typisierte Ids

**Eigenschaften**

- `Id : TIdType { get; }`
  <br>Die enthaltene echte (konkrete) Id

### `Money`

`class`

Oberklasse für Geldbeträge

**Konstruktoren**

- `Money(decimal d, Currency currency = null)`
  <br>Konstruktor mit Betrag (nimmt Standart-Währung)

**Methoden**

- `Add(Money m1, Money m2) : Money`
  <br>+ Operator zum Addieren
- `Compare(Money m1, Money m2) : int`
  <br>Vergleicht zwei Money-Werte 1) beide null -&gt; 0 2) m1 null -&gt; -1 3) m2 null -&gt; 1 4) sonst -&gt; decimal.Compare
- `CompareTo(object obj) : int`
  <br>Vergleicht mit dem angegebenen Objekt obj. 1) obj == null -&gt; 1 2) obj nicht vom Type Money -&gt; ArgumentException 3) sonst -&gt; Money.Compare dieser und der angegebenen Money-Klasse.
- `ConvertCurrency(Currency targetCurrency, DateTime currencyRateTargetDate = null) : Money`
- `Divide(Money m, decimal d) : Money`
  <br>Division : Money mit decimal
- `EnsureSameCurrencyAs(Money other) : Money`
- `HasValidNumberOfDecimalPlaces() : bool`
- `Multiply(Money m, decimal d) : Money`
  <br>Multiplikation : Money mit decimal
- `Multiply(Money m, int i) : Money`
  <br>Multiplikation : Money mit int
- `Multiply(Money m1, Money m2) : Money`
  <br>Multiplikation von Money mit Money ergibt Exception! Denn : Wieviel ist 4 Euro mal 3 Euro ? Richtig: 12 Quadrateuro...
- `Multiply(decimal d, Money m) : Money`
  <br>Multiplikation : decimal mit Money
- `Multiply(double d, Money m) : Money`
  <br>Multiplikation : double mit Money
- `Multiply(int i, Money m) : Money`
  <br>Multiplikation : int mit Money
- `Negate(Money m) : Money`
  <br>- Negations-Operator
- `Parse(string s, IFormatProvider culture = null) : Money`
  <br>Versucht, aus einem String einen Geldbetrag zu parsen. Zuerst wird anhand einer Liste bekannter Währungen nach einem Währungshinweis gesucht. Wird eine eindeutige Währung gefunden, so wird das Zeichen bzw. der Name entfernt und, falls keine Culture übergeben wurde, anhand eines (in der Währung hinterlegten) Standard-CultureCode eine Culture erstellt. Wird kein Währungshinweis gefunden, so wird versucht, direkt den Zahlenwert zu parsen.
- `ParseOrNull(string s, IFormatProvider culture = null) : Money`
  <br>Parses a string to a Money object or returns null if the string is not parsable.
- `Round() : decimal`
- `SetCurrency(Currency currency) : Money`
- `Subtract(Money m1, Money m2) : Money`
  <br>- Operator zum Subtrahieren
- `TryParse(string s, IFormatProvider culture, out Money money) : bool`
- `TryParse(string s, out Money money) : bool`

**Eigenschaften**

- `Amount : decimal { get; }`
  <br>Betrag
- `Currency : Currency { get; set; }`
  <br>Currency for this
- `IsNegative : bool { get; }`
  <br>Negativer Betrag
- `IsPositive : bool { get; }`
  <br>Positiver Betrag
- `IsZero : bool { get; }`
  <br>Ist 0

**Felder**

- `DECIMALS : int`
  <br>Rundungsgenauigkeit von Round()
- `Zero : Money`
  <br>Nullobject für 0.0

### `Quote`

`class`

Quoten

**Konstruktoren**

- `Quote(double value)`
  <br>Konstruktor

**Methoden**

- `Compare(Quote q1, Quote q2) : int`
- `CompareTo(object obj) : int`
- `Parse(string s) : Quote`
  <br>Parst ein Zahl
- `ToString(string format, IFormatProvider formatProvider) : string`

**Felder**

- `One : Quote`
  <br>Nullobject für 1.0 = 100%
- `Zero : Quote`
  <br>Nullobject für 0.0

### `RangeLength<T>`

`struct`

_Keine Beschreibung._

**Konstruktoren**

- `RangeLength(T delta, IRangeMath<T> math = null)`
- `RangeLength(double delta, IRangeMath<T> math = null)`

**Methoden**

- `AddTo(T value) : T`
- `CompareTo(RangeLength<T> other) : int`
- `Equals(RangeLength<T> other) : bool`
- `SubtractFrom(T value) : T`

**Eigenschaften**

- `Delta : double { get; }`

### `RangeMath<T>`

`static class`

_Keine Beschreibung._

**Methoden**

- `AddDelta(T v, double d) : T`
- `AddSteps(T v, RangeLength<T> step, int steps) : T`
- `Clamp(T v, IRange<T> bounds) : T`
- `Clamp<T>(T value, T min, T max) : T`
- `Delta(IRange<T> r) : double`
- `FromDouble(double d) : T`
- `Lerp(IRange<T> size, double pct) : T`
- `Percent(T v, IRange<T> size) : double`
- `SnapToStep(T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = 0) : T`
- `Span(IRange<T> r) : double`
- `ToDouble(T v) : double`

### `RangeOf<T>`

`struct`

Implementation of a range in a struct.

**Konstruktoren**

- `RangeOf(IRange<T> other, Func<IRange<T>, IRange<T>, double, bool> areAdjacentFncFunc = null)`
- `RangeOf(T start, T end, Func<IRange<T>, IRange<T>, double, bool> areAdjacentFncFunc = null)`
- `RangeOf(T startAndEnd, Func<IRange<T>, IRange<T>, double, bool> areAdjacentFncFunc = null)`

**Methoden**

- `ClampLength(RangeLength<T> min, RangeLength<T> max) : RangeOf<T>`
- `Contains(T value) : bool`
- `Enumerate(RangeLength<T> step, bool includeEnd = true) : IEnumerable<T>`
- `Enumerate(bool includeEnd = true) : IEnumerable<T>`
- `Intersection(IRange<T> other) : IRange<T>`
- `Intersects(IRange<T> other) : bool`
- `IsAdjacent(IRange<T> other, double tolerance = 0) : bool`
- `IsInRange(T value) : bool`
- `Union(IRange<T> other) : IRange<T>`

**Eigenschaften**

- `End : T { get; }`
- `Length : RangeLength<T> { get; }`
- `Start : T { get; }`

### `SimpleRange<T>`

`class`

Implementation of a simple range.

**Konstruktoren**

- `SimpleRange(IRange<T> existing)`
- `SimpleRange(T start, T end)`
- `SimpleRange(T startAndEnd)`

**Methoden**

- `ClampLength(RangeLength<T> min, RangeLength<T> max) : RangeOf<T>`
- `Contains(T value) : bool`
- `Intersection(IRange<T> other) : IRange<T>`
- `Intersects(IRange<T> other) : bool`
- `IsAdjacent(IRange<T> other, double tolerance = 0) : bool`
- `IsInRange(T value) : bool`
- `Union(IRange<T> other) : IRange<T>`

**Eigenschaften**

- `End : T { get; }`
- `Length : RangeLength<T> { get; }`
- `Start : T { get; }`

### `SmallProcessInfo`

`class`

Represents basic information about a running process, including its ID, executable path, and command-line arguments.

**Konstruktoren**

- `SmallProcessInfo()`

**Eigenschaften**

- `CommandLine : string { get; set; }`
  <br>Gets or sets the command-line arguments used to start the process.
- `FileName : string { get; }`
  <br>Gets the file name (without path) of the process executable.
- `Id : int { get; set; }`
  <br>Gets or sets the process ID.
- `Path : string { get; set; }`
  <br>Gets or sets the full path to the process executable.
- `Process : Process { get; set; }`
  <br>Gets or sets the Process object.

### `SnapPolicy`

`enum`

_Keine Beschreibung._

**Werte**

- `Ceiling`
- `Floor`
- `Nearest`
- `value__`

### `SuperType<T>`

`abstract class`

SuperType base class for supertype pattern instead of enum

**Methoden**

- `CompareTo(SuperType<T> other) : int`
- `Equals(SuperType<T> other) : bool`
- `Equals(T other) : bool`
- `Equals(int other) : bool`
  <br>Equalses the specified other.
- `Get(int id) : T`
  <br>Indexer
- `Get(string identifier) : T`
  <br>Identifier
- `GetResourceName() : string`

**Eigenschaften**

- `All : T[] { get; }`
  <br>List of all member
- `Description : string { get; }`
  <br>Description
- `Id : int { get; }`
  <br>Id
- `Identifier : string { get; }`
  <br>Identifier
- `Name : string { get; }`
  <br>Name

### `SymbolLinkInfo`

`class`

Represents information about a symbolic link, including the link path and its target.

**Konstruktoren**

- `SymbolLinkInfo(string linkName, string target)`
  <br>Initializes a new instance of the SymbolLinkInfo class.

**Eigenschaften**

- `LinkName : string { get; }`
  <br>Gets the path to the symbolic link.
- `Target : string { get; }`
  <br>Gets the target path that the symbolic link points to.

## Nextended.Core.Types.Ranges

### `DateRange`

`class`

A date (without time) range.

**Konstruktoren**

- `DateRange(DateOnly start, DateOnly end)`
- `DateRange(DateOnly startAndEnd)`

### `DateRangeLegacy`

`class`

Zeitbereich zwischen zwei Dates

**Konstruktoren**

- `DateRangeLegacy(Date startDate, Date endDate)`
  <br>Konstruktor

**Eigenschaften**

- `EndDate : Date { get; }`
  <br>Ende
- `StartDate : Date { get; }`
  <br>Beginn

### `DateTimeRange`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DateTimeRange(DateTime start, DateTime end)`
- `DateTimeRange(DateTime startAndEnd)`

### `TimeRange`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeRange(TimeOnly start, TimeOnly end)`
- `TimeRange(TimeOnly startAndEnd)`

### `TimeSpanRange`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeSpanRange(TimeSpan start, TimeSpan end)`
- `TimeSpanRange(TimeSpan startAndEnd)`

## Nextended.Core.Types.Ranges.Math

### `DateOnlyRangeMath`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DateOnlyRangeMath()`

**Methoden**

- `Add(DateOnly v, double d) : DateOnly`
- `Difference(DateOnly s, DateOnly e) : double`
- `FromDouble(double d) : DateOnly`
- `ToDouble(DateOnly v) : double`

### `DateTimeRangeMath`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `DateTimeRangeMath()`

**Methoden**

- `Add(DateTime value, double delta) : DateTime`
- `Difference(DateTime start, DateTime end) : double`
- `FromDouble(double value) : DateTime`
- `ToDouble(DateTime value) : double`

### `NumericRangeMath<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `NumericRangeMath()`

**Methoden**

- `Add(T value, double delta) : T`
- `Difference(T start, T end) : double`
- `FromDouble(double value) : T`
- `ToDouble(T value) : double`

### `RangeMathFactory`

`static class`

_Keine Beschreibung._

**Methoden**

- `For<T>() : IRangeMath<T>`

### `TimeOnlyRangeMath`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeOnlyRangeMath()`

**Methoden**

- `Add(TimeOnly v, double d) : TimeOnly`
- `Difference(TimeOnly s, TimeOnly e) : double`
- `FromDouble(double d) : TimeOnly`
- `ToDouble(TimeOnly v) : double`

### `TimeSpanRangeMath`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `TimeSpanRangeMath()`

**Methoden**

- `Add(TimeSpan v, double d) : TimeSpan`
- `Difference(TimeSpan s, TimeSpan e) : double`
- `FromDouble(double d) : TimeSpan`
- `ToDouble(TimeSpan v) : double`

### `UniversalRangeMath<T>`

`class`

_Keine Beschreibung._

**Konstruktoren**

- `UniversalRangeMath()`

**Methoden**

- `Add(T value, double delta) : T`
- `Difference(T start, T end) : double`
- `FromDouble(double value) : T`
- `ToDouble(T value) : double`

↩ [Zurück zur Paketseite](/de/projects/core)
