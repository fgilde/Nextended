using System;
using Nextended.Core.Types.Ranges.Math;

namespace Nextended.Core.Types;

public readonly struct RangeLength<T> : IEquatable<RangeLength<T>>, IComparable<RangeLength<T>>
    where T : IComparable<T>
{
    private readonly IRangeMath<T> _math;

    public RangeLength(double delta, IRangeMath<T>? math = null)
    {
        _math = math ?? RangeMathFactory.For<T>();
        Delta = delta;
    }

    public RangeLength(T delta, IRangeMath<T>? math = null)
    {
        _math = math ?? RangeMathFactory.For<T>();
        Delta = _math.ToDouble(delta);
    }


    public double Delta { get; }

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

    public static implicit operator RangeLength<T>(T value) => new(value);

    // IEquatable<RangeLength<T>>
    public bool Equals(RangeLength<T> other) => Delta.Equals(other.Delta);

    public override bool Equals(object? obj) => obj is RangeLength<T> other && Equals(other);

    public override int GetHashCode() => Delta.GetHashCode();

    public int CompareTo(RangeLength<T> other) => Delta.CompareTo(other.Delta);

    public static bool operator ==(RangeLength<T> a, RangeLength<T> b) => a.Equals(b);
    public static bool operator !=(RangeLength<T> a, RangeLength<T> b) => !a.Equals(b);

    public static bool operator >(RangeLength<T> a, RangeLength<T> b) => a.CompareTo(b) > 0;
    public static bool operator <(RangeLength<T> a, RangeLength<T> b) => a.CompareTo(b) < 0;
    public static bool operator >=(RangeLength<T> a, RangeLength<T> b) => a.CompareTo(b) >= 0;
    public static bool operator <=(RangeLength<T> a, RangeLength<T> b) => a.CompareTo(b) <= 0;

    public override string ToString() => $"Δ={Delta}";
}
