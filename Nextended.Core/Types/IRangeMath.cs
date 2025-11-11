using Nextended.Core.Contracts;
using Nextended.Core.Types.Ranges.Math;
using System;

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

    public static T Clamp(T v, IRange<T> bounds)
    {
        var d = M.ToDouble(v);
        var mi = M.ToDouble(bounds.Start);
        var ma = M.ToDouble(bounds.End);
        if (d < mi) return bounds.Start;
        if (d > ma) return bounds.End;
        return v;
    }

    public static double Percent(T v, IRange<T> size)
    {
        var a = M.ToDouble(v);
        var mi = M.ToDouble(size.Start);
        var ma = M.ToDouble(size.End);
        var span = Math.Max(1e-12, ma - mi);
        return (a - mi) / span;
    }



    public static T Lerp(IRange<T> size, double pct)
        => M.FromDouble(M.ToDouble(size.Start) + (M.ToDouble(size.End) - M.ToDouble(size.Start)) * Clamp(pct, 0, 1));

    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }

    public static T SnapToStep(T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = SnapPolicy.Nearest)
    {
        var stepLen = Math.Abs(step.Delta);
        if (stepLen <= 0) return v;


        var from = M.ToDouble(size.Start);
        var dv = M.ToDouble(v) - from;
        double n = dv / stepLen;

        double k = policy switch
        {
            SnapPolicy.Floor => Math.Floor(n),
            SnapPolicy.Ceiling => Math.Ceiling(n),
            _ => Math.Round(n, MidpointRounding.AwayFromZero)
        };
        var snapped = from + k * stepLen;
        return Clamp(M.FromDouble(snapped), size);
    }



    public static T AddSteps(T v, RangeLength<T> step, int steps)
        => M.Add(v, step.Delta * steps);
}