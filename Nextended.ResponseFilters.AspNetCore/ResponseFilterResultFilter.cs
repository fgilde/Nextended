using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
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
        if (context.Result is ObjectResult { Value: { } value })
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
            }
        }

        await next().ConfigureAwait(false);
    }
}
