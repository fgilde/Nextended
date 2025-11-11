using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types.Ranges;

public sealed class DateTimeRange : SimpleRange<DateTime>
{
    public DateTimeRange(DateTime startAndEnd) : base(startAndEnd) { }

    public DateTimeRange(DateTime start, DateTime end) : base(start, end) { }

    public override bool IsAdjacent(IRange<DateTime> other, double tolerance = 0)
    {
        return End.AddTicks(1) == other.Start || other.End.AddTicks(1) == Start;
    }

    public override string ToString() =>
        $"DateTimeRange: [{Start:yyyy-MM-dd HH:mm:ss.fffffff} - {End:yyyy-MM-dd HH:mm:ss.fffffff}]";
}