using Nextended.Core.Contracts;
using Nextended.Core.Types.Ranges.Math;
using System;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types;

public interface IRangeMath<T>
{
    double ToDouble(T value);
    T FromDouble(double value);
    double Difference(T start, T end);
    T Add(T value, double delta);
}

public static class RangeMath<T> where T : IComparable<T>
{
    private static IRangeMath<T> M => RangeMathFactory.For<T>();

    public static double ToDouble(T v) => M.ToDouble(v);
    public static T FromDouble(double d) => M.FromDouble(d);
    public static double Delta(IRange<T> r) => M.Difference(r.Start, r.End);
    public static T AddDelta(T v, double d) => M.Add(v, d);

    public static T Clamp(T v, IRange<T> bounds) => M.Clamp(v, bounds);

    public static double Percent(T v, IRange<T> size) => M.Percent(v, size);

    public static T Lerp(IRange<T> size, double pct) => M.Lerp(size, pct);

    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }

    public static T SnapToStep(T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = SnapPolicy.Nearest) => M.SnapToStep(v, size, step, policy);
    public static T AddSteps(T v, RangeLength<T> step, int steps) => M.AddSteps(v, step, steps);
}