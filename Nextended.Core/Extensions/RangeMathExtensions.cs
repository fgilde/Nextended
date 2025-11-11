using Nextended.Core.Contracts;
using Nextended.Core.Types;
using System;

namespace Nextended.Core.Extensions;

public static class RangeMathExtensions
{
    public static double Percent<T>(this IRangeMath<T> math, T v, IRange<T> size) where T : IComparable<T>
    {
        var a = math.ToDouble(v);
        var mi = math.ToDouble(size.Start);
        var ma = math.ToDouble(size.End);
        var span = Math.Max(1e-12, ma - mi);
        return (a - mi) / span;
    }

    public static T Lerp<T>(this IRangeMath<T> math, IRange<T> size, double pct) where T : IComparable<T>

        => math.FromDouble(math.ToDouble(size.Start) + (math.ToDouble(size.End) - math.ToDouble(size.Start)) * RangeMath<T>.Clamp(pct, 0, 1));

    public static T Clamp<T>(this IRangeMath<T> math, T v, IRange<T> bounds) where T : IComparable<T>
    {
        var d = math.ToDouble(v);
        var mi = math.ToDouble(bounds.Start);
        var ma = math.ToDouble(bounds.End);
        if (d < mi) return bounds.Start;
        if (d > ma) return bounds.End;
        return v;
    }

    public static T SnapToStep<T>(this IRangeMath<T> math, T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = SnapPolicy.Nearest) where T : IComparable<T>
    {
        var stepLen = Math.Abs(step.Delta);
        if (stepLen <= 0) return v;


        var from = math.ToDouble(size.Start);
        var dv = math.ToDouble(v) - from;
        double n = dv / stepLen;

        double k = policy switch
        {
            SnapPolicy.Floor => Math.Floor(n),
            SnapPolicy.Ceiling => Math.Ceiling(n),
            _ => Math.Round(n, MidpointRounding.AwayFromZero)
        };
        var snapped = from + k * stepLen;
        return math.Clamp(math.FromDouble(snapped), size);
    }

    public static T AddSteps<T>(this IRangeMath<T> math, T v, RangeLength<T> step, int steps) where T : IComparable<T> => math.Add(v, step.Delta * steps);

    public static double Span<T>(this IRangeMath<T> math, IRange<T> r) where T : IComparable<T> => Math.Abs(math.Difference(r.Start, r.End));

    public static IRange<T> Normalize<T>(this IRange<T> r) where T : IComparable<T>
        => (r.Start.CompareTo(r.End) <= 0) ? r : new SimpleRange<T>(r.End, r.Start);


    public static IRange<T> SnapRange<T>(this IRangeMath<T> math, IRange<T> r, IRange<T> size, RangeLength<T> step) where T : IComparable<T>
    {
        var s = math.SnapToStep(r.Start, size, step);
        var e = math.SnapToStep(r.End, size, step);
        if (s.CompareTo(e) > 0) { var tmp = s; s = e; e = tmp; }
        return new RangeOf<T>(s, e);
    }
}