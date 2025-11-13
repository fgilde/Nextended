using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace Nextended.Core.Types.Ranges.Math;

public sealed class UniversalRangeMath<T> : IRangeMath<T>
{
    private static ClassMappingSettings? _mappingSettings;
    public UniversalRangeMath()
    {
        _mappingSettings ??= ClassMappingSettings.Default.AddAllLoadedTypeConverters();
    }

    public double ToDouble(T value) => value.MapTo<double>(_mappingSettings);
    public T FromDouble(double value) => value.MapTo<T>(_mappingSettings);
    public double Difference(T start, T end) => ToDouble(end) - ToDouble(start);
    public T Add(T value, double delta) => FromDouble(ToDouble(value) + delta);
}