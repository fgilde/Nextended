using System;

namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// Apply this to properties that cant be cloned, eg ImageSource and other controls.
    /// Those property will still be copied insted of cloning
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NoneCloneable : Attribute
    {
    }
}
