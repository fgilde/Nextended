#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;

namespace Nextended.Core.Types.Ranges.Math;

[RegisterAs(typeof(IRangeMath<DateOnly>), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public sealed class DateOnlyRangeMath : IRangeMath<DateOnly>
{
    public double ToDouble(DateOnly v) => v.DayNumber;
    public DateOnly FromDouble(double d) => DateOnly.FromDayNumber(Convert.ToInt32(d));
    public double Difference(DateOnly s, DateOnly e) => e.DayNumber - s.DayNumber;
    public DateOnly Add(DateOnly v, double d) => DateOnly.FromDayNumber(v.DayNumber + Convert.ToInt32(d));
}
#endif