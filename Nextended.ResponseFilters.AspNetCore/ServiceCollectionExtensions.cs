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
    public static IServiceCollection AddNextendedResponseFilters(
        this IServiceCollection services,
        Assembly[]? assemblies = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        assemblies ??= new[] { Assembly.GetCallingAssembly() };

        services.AddResponseFilters(assemblies, lifetime);
        services.AddTransient<ResponseFilterResultFilter>();
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<ResponseFilterResultFilter>();
        });

        return services;
    }
}
