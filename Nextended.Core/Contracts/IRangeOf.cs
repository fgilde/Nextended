using System;

namespace Nextended.Core.Contracts;

/// <summary>
/// Defines a generic interface for a range.
/// </summary>
public interface IRange<T> where T : IComparable<T>
{
    /// <summary>
    /// The start value of the range.
    /// </summary>
    T Start { get; }

    /// <summary>
    /// The end value of the range.
    /// </summary>
    T End { get; }

    /// <summary>
    /// Checks whether a given value is within the range (inclusive of start and end).
    /// </summary>
    bool Contains(T value);

    /// <summary>
    /// Alias for Contains, to semantically support "isInRange".
    /// </summary>
    bool IsInRange(T value);

    /// <summary>
    /// Checks if this range intersects with another range.
    /// </summary>
    bool Intersects(IRange<T> other);

    /// <summary>
    /// Returns the intersection of the two ranges.
    /// If the ranges do not intersect, null is returned.
    /// </summary>
    IRange<T>? Intersection(IRange<T> other);

    /// <summary>
    /// Returns the union of the two ranges, provided that the ranges intersect or are adjacent.
    /// If the ranges are separate, an exception is thrown.
    /// </summary>
    IRange<T> Union(IRange<T> other);
}
