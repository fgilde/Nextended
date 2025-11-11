using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types;

/// <summary>
/// Implementation of a simple range.
/// </summary>
public class SimpleRange<T> : IRange<T> where T : IComparable<T>
{
    protected readonly RangeOf<T> Range;

    public SimpleRange(T startAndEnd) : this(startAndEnd, startAndEnd)
    { }

    public SimpleRange(T start, T end)
    {
        Range = new RangeOf<T>(start, end, (me, other, tolerance) => IsAdjacent(other, tolerance));
    }

    public SimpleRange(IRange<T> existing) : this(existing.Start, existing.End)
    { }

    /// <inheritdoc />
    public T Start => Range.Start;

    /// <inheritdoc />
    public T End => Range.End;

    /// <inheritdoc />
    public bool Contains(T value) => Range.Contains(value);

    /// <inheritdoc />
    public bool IsInRange(T value) => Range.IsInRange(value);

    /// <inheritdoc />
    public bool Intersects(IRange<T> other) => Range.Intersects(other);

    /// <inheritdoc />
    public IRange<T>? Intersection(IRange<T> other) => Range.Intersection(other);

    /// <inheritdoc />
    public IRange<T> Union(IRange<T> other) => Range.Union(other);


    public RangeLength<T> Length => Range.Length;

    public static SimpleRange<T> operator +(SimpleRange<T> range, RangeLength<T> len)
        => new(range.Range + len);

    public static SimpleRange<T> operator -(SimpleRange<T> range, RangeLength<T> len)
        => new(range.Range - len);

    public static RangeLength<T> operator -(SimpleRange<T> a, SimpleRange<T> b) => a.Range - b.Range;

    public static SimpleRange<T> operator +(SimpleRange<T> a, SimpleRange<T> b) => new(a.Range + b.Range);

    public RangeOf<T> ClampLength(RangeLength<T> min, RangeLength<T> max) => Range.ClampLength(min, max);

    /// <summary>
    /// Abstract method to check if two ranges are adjacent.
    /// Needs to be implemented in derived classes.
    /// </summary>
    public virtual bool IsAdjacent(IRange<T> other, double tolerance = 0) => new RangeOf<T>(this).IsAdjacent(other, tolerance);

    public override string ToString() => Range.ToString();
}