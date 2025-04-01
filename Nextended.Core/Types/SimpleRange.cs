using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types;

/// <summary>
/// Implementation of a simple range.
/// </summary>
public class SimpleRange<T> : IRange<T> where T : IComparable<T>
{
    protected readonly RangeOf<T> Range;

    protected SimpleRange(T startAndEnd) : this(startAndEnd, startAndEnd)
    {}
    protected SimpleRange(T start, T end)
    {
        Range = new RangeOf<T>(start, end, (me, other) => AreAdjacent(other));
    }

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

    /// <summary>
    /// Abstract method to check if two ranges are adjacent.
    /// Needs to be implemented in derived classes.
    /// </summary>
    public virtual bool AreAdjacent(IRange<T> other) => false;

    public override string ToString() => Range.ToString();
}