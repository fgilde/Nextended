# Extension Methods Reference

This page provides an overview of the extension methods available in Nextended.Core.

## String Extensions

Located in `Nextended.Core.Extensions.StringExtensions`

### Case Conversions

| Method | Description | Example |
|--------|-------------|---------|
| `ToCamelCase()` | Converts to camelCase | `"hello world"` → `"helloWorld"` |
| `ToPascalCase()` | Converts to PascalCase | `"hello world"` → `"HelloWorld"` |
| `ToSnakeCase()` | Converts to snake_case | `"HelloWorld"` → `"hello_world"` |
| `ToKebabCase()` | Converts to kebab-case | `"HelloWorld"` → `"hello-world"` |

### Validation

| Method | Description |
|--------|-------------|
| `IsNullOrEmpty()` | Checks if string is null or empty |
| `IsNullOrWhiteSpace()` | Checks if string is null, empty, or whitespace |
| `IsValidEmail()` | Validates email format |
| `IsValidUrl()` | Validates URL format |

### Manipulation

| Method | Description |
|--------|-------------|
| `Truncate(int length)` | Truncates string to specified length |
| `RemoveWhitespace()` | Removes all whitespace characters |
| `Reverse()` | Reverses the string |
| `Left(int length)` | Returns leftmost characters |
| `Right(int length)` | Returns rightmost characters |

## DateTime Extensions

Located in `Nextended.Core.Extensions.DateTimeExtensions`

### Business Days

| Method | Description |
|--------|-------------|
| `AddBusinessDays(int days)` | Adds business days (skips weekends) |
| `IsBusinessDay()` | Checks if date is a business day |
| `IsWeekend()` | Checks if date is weekend |
| `NextBusinessDay()` | Gets next business day |

### Date Operations

| Method | Description |
|--------|-------------|
| `StartOfDay()` | Returns start of day (00:00:00) |
| `EndOfDay()` | Returns end of day (23:59:59) |
| `StartOfWeek()` | Returns start of week |
| `EndOfWeek()` | Returns end of week |
| `StartOfMonth()` | Returns start of month |
| `EndOfMonth()` | Returns end of month |

## Collection Extensions (IEnumerable<T>)

Located in `Nextended.Core.Extensions.EnumerableExtensions`

### LINQ Enhancements

| Method | Description |
|--------|-------------|
| `Batch(int size)` | Splits collection into batches |
| `DistinctBy<TKey>(Func<T, TKey> keySelector)` | Distinct by property |
| `ForEach(Action<T> action)` | Performs action on each element |
| `ForEach(Action<T, int> action)` | ForEach with index |

### Safe Operations

| Method | Description |
|--------|-------------|
| `SafeAny()` | Safe Any() that handles null collections |
| `SafeCount()` | Safe Count() that handles null collections |
| `SafeFirstOrDefault()` | Safe FirstOrDefault() that handles null |

### Utility Methods

| Method | Description |
|--------|-------------|
| `IsNullOrEmpty()` | Checks if collection is null or empty |
| `None()` | Opposite of Any() |
| `WhereNotNull()` | Filters out null elements |

## Object Extensions

Located in `Nextended.Core.Extensions.ObjectExtensions`

### Mapping

| Method | Description |
|--------|-------------|
| `MapTo<TTarget>()` | Maps object to target type |
| `MapTo<TTarget>(ClassMappingSettings)` | Maps with custom settings |
| `MapTo<TTarget>(TTarget target)` | Maps to existing instance |

### Cloning

| Method | Description |
|--------|-------------|
| `DeepClone()` | Creates deep copy of object |
| `ShallowClone()` | Creates shallow copy of object |

### Reflection

| Method | Description |
|--------|-------------|
| `GetPropertyValue(string name)` | Gets property value by name |
| `SetPropertyValue(string name, object value)` | Sets property value by name |
| `HasProperty(string name)` | Checks if property exists |

## Type Extensions

Located in `Nextended.Core.Extensions.TypeExtensions`

### Type Inspection

| Method | Description |
|--------|-------------|
| `IsNumeric()` | Checks if type is numeric |
| `IsNullable()` | Checks if type is nullable |
| `GetUnderlyingType()` | Gets underlying type of nullable |
| `HasAttribute<T>()` | Checks for attribute |
| `GetAttribute<T>()` | Gets attribute instance |

### Property Operations

| Method | Description |
|--------|-------------|
| `GetPublicProperties()` | Gets all public properties |
| `GetProperty(string name)` | Gets property by name |
| `CreateInstance()` | Creates new instance of type |

## Task Extensions

Located in `Nextended.Core.Extensions.TaskExtensions`

### Async Utilities

| Method | Description |
|--------|-------------|
| `WithTimeout(TimeSpan)` | Adds timeout to async operation |
| `FireAndForget()` | Executes task without waiting |
| `FireAndForget(Action<Exception>)` | Fire and forget with error handler |
| `WaitSafely()` | Safe synchronous wait |

## Serialization Extensions

Located in `Nextended.Core.Extensions.SerializationHelper`

### JSON

| Method | Description |
|--------|-------------|
| `ToJson()` | Serializes to JSON |
| `ToJson(Formatting)` | Serializes with formatting |
| `FromJson<T>()` | Deserializes from JSON |

### XML

| Method | Description |
|--------|-------------|
| `ToXml()` | Serializes to XML |
| `FromXml<T>()` | Deserializes from XML |

### YAML

| Method | Description |
|--------|-------------|
| `ToYaml()` | Serializes to YAML |
| `FromYaml<T>()` | Deserializes from YAML |

## Usage Examples

### String Extensions

```csharp
using Nextended.Core.Extensions;

string text = "hello world";
string camelCase = text.ToCamelCase();     // "helloWorld"
string pascalCase = text.ToPascalCase();   // "HelloWorld"
bool isEmail = "test@example.com".IsValidEmail(); // true
```

### Collection Extensions

```csharp
using Nextended.Core.Extensions;

var numbers = new[] { 1, 2, 3, 4, 5 };

// Batch processing
foreach (var batch in numbers.Batch(2))
{
    // Process batch
}

// Safe operations
List<int> nullList = null;
bool hasItems = nullList.SafeAny(); // false, no exception
```

### Object Mapping

```csharp
using Nextended.Core.Extensions;

var source = new SourceClass { Name = "John" };
var target = source.MapTo<TargetClass>();

// With settings
var settings = ClassMappingSettings.Default
    .IgnoreProperties<SourceClass>(s => s.InternalField);
var result = source.MapTo<TargetClass>(settings);
```

### Task Extensions

```csharp
using Nextended.Core.Extensions;

// Add timeout
var result = await LongRunningOperationAsync()
    .WithTimeout(TimeSpan.FromSeconds(30));

// Fire and forget
SendEmailAsync().FireAndForget(
    ex => Logger.LogError(ex, "Email failed")
);
```

## See Also

- [Custom Types Reference](types.md)
- [Common Use Cases](../examples/common-use-cases.md)
- [Project Documentation](../projects/core.md)
