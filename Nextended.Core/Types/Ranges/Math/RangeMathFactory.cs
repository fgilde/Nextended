using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types.Ranges.Math;

public static class RangeMathFactory
{
    private static readonly ConcurrentDictionary<Type, object> Cache = new();

    public static IRangeMath<T>? For<T>() where T : IComparable<T>
    {
        var t = typeof(T);
        if (t == typeof(DateTime)) return (IRangeMath<T>)(object)new DateTimeRangeMath();
        if (t == typeof(TimeSpan)) return (IRangeMath<T>)(object)new TimeSpanRangeMath();

#if !NETSTANDARD
        if (t == typeof(DateOnly)) return (IRangeMath<T>)(object)new DateOnlyRangeMath();
        if (t == typeof(TimeOnly)) return (IRangeMath<T>)(object)new TimeOnlyRangeMath();
#endif
        return (IRangeMath<T>)Cache.GetOrAdd(t, static key => CreateFor(key) ?? new UniversalRangeMath<T>());
    }

    private static object? CreateFor(Type t)
    {
        var targetInterface = typeof(IRangeMath<>).MakeGenericType(t);

        var implType =
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                { try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(x => x != null)!;
                    }
                })
                .FirstOrDefault(type =>
                    type is not null &&
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    targetInterface.IsAssignableFrom(type) &&
                    type.GetConstructor(Type.EmptyTypes) != null);

        return implType?.CreateInstance();
    }

}