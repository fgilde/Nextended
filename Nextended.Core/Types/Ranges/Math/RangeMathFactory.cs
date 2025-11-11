using System;

namespace Nextended.Core.Types.Ranges.Math;

public static class RangeMathFactory
{
    public static IRangeMath<T> For<T>() where T : IComparable<T>
    {
        var t = typeof(T);
        if (t == typeof(DateTime)) return (IRangeMath<T>)(object)new DateTimeRangeMath();
        if (t == typeof(TimeSpan)) return (IRangeMath<T>)(object)new TimeSpanRangeMath();

#if !NETSTANDARD
        if (t == typeof(DateOnly)) return (IRangeMath<T>)(object)new DateOnlyRangeMath();
        if (t == typeof(TimeOnly)) return (IRangeMath<T>)(object)new TimeOnlyRangeMath();
#endif

        return new NumericRangeMath<T>();
    }
}