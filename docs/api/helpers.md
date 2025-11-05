# Helper Utilities Reference

This page provides an overview of the helper utility classes available in Nextended.Core.Helper.

## FileHelper

**Namespace**: `Nextended.Core.Helper`

Provides utility methods for file and directory operations, including symbolic links, file locking detection, path manipulation, and file system operations with Windows shell integration.

### Key Methods

| Method | Description |
|--------|-------------|
| `NextAvailableFilename(string path)` | Generates next available filename if file exists |
| `CreateSymbolicLink(string linkName, string target)` | Creates a symbolic link |
| `WhoIsLocking(string path)` | Finds processes locking a file |
| `GetAllFiles(string path, string[] filters)` | Gets all files recursively with filters |
| `GetRelativePath(...)` | Converts absolute path to relative path |
| `GetAbsolutePath(string path, string basePath)` | Converts relative path to absolute path |
| `CopyFolder(string source, string dest, bool confirm)` | Copies folder using Windows shell |
| `MoveToRecycleBin(string path)` | Moves file to recycle bin |
| `GetReadableFileSize(double fileSize)` | Formats file size as human-readable string |

### Example Usage

```csharp
using Nextended.Core.Helper;

// Find what process is locking a file
var processes = FileHelper.WhoIsLocking(@"C:\myfile.txt", ignoreExceptions: true);
foreach (var process in processes)
{
    Console.WriteLine($"File is locked by: {process.ProcessName}");
}

// Get next available filename
string newFile = FileHelper.NextAvailableFilename(@"C:\document.txt");
// If document.txt exists, returns "C:\document (1).txt"

// Get human-readable file size
long bytes = 1536000;
string size = FileHelper.GetReadableFileSize(bytes); // "1.46 MB"

// Copy folder with Windows shell
FileHelper.CopyFolder(@"C:\Source", @"C:\Destination", confirmOverwrites: false);
```

---

## ScriptHelper

**Namespace**: `Nextended.Core.Helper`

Provides methods for executing scripts and command-line applications with customizable execution settings, output capture, and error handling.

### Key Methods

| Method | Description |
|--------|-------------|
| `ExecuteScript(string fileName, string args, ScriptExecutionSettings)` | Executes a script synchronously |
| `ExecuteScriptAsync(...)` | Executes a script asynchronously |
| `IsPowerShell(string filename)` | Checks if file is a PowerShell script |

### ScriptExecutionSettings

Configuration class for script execution:

| Property | Description |
|----------|-------------|
| `IsHidden` | Whether to hide the process window |
| `TrackLiveOutput` | Whether to capture output in real-time |
| `WaitForProcessExit` | Whether to wait for process completion |
| `RequiresAdminPrivileges` | Whether admin privileges are required |
| `ExecuteWithCmd` | Whether to execute via cmd.exe |

### Example Usage

```csharp
using Nextended.Core.Helper;

// Execute a PowerShell script
var settings = ScriptExecutionSettings.Default;
var result = ScriptHelper.ExecuteScript(
    "powershell.exe",
    "-File script.ps1",
    settings,
    onDataReceived: output => Console.WriteLine(output),
    onError: error => Console.Error.WriteLine(error)
);

if (result.ProcessResult)
{
    Console.WriteLine("Script executed successfully");
}

// Execute asynchronously
var asyncResult = await ScriptHelper.ExecuteScriptAsync(
    "node",
    "app.js",
    ScriptExecutionSettings.Default
);
```

---

## ProcessHelper

**Namespace**: `Nextended.Core.Helper`

Provides utility methods for working with system processes, including retrieving process information such as executable paths and command-line arguments.

### Key Methods

| Method | Description |
|--------|-------------|
| `GetProcesses()` | Gets list of all running processes with details |

### Example Usage

```csharp
using Nextended.Core.Helper;

// Get all running processes with full details
var processes = ProcessHelper.GetProcesses();
foreach (var proc in processes)
{
    Console.WriteLine($"Process: {proc.Process.ProcessName}");
    Console.WriteLine($"Path: {proc.Path}");
    Console.WriteLine($"CommandLine: {proc.CommandLine}");
    Console.WriteLine();
}
```

---

## ProcessWatcher

**Namespace**: `Nextended.Core.Helper`

Monitors system processes and raises events when processes are started or stopped.

### Events

| Event | Description |
|-------|-------------|
| `NewProcessesStarted` | Raised when new processes are detected |
| `ProcessesStopped` | Raised when processes terminate |

### Example Usage

```csharp
using Nextended.Core.Helper;

var watcher = new ProcessWatcher();

watcher.NewProcessesStarted += (sender, processes) => {
    foreach (var proc in processes)
    {
        Console.WriteLine($"New process started: {proc.Process.ProcessName}");
    }
};

watcher.ProcessesStopped += (sender, processes) => {
    foreach (var proc in processes)
    {
        Console.WriteLine($"Process stopped: {proc.Process.ProcessName}");
    }
};

watcher.Start();

// ... do work ...

watcher.Stop();
watcher.Dispose();
```

---

## ReflectionHelper

**Namespace**: `Nextended.Core.Helper`

Provides advanced reflection utilities for inspecting and manipulating types, properties, and methods at runtime.

### Key Classes

#### ReflectReadSettings

Configuration for reflection operations:

| Property | Description |
|----------|-------------|
| `BindingFlags` | Flags controlling which members to reflect |
| `TypeMatch` | Type matching strategy (ExactType, IsAssignableTo, etc.) |
| `TraverseHierarchy` | Whether to traverse type hierarchy |
| `MemberDistinct` | Deduplication strategy |
| `MemberMethod` | Which member types to retrieve (fields, properties, or both) |

### Enums

#### MemberMethod
- `GetFields` - Retrieve only fields
- `GetProperty` - Retrieve only properties
- `All` - Retrieve all member types

#### ReflectTypeMatch
- `NoCheck` - No type checking
- `ExactType` - Match exact type only
- `IsAssignableTo` - Match assignable types
- `IsAssignableFrom` - Match types assignable from

---

## TypeExtender

**Namespace**: `Nextended.Core.Helper`

Dynamically creates new types at runtime by adding properties, fields, and attributes to existing types or creating new types from scratch.

### Key Methods

| Method | Description |
|--------|-------------|
| `AddProperty<T>(string name)` | Adds a property to the type |
| `AddField<T>(string name)` | Adds a field to the type |
| `AddAttribute<T>(object[] args)` | Adds an attribute to the type |
| `FetchType()` | Returns the constructed type |

### Example Usage

```csharp
using Nextended.Core.Helper;

// Create a new type dynamically
var extender = new TypeExtender("DynamicPerson");
extender.AddProperty<string>("FirstName");
extender.AddProperty<string>("LastName");
extender.AddProperty<int>("Age");

Type dynamicType = extender.FetchType();
object instance = Activator.CreateInstance(dynamicType);

// Set properties using reflection
dynamicType.GetProperty("FirstName").SetValue(instance, "John");
dynamicType.GetProperty("LastName").SetValue(instance, "Doe");
dynamicType.GetProperty("Age").SetValue(instance, 30);
```

---

## SystemHelper

**Namespace**: `Nextended.Core.Helper`

Provides system-level utility methods for querying hardware and environment information.

### Key Methods

| Method | Description |
|--------|-------------|
| `IsVirtualMachine()` | Detects if running in a virtual machine |

### Example Usage

```csharp
using Nextended.Core.Helper;

if (SystemHelper.IsVirtualMachine())
{
    Console.WriteLine("Running in a virtual machine");
}
else
{
    Console.WriteLine("Running on physical hardware");
}
```

---

## SecurityHelper

**Namespace**: `Nextended.Core.Helper`

Provides security-related utility methods for checking user privileges and permissions.

### Key Methods

| Method | Description |
|--------|-------------|
| `IsCurrentProcessAdmin()` | Checks if process has admin privileges |

### Example Usage

```csharp
using Nextended.Core.Helper;

if (SecurityHelper.IsCurrentProcessAdmin())
{
    Console.WriteLine("Running with administrator privileges");
    // Perform admin tasks
}
else
{
    Console.WriteLine("Running with standard user privileges");
    // Request elevation or show message
}
```

---

## StructuredDataTypeValidator

**Namespace**: `Nextended.Core.Helper`

Provides methods to validate and detect structured data formats (JSON, XML, YAML).

### Key Methods

| Method | Description |
|--------|-------------|
| `DetectInputType(string content)` | Detects data format from content |
| `TryDetectInputType(string content, out type)` | Tries to detect format |
| `IsValidData(string data, StructuredDataType)` | Validates data format |
| `IsValidJson(string data)` | Validates JSON |
| `IsValidXml(string data)` | Validates XML |
| `IsValidYaml(string data)` | Validates YAML |

### Example Usage

```csharp
using Nextended.Core.Helper;

string jsonData = "{\"name\":\"John\",\"age\":30}";
string xmlData = "<person><name>John</name><age>30</age></person>";

// Detect format automatically
var jsonType = StructuredDataTypeValidator.DetectInputType(jsonData);
// Returns StructuredDataType.Json

// Validate specific format
bool isValid = StructuredDataTypeValidator.IsValidJson(jsonData); // true
bool isValidXml = StructuredDataTypeValidator.IsValidXml(jsonData); // false

// Try detect with out parameter
if (StructuredDataTypeValidator.TryDetectInputType(xmlData, out var detectedType))
{
    Console.WriteLine($"Detected format: {detectedType}"); // Xml
}
```

---

## EnvironmentSetScope

**Namespace**: `Nextended.Core.Helper`

Provides a scope for temporarily setting environment variables that are automatically restored when disposed.

### Example Usage

```csharp
using Nextended.Core.Helper;

var varsToSet = new Dictionary<string, string>
{
    { "MY_VAR", "temporary_value" },
    { "ANOTHER_VAR", "temp_value_2" }
};

using (new EnvironmentSetScope(varsToSet))
{
    // Environment variables are set here
    Console.WriteLine(Environment.GetEnvironmentVariable("MY_VAR")); // "temporary_value"
    
    // Do work with temporary environment
}

// Variables are automatically restored to original values
Console.WriteLine(Environment.GetEnvironmentVariable("MY_VAR")); // original value or null
```

---

## ClassMapper

**Namespace**: `Nextended.Core.Helper`

Provides object-to-object mapping functionality. See [Class Mapping Reference](class-mapping.md) for comprehensive documentation.

---

## EnumHelper

**Namespace**: `Nextended.Core.Helper`

Provides utility methods for working with enumerations.

### Key Types

#### Enum<T>

Generic helper class for enum operations:

| Property/Method | Description |
|-----------------|-------------|
| `Values` | Property that gets all enum values |
| `Parse(string name)` | Parses string to enum |
| `TryParse(string name)` | Safe parse with null return |
| `GetName(T value)` | Gets name of enum value |
| `GetDictionary()` | Gets enum as dictionary |
| `GetAttributes<TAttribute>(T value)` | Gets custom attributes |
| `DescriptionFor(T value)` | Gets description attribute value |

### Example Usage

```csharp
using Nextended.Core.Helper;

public enum Status
{
    [Description("Not started")]
    Pending,
    [Description("In progress")]
    Active,
    [Description("Finished")]
    Complete
}

// Get all values
var allStatuses = Enum<Status>.Values;

// Parse from string
var status = Enum<Status>.Parse("Active");

// Get description attribute
string description = Enum<Status>.DescriptionFor(Status.Active); // "In progress"

// Get as dictionary
var dict = Enum<Status>.GetDictionary();
foreach (var kvp in dict)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

---

## See Also

- [Extension Methods Reference](extensions.md)
- [Custom Types Reference](types.md)
- [Class Mapping Reference](class-mapping.md)
- [Nextended.Core Documentation](../projects/core.md)
