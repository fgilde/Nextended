using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nextended.ResponseFilters.Extensions;

namespace Nextended.ResponseFilters.AspNetCore;

/// <summary>DI registration for the ASP.NET Core adapter.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the response filter pipeline, discover all <see cref="ResponseFilter{T}"/> implementations
    /// in the given assemblies, and plug the <see cref="ResponseFilterResultFilter"/> into MVC globally.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan. If none are provided, the calling assembly is used.</param>
    /// <param name="lifetime">Lifetime for discovered filters.</param>
    /// <param name="configure">
    /// Optional callback to configure <see cref="ResponseFilterOptions"/> — for example to switch from
    /// the default <see cref="FilterExceptionBehavior.Rethrow"/> to
    /// <see cref="FilterExceptionBehavior.LogAndContinue"/>, or to opt specific response types out
    /// of the pipeline entirely.
    /// </param>
    /// <example>
    /// <code>
    /// builder.Services.AddNextendedResponseFilters(
    ///     assemblies: new[] { typeof(MyFilter).Assembly },
    ///     configure: opts =>
    ///     {
    ///         opts.ExceptionBehavior = FilterExceptionBehavior.LogAndContinue;
    ///         opts.SkipResponseType = t => t.Namespace?.StartsWith("Volo.Abp") == true;
    ///         // Per-request gate — only run the pipeline for /api/app/* responses.
    ///         opts.ShouldHandle = (request, type) =>
    ///             Task.FromResult(request.Path.StartsWithSegments("/api/app"));
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddNextendedResponseFilters(
        this IServiceCollection services,
        Assembly[]? assemblies = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<ResponseFilterOptions>? configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        
        services.AddResponseFilters(assemblies, lifetime, configure);
        services.AddTransient<ResponseFilterResultFilter>();
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<ResponseFilterResultFilter>();
        });

        return services;
    }
}
