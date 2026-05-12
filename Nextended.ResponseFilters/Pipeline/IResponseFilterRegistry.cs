namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Look-up by target type. Implementations should resolve filters from DI per request
/// (so filters can have scoped dependencies) and may cache the type-to-implementation map.
/// </summary>
public interface IResponseFilterRegistry
{
    /// <summary>
    /// All filters registered for <paramref name="type"/>. Multiple filters per type are allowed
    /// and applied in registration order.
    /// </summary>
    IReadOnlyList<IResponseFilter> GetFilters(Type type);

    /// <summary>True if any filter is registered for <paramref name="type"/>.</summary>
    bool HasFilters(Type type);
}
