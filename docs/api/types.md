# Custom Types Reference

This page documents the custom types provided by Nextended.Core.

## Money

**Namespace**: `Nextended.Core.Types`

A precise decimal type specifically designed for financial calculations with currency support.

### Constructor

```csharp
public Money(decimal amount, Currency currency)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Amount` | `decimal` | The monetary amount |
| `Currency` | `Currency` | The currency type |

### Methods

| Method | Description |
|--------|-------------|
| `ConvertTo(Currency, decimal rate)` | Converts to another currency |
| `ToString()` | Formats as currency string |

### Operators

| Operator | Description |
|----------|-------------|
| `+` | Addition |
| `-` | Subtraction |
| `*` | Multiplication |
| `/` | Division |
| `==`, `!=` | Equality comparison |
| `<`, `>`, `<=`, `>=` | Relational comparison |

### Usage Example

```csharp
using Nextended.Core.Types;

// Create money instances
var price = new Money(99.99m, Currency.USD);
var tax = price * 0.20m;              // Calculate 20% tax
var total = price + tax;              // $119.99

// Currency conversion
var priceInEur = price.ConvertTo(Currency.EUR, 0.85m);

// Comparisons
if (total > new Money(100m, Currency.USD))
{
    Console.WriteLine("Total exceeds $100");
}

// Formatting
Console.WriteLine(total.ToString()); // "$119.99"
```

---

## Date

**Namespace**: `Nextended.Core.Types`

A date-only type without time components (similar to `DateOnly` in .NET 6+).

### Constructor

```csharp
public Date(int year, int month, int day)
```

### Static Properties

| Property | Type | Description |
|----------|------|-------------|
| `Today` | `Date` | Gets current date |
| `MinValue` | `Date` | Minimum date value |
| `MaxValue` | `Date` | Maximum date value |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Year` | `int` | Year component |
| `Month` | `int` | Month component |
| `Day` | `int` | Day component |
| `DayOfWeek` | `DayOfWeek` | Day of the week |

### Methods

| Method | Description |
|--------|-------------|
| `AddDays(int)` | Adds specified days |
| `AddMonths(int)` | Adds specified months |
| `AddYears(int)` | Adds specified years |
| `ToDateTime()` | Converts to DateTime |

### Operators

| Operator | Description |
|----------|-------------|
| `==`, `!=` | Equality comparison |
| `<`, `>`, `<=`, `>=` | Relational comparison |
| `-` | Subtracts dates (returns TimeSpan) |

### Usage Example

```csharp
using Nextended.Core.Types;

// Create dates
var today = Date.Today;
var birthday = new Date(1990, 1, 15);
var tomorrow = today.AddDays(1);

// Date arithmetic
var nextWeek = today.AddDays(7);
var nextYear = today.AddYears(1);

// Comparisons
if (today > birthday)
{
    Console.WriteLine("Birthday was in the past");
}

// Calculate age
var age = (today - birthday).Days / 365;
Console.WriteLine($"Age: {age} years");
```

---

## BaseId<T>

**Namespace**: `Nextended.Core.Types`

A generic strongly-typed ID wrapper that provides type safety for identifiers.

### Constructor

```csharp
public BaseId(T value)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | The underlying value |

### Usage Example

```csharp
using Nextended.Core.Types;

// Define typed IDs
public class UserId : BaseId<int>
{
    public UserId(int value) : base(value) { }
}

public class OrderId : BaseId<Guid>
{
    public OrderId(Guid value) : base(value) { }
}

// Use in domain models
public class User
{
    public UserId Id { get; set; }
    public string Name { get; set; }
}

public class Order
{
    public OrderId Id { get; set; }
    public UserId UserId { get; set; }  // Type-safe foreign key
}

// Usage
var userId = new UserId(123);
var orderId = new OrderId(Guid.NewGuid());

// Compile-time type safety
var user = new User { Id = userId };
// user.Id = orderId; // Compile error - type mismatch!
```

---

## SuperType<TType>

**Namespace**: `Nextended.Core.Types`

An advanced entity type that has a relationship with one or more subtypes, useful for polymorphic domain models.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `TType` | The type discriminator |

### Usage Example

```csharp
using Nextended.Core.Types;

// Define type enum
public enum VehicleType
{
    Car,
    Truck,
    Motorcycle
}

// Define base type
public class Vehicle : SuperType<VehicleType>
{
    public string Model { get; set; }
    public string Color { get; set; }
}

// Usage
var car = new Vehicle 
{ 
    Type = VehicleType.Car,
    Model = "Tesla Model 3",
    Color = "Red"
};

var truck = new Vehicle
{
    Type = VehicleType.Truck,
    Model = "Ford F-150",
    Color = "Blue"
};

// Query by type
var vehicles = GetVehicles();
var cars = vehicles.Where(v => v.Type == VehicleType.Car);
```

---

## Range<T>

**Namespace**: `Nextended.Core.Types`

A generic range type for representing intervals and boundaries.

### Constructor

```csharp
public Range(T start, T end)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Start` | `T` | Range start value |
| `End` | `T` | Range end value |

### Methods

| Method | Description |
|--------|-------------|
| `Contains(T value)` | Checks if value is within range |
| `Overlaps(Range<T> other)` | Checks if ranges overlap |
| `IsValid()` | Validates range (start <= end) |

### Usage Example

```csharp
using Nextended.Core.Types;

// Numeric range
var ageRange = new Range<int>(18, 65);
bool isAdult = ageRange.Contains(25); // true

// Date range
var dateRange = new Range<DateTime>(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 12, 31)
);
bool isInYear = dateRange.Contains(DateTime.Now);

// Check overlaps
var q1 = new Range<DateTime>(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 3, 31)
);
var q2 = new Range<DateTime>(
    new DateTime(2024, 3, 1),
    new DateTime(2024, 5, 31)
);
bool overlaps = q1.Overlaps(q2); // true (March overlap)

// Price range
var priceRange = new Range<decimal>(10.00m, 100.00m);
var products = GetProducts()
    .Where(p => priceRange.Contains(p.Price));
```

---

## Currency

**Namespace**: `Nextended.Core.Types`

An enumeration of supported currencies for use with the Money type.

### Values

```csharp
public enum Currency
{
    USD,    // US Dollar
    EUR,    // Euro
    GBP,    // British Pound
    JPY,    // Japanese Yen
    CHF,    // Swiss Franc
    CAD,    // Canadian Dollar
    AUD,    // Australian Dollar
    // ... and more
}
```

### Usage Example

```csharp
using Nextended.Core.Types;

var usdPrice = new Money(100m, Currency.USD);
var eurPrice = new Money(85m, Currency.EUR);
var gbpPrice = new Money(75m, Currency.GBP);
```

---

## SimpleRange<T>

**Namespace**: `Nextended.Core.Types`

A simplified version of Range<T> for common scenarios.

Similar to `Range<T>` but with simplified API for common use cases.

---

## Hierarchical<T>

**Namespace**: `Nextended.Core.Types`

Represents hierarchical data structures with parent-child relationships.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Parent` | `Hierarchical<T>` | Parent node |
| `Children` | `List<Hierarchical<T>>` | Child nodes |
| `Value` | `T` | Node value |

### Usage Example

```csharp
using Nextended.Core.Types;

public class Category : Hierarchical<Category>
{
    public string Name { get; set; }
}

// Build hierarchy
var electronics = new Category { Name = "Electronics" };
var computers = new Category 
{ 
    Name = "Computers",
    Parent = electronics 
};
electronics.Children.Add(computers);

var laptops = new Category 
{ 
    Name = "Laptops",
    Parent = computers 
};
computers.Children.Add(laptops);

// Navigate hierarchy
var topLevel = laptops.Parent.Parent; // electronics
```

---

## Best Practices

### Money Type

1. **Always specify currency**:
   ```csharp
   var price = new Money(99.99m, Currency.USD); // Good
   ```

2. **Use for all financial calculations**:
   ```csharp
   Money total = items.Sum(i => i.Price); // Safe
   decimal total = items.Sum(i => i.Price.Amount); // Loses currency info
   ```

### Date Type

1. **Use for date-only operations**:
   ```csharp
   Date birthDate = new Date(1990, 1, 1); // Good
   DateTime birthDate = new DateTime(1990, 1, 1); // Has unnecessary time
   ```

2. **Compare dates without time concerns**:
   ```csharp
   if (Date.Today > startDate) // Clean comparison
   ```

### BaseId<T>

1. **Create specific ID types for each entity**:
   ```csharp
   public class UserId : BaseId<int> { }
   public class OrderId : BaseId<int> { }
   // Now UserId and OrderId are not interchangeable
   ```

### Range<T>

1. **Validate ranges before use**:
   ```csharp
   var range = new Range<int>(start, end);
   if (!range.IsValid())
       throw new ArgumentException("Invalid range");
   ```

---

## SmallProcessInfo

**Namespace**: `Nextended.Core.Types`

Represents basic information about a running process, including its ID, executable path, and command-line arguments.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Process ID |
| `Process` | `Process` | The Process object |
| `CommandLine` | `string` | Command-line arguments |
| `Path` | `string` | Full path to executable |
| `FileName` | `string` | File name without path |

### Usage Example

```csharp
using Nextended.Core.Helper;
using Nextended.Core.Types;

// Get all running processes with details
var processes = ProcessHelper.GetProcesses();
foreach (SmallProcessInfo proc in processes)
{
    Console.WriteLine($"Process: {proc.FileName}");
    Console.WriteLine($"  ID: {proc.Id}");
    Console.WriteLine($"  Path: {proc.Path}");
    Console.WriteLine($"  Command: {proc.CommandLine}");
}
```

---

## SymbolLinkInfo

**Namespace**: `Nextended.Core.Types`

Represents information about a symbolic link, including the link path and its target.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `LinkName` | `string` | Path to the symbolic link |
| `Target` | `string` | Target path the link points to |

### Usage Example

```csharp
using Nextended.Core.Types;
using Nextended.Core.Helper;

// Create a symbolic link
var linkInfo = new SymbolLinkInfo(@"C:\MyLink", @"C:\Target\Folder");
FileHelper.CreateSymbolicLink(linkInfo);

Console.WriteLine($"Created link: {linkInfo.LinkName} -> {linkInfo.Target}");

// Remove symbolic link
FileHelper.RemoveSymbolicLink(linkInfo.LinkName);
```

---

## DataUrl

**Namespace**: `Nextended.Core.Types`

Represents a data URL (RFC 2397) that encodes binary data in a base64 string with an optional MIME type.

### Constructor

```csharp
public DataUrl(byte[] bytes, string mimeType = null)
public DataUrl(string url)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Bytes` | `byte[]` | Binary data encoded in the URL |
| `MimeType` | `string` | MIME type of the data |

### Methods

| Method | Description |
|--------|-------------|
| `ToString()` | Returns the data URL string |
| `Parse(string url)` | Parses a data URL string |
| `TryParse(string url, out DataUrl)` | Tries to parse a data URL |

### Usage Example

```csharp
using Nextended.Core.Types;

// Create from binary data
byte[] imageData = File.ReadAllBytes("photo.jpg");
var dataUrl = new DataUrl(imageData, "image/jpeg");
string dataUrlString = dataUrl.ToString();
// Returns: "data:image/jpeg;base64,/9j/4AAQSkZJRg..."

// Parse existing data URL
string urlString = "data:text/plain;base64,SGVsbG8gV29ybGQh";
var parsed = new DataUrl(urlString);
string text = Encoding.UTF8.GetString(parsed.Bytes); // "Hello World!"

// Try parse with error handling
if (DataUrl.TryParse(urlString, out var result))
{
    Console.WriteLine($"MIME type: {result.MimeType}");
    Console.WriteLine($"Data size: {result.Bytes.Length} bytes");
}

// Use in HTML/CSS contexts
var bgImage = new DataUrl(imageBytes, "image/png");
string css = $"background-image: url('{bgImage}');";
```

---

## See Also

- [Extension Methods Reference](extensions.md)
- [Common Use Cases](../examples/common-use-cases.md)
- [Nextended.Core Documentation](../projects/core.md)
