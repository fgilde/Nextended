# Nextended.Core

The foundation library providing essential extension methods, custom types, and utilities for .NET development.

## Overview

Nextended.Core is the base library that all other Nextended packages depend on. It provides a comprehensive set of extension methods and custom types designed to enhance productivity and code quality.

## Installation

```bash
dotnet add package Nextended.Core
```

## Key Features

### 1. Extension Methods

Nextended.Core provides extensive extension methods for built-in .NET types:

#### String Extensions
- Case conversions (ToCamelCase, ToPascalCase, ToSnakeCase)
- Validation helpers (IsNullOrEmpty, IsNullOrWhiteSpace)
- String manipulation (Truncate, Remove, Replace patterns)
- Encoding/Decoding utilities

```csharp
using Nextended.Core.Extensions;

string text = "hello world";
string camelCase = text.ToCamelCase();     // "helloWorld"
string pascalCase = text.ToPascalCase();   // "HelloWorld"
string snakeCase = text.ToSnakeCase();     // "hello_world"
```

#### DateTime Extensions
- Date calculations (AddBusinessDays, IsWeekend, IsBusinessDay)
- Date comparisons and ranges
- Formatting helpers
- Time zone conversions

```csharp
DateTime date = DateTime.Now;
DateTime nextBusinessDay = date.AddBusinessDays(5);
bool isWeekend = date.IsWeekend();
```

#### Collection Extensions (Enumerable)
- Advanced LINQ operations
- Batch processing (Batch, Chunk)
- Distinct operations (DistinctBy)
- ForEach and iteration helpers
- Safe operations (SafeAny, SafeFirstOrDefault)

```csharp
var items = new[] { 1, 2, 3, 4, 5 };
var batches = items.Batch(2); // [[1,2], [3,4], [5]]

var users = GetUsers();
users.ForEach(u => Console.WriteLine(u.Name));
```

#### Type Extensions
- Type inspection and reflection
- Generic type operations
- Inheritance checks
- Attribute retrieval

```csharp
Type type = typeof(MyClass);
bool hasAttribute = type.HasAttribute<SerializableAttribute>();
var properties = type.GetPublicProperties();
```

#### Object Extensions
- Deep cloning
- Property access and manipulation
- Conversion utilities
- Null-safe operations
- Object-to-object mapping

```csharp
var original = new MyClass { Name = "Test" };
var clone = original.DeepClone();

object value = myObject.GetPropertyValue("PropertyName");

// Map between different types
var dto = entity.MapTo<EntityDto>();
```

See the [Class Mapping Reference](../api/class-mapping.md) for comprehensive mapping documentation.

#### Task Extensions
- Async/await utilities
- Timeout operations
- Fire-and-forget patterns
- Cancellation helpers

```csharp
await MyAsyncMethod().WithTimeout(TimeSpan.FromSeconds(30));

// Fire and forget
MyAsyncMethod().FireAndForget(ex => Logger.LogError(ex));
```

### 2. Custom Types

#### Money Type
Precise decimal type for financial calculations with currency support.

```csharp
using Nextended.Core.Types;

var price = new Money(99.99m, Currency.USD);
var tax = price * 0.20m;
var total = price + tax;

Console.WriteLine(total); // $119.99
```

#### Date Type
Date-only type without time components (similar to DateOnly in .NET 6+).

```csharp
var today = Date.Today;
var tomorrow = today.AddDays(1);
var lastWeek = today.AddDays(-7);

if (today > lastWeek)
{
    Console.WriteLine("Today is after last week");
}
```

#### BaseId Type
Generic strongly-typed ID wrapper for type-safe identifiers.

```csharp
public class UserId : BaseId<int>
{
    public UserId(int value) : base(value) { }
}

public class User
{
    public UserId Id { get; set; }
    public string Name { get; set; }
}

var userId = new UserId(123);
```

#### SuperType
Advanced entity type with subtype relationships.

```csharp
public class Vehicle : SuperType<VehicleType>
{
    public string Model { get; set; }
}

var car = new Vehicle { Type = VehicleType.Car, Model = "Tesla Model 3" };
```

#### Range Type
Generic range type for intervals and boundaries.

```csharp
var range = new Range<int>(1, 100);
bool isInRange = range.Contains(50); // true

var dateRange = new Range<DateTime>(
    DateTime.Today, 
    DateTime.Today.AddDays(7)
);
```

### 3. Class Mapping

Fast and flexible object mapping without external dependencies.

```csharp
using Nextended.Core.Extensions;

// Simple mapping
var userDto = user.MapTo<UserDto>();

// Advanced mapping with settings
var settings = ClassMappingSettings.Default
    .IgnoreProperties<User>(u => u.Password)
    .IgnoreProperties<UserDto>(dto => dto.CalculatedField);

var result = user.MapTo<UserDto>(settings);

// Map collections
var userDtos = users.MapTo<UserDto[]>();
```

#### Mapping Features
- Automatic property matching by name
- Type conversion
- Nested object mapping
- Collection mapping
- Custom converters
- Property ignoring
- Bi-directional mapping

### 4. Validation and Checking

```csharp
using Nextended.Core;

// Argument validation
Check.NotNull(parameter, nameof(parameter));
Check.NotEmpty(collection, nameof(collection));
Check.Range(value, 1, 100, nameof(value));

// Fluent validation
Check.That(age >= 18, "Must be 18 or older");
```

### 5. Serialization Helpers

```csharp
using Nextended.Core.Extensions;

// JSON serialization
var json = myObject.ToJson();
var obj = json.FromJson<MyClass>();

// XML serialization
var xml = myObject.ToXml();
var obj = xml.FromXml<MyClass>();

// YAML serialization
var yaml = myObject.ToYaml();
```

### 6. Hashing and Encryption

```csharp
using Nextended.Core.Hashing;
using Nextended.Core.Encryption;

// Hashing
string hash = HashHelper.ComputeSha256Hash(text);

// Encryption/Decryption
string encrypted = EncryptionHelper.Encrypt(plainText, key);
string decrypted = EncryptionHelper.Decrypt(encrypted, key);
```

### 7. Exposed Object Pattern

Dynamic property access without reflection overhead.

```csharp
using Nextended.Core;

dynamic exposed = new ExposedObject(myObject);
exposed.PrivateField = "New Value";
var value = exposed.PrivateMethod("arg");
```

### 8. Notification Objects

INotifyPropertyChanged implementation helpers.

```csharp
using Nextended.Core;

public class ViewModel : NotificationObject
{
    private string _name;
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
```

## Supported Frameworks

- .NET Standard 2.0
- .NET Standard 2.1
- .NET 8.0
- .NET 9.0

## Dependencies

- `Newtonsoft.Json` - JSON serialization
- `YamlDotNet` - YAML serialization
- `System.Linq.Dynamic.Core` - Dynamic LINQ
- `StringToExpression` - Expression parsing
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI support

## Examples

### Complete Example: User Management

```csharp
using Nextended.Core.Extensions;
using Nextended.Core.Types;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Date BirthDate { get; set; }
    public Money Salary { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public int Age { get; set; }
}

// Create user
var user = new User
{
    Id = 1,
    FirstName = "John",
    LastName = "Doe",
    BirthDate = new Date(1990, 1, 1),
    Salary = new Money(50000m, Currency.USD)
};

// Map to DTO with custom logic
var settings = ClassMappingSettings.Default
    .WithMapping<User, UserDto>((src, dst) => 
    {
        dst.FullName = $"{src.FirstName} {src.LastName}";
        dst.Age = (Date.Today - src.BirthDate).Days / 365;
    });

var dto = user.MapTo<UserDto>(settings);

// Serialize
var json = dto.ToJson();
Console.WriteLine(json);
```

## API Reference

For a complete API reference, see:
- [Extension Methods Reference](../api/extensions.md)
- [Custom Types Reference](../api/types.md)
- [Class Mapping Reference](../api/class-mapping.md)
- [Helper Utilities Reference](../api/helpers.md)
- [Encryption & Security Reference](../api/encryption.md)

## Related Projects

- [Nextended.Cache](cache.md) - Builds on Core for caching
- [Nextended.EF](ef.md) - EF extensions using Core utilities
- All other Nextended projects depend on Core

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Core/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Core)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
