using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the key transform via <c>Using(...)</c>, then close with the
/// standard predicate vocabulary. The transform receives the property's <em>serialized</em> key
/// (i.e. after any <c>JsonNamingPolicy</c> / <c>[JsonPropertyName]</c>) and returns the new key.
/// </summary>
public sealed class TransformKeyBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor[] _accessors;

    internal TransformKeyBuilder(ResponseFilter<T> filter, PropertyAccessor[] accessors)
    {
        _filter = filter;
        _accessors = accessors;
    }

    /// <summary>Map the current serialized key to a new one (e.g. <c>k =&gt; "x_" + k</c>).</summary>
    public TransformKeyTerminal<T> Using(Func<string, string> keyTransform)
    {
        if (keyTransform is null) throw new ArgumentNullException(nameof(keyTransform));
        return new TransformKeyTerminal<T>(_filter, _accessors, keyTransform);
    }

    /// <summary>Every public, readable instance property of <typeparamref name="T"/> (used by <c>TransformKeys</c>).</summary>
    internal static PropertyAccessor[] AllAccessors()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Select(PropertyAccessor.For)
                    .ToArray();
}

/// <summary>Terminal phase of a <c>TransformKey</c>/<c>TransformKeys</c> rule — applies the predicate vocabulary.</summary>
public sealed class TransformKeyTerminal<T> : RuleBuilderBase<TransformKeyTerminal<T>, T> where T : class
{
    private readonly PropertyAccessor[] _accessors;
    private readonly Func<string, string> _keyTransform;

    internal TransformKeyTerminal(ResponseFilter<T> filter, PropertyAccessor[] accessors, Func<string, string> keyTransform) : base(filter)
    {
        _accessors = accessors;
        _keyTransform = keyTransform;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var names = FilterProperties(_accessors).Select(a => a.Property.Name).ToArray();
        var transform = _keyTransform;
        Filter.AddRule(new StructuralEditRule<T>(
            predicate,
            (_, _) => BuildEdits(names, transform)));
    }

    private static IEnumerable<StructuralEdit> BuildEdits(string[] names, Func<string, string> transform)
        => names.Select(n => StructuralEdit.TransformKey(n, transform));
}
