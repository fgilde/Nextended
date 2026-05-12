namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Entry point for filter execution. Walks an arbitrary object graph and applies all registered
/// <see cref="IResponseFilter"/> instances whose target type matches a visited object.
/// </summary>
public interface IResponseFilterPipeline
{
    ValueTask ProcessAsync(object? root, IResponseFilterContext context);
}
