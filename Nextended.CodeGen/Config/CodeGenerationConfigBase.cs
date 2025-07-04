﻿namespace Nextended.CodeGen.Config;

public class CodeGenerationConfigBase
{
    /// <summary>
    /// Default namespace for generated classes. Can be overwritten for DTOs in the AutoGenerateDtoAttribute.
    /// If null same as source will be used.
    /// (can be overwritten per class in Attribute layer)
    /// </summary>
    public string? Namespace { get; set; } = "Nextended.CodeGen.Generated";

    /// <summary>
    /// Default suffix for generated DTOs will only work when not set on Attribute layer.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Default prefix for generated DTOs will only work when not set on Attribute layer.
    /// (can be overwritten per class in Attribute layer)
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the configuration for the file location where the generated code will be placed.
    /// </summary>
    /// <value>
    /// An instance of <see cref="FileLocationConfig"/> that specifies the path and relative target
    /// for the generated files.
    /// </value>
    public FileLocationConfig LocationConfig { get; set; }
}