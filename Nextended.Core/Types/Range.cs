using Nextended.Core.Contracts;
using Nextended.Core.Extensions;
using Nextended.Core.Types.Ranges.Math;
using System;

namespace Nextended.Core.Types;

/// <summary>
/// Implementation of a range in a struct.
/// </summary>
public readonly struct RangeOf<T> : IRange<T> where T : IComparable<T>
{
    /// <inheritdoc />
    public T Start { get; }

    /// <inheritdoc />
    public T End { get; }

    private readonly Func<IRange<T>, IRange<T>, double, bool>? _areAdjacentFncFunc;
    private static readonly IRangeMath<T> M = RangeMathFactory.For<T>();


    public RangeLength<T> Length => new(M.Span(this), M);

    public static RangeOf<T> operator +(RangeOf<T> range, RangeLength<T> len)
        => new(len.AddTo(range.Start), len.AddTo(range.End));

    public static RangeOf<T> operator -(RangeOf<T> range, RangeLength<T> len)
        => new(len.SubtractFrom(range.Start), len.SubtractFrom(range.End));

    public static RangeLength<T> operator -(RangeOf<T> a, RangeOf<T> b)
        => new(M.Difference(b.Start, a.Start), M);

    public static RangeOf<T> operator +(RangeOf<T> a, RangeOf<T> b)
    {
        if (!a.Intersects(b) && a.End.CompareTo(b.Start) < 0)
            throw new InvalidOperationException("Ranges are disjoint; cannot union.");
        return (RangeOf<T>)a.Union(b);
    }
    

    /// <summary>
    /// Constructor
    /// </summary>
    public RangeOf(T startAndEnd, Func<IRange<T>, IRange<T>, double, bool>? areAdjacentFncFunc = null) : this(startAndEnd, startAndEnd, areAdjacentFncFunc)
    { }

    public RangeOf(IRange<T> other, Func<IRange<T>, IRange<T>, double, bool>? areAdjacentFncFunc = null) : this(other.Start, other.End, areAdjacentFncFunc)
    {}

    /// <summary>
    /// Constructor that ensures that the start value is less than or equal to the end value.
    /// </summary>
    public RangeOf(T start, T end, Func<IRange<T>, IRange<T>, double, bool>? areAdjacentFncFunc = null)
    {
        _areAdjacentFncFunc = areAdjacentFncFunc;
        if (start.CompareTo(end) > 0)
        {
            throw new ArgumentException("Start value isn't less than or equal to the end value ");
        }
        Start = start;
        End = end;
    }

    /// <summary>
    /// Checks if a value is within the range (inclusive of start and end).
    /// </summary>
    public bool Contains(T value) => Start.CompareTo(value) <= 0 && value.CompareTo(End) <= 0;

    /// <summary>
    /// Alias for Contains.
    /// </summary>
    public bool IsInRange(T value) => Contains(value);

    /// <summary>
    /// Checks if this range intersects with another range.
    /// </summary>
    public bool Intersects(IRange<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        // the ranges overlap if:
        // - the start of this range is less than or equal to the end of the other range AND
        // - the end of this range is greater than or equal to the start of the other range.
        return Start.CompareTo(other.End) <= 0 && End.CompareTo(other.Start) >= 0;
    }

    /// <summary>
    /// Returns the intersection of the two ranges.
    /// If the ranges do not intersect, null is returned.
    /// </summary>
    public IRange<T>? Intersection(IRange<T> other)
    {
        if (!Intersects(other)) return null;
        var s = Max(Start, other.Start);
        var e = Min(End, other.End);
        return new RangeOf<T>(s, e);
    }

    /// <summary>
    /// Returns the union of the two ranges if they intersect or are adjacent.
    /// If the ranges are separate, an exception is thrown.
    /// </summary>
    public IRange<T> Union(IRange<T> other)
    {
        if (!Intersects(other) && !IsAdjacent(other))
            throw new InvalidOperationException("Ranges are disjoint and not adjacent.");
        var s = Min(Start, other.Start);
        var e = Max(End, other.End);
        return new RangeOf<T>(s, e);
    }

    public bool IsAdjacent(IRange<T> other, double tolerance = 0)
    {
        if (_areAdjacentFncFunc != null)
            return _areAdjacentFncFunc(this, other, tolerance);

        var math = RangeMathFactory.For<T>();
        var gap1 = math.Difference(End, other.Start);
        var gap2 = math.Difference(other.End, Start);
        return Math.Abs(gap1) <= tolerance || Math.Abs(gap2) <= tolerance;
    }

    public RangeOf<T> ClampLength(RangeLength<T> min, RangeLength<T> max)
    {
        var len = Length.Delta;
        var minL = Math.Min(min.Delta, max.Delta);
        var maxL = Math.Max(min.Delta, max.Delta);
        var newLen = RangeMath<T>.Clamp(len, minL, maxL);
        var end = M.Add(Start, newLen);
        return new RangeOf<T>(Start, end);
    }

    private static T Min(T a, T b) => a.CompareTo(b) <= 0 ? a : b;
    private static T Max(T a, T b) => a.CompareTo(b) >= 0 ? a : b;

    public override string ToString()
    {
        return $"[{Start} - {End}]";
    }
}
