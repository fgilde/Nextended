using System;
using System.Runtime.Serialization;
using Nextended.Core.Contracts;

namespace Nextended.Core.Types.Ranges;

#if !NETSTANDARD
/// <summary>
/// A date (without time) range.
/// </summary>
public sealed class DateRange : SimpleRange<DateOnly>
{
    public DateRange(DateOnly startAndEnd) : base(startAndEnd)
    {
    }

    public DateRange(DateOnly start, DateOnly end) : base(start, end)
    {}

    public override bool AreAdjacent(IRange<DateOnly> other)
    {
        return End.AddDays(1) == other.Start || other.End.AddDays(1) == Start;
    }

    public override string ToString() =>
        $"DateRange: [{Start:yyyy-MM-dd} - {End:yyyy-MM-dd}]";
}

#else
public class DateRange: DateRangeLegacy
{
    public DateRange(Date start, Date end) : base(start, end)
    {}
    public override bool AreAdjacent(IRange<Date> other)
    {
        return End.AddDays(1) == other.Start || other.End.AddDays(1) == Start;
    }
    public override string ToString() =>
        $"DateRange: [{Start} - {End}]";
}

#endif

/// <summary>
/// Zeitbereich zwischen zwei Dates
/// </summary>
[DataContract]
public class DateRangeLegacy: SimpleRange<Date>
{
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="startDate">Beginn</param>
    /// <param name="endDate">Ende</param>
    public DateRangeLegacy(Date startDate, Date endDate) : base(startDate, endDate){}

    /// <summary>
    /// Beginn
    /// </summary>
    [DataMember]
    public Date StartDate => Start;

    /// <summary>
    /// Ende
    /// </summary>
    [DataMember]
    public Date EndDate => End;

    public override bool AreAdjacent(IRange<Date> other)
    {
        return End.AddDays(1) == other.Start || other.End.AddDays(1) == Start;
    }
}