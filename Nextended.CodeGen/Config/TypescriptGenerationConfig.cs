namespace Nextended.CodeGen.Config;

public class TypescriptGenerationConfig: CodeGenerationConfigBase
{
    public UnmappedTypeHandling UnmappedTypeHandling { get; set; }
    public PropertyNamingScheme PropertyNamingScheme { get; set; }
    public TypeDeclarationKind TypeDeclarationKind { get; set; }
}
