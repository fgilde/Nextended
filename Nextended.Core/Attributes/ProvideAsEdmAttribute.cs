using System;

namespace Nextended.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ProvideAsEdmAttribute : Attribute
{
    public ProvideAsEdmAttribute()
    {
        
    }

    public ProvideAsEdmAttribute(string name)
    {
        Name = name;
    }

    public bool ProvideInherits { get; set; }

    public string? Name { get; }
}

