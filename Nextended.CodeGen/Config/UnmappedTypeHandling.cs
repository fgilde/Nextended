namespace Nextended.CodeGen.Config;

public enum UnmappedTypeHandling
{
    Ignore,
    UseTypescriptAnyType,
    ForceMapping
}

public enum PropertyNamingScheme
{
    CamelCase,
    PascalCase
}

public enum TypeDeclarationKind
{
    Type,
    Interface
}