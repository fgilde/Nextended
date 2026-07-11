using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Nextended.ResponseFilters.Json;

/// <summary>
/// Serializes an object graph to a <see cref="JsonNode"/> and replays the <see cref="StructuralEdit"/>s
/// recorded in a <see cref="StructuralEditBook"/> against it — the only place a property can actually be
/// removed, renamed, or have an extra key added, since a POCO can't express that at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Mapping from CLR instance to JSON node is done by walking the graph in lockstep with the produced
/// tree, using the serializer's own <see cref="JsonTypeInfo"/> metadata so that naming policies and
/// <c>[JsonPropertyName]</c> are honoured. Children are visited before an owner's own edits are applied,
/// so a rename at one level never hides an edited subtree beneath it.
/// </para>
/// <para>
/// Limitation: values reached only through dictionary entries (serialized as a JSON object keyed by the
/// dictionary keys) are not descended into for nested structural edits — top-level and
/// collection/array/complex-property graphs are. Value mutations (Nullify, Mask, …) are unaffected as
/// they run earlier, in place.
/// </para>
/// </remarks>
public static class JsonStructuralTransformer
{
    /// <summary>
    /// Serialize <paramref name="root"/> and apply all edits from <paramref name="edits"/>. Returns the
    /// resulting <see cref="JsonNode"/> (which may be <c>null</c> when <paramref name="root"/> is null).
    /// </summary>
    public static JsonNode? Transform(object? root, StructuralEditBook edits, JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;

        var node = root is null
            ? null
            : JsonSerializer.SerializeToNode(root, root.GetType(), options);

        if (root is not null && node is not null && edits.HasAny && options.TypeInfoResolver is not null)
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            Apply(root, node, edits, options, visited);
        }

        return node;
    }

    private static void Apply(
        object instance,
        JsonNode node,
        StructuralEditBook edits,
        JsonSerializerOptions options,
        HashSet<object> visited)
    {
        if (!visited.Add(instance)) return;

        // Collections map to JSON arrays; serialization preserves enumeration order, so we can align by index.
        if (node is JsonArray array && instance is IEnumerable enumerable and not string)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (index >= array.Count) break;
                var childNode = array[index++];
                if (item is not null && childNode is JsonObject or JsonArray)
                {
                    Apply(item, childNode, edits, options, visited);
                }
            }
            return;
        }

        if (node is not JsonObject jsonObject) return;

        JsonTypeInfo typeInfo;
        try
        {
            typeInfo = options.GetTypeInfo(instance.GetType());
        }
        catch (System.Exception)
        {
            return; // No metadata (e.g. AOT without a resolver entry) — nothing structural we can safely do.
        }

        // Descend into complex children FIRST, using the original serialized keys. Applying this node's
        // own renames afterwards therefore can't move a subtree out from under the recursion.
        foreach (var property in typeInfo.Properties)
        {
            var getter = property.Get;
            if (getter is null) continue;

            var childNode = jsonObject.TryGetPropertyValue(property.Name, out var cn) ? cn : null;
            if (childNode is not (JsonObject or JsonArray)) continue;

            var childValue = getter(instance);
            if (childValue is null) continue;

            Apply(childValue, childNode, edits, options, visited);
        }

        var ownerEdits = edits.ForOwner(instance);
        if (ownerEdits is null) return;

        var jsonKeyByClrName = BuildJsonKeyMap(typeInfo);
        foreach (var edit in ownerEdits)
        {
            ApplyEdit(edit, jsonObject, jsonKeyByClrName, options);
        }
    }

    private static void ApplyEdit(
        StructuralEdit edit,
        JsonObject jsonObject,
        IReadOnlyDictionary<string, string> jsonKeyByClrName,
        JsonSerializerOptions options)
    {
        switch (edit.Kind)
        {
            case StructuralEditKind.Remove:
                if (edit.PropertyName is not null && jsonKeyByClrName.TryGetValue(edit.PropertyName, out var removeKey))
                {
                    jsonObject.Remove(removeKey);
                }
                break;

            case StructuralEditKind.Rename:
                if (edit.PropertyName is not null && edit.NewName is not null &&
                    jsonKeyByClrName.TryGetValue(edit.PropertyName, out var fromKey))
                {
                    RenameKey(jsonObject, fromKey, edit.NewName);
                }
                break;

            case StructuralEditKind.TransformKey:
                if (edit.PropertyName is not null && edit.KeyTransform is not null &&
                    jsonKeyByClrName.TryGetValue(edit.PropertyName, out var currentKey))
                {
                    RenameKey(jsonObject, currentKey, edit.KeyTransform(currentKey));
                }
                break;

            case StructuralEditKind.AddProperty:
                if (edit.NewName is not null)
                {
                    jsonObject[edit.NewName] = edit.Value is null
                        ? null
                        : JsonSerializer.SerializeToNode(edit.Value, edit.Value.GetType(), options);
                }
                break;
        }
    }

    private static void RenameKey(JsonObject jsonObject, string fromKey, string toKey)
    {
        if (fromKey == toKey) return;
        if (!jsonObject.TryGetPropertyValue(fromKey, out var value)) return;

        jsonObject.Remove(fromKey);
        // Detach the moved node from its old slot before re-inserting (a node may have only one parent).
        jsonObject[toKey] = value?.DeepClone();
    }

    private static Dictionary<string, string> BuildJsonKeyMap(JsonTypeInfo typeInfo)
    {
        var map = new Dictionary<string, string>(System.StringComparer.Ordinal);
        foreach (var property in typeInfo.Properties)
        {
            if (property.AttributeProvider is MemberInfo member)
            {
                map[member.Name] = property.Name;
            }
        }
        return map;
    }
}
