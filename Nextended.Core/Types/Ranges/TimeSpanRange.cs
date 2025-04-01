using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types.Ranges;

public sealed class TimeSpanRange : SimpleRange<TimeSpan>
{
    public TimeSpanRange(TimeSpan startAndEnd) : base(startAndEnd) { }

    public TimeSpanRange(TimeSpan start, TimeSpan end) : base(start, end) { }

    public override bool AreAdjacent(IRange<TimeSpan> other)
    {
        return End.Add(TimeSpan.FromTicks(1)) == other.Start || other.End.Add(TimeSpan.FromTicks(1)) == Start;
    }

    public override string ToString() =>
        $"TimeSpanRange: [{Start} - {End}]";
}