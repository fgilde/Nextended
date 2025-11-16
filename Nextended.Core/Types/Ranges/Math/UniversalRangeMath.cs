using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using System;

namespace Nextended.Core.Types.Ranges.Math;

public sealed class UniversalRangeMath<T> : IRangeMath<T>
{
    private static ClassMappingSettings? _mappingSettings;
    public UniversalRangeMath()
    {
        _mappingSettings ??= ClassMappingSettings.Default.AddAllLoadedTypeConverters();
    }

    public double ToDouble(T value)
    {
        try
        {
            return Convert.ToDouble(value);
        }
        catch (Exception)
        {
            return value?.MapTo<double>(_mappingSettings ?? ClassMappingSettings.Default) ?? 0;

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
            return value.MapTo<T>(_mappingSettings ?? ClassMappingSettings.Default);
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