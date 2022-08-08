using System;

namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// Ignore Properties or Field that containe this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ClonerIgnoreAttribute : Attribute
    {
    }
}
