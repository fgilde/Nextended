using System.Linq;
using Nextended.ResponseFilters.Reflection;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Transposed entry point: select a set of properties first (by name via
/// <see cref="ResponseFilter{T}.Properties"/>, or by metadata via
/// <see cref="ResponseFilter{T}.PropertiesWhere"/>), then choose a type-agnostic operation to apply to
/// all of them. Every operation returns the same builder the direct API returns, so the full terminal
/// vocabulary (<c>When</c>/<c>Unless</c>/<c>Always</c>/<c>WhenProperty</c>) stays available.
/// </summary>
/// <example>
/// <code>
/// PropertiesWhere(p =&gt; p.GetCustomAttribute&lt;SecretAttribute&gt;() != null).Remove().Always();
/// Properties(x =&gt; x.Name, x =&gt; x.Id).Nullify().When(...);
/// </code>
/// </example>
public sealed class PropertySetBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor[] _accessors;

    internal PropertySetBuilder(ResponseFilter<T> filter, PropertyAccessor[] accessors)
    {
        _filter = filter;
        _accessors = accessors;
    }

    /// <summary>Set every selected property to <c>null</c> (see <see cref="ResponseFilter{T}.Nullify"/>).</summary>
    public NullifyBuilder<T> Nullify() => new(_filter, _accessors);

    /// <summary>Remove every selected property from the serialized output (see <see cref="ResponseFilter{T}.Remove"/>).</summary>
    public RemoveBuilder<T> Remove() => new(_filter, _accessors);

    /// <summary>Reset every selected property to <c>default(TProperty)</c> (see <see cref="ResponseFilter{T}.SetToDefault"/>).</summary>
    public SetToDefaultBuilder<T> SetToDefault() => new(_filter, _accessors);

    /// <summary>Transform every selected property's serialized key (see <see cref="ResponseFilter{T}.TransformKeys"/>).</summary>
    public TransformKeyBuilder<T> TransformKey() => new(_filter, _accessors);
}
