using System;

namespace Nextended.ResponseFilters;

/// <summary>
/// The kind of structural change a <see cref="StructuralEdit"/> describes.
/// </summary>
public enum StructuralEditKind
{
    /// <summary>Drop a property entirely so it no longer appears in the serialized output.</summary>
    Remove,

    /// <summary>Rename a property's serialized key to a fixed name.</summary>
    Rename,

    /// <summary>Transform a property's serialized key through a function.</summary>
    TransformKey,

    /// <summary>Inject an additional key/value pair that does not exist on the CLR type.</summary>
    AddProperty
}

/// <summary>
/// A structural change to apply to an object's serialized representation. Unlike value mutators
/// (<c>Nullify</c>, <c>Mask</c>, …) which mutate the DTO in place, structural edits cannot be
/// expressed on a strongly-typed POCO — a property can't be removed or its key renamed at runtime.
/// They are therefore recorded per-instance in the <see cref="StructuralEditBook"/> and applied at
/// serialization time (see <c>JsonStructuralTransformer</c>).
/// </summary>
public sealed class StructuralEdit
{
    private StructuralEdit(
        StructuralEditKind kind,
        string? propertyName,
        string? newName,
        Func<string, string>? keyTransform,
        object? value)
    {
        Kind = kind;
        PropertyName = propertyName;
        NewName = newName;
        KeyTransform = keyTransform;
        Value = value;
    }

    /// <summary>What this edit does.</summary>
    public StructuralEditKind Kind { get; }

    /// <summary>
    /// The CLR property name the edit targets (for <see cref="StructuralEditKind.Remove"/>,
    /// <see cref="StructuralEditKind.Rename"/>, <see cref="StructuralEditKind.TransformKey"/>).
    /// The transformer resolves this to the actual serialized JSON key. <c>null</c> for
    /// <see cref="StructuralEditKind.AddProperty"/>.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// For <see cref="StructuralEditKind.Rename"/> the new serialized key; for
    /// <see cref="StructuralEditKind.AddProperty"/> the key of the injected property.
    /// </summary>
    public string? NewName { get; }

    /// <summary>For <see cref="StructuralEditKind.TransformKey"/>: maps the current serialized key to the new one.</summary>
    public Func<string, string>? KeyTransform { get; }

    /// <summary>For <see cref="StructuralEditKind.AddProperty"/>: the already-computed value to serialize.</summary>
    public object? Value { get; }

    /// <summary>Drop <paramref name="propertyName"/> from the output.</summary>
    public static StructuralEdit Remove(string propertyName)
        => new(StructuralEditKind.Remove, propertyName, null, null, null);

    /// <summary>Rename <paramref name="propertyName"/>'s serialized key to <paramref name="newName"/>.</summary>
    public static StructuralEdit Rename(string propertyName, string newName)
        => new(StructuralEditKind.Rename, propertyName, newName, null, null);

    /// <summary>Transform <paramref name="propertyName"/>'s serialized key through <paramref name="keyTransform"/>.</summary>
    public static StructuralEdit TransformKey(string propertyName, Func<string, string> keyTransform)
        => new(StructuralEditKind.TransformKey, propertyName, null, keyTransform, null);

    /// <summary>Inject a new key <paramref name="name"/> with the given <paramref name="value"/>.</summary>
    public static StructuralEdit AddProperty(string name, object? value)
        => new(StructuralEditKind.AddProperty, null, name, null, value);
}
