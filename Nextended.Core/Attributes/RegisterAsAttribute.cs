using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Nextended.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RegisterAsAttribute: System.Attribute
{
    public Type RegisterAsType { get; }
    public bool Enabled { get; set; } = true;

    public bool RegisterAsImplementation { get; set; } = false;

    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Transient;

    public int Order { get; set; }

    internal Type ImplementationType { get; private set;  }

    internal RegisterAsAttribute SetImplementationType(Type implType)
    {
        ImplementationType = implType;
        return this;
    }

    protected virtual bool IsEnabled()
    {
        return Enabled;
    }

    public RegisterAsAttribute(Type registerAsType, int order = 99)
    {
        RegisterAsType = registerAsType;
        Order = order;
    }

    public IEnumerable<ServiceDescriptor> GetServiceDescriptor()
    {
        var implementationType = ImplementationType;

        if (!IsEnabled())
        {
            yield break;
        }

        yield return new ServiceDescriptor(RegisterAsType, implementationType, ServiceLifetime);
        if (RegisterAsImplementation && RegisterAsType != implementationType)
            yield return new ServiceDescriptor(implementationType, implementationType, ServiceLifetime);
    }
}
