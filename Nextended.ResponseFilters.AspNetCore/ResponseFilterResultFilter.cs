using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
public sealed class ResponseFilterResultFilter(IResponseFilterPipeline pipeline) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value })
        {
            var filterContext = new ResponseFilterContext(
                context.HttpContext.RequestServices,
                context.HttpContext.RequestAborted);
            await pipeline.ProcessAsync(value, filterContext).ConfigureAwait(false);
        }

        await next().ConfigureAwait(false);
    }
}
