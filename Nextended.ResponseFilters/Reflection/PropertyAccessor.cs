using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.ResponseFilters.Reflection;

/// <summary>
/// Compiled delegate-based getter/setter for a <see cref="PropertyInfo"/>.
/// Replaces <c>PropertyInfo.GetValue</c> / <c>SetValue</c> for hot-path use; ~10-50x faster than raw reflection.
/// Instances are cached per <see cref="PropertyInfo"/>.
/// </summary>
public sealed class PropertyAccessor
{
    private static readonly ConcurrentDictionary<PropertyInfo, PropertyAccessor> Cache = new();

    public PropertyInfo Property { get; }
    public Type DeclaringType { get; }
    public Type PropertyType { get; }

    public Func<object, object?>? Getter { get; }
    public Action<object, object?>? Setter { get; }

    public bool CanRead => Getter != null;
    public bool CanWrite => Setter != null;

    private PropertyAccessor(PropertyInfo property)
    {
        Property = property;
        DeclaringType = property.DeclaringType ?? throw new InvalidOperationException(
            $"Property '{property.Name}' has no declaring type.");
        PropertyType = property.PropertyType;

        if (property.CanRead && property.GetIndexParameters().Length == 0)
        {
            Getter = BuildGetter(property);
        }
        if (property.CanWrite && property.GetIndexParameters().Length == 0)
        {
            Setter = BuildSetter(property);
        }
    }

    public static PropertyAccessor For(PropertyInfo property)
        => Cache.GetOrAdd(property, p => new PropertyAccessor(p));

    public object? GetValue(object instance)
    {
        if (Getter == null)
        {
            throw new InvalidOperationException(
                $"Property '{DeclaringType.FullName}.{Property.Name}' is not readable.");
        }
        return Getter(instance);
    }

    public void SetValue(object instance, object? value)
    {
        if (Setter == null)
        {
            throw new InvalidOperationException(
                $"Property '{DeclaringType.FullName}.{Property.Name}' is not writable.");
        }
        Setter(instance, value);
    }

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        // (object instance) => (object) ((TDeclaring) instance).<Property>
        var instance = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(instance, property.DeclaringType!);
        var access = Expression.Property(cast, property);
        var convert = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(convert, instance).Compile();
    }

    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        // (object instance, object value) => ((TDeclaring) instance).<Property> = (TProp) value
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var castInstance = Expression.Convert(instance, property.DeclaringType!);
        var castValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(castInstance, property), castValue);
        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }
}
