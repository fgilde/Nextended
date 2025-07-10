using System;

namespace Nextended.Core.Attributes;

/// <summary>
/// Set this attribute on a property or field to ignore it during the generation of DTOs or COM interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IgnoreOnGenerationAttribute : Attribute
{ }