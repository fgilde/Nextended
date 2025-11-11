using System;

namespace Nextended.Core.Types.Ranges.Math;

public sealed class NumericRangeMath<T> : IRangeMath<T>
{
    public double ToDouble(T value) => Convert.ToDouble(value);
    public T FromDouble(double value) => (T)Convert.ChangeType(value, typeof(T));
    public double Difference(T start, T end) => ToDouble(end) - ToDouble(start);
    public T Add(T value, double delta) => FromDouble(ToDouble(value) + delta);
}