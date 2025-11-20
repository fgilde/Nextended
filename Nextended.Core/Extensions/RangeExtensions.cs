#if !NETSTANDARD
using Nextended.Core.Types;
using System;

namespace Nextended.Core.Extensions;

public static class RangeExtensions
{

    public static Range ToRange(this SimpleRange<int> range)
    {
        var start = range.Start;
        var endExclusive = range.End + 1;

        return new Range(start, endExclusive);
    }

    public static Range ToRange(this RangeOf<int> range, int? length = null)
    {
        var start = range.Start;

        if (length is null || range.End == int.MaxValue)
        {
            return new Range(start, new Index(0, fromEnd: true));
        }

        var endExclusive = Math.Min(range.End + 1, length.Value);
        return new Range(start, endExclusive);
    }

    public static RangeOf<int> ToRangeOfInt(this Range range)
    {
        int start = range.Start.Value;
        int end = range.End.Value;
        return new RangeOf<int>(start, end);
    }

}
#endif