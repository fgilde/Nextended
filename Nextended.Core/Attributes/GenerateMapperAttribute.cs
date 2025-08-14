using System;

namespace Nextended.Core.Attributes
{
    /// <summary>
    /// Attribute to mark types that should have strongly-typed mappers generated at compile-time,
    /// replacing reflection-based mapping with high-performance generated code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class GenerateMapperAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the namespace for the generated mapper classes.
        /// If not specified, uses the same namespace as the target type.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets whether to generate mapping methods for all properties.
        /// Default is true.
        /// </summary>
        public bool IncludeAllProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate mapping methods for inherited properties.
        /// Default is true.
        /// </summary>
        public bool IncludeInheritedProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate bidirectional mapping methods.
        /// Default is false (only generates To{TargetType} methods).
        /// </summary>
        public bool GenerateBidirectional { get; set; } = false;
    }
}