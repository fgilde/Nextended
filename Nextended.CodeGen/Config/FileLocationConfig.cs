using Nextended.CodeGen.Enums;

namespace Nextended.CodeGen.Config;

public class FileLocationConfig
{
    /// <summary>
    /// The path where the generated file will be saved.
    /// By default it's null and will be added to the generated code context.
    /// If a path is set, the file will be saved to that path and NOT added to the generated code context.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// If the <see cref="Path"/> is a relative path, this property defines where the base path came from.
    /// </summary>
    public PathRelativeTarget RelativeTarget { get; set; }
}