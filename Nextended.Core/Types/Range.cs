using System;
using Nextended.Core.Contracts;

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

    private readonly Func<IRange<T>, IRange<T>, bool>? _areAdjacentFncFunc;

    /// <summary>
    /// Constructor
    /// </summary>
    public RangeOf(T startAndEnd, Func<IRange<T>, IRange<T>, bool>? areAdjacentFncFunc = null) : this(startAndEnd, startAndEnd, areAdjacentFncFunc)
    {}

    /// <summary>
    /// Constructor that ensures that the start value is less than or equal to the end value.
    /// </summary>
    public RangeOf(T start, T end, Func<IRange<T>, IRange<T>, bool>? areAdjacentFncFunc = null)
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
    public bool Contains(T value)
    {
        return Start.CompareTo(value) <= 0 && value.CompareTo(End) <= 0;
    }

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
        if (!Intersects(other))
        {
            return null;
        }
        // Der Schnittbereich startet beim höheren der beiden Startwerte
        T newStart = Start.CompareTo(other.Start) >= 0 ? Start : other.Start;
        // Der Schnittbereich endet beim kleineren der beiden Endwerte
        T newEnd = End.CompareTo(other.End) <= 0 ? End : other.End;
        return new RangeOf<T>(newStart, newEnd);
    }

    /// <summary>
    /// Returns the union of the two ranges if they intersect or are adjacent.
    /// If the ranges are separate, an exception is thrown.
    /// </summary>
    public IRange<T> Union(IRange<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (!Intersects(other) && !AreAdjacent(this, other))
        {
            throw new ArgumentException("Ranges do not overlap and are not adjacent.");
        }
        T newStart = Start.CompareTo(other.Start) <= 0 ? Start : other.Start;
        T newEnd = End.CompareTo(other.End) >= 0 ? End : other.End;
        return new RangeOf<T>(newStart, newEnd);
    }

    /// <summary>
    /// Checks if two ranges are adjacent.
    /// Note: For generic T, this check is not generally possible.
    /// For specific types (like int or DateTime) a specific logic may need to be implemented.
    /// </summary>
    public bool AreAdjacent(IRange<T> first, IRange<T> second)
    {
        if(_areAdjacentFncFunc == null)
        {
            throw new InvalidOperationException("No function to check adjacency is provided.");
        }
        return _areAdjacentFncFunc(first, second);
    }

    public override string ToString()
    {
        return $"[{Start} - {End}]";
    }
}
