using System;

namespace Nextended.Core.Attributes
{
    /// <summary>
    /// Attribute to specify target types for mapping generation.
    /// Use this with GenerateMapperAttribute to define specific types to map to/from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class MapToAttribute : Attribute
    {
        /// <summary>
        /// The type to generate mapping methods for.
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// Gets or sets the method name for the mapping method.
        /// If not specified, uses "To{TargetTypeName}".
        /// </summary>
        public string? MethodName { get; set; }

        /// <summary>
        /// Gets or sets whether to ignore properties that don't exist in the target type.
        /// Default is false (will cause compilation error for missing properties).
        /// </summary>
        public bool IgnoreMissingProperties { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of MapToAttribute.
        /// </summary>
        /// <param name="targetType">The type to generate mapping methods for.</param>
        public MapToAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}