using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types.Ranges;

public sealed class DateTimeRange : SimpleRange<DateTime>
{
    public DateTimeRange(DateTime startAndEnd) : base(startAndEnd) { }

    public DateTimeRange(DateTime start, DateTime end) : base(start, end) { }

    public override bool AreAdjacent(IRange<DateTime> other)
    {
        // Zwei DateTimeRanges gelten als angrenzend, wenn:
        // - das Ende dieses Bereichs plus 1 Tick genau dem Start des anderen entspricht
        // - oder umgekehrt
        return End.AddTicks(1) == other.Start || other.End.AddTicks(1) == Start;
    }

    public override string ToString() =>
        $"DateTimeRange: [{Start:yyyy-MM-dd HH:mm:ss.fffffff} - {End:yyyy-MM-dd HH:mm:ss.fffffff}]";
}