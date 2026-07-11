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
    private readonly string[] _propertyNames;

    internal TransformKeyBuilder(ResponseFilter<T> filter, string[] propertyNames)
    {
        _filter = filter;
        _propertyNames = propertyNames;
    }

    /// <summary>Map the current serialized key to a new one (e.g. <c>k =&gt; "x_" + k</c>).</summary>
    public TransformKeyTerminal<T> Using(Func<string, string> keyTransform)
    {
        if (keyTransform is null) throw new ArgumentNullException(nameof(keyTransform));
        return new TransformKeyTerminal<T>(_filter, _propertyNames, keyTransform);
    }

    /// <summary>Enumerate the public, readable instance properties of <typeparamref name="T"/> (used by <c>TransformKeys</c>).</summary>
    internal static string[] AllPropertyNames()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Select(p => p.Name)
                    .ToArray();
}

/// <summary>Terminal phase of a <c>TransformKey</c>/<c>TransformKeys</c> rule — applies the predicate vocabulary.</summary>
public sealed class TransformKeyTerminal<T> : RuleBuilderBase<TransformKeyTerminal<T>, T> where T : class
{
    private readonly string[] _propertyNames;
    private readonly Func<string, string> _keyTransform;

    internal TransformKeyTerminal(ResponseFilter<T> filter, string[] propertyNames, Func<string, string> keyTransform) : base(filter)
    {
        _propertyNames = propertyNames;
        _keyTransform = keyTransform;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var names = _propertyNames;
        var transform = _keyTransform;
        Filter.AddRule(new StructuralEditRule<T>(
            predicate,
            (_, _) => BuildEdits(names, transform)));
    }

    private static IEnumerable<StructuralEdit> BuildEdits(string[] names, Func<string, string> transform)
        => names.Select(n => StructuralEdit.TransformKey(n, transform));
}
