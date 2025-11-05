using System;

namespace Nextended.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class IncludeInDetailsAttribute(string? group = null, int maxDepth = 6) : Attribute
{
    public string? Group { get; } = group;
    public int MaxDepth { get; } = maxDepth;
}