using System;

namespace Nextended.Web;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ProvideEdmAttribute : Attribute
{
    public ProvideEdmAttribute()
    {
        
    }

    public ProvideEdmAttribute(string name)
    {
        Name = name;
    }

    public bool ProvideInherits { get; set; }

    public string? Name { get; }
}

