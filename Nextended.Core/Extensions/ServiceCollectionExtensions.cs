using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nextended.Core.Attributes;

namespace Nextended.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all types marked with <see cref="RegisterAsAttribute"/> from the specified assemblies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">The assemblies to scan for types</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllWithRegisterAsAttribute(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.RegisterAllWithRegisterAsAttribute(null, assemblies);
    }

    /// <summary>
    /// Registers all types marked with <see cref="RegisterAsAttribute"/> from the specified assemblies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <param name="assemblies">The assemblies to scan for types</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllWithRegisterAsAttribute(this IServiceCollection services, Action<ServiceDescriptor> onRegistered, params Assembly[] assemblies)
    {
        if(assemblies.IsNullOrEmpty())
            assemblies = [Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly()];
        foreach (var assembly in assemblies)
        {
            assembly.GetTypes()
                .SelectMany(t => t.GetCustomAttributes<RegisterAsAttribute>().Select(a => a.SetImplementationType(t)))
                .OrderBy(a => a.Order)
                .Apply(a =>
                {
                    foreach (var serviceDescriptor in a.GetServiceDescriptor())
                    {
                        if (a.ReplaceServices)
                        {
                            services.Replace(serviceDescriptor);
                        }
                        else
                        {
                            services.Add(serviceDescriptor);
                        }

                        onRegistered?.Invoke(serviceDescriptor);
                    }
                });
        }
        return services;
    }

    /// <summary>
    /// Registers all implementations of the specified interface type
    /// </summary>
    /// <typeparam name="TInterface">The interface type to search implementations for</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assembliesToSearchImplementationsIn">Optional assemblies to search; if null, uses calling, executing, and entry assemblies</param>
    /// <param name="lifeTime">The service lifetime</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllImplementationsOf<TInterface>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] {typeof(TInterface)}, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    /// <summary>
    /// Registers all implementations of the specified interface types
    /// </summary>
    /// <typeparam name="TInterface1">The first interface type</typeparam>
    /// <typeparam name="TInterface2">The second interface type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assembliesToSearchImplementationsIn">Optional assemblies to search; if null, uses calling, executing, and entry assemblies</param>
    /// <param name="lifeTime">The service lifetime</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    /// <summary>
    /// Registers all implementations of the specified interface types
    /// </summary>
    /// <typeparam name="TInterface1">The first interface type</typeparam>
    /// <typeparam name="TInterface2">The second interface type</typeparam>
    /// <typeparam name="TInterface3">The third interface type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assembliesToSearchImplementationsIn">Optional assemblies to search; if null, uses calling, executing, and entry assemblies</param>
    /// <param name="lifeTime">The service lifetime</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2), typeof(TInterface3) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    /// <summary>
    /// Registers all implementations of the specified interface types
    /// </summary>
    /// <typeparam name="TInterface1">The first interface type</typeparam>
    /// <typeparam name="TInterface2">The second interface type</typeparam>
    /// <typeparam name="TInterface3">The third interface type</typeparam>
    /// <typeparam name="TInterface4">The fourth interface type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assembliesToSearchImplementationsIn">Optional assemblies to search; if null, uses calling, executing, and entry assemblies</param>
    /// <param name="lifeTime">The service lifetime</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3, TInterface4>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, 
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    /// <summary>
    /// Registers all implementations of the specified interface types
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="interfacesToSearchImplementationsFor">The interface types to search implementations for</param>
    /// <param name="assembliesToSearchImplementationsIn">Optional assemblies to search; if null, uses calling, executing, and entry assemblies</param>
    /// <param name="lifeTime">The service lifetime</param>
    /// <param name="onRegistered">Optional callback invoked for each registered service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterAllImplementationsOf(this IServiceCollection services,
        Type[] interfacesToSearchImplementationsFor,
        Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient,
        Action<ServiceDescriptor> onRegistered = null)
    {
        assembliesToSearchImplementationsIn = ((assembliesToSearchImplementationsIn.IsNullOrEmpty()
            ? new[] { Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() }.Concat(interfacesToSearchImplementationsFor.Select(t => t.Assembly))
            : assembliesToSearchImplementationsIn) ?? Array.Empty<Assembly>()).Where(a => a != null).Distinct().ToArray();
        var types = assembliesToSearchImplementationsIn.SelectMany(a => a.GetTypes()).Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType);
        foreach (var type in types)
        {
            foreach (var registerType in interfacesToSearchImplementationsFor)
            {
                if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == registerType))
                {
                    var serviceType = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == registerType);
                    var serviceDescriptor = new ServiceDescriptor(serviceType, type, lifeTime);
                    services.Add(serviceDescriptor);
                    onRegistered?.Invoke(serviceDescriptor);
                }
                else if (registerType.IsAssignableFrom(type))
                {
                    var serviceDescriptor = new ServiceDescriptor(registerType, type, lifeTime);
                    services.Add(serviceDescriptor);
                    onRegistered?.Invoke(serviceDescriptor);
                }
            }
        }
        return services;
    }
}