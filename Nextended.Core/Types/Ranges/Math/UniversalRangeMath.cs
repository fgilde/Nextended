using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using System;

namespace Nextended.Core.Types.Ranges.Math;

public sealed class UniversalRangeMath<T> : IRangeMath<T>
{
    private static ClassMappingSettings? _mappingSettings;

    // Only needed when Convert cannot handle T. Loading every TypeConverter is slow and throws
    // (and swallows) exceptions for converters without a usable constructor, so numbers never pay for it.
    private static ClassMappingSettings MappingSettings => _mappingSettings ??= ClassMappingSettings.Default.AddAllLoadedTypeConverters();

    public double ToDouble(T value)
    {
        try
        {
            return Convert.ToDouble(value);
        }
        catch (Exception)
        {
            return value?.MapTo<double>(MappingSettings) ?? 0;

        }
    }

    public T FromDouble(double value)
    {
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception)
        {
            return value.MapTo<T>(MappingSettings);
        }
    }

    public double Difference(T start, T end) => ToDouble(end) - ToDouble(start);
    public T Add(T value, double delta) => FromDouble(ToDouble(value) + delta);
}



public sealed class NumericRangeMath<T> : IRangeMath<T>
{
    public double ToDouble(T value) => Convert.ToDouble(value);
    public T FromDouble(double value) => (T)Convert.ChangeType(value, typeof(T));
    public double Difference(T start, T end) => ToDouble(end) - ToDouble(start);
    public T Add(T value, double delta) => FromDouble(ToDouble(value) + delta);
}