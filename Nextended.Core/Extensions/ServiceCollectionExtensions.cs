using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;

namespace Nextended.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterAllWithRegisterAsAttribute(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.RegisterAllWithRegisterAsAttribute(null, assemblies);
    }

    public static IServiceCollection RegisterAllWithRegisterAsAttribute(this IServiceCollection services, Action<ServiceDescriptor> onRegistered, params Assembly[] assemblies)
    {
        if(assemblies.IsNullOrEmpty())
            assemblies = [Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly()];
        foreach (var assembly in assemblies)
        {
            assembly.GetTypes()
                .SelectMany(t => t.GetCustomAttributes<RegisterAsAttribute>().Select(a => a.SetImplementationType(t)))
                .OrderBy(a => a.Order)
                .SelectMany(a => a.GetServiceDescriptor())
                .Apply(s =>
                {
                    services.Add(s);
                    onRegistered?.Invoke(s);
                });
        }
        return services;
    }


    public static IServiceCollection RegisterAllImplementationsOf<TInterface>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] {typeof(TInterface)}, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null,
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2), typeof(TInterface3) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

    public static IServiceCollection RegisterAllImplementationsOf<TInterface1, TInterface2, TInterface3, TInterface4>(this IServiceCollection services, Assembly[] assembliesToSearchImplementationsIn = null, 
        ServiceLifetime lifeTime = ServiceLifetime.Transient, Action<ServiceDescriptor> onRegistered = null) => services.RegisterAllImplementationsOf(new[] { typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4) }, assembliesToSearchImplementationsIn, lifeTime, onRegistered);

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
                if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == registerType)){
                    var serviceType = type.GetInterfaces().First(i => i.GetGenericTypeDefinition() == registerType);
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