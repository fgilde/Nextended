#if !NETSTANDARD
using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types.Ranges;

public sealed class TimeRange : SimpleRange<TimeOnly>
{
    public TimeRange(TimeOnly startAndEnd) : base(startAndEnd) { }

    public TimeRange(TimeOnly start, TimeOnly end) : base(start, end) { }

    public override bool IsAdjacent(IRange<TimeOnly> other, double tolerance = 0)
    {
        return End.Add(TimeSpan.FromTicks(1)) == other.Start || other.End.Add(TimeSpan.FromTicks(1)) == Start;
    }

    public override string ToString() =>
        $"TimeOnlyRange: [{Start} - {End}]";
}

#endif