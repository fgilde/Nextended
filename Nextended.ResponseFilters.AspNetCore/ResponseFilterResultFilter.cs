using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nextended.ResponseFilters.Json;
using Nextended.ResponseFilters.Pipeline;

namespace Nextended.ResponseFilters.AspNetCore;

/// <summary>
/// MVC result filter that runs the <see cref="IResponseFilterPipeline"/> over <see cref="ObjectResult.Value"/>
/// before the action result is serialized.
/// </summary>
/// <remarks>
/// Registered globally via <c>AddNextendedResponseFilters</c>. Failures inside the pipeline are swallowed
/// (logged inside the pipeline) so a misbehaving filter cannot 500 the request.
/// </remarks>
public sealed class ResponseFilterResultFilter(
    IResponseFilterPipeline pipeline,
    IOptions<ResponseFilterOptions> options)
    : IAsyncResultFilter
{
    private readonly ResponseFilterOptions _options = options.Value;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value } objectResult)
        {
            // Per-request opt-in/out gate. When ShouldHandle is null, behaviour is identical
            // to the previous "always handle" path — no extra await, no allocation.
            var shouldHandle = _options.ShouldHandle;
            if (shouldHandle is null || await shouldHandle(context.HttpContext.Request, value.GetType()).ConfigureAwait(false))
            {
                var filterContext = new ResponseFilterContext(
                    context.HttpContext.RequestServices,
                    context.HttpContext.RequestAborted);
                await pipeline.ProcessAsync(value, filterContext).ConfigureAwait(false);

                // Structural edits (remove/rename/add-key) can't be expressed on the POCO, so replay
                // them against the serialized JSON tree and hand the formatter that tree instead.
                if (filterContext.StructuralEdits.HasAny)
                {
                    var jsonOptions = context.HttpContext.RequestServices
                        .GetService<IOptions<JsonOptions>>()?.Value.JsonSerializerOptions;
                    var node = JsonStructuralTransformer.Transform(value, filterContext.StructuralEdits, jsonOptions);
                    objectResult.Value = node;
                    // The tree is a JsonNode now — clear the declared type so the formatter serializes
                    // the node itself rather than trying to shape it as the original DTO type.
                    objectResult.DeclaredType = null;
                }
            }
        }

        await next().ConfigureAwait(false);
    }
}
