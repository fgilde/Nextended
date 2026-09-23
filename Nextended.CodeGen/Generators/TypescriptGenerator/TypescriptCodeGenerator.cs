using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Generators.DtoGeneration;
using Nextended.Core.Extensions;
using Nextended.CodeGen.Helper;
using StringExtensions = Nextended.CodeGen.Helper.StringExtensions;

namespace Nextended.CodeGen.Generators.TypescriptGenerator;

public class TypescriptCodeGenerator
{
    private readonly DtoGenerationSymbols _symbols;
    private readonly MainConfig _config;
    private readonly bool _forceMapUnmappedType;
    private readonly INamedTypeSymbol _type;
    private readonly StringBuilder _imports = new(), _content = new();
    private readonly HashSet<ITypeSymbol> _imported = new(SymbolEqualityComparer.Default);
    private readonly string _dtoClassName;
    private readonly Dictionary<ITypeSymbol, string> _cachedTypes;
    
    private static readonly ImmutableHashSet<string> KnownTypes =
    [
        nameof(Guid),
        nameof(DateTimeOffset),
        nameof(DateTime),
        "DateOnly"
    ];
    
    public TypescriptCodeGenerator(INamedTypeSymbol typeSymbol, DtoGenerationSymbols symbols, MainConfig config, Dictionary<ITypeSymbol, string> cachedTypes)
    {
        _symbols = symbols;
        _config = config;
        _forceMapUnmappedType  = config.Typescript.UnmappedTypeHandling == UnmappedTypeHandling.ForceMapping ;
        _type = typeSymbol;
        _dtoClassName = DtoGenerationSymbols.GetDtoClassName(typeSymbol, _type.ClassCfg(_symbols, _forceMapUnmappedType), false);
        _cachedTypes = cachedTypes;
    }


    public GeneratedFile Write()
    {

        if (_type.TypeKind == TypeKind.Enum)
        {
            WriteEnum();
        }
        else
        {
            WriteType();
        }

        return new GeneratedFile(_dtoClassName + ".g.ts", "", _imports.Append(_content).ToString(), outputPath: _config.Typescript.OutputPath);
    }

    private void WriteEnum()
    {
        WritePreclassStrings();
        
        _content.Append("export enum ").Append(_dtoClassName).Append("{\n");
        
        foreach (var member in _type.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst))
        {
            if (member.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _symbols.Ignore)))
                continue;
            
            _content.Append("\t").Append(member.Name).Append(",\n");
        }

        _content.Append("}");
    }

    private void WriteType()
    {
        if (TsHelper.HasBaseType(_type))
        {
            WriteImport(_type.BaseType!);
        }

        WritePreclassStrings();
        WriteTypeDeclaration();
        WriteProperties();

        _content.Append("}");
    }

    private void WritePreclassStrings()
    {
        if (_config.DtoGeneration?.CreateComments == true)
        {
            _content.AppendLine($"//AUTO-GENERATED FROM {_type.Name}");
        }

        var cfg = _type.ClassCfg(_symbols, _forceMapUnmappedType);
        if (!string.IsNullOrEmpty(cfg.PreClassString))
        {
            _content.Append("\n").Append(cfg.PreClassString ??
                                         _config.DtoGeneration?.PreClassString);
        }
    }

    private void WriteImport(INamedTypeSymbol type)
    {
        if (type.UnwrapNullableTypeSymbol() is not INamedTypeSymbol unwrapped)
        {
            return;
        }
        
        if (unwrapped.IsGenericType)
        {
            foreach (var arg in type.TypeArguments)
            {
                foreach (var symbol in TsHelper.GetNamedTypeSymbol(arg))
                {
                    WriteImport(symbol);
                }
            }
        }

        if (!TsHelper.RequiresImport(type) || !_imported.Add(unwrapped))
        {
            return;
        }
        
        var name = ToTsType(unwrapped);
        if (!string.IsNullOrEmpty(name))
        {
            if (name!.Contains("<"))
            {
                name = name.Substring(0, name.IndexOf("<", StringComparison.Ordinal));
            }
            
            _imports.AppendLine($"import {{ type {name} }} from './{name}.g'");
        }
    }
    
    private void WriteTypeDeclaration()
    {
        _content.Append(_config.Typescript.TypeDeclarationKind == TypeDeclarationKind.Type
            ? "\nexport type "
            : "\nexport interface ");

        _content.Append(_dtoClassName);
        
        if (_type.IsGenericType)
        {
            _content.Append("<")
                .Append(string.Join(", ", _type.TypeParameters.Select(t =>
                {
                    if (t.ConstraintTypes.Length > 0 
                        && t is { HasReferenceTypeConstraint: true })
                    {
                        var typeName = ToTsType(t.ConstraintTypes.First(c => c.IsReferenceType));
                        if(!string.IsNullOrEmpty(typeName))
                        {
                            return t.Name + " extends " + typeName;
                        }
                    }
                    
                    return t.Name;
                })));

            _content.Append(">");
        }
        
        if (_config.Typescript.TypeDeclarationKind == TypeDeclarationKind.Type)
        {
            _content.Append(" = ");
        }

        if (TsHelper.HasBaseType(_type))
        {
            var typeName = ToTsType(_type.BaseType!); 
            if(!string.IsNullOrEmpty(typeName))
            {
                if (_config.Typescript.TypeDeclarationKind == TypeDeclarationKind.Interface)
                {
                    _content.Append(" extends ");
                }
                
                _content.Append(typeName);
                
                if (_config.Typescript.TypeDeclarationKind == TypeDeclarationKind.Type)
                {
                    _content.Append(" &");
                }
            }
            
        }

        _content.Append(" {\n");
    }

    private void WriteProperties()
    {
        var props = _symbols.GetDtoProperties(_type, _symbols.Ignore, _forceMapUnmappedType);

        foreach (var prop in props)
        {
            var typeName = ToTsType(prop.Type);
            
            if (string.IsNullOrEmpty(typeName) &&
                _config.Typescript.UnmappedTypeHandling == UnmappedTypeHandling.Ignore)
            {
                continue;
            }
            
            foreach (var symbol in TsHelper.GetNamedTypeSymbol(prop.Type))
            {
                if (!symbol.Equals(_type, SymbolEqualityComparer.Default))
                {
                    WriteImport(symbol);
                }
            }
                
            var propName = _config.Typescript.PropertyNamingScheme == PropertyNamingScheme.CamelCase
                ? prop.Name.ToCamel()
                : StringExtensions.ToPascalCase(prop.Name);

            _content.Append("\t").Append(propName).Append(" : ").Append(typeName ?? "any");
            
            if (prop.IsNullable())
            {
                _content.Append(" | null");
            }

            _content.Append(";\n");
        }
    }
    
    public string? ToTsType(ITypeSymbol namedTypeSymbol)
    {
        var typeSymbol = namedTypeSymbol.UnwrapNullableTypeSymbol();

        if (_cachedTypes.TryGetValue(typeSymbol, out var type))
        {
            return type;
        }

        var t =  typeSymbol switch
        {
            INamedTypeSymbol nts => ToTsTypeCore(nts),
            ITypeParameterSymbol tps => tps.Name,
            IArrayTypeSymbol ats  => ToTsTypeCore(ats),
            _ => ""
        };

        if (!string.IsNullOrEmpty(t))
        {
            _cachedTypes.Add(typeSymbol, t!);
        }

        return t;
    }

    private string? ToTsTypeCore(IArrayTypeSymbol ats)
    {
        if (ats is { ElementType: INamedTypeSymbol nts })
        {
            var typeName = ToTsType(nts);
            if (!string.IsNullOrEmpty(typeName))
            {
                return typeName + "[]";
            }
        }

        return null;
    }

    private string? ToTsTypeCore(INamedTypeSymbol nts)
    {
        if (DtoGenerationSymbols.IsDictionaryType(nts))
        {
            var types = nts.TypeArguments.Select(ToTsType).ToList();

            return types.Any(string.IsNullOrEmpty) ? null : $"Record<{types[0]}, {types[1]}>";
        }

        if (DtoGenerationSymbols.IsSimpleEnumerableType(nts))
        {
            var type = nts.TypeArguments.Select(ToTsType).First();
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }
            
            return type + "[]";
        }
        
        if (nts.ContainingNamespace?.ToDisplayString() == "System" && KnownTypes.Contains(nts.Name))
        {
            return "string";
        }
        
        if (nts.IsGenericType)
        {
            var cfg = nts.ClassCfgOrNull(_symbols, _forceMapUnmappedType);
            if (cfg == null)
            {
                return null;
            }
            
            return DtoGenerationSymbols.GetDtoClassName(nts, cfg, false)  + "<" + string.Join(", ", nts.TypeArguments.Select(ToTsType)) + ">";
        }
        
        if ((nts.IsReferenceType && nts.SpecialType != SpecialType.System_String) || nts.TypeKind == TypeKind.Enum)
        {
            var cfg = nts.ClassCfgOrNull(_symbols, _forceMapUnmappedType);
            return cfg == null ? null : DtoGenerationSymbols.GetDtoClassName(nts, cfg, false);
        }


        switch(nts.SpecialType)
        {
            case SpecialType.System_Int16 
                or SpecialType.System_Int32 
                or SpecialType.System_Int64
                or SpecialType.System_UInt16
                or SpecialType.System_UInt32
                or SpecialType.System_UInt64
                or SpecialType.System_Decimal
                or SpecialType.System_Double:
                return "number";

            case SpecialType.System_String 
                or SpecialType.System_Char 
                or SpecialType.System_DateTime:
                return "string"; 

            case SpecialType.System_Boolean : 
                return "boolean";
        }

        return "any";
    }
}