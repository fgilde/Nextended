using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the new key via <c>To(...)</c>, then close with the standard
/// predicate vocabulary (<c>When/Unless/Always/...</c>).
/// </summary>
public sealed class RenameBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly string _propertyName;

    internal RenameBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _propertyName = accessor.Property.Name;
    }

    /// <summary>Rename the property's serialized key to <paramref name="newName"/>.</summary>
    public RenameTerminal<T> To(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("New name must be non-empty.", nameof(newName));
        return new RenameTerminal<T>(_filter, _propertyName, newName);
    }
}

/// <summary>Terminal phase of a <c>Rename</c> rule — applies the predicate vocabulary.</summary>
public sealed class RenameTerminal<T> : RuleBuilderBase<RenameTerminal<T>, T> where T : class
{
    private readonly string _propertyName;
    private readonly string _newName;

    internal RenameTerminal(ResponseFilter<T> filter, string propertyName, string newName) : base(filter)
    {
        _propertyName = propertyName;
        _newName = newName;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var propertyName = _propertyName;
        var newName = _newName;
        Filter.AddRule(new StructuralEditRule<T>(
            predicate,
            (_, _) => new[] { StructuralEdit.Rename(propertyName, newName) }));
    }
}
