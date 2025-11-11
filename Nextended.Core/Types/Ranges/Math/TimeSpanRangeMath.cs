using System;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;

namespace Nextended.Core.Types.Ranges.Math;

[RegisterAs(typeof(IRangeMath<TimeSpan>), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public sealed class TimeSpanRangeMath : IRangeMath<TimeSpan>
{
    public double ToDouble(TimeSpan v) => v.Ticks;
    public TimeSpan FromDouble(double d) => new(Convert.ToInt64(d));
    public double Difference(TimeSpan s, TimeSpan e) => e.Ticks - s.Ticks;
    public TimeSpan Add(TimeSpan v, double d) => v.Add(TimeSpan.FromTicks(Convert.ToInt64(d)));
}