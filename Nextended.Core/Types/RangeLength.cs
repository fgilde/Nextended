using System;
using Nextended.Core.Types.Ranges.Math;

namespace Nextended.Core.Types;

public readonly struct RangeLength<T>(double delta, IRangeMath<T>? math = null)
    where T : IComparable<T>
{
    private readonly IRangeMath<T> _math = math ?? RangeMathFactory.For<T>();
    public double Delta { get; } = delta;

    public T AddTo(T value) => _math.Add(value, Delta);
    public T SubtractFrom(T value) => _math.Add(value, -Delta);

    public static RangeLength<T> operator +(RangeLength<T> a, RangeLength<T> b)
        => new(a.Delta + b.Delta, a._math);

    public static RangeLength<T> operator -(RangeLength<T> a, RangeLength<T> b)
        => new(a.Delta - b.Delta, a._math);

    public static RangeLength<T> operator *(RangeLength<T> a, double factor)
        => new(a.Delta * factor, a._math);

    public static RangeLength<T> operator /(RangeLength<T> a, double divisor)
        => new(a.Delta / divisor, a._math);

    public static bool operator ==(RangeLength<T> a, RangeLength<T> b) => a.Delta.Equals(b.Delta);
    public static bool operator !=(RangeLength<T> a, RangeLength<T> b) => !a.Delta.Equals(b.Delta);

    public override bool Equals(object? obj) => obj is RangeLength<T> l && this == l;
    public override int GetHashCode() => Delta.GetHashCode();
    public override string ToString() => $"Δ={Delta}";
}
