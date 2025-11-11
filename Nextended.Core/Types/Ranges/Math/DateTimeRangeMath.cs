using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;
using System;

namespace Nextended.Core.Types.Ranges.Math;

[RegisterAs(typeof(IRangeMath<DateTime>), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public sealed class DateTimeRangeMath : IRangeMath<DateTime>
{
    public double ToDouble(DateTime value) => value.Ticks;
    public DateTime FromDouble(double value) => new(Convert.ToInt64(value));
    public double Difference(DateTime start, DateTime end) => end.Ticks - start.Ticks;
    public DateTime Add(DateTime value, double delta) => value.AddTicks(Convert.ToInt64(delta));
}