using System;

namespace Nextended.ResponseFilters;

/// <summary>
/// Pipeline-wide options. Configure once at registration time via
/// <c>services.AddResponseFilters(..., configure: o =&gt; { … })</c>.
/// </summary>
public sealed class ResponseFilterOptions
{
    /// <summary>
    /// How the pipeline reacts when a filter rule throws.
    /// Default: <see cref="FilterExceptionBehavior.Rethrow"/> — surface bugs early.
    /// </summary>
    public FilterExceptionBehavior ExceptionBehavior { get; set; } = FilterExceptionBehavior.Rethrow;

    /// <summary>
    /// When <c>true</c> (default), the pipeline performs a one-time reachability analysis per
    /// response root type. If no registered filter's target type is reachable in the type graph,
    /// the entire pipeline is skipped — no reflection, no graph walk.
    /// </summary>
    /// <remarks>
    /// Cache is process-wide and built lazily. Disable only if your types have run-time polymorphism
    /// (e.g. <c>List&lt;object&gt;</c> holding heterogeneous DTOs that the static analyzer can't see).
    /// </remarks>
    public bool SkipUnaffectedResponses { get; set; } = true;

    /// <summary>
    /// Optional opt-out predicate. When set and returns <c>true</c> for the response root type,
    /// the pipeline is skipped for that response (evaluated before <see cref="SkipUnaffectedResponses"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// opts.SkipResponseType = t =&gt; typeof(Stream).IsAssignableFrom(t) || t.Namespace?.StartsWith("Volo.Abp") == true;
    /// </code>
    /// </example>
    public Func<Type, bool>? SkipResponseType { get; set; }
}

/// <summary>How <see cref="ResponseFilterOptions.ExceptionBehavior"/> shapes the pipeline's response to thrown filter rules.</summary>
public enum FilterExceptionBehavior
{
    /// <summary>
    /// Let exceptions propagate (default). This is the right choice for almost every app — a filter
    /// throwing a <c>BusinessException</c>, <c>UserFriendlyException</c>, or any other domain error
    /// should reach the framework's global exception handler unchanged.
    /// </summary>
    Rethrow,

    /// <summary>
    /// Catch exceptions thrown by filter rules, log them via <c>ILogger&lt;ResponseFilterPipeline&gt;</c>,
    /// and continue with remaining filters. The response is returned partially filtered. Use only
    /// in pipelines where filter robustness matters more than visibility (e.g. a public CMS that
    /// must never 500).
    /// </summary>
    /// <remarks>
    /// Even in this mode, <see cref="System.OperationCanceledException"/> always propagates so that
    /// request aborts and host shutdown work correctly.
    /// </remarks>
    LogAndContinue,
}
