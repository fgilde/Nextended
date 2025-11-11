#if !NETSTANDARD
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;
using System;

namespace Nextended.Core.Types.Ranges.Math;

[RegisterAs(typeof(IRangeMath<TimeOnly>), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public sealed class TimeOnlyRangeMath : IRangeMath<TimeOnly>
{
    public double ToDouble(TimeOnly v) => v.Ticks;
    public TimeOnly FromDouble(double d) => new TimeOnly(Convert.ToInt64(d));
    public double Difference(TimeOnly s, TimeOnly e) => e.Ticks - s.Ticks;
    public TimeOnly Add(TimeOnly v, double d) => v.Add(TimeSpan.FromTicks(Convert.ToInt64(d)));
}
#endif