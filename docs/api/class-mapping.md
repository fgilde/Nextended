# Class Mapping

Nextended.Core provides a powerful object-to-object mapping system that allows you to convert between different types without external dependencies like AutoMapper. The class mapping feature is lightweight, flexible, and supports a wide range of conversion scenarios.

## Overview

The class mapping system consists of three main components:

- **ClassMapper**: The core class that performs the actual mapping operations
- **ClassMappingSettings**: Configuration class that controls mapping behavior
- **ClassMappingExtensions**: Extension methods that provide a fluent API for mapping

## Quick Start

### Basic Mapping

The simplest way to map objects is using the `MapTo<T>()` extension method:

```csharp
using Nextended.Core.Extensions;

public class Source
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Destination
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Map from source to destination
var source = new Source { Name = "John", Age = 30 };
var destination = source.MapTo<Destination>();

// Result: destination.Name = "John", destination.Age = 30
```

### Type Conversion

The mapper automatically handles type conversions:

```csharp
public class Source
{
    public int Amount { get; set; }
    public DateTime Date { get; set; }
}

public class Destination
{
    public string Amount { get; set; }  // int → string
    public DateOnly Date { get; set; }   // DateTime → DateOnly
}

var source = new Source { Amount = 100, Date = DateTime.Now };
var destination = source.MapTo<Destination>();

// Automatic conversions:
// - int to string: "100"
// - DateTime to DateOnly
```

## ClassMapper

The `ClassMapper` class is the core component that performs object mapping.

### Creating a Mapper Instance

```csharp
// Create with default settings
var mapper = new ClassMapper();

// Create with custom settings
var settings = new ClassMappingSettings();
var mapper = new ClassMapper(settings);

// Set or update settings
mapper.SetSettings(settings);
mapper.SetSettings(s => s.IgnoreExceptions = true);
```

### Mapping Methods

#### Map to Type

```csharp
// Map to specific type
object result = mapper.Map<Source>(source, typeof(Destination));

// Generic version
Destination result = mapper.Map<Source, Destination>(source);
```

#### Map with Custom Assignments

```csharp
var result = mapper.Map<Source, Destination>(
    source,
    (dest, src) => dest.FullName = src.FirstName + " " + src.LastName,
    (dest, src) => dest.Age = src.YearOfBirth != 0 ? DateTime.Now.Year - src.YearOfBirth : 0
);
```

#### Async Mapping

```csharp
// Async mapping for large objects or collections
var result = await mapper.MapAsync<Source, Destination>(source);
```

### Default Mapper Instance

You can set a default mapper instance that will be used by all extension methods:

```csharp
var mapper = new ClassMapper()
    .SetSettings(s => s.IgnoreExceptions = true)
    .SetAsDefaultMapper();

// Now all MapTo calls will use this instance
var result = source.MapTo<Destination>();
```

## ClassMappingSettings

`ClassMappingSettings` controls all aspects of the mapping behavior.

### Creating Settings

```csharp
// Default settings
var settings = ClassMappingSettings.Default;

// Fast settings (optimized for performance)
var fastSettings = ClassMappingSettings.Fast;

// Custom settings
var settings = new ClassMappingSettings()
    .Set(s => s.IgnoreExceptions = true)
    .Set(s => s.IncludePrivateFields = false);
```

### Configuration Options

#### Exception Handling

```csharp
var settings = new ClassMappingSettings();
settings.IgnoreExceptions = true; // Continue mapping even if errors occur
```

#### Private Members

```csharp
settings.IncludePrivateFields = true; // Map private fields (default: false)
```

#### Abstract Members

```csharp
settings.CoverUpAbstractMembers = true; // Create implementations for abstract types
```

#### Async Operations

```csharp
settings.ShouldEnumeratePropertiesAsync = true; // Map properties in parallel
settings.ShouldEnumerateListsAsync = true;      // Process large lists in parallel
settings.MinListCountToEnumerateAsync = 100;    // Minimum items for async processing
```

#### Value Type Handling

```csharp
// Convert default value types (0, false) to null for reference types
settings.DefaultValueTypeValuesAsNullForNonValueTypes = true;
```

#### Type Conversion Options

```csharp
settings.AllowGuidConversion = true;              // Allow int/long ↔ Guid conversion
settings.MatchCaseForEnumNameConversion = false;  // Case-insensitive enum conversion
settings.SearchForTryParseInTargetTypes = true;   // Use TryParse methods when available
```

#### JSON Serialization

```csharp
settings.ObjectToStringWithJSON = true;           // Serialize objects to JSON strings
settings.CanConvertFromJSON = true;               // Deserialize JSON strings to objects
settings.AutoCheckForDataContractJsonSerializer = true; // Use DataContract when applicable
```

#### Dependency Injection

```csharp
settings.TryContainerResolve = true;  // Try to resolve types using DI
settings.ServiceProvider = serviceProvider; // Set the service provider
settings.CheckCyclicDependencies = true; // Check for circular references
```

### Ignoring Properties

```csharp
// Ignore specific properties
settings.IgnoreProperties<Source>(
    s => s.Password,
    s => s.InternalId
);

// Ignore by MemberInfo
settings.IgnoreProperties(typeof(Source).GetProperty("Password"));
```

### Property Assignment (Different Names)

When source and destination have properties with different names:

```csharp
// Map FirstName to Name
settings.AddAssignment<Source, Destination>(
    src => src.FirstName,
    dest => dest.Name
);

// Fluent syntax
var settings = ClassMappingSettings.Default
    .Assign<Source>(s => s.FirstName)
    .To<Destination>(d => d.Name)
    .Settings();
```

### Type Converters

#### Add Custom Converter

```csharp
// Add converter with function
settings.AddConverter<string, int>(s => int.Parse(s));

// Add converter for assignable types
settings.AddConverter<BaseClass, DerivedClass>(allowAssignableInputs: true);

// Add type converter instance
settings.AddConverter(new MyCustomTypeConverter());
```

#### Global Converters

Global converters apply to all mappings:

```csharp
// Add global converter
ClassMappingSettings.AddGlobalConverter<DateTime, string>(
    dt => dt.ToString("yyyy-MM-dd")
);

// Remove global converter
ClassMappingSettings.RemoveGlobalConverter(converter);

// Clear all global converters
ClassMappingSettings.ClearGlobalConverters();
```

#### Built-in Converters

The following conversions are available by default:

- DateTime ↔ DateOnly
- DateTime ↔ TimeOnly
- TimeOnly ↔ TimeSpan
- Numeric types (int, long, double, etc.)
- String ↔ Enum
- String ↔ Guid
- Collections and arrays
- Nullable types

### Set as Default

Make settings the default for all future mappings:

```csharp
var settings = new ClassMappingSettings()
    .Set(s => s.IgnoreExceptions = true)
    .SetAsDefault();
```

## ClassMappingExtensions

Extension methods provide a fluent API for mapping operations.

### MapTo Methods

```csharp
// Simple mapping
var dest = source.MapTo<Destination>();

// Mapping with settings
var dest = source.MapTo<Destination>(ClassMappingSettings.Fast);

// Mapping with custom assignments
var dest = source.MapTo<Source, Destination>(
    (d, s) => d.FullName = $"{s.FirstName} {s.LastName}"
);

// Mapping with settings and assignments
var dest = source.MapTo<Source, Destination>(
    ClassMappingSettings.Default,
    (d, s) => d.FullName = $"{s.FirstName} {s.LastName}"
);
```

### MapToAsync Methods

```csharp
// Async mapping
var dest = await source.MapToAsync<Destination>();

// Async with settings
var dest = await source.MapToAsync<Destination>(ClassMappingSettings.Fast);

// Async with assignments
var dest = await source.MapToAsync<Source, Destination>(
    (d, s) => d.Calculated = s.Value * 2
);
```

### Mapping Collections

```csharp
IEnumerable<Source> sources = GetSources();

// Map each element
IEnumerable<Destination> destinations = sources.MapElementsTo<Destination>();

// Map with custom settings
var destinations = sources.MapElementsTo<Destination>(
    ClassMappingSettings.Default.Set(s => s.IgnoreExceptions = true)
);
```

### Dynamic Type Mapping

```csharp
Type targetType = typeof(Destination);

// Map to runtime type
object result = source.MapTo(targetType);

// Async version
object result = await source.MapToAsync(targetType);
```

## Common Scenarios

### Scenario 1: Simple DTO Mapping

```csharp
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    // No PasswordHash for security
}

// Map entity to DTO (only matching properties are mapped)
var entity = GetUserEntity();
var dto = entity.MapTo<UserDto>();
```

### Scenario 2: Type Conversions

```csharp
public class OrderData
{
    public string OrderId { get; set; }
    public string Amount { get; set; }
    public string Date { get; set; }
}

public class Order
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

var data = new OrderData 
{ 
    OrderId = "123e4567-e89b-12d3-a456-426614174000",
    Amount = "199.99",
    Date = "2024-01-15"
};

var order = data.MapTo<Order>();
// Automatic conversions: string → Guid, string → decimal, string → DateTime
```

### Scenario 3: Property Name Mismatch

```csharp
public class ApiResponse
{
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email_address { get; set; }
}

public class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

var settings = ClassMappingSettings.Default
    .AddAssignment<ApiResponse, User>(
        api => api.first_name,
        user => user.FirstName)
    .AddAssignment<ApiResponse, User>(
        api => api.last_name,
        user => user.LastName)
    .AddAssignment<ApiResponse, User>(
        api => api.email_address,
        user => user.Email);

var response = GetApiResponse();
var user = response.MapTo<User>(settings);
```

### Scenario 4: Custom Calculations

```csharp
public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int YearOfBirth { get; set; }
    public decimal HourlyRate { get; set; }
}

public class EmployeeReport
{
    public string FullName { get; set; }
    public int Age { get; set; }
    public decimal AnnualSalary { get; set; }
}

var employee = GetEmployee();
var report = employee.MapTo<Employee, EmployeeReport>(
    (rpt, emp) => rpt.FullName = $"{emp.FirstName} {emp.LastName}",
    (rpt, emp) => rpt.Age = DateTime.Now.Year - emp.YearOfBirth,
    (rpt, emp) => rpt.AnnualSalary = emp.HourlyRate * 40 * 52
);
```

### Scenario 5: Nested Objects

```csharp
public class Person
{
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class PersonDto
{
    public string Name { get; set; }
    public AddressDto Address { get; set; }
}

public class AddressDto
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

// Nested objects are automatically mapped recursively
var person = GetPerson();
var personDto = person.MapTo<PersonDto>();
// Both Person and Address are mapped to their DTO counterparts
```

### Scenario 6: Collection Mapping

```csharp
public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

List<OrderItem> items = GetOrderItems();

// Map collection with custom calculations
var itemDtos = items.Select(item => 
    item.MapTo<OrderItem, OrderItemDto>(
        (dto, src) => dto.Total = src.Quantity * src.Price
    )).ToList();

// Or use MapElementsTo for simple mappings
var itemDtos = items.MapElementsTo<OrderItemDto>();
```

### Scenario 7: Dictionary to Object

```csharp
var dictionary = new Dictionary<string, object>
{
    ["Name"] = "John Doe",
    ["Age"] = 30,
    ["Email"] = "john@example.com"
};

// Map dictionary to object
var person = dictionary.MapTo<Person>();
// Properties are set based on dictionary keys
```

### Scenario 8: JSON String Conversion

```csharp
var settings = new ClassMappingSettings
{
    CanConvertFromJSON = true,
    ObjectToStringWithJSON = true
};

// Object to JSON string
var person = new Person { Name = "John", Age = 30 };
string json = person.MapTo<string>(settings);

// JSON string to object
var personCopy = json.MapTo<Person>(settings);
```

### Scenario 9: Money Type Conversion

```csharp
public class OrderData
{
    public string Name { get; set; }
    public string Amount { get; set; }
}

public class Order
{
    public string Name { get; set; }
    public Money Amount { get; set; }
}

var data = new OrderData { Name = "Product", Amount = "123.45" };
var order = data.MapTo<Order>();
// Amount "123.45" is automatically converted to Money type
```

### Scenario 10: Async Mapping for Large Collections

```csharp
var settings = new ClassMappingSettings
{
    ShouldEnumerateListsAsync = true,
    MinListCountToEnumerateAsync = 100
};

List<LargeObject> largeList = GetLargeList(); // e.g., 10,000 items

// Async mapping for better performance
var results = await largeList
    .Select(item => item.MapToAsync<LargeObjectDto>(settings))
    .ToListAsync();
```

## Advanced Features

### Abstract Type Coverage

When mapping to abstract types or interfaces:

```csharp
var settings = new ClassMappingSettings
{
    CoverUpAbstractMembers = true
};

// Create concrete implementation of interface
IUser user = source.MapTo<IUser>(settings);
```

### Private Field Mapping

```csharp
var settings = new ClassMappingSettings
{
    IncludePrivateFields = true
};

// Map private fields (useful for testing or special scenarios)
var result = source.MapTo<Destination>(settings);
```

### BaseId Type Support

Nextended has special support for BaseId types (strongly-typed IDs):

```csharp
public class UserId : BaseId<Guid, UserId> { }

public class UserData
{
    public Guid Id { get; set; }
}

public class User
{
    public UserId Id { get; set; }
}

var data = new UserData { Id = Guid.NewGuid() };
var user = data.MapTo<User>();
// Guid is automatically wrapped in UserId
```

### Enum Conversion with XmlEnum

```csharp
public enum Status
{
    [XmlEnum("active")]
    Active,
    
    [XmlEnum("inactive")]
    Inactive
}

// String to enum using XmlEnum attribute
var status = "active".MapTo<Status>();
// Result: Status.Active

// Enum to string using XmlEnum attribute
var statusString = Status.Active.MapTo<string>();
// Result: "active"
```

## Performance Considerations

### Fast Settings

For performance-critical scenarios, use `ClassMappingSettings.Fast`:

```csharp
var settings = ClassMappingSettings.Fast;
// This configuration:
// - Ignores exceptions
// - Skips DataContract checks
// - Enables async property enumeration
// - Disables container resolution
// - Disables TryParse search
```

### Async Processing

Enable async processing for large objects or collections:

```csharp
var settings = new ClassMappingSettings
{
    ShouldEnumeratePropertiesAsync = true,  // Parallel property mapping
    ShouldEnumerateListsAsync = true,        // Parallel list processing
    MinListCountToEnumerateAsync = 100       // Threshold for async
};
```

### Reuse Settings and Mapper

Create and reuse settings to avoid repeated configuration:

```csharp
// Create once
var settings = new ClassMappingSettings()
    .Set(s => s.IgnoreExceptions = true)
    .AddConverter<string, DateTime>(DateTime.Parse)
    .SetAsDefault();

// Reuse many times
var result1 = source1.MapTo<Destination>();
var result2 = source2.MapTo<Destination>();
```

## Testing Support

The class mapping system is extensively tested. See `Nextended.Core.Tests/ClassMappingTests.cs` for comprehensive examples including:

- Basic type conversions
- DateOnly and TimeOnly mapping
- Interface implementation
- Enum conversions
- Collection mapping
- Dictionary to object mapping
- Nested object mapping
- Private field mapping
- Custom converter usage
- Async operations

## Best Practices

1. **Use Default Settings for Most Cases**: The default settings work well for most scenarios.

2. **Create Custom Settings for Special Cases**: When you need specific behavior, create dedicated settings:
   ```csharp
   var apiSettings = new ClassMappingSettings()
       .Set(s => s.IgnoreExceptions = true)
       .AddConverter<string, DateTime>(DateTime.Parse);
   ```

3. **Ignore Sensitive Properties**: Explicitly ignore properties that shouldn't be mapped:
   ```csharp
   settings.IgnoreProperties<User>(u => u.Password, u => u.PasswordHash);
   ```

4. **Use Type Converters for Complex Conversions**: Instead of custom assignments, use type converters:
   ```csharp
   settings.AddConverter<string, ComplexType>(str => ComplexType.Parse(str));
   ```

5. **Test Your Mappings**: Always test mappings, especially with edge cases and null values.

6. **Consider Performance**: For large objects or collections, use async mapping and fast settings.

7. **Document Custom Converters**: If you add custom converters, document their behavior.

## Common Pitfalls

1. **Circular References**: Be careful with circular references. Enable `CheckCyclicDependencies` if needed:
   ```csharp
   settings.CheckCyclicDependencies = true;
   ```

2. **Property Name Mismatches**: Remember that properties must have the same name. Use `AddAssignment` for different names.

3. **Type Compatibility**: Not all types can be converted. Add custom converters for special cases.

4. **Null Handling**: The mapper generally preserves null values. Use settings to control this behavior:
   ```csharp
   settings.DefaultValueTypeValuesAsNullForNonValueTypes = true;
   ```

5. **Exception Handling**: By default, exceptions are thrown. Set `IgnoreExceptions = true` for lenient mapping.

## See Also

- [Extension Methods Reference](extensions.md) - Complete list of extension methods including MapTo
- [Custom Types](types.md) - Custom types like Money, Date, and BaseId that have special mapping support
- [Core Documentation](../projects/core.md) - Main Nextended.Core documentation
