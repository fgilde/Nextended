﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Nextended.Core.Attributes;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Generators.DtoGeneration;

public class DtoGenerationSymbols
{
    public INamedTypeSymbol? AutoGenerateDto, Ignore, PropertySetting;
    public DtoGenerationConfig Config { get; private set; }

    public static DtoGenerationSymbols Collect(Compilation c, DtoGenerationConfig config) => new()
    {
        AutoGenerateDto = c.GetTypeByMetadataName(typeof(AutoGenerateDtoAttribute).FullName!),
        Ignore = c.GetTypeByMetadataName(typeof(IgnoreOnGenerationAttribute).FullName!),
        PropertySetting = c.GetTypeByMetadataName(typeof(GenerationPropertySettingAttribute).FullName!),
        Config = config
    };

    public List<INamedTypeSymbol> FindTypesWithAttribute(Compilation compilation)
    {
        return AutoGenerateDto != null
            ? FindTypesWithAttributeFromAllAssemblies(compilation, AutoGenerateDto)
            : new List<INamedTypeSymbol>();
    }

    public static List<INamedTypeSymbol> FindTypesWithAttributeFromAllAssemblies(
        Compilation compilation, params INamedTypeSymbol[] attributeTypes)
    {
        static IEnumerable<INamedTypeSymbol> FindAllTypes(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol childNs)
                {
                    foreach (var t in FindAllTypes(childNs))
                        yield return t;
                }
                else if (member is INamedTypeSymbol t)
                {
                    yield return t;
                }
            }
        }

        static bool IsAttributeOrDerived(INamedTypeSymbol? attrClass, INamedTypeSymbol expectedBase)
        {
            while (attrClass != null)
            {
                if (SymbolEqualityComparer.Default.Equals(attrClass, expectedBase))
                    return true;
                attrClass = attrClass.BaseType;
            }
            return false;
        }

        var allTypes = FindAllTypes(compilation.GlobalNamespace).ToList();

        var attributedTypes = allTypes
            .Where(symbol => symbol.GetAttributes().Any(a =>
                attributeTypes.Any(attr => IsAttributeOrDerived(a.AttributeClass, attr))))
            .ToList();

        // Neue Liste, damit keine Duplikate und übersichtlich!
        var result = new HashSet<INamedTypeSymbol>(attributedTypes, SymbolEqualityComparer.Default);

        // Abgeleitete Typen finden (nur falls AutoGenerateDerived)
        foreach (var baseType in attributedTypes)
        {
            // Attribut-Instanz holen
            var autoAttr = baseType.GetAttributeInstance<AutoGenerateDtoAttribute>(attributeTypes[0]);
            if (autoAttr?.AutoGenerateDerived == true)
            {
                foreach (var t in allTypes.Where(t => t.TypeKind == TypeKind.Class))
                {
                    var curr = t.BaseType;
                    while (curr != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(curr, baseType))
                        {
                            result.Add(t);
                            break;
                        }
                        curr = curr.BaseType;
                    }
                }
            }
        }
        return result.ToList();

        //var allTypes = FindAllTypes(compilation.GlobalNamespace);

        //return allTypes
        //    .Where(symbol => symbol.GetAttributes().Any(a =>
        //        attributeTypes.Any(attr => IsAttributeOrDerived(a.AttributeClass, attr))))
        //    .ToList();
    }

    public static string GetDtoClassName(ITypeSymbol type, AutoGenerateDtoAttribute? attr, bool asInterface)
    {
        var main = !string.IsNullOrEmpty(attr?.GeneratedClassName) ? attr.GeneratedClassName : type.Name;
        int idx = main.IndexOf('`');
        if (idx >= 0) main = main.Substring(0, idx);
        var prefix = attr?.Prefix ?? string.Empty;
        var suffix = attr?.Suffix ?? string.Empty;
        return $"{(asInterface ? "I" : "")}{prefix}{main}{suffix}";
    }

    public static string GetDtoPropertyName(IPropertySymbol prop, GenerationPropertySettingAttribute? info)
        => !string.IsNullOrEmpty(info?.PropertyName) ? info.PropertyName : prop.Name;

    public static string GetToDtoMethodName(ISymbol type, AutoGenerateDtoAttribute? attr)
    {
        if (attr?.ToDtoMethodName != null && !string.IsNullOrWhiteSpace(attr.ToDtoMethodName))
            return attr.ToDtoMethodName;
        return $"To{attr?.Prefix}{attr?.Suffix}";
    }

    public static string GetToSourceMethodName(ISymbol type, AutoGenerateDtoAttribute? attr)
    {
        if (attr?.ToSourceMethodName != null && !string.IsNullOrWhiteSpace(attr.ToSourceMethodName))
            return attr.ToSourceMethodName;
        return "ToNet";
    }

    public static IEnumerable<IPropertySymbol> GetDtoProperties(INamedTypeSymbol type, INamedTypeSymbol? ignoreAttr)
        => type.GetMembers().OfType<IPropertySymbol>().Where(p =>
            p.DeclaredAccessibility == Accessibility.Public &&
            !p.IsStatic &&
            (ignoreAttr == null || !p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ignoreAttr)))
        );

    public static string GetDtoPropertyType(
        IPropertySymbol prop,
        Dictionary<string, INamedTypeSymbol> comTypes,
        DtoGenerationSymbols symbols,
        bool asInterface)
    {
        var propType = prop.Type;
        bool isNullable = false;
        ITypeSymbol underlyingType = propType;
        if (propType is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nts)
        {
            isNullable = true;
            underlyingType = nts.TypeArguments[0];
        }
        string typeString;
        if (IsDtoEnumType(underlyingType, comTypes))
        {
            var targetClass = comTypes[underlyingType.ToDisplayString()];
            var targetAttr = targetClass.ClassCfg(symbols);
            var ns = !string.IsNullOrWhiteSpace(targetAttr.Namespace) ? $"{targetAttr.Namespace}." : "";
            typeString = ns+GetDtoClassName(targetClass, targetAttr, false);
        }
        else if (IsDtoType(underlyingType, comTypes))
        {
            var targetClass = comTypes[underlyingType.ToDisplayString()];
            var targetAttr = targetClass.ClassCfg(symbols);
            //var ns = !string.IsNullOrWhiteSpace(targetAttr.Namespace) ? $"{targetAttr.Namespace}." : "";
            typeString = GetDtoClassName(targetClass, targetAttr, asInterface);
        }
        else
        {
            typeString = underlyingType.ToDisplayString();
        }
        if (isNullable && !typeString.EndsWith("?"))
            typeString += "?";
        return typeString;
    }

    public static bool IsDtoType(ITypeSymbol type, Dictionary<string, INamedTypeSymbol> comTypes)
        => comTypes.ContainsKey(type.ToDisplayString());

    public static bool IsDtoEnumType(ITypeSymbol type, Dictionary<string, INamedTypeSymbol> comTypes)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nts)
            type = nts.TypeArguments[0];
        return comTypes.TryGetValue(type.ToDisplayString(), out var t) && t.TypeKind == TypeKind.Enum;
    }


    /// <summary>
    /// Liefert alle Namespaces, auf die Properties des Typs verweisen (für Usings).
    /// </summary>
    public static IEnumerable<string> GetReferencedNamespaces(
        INamedTypeSymbol type,
        Dictionary<string, INamedTypeSymbol> dtoTypeDict)
    {
        return GetDtoProperties(type, null)
            .Select(p => p.Type)
            .Where(t => dtoTypeDict.ContainsKey(t.ToDisplayString()))
            .Select(t => dtoTypeDict[t.ToDisplayString()].ContainingNamespace.ToDisplayString())
            .Where(ns => !string.IsNullOrWhiteSpace(ns));
    }

    public static string[] GetUsings(INamedTypeSymbol type, Dictionary<string, INamedTypeSymbol> dtoTypeDict,
        AutoGenerateDtoAttribute autoGenAttr, DtoGenerationConfig cfg)
    {
        return GetUsings([type], dtoTypeDict, autoGenAttr, cfg);
    }

    public static string[] GetUsings(IEnumerable<INamedTypeSymbol> types, Dictionary<string, INamedTypeSymbol> dtoTypeDict, 
        AutoGenerateDtoAttribute? autoGenAttr, DtoGenerationConfig cfg )
    {
        // TODO: Setting check from AutoGenerateDtoAttribute
        var usings = new HashSet<string>(autoGenAttr?.Usings ?? cfg.Usings ?? Enumerable.Empty<string>())
        {
            "System",
        };
        if (autoGenAttr?.IsComCompatible ?? true)
            usings.Add("System.Runtime.InteropServices");
        foreach (var t in types)
        {
            if(autoGenAttr?.AddContainingNamespaceUsings == true || cfg?.AddContainingNamespaceUsings == true)
                usings.Add(t.ContainingNamespace.ToDisplayString());
            if(autoGenAttr?.AddReferencedNamespacesUsings == true || cfg?.AddReferencedNamespacesUsings == true)
                usings.UnionWith(GetReferencedNamespaces(t, dtoTypeDict));
        }
        return usings.ToArray();
    }

    public static string RegionNameFor(INamedTypeSymbol type, AutoGenerateDtoAttribute autoGenAttr)
    {
        return $"{(autoGenAttr.IsComCompatible == true ? "COM" : "Dto")} class for {type.Name}";
    }

    public static string GetGenericTypeParameters(ITypeSymbol type)
    {
       return type is INamedTypeSymbol named ? GetGenericTypeParameters(named) : "";
    }
    
    public static string GetGenericTypeParameters(INamedTypeSymbol type)
    {
        if (!type.IsGenericType) return "";
        return "<" + string.Join(", ", type.TypeParameters.Select(tp => tp.Name)) + ">";
    }

    public string GetBaseTypeString(INamedTypeSymbol type, Dictionary<string, INamedTypeSymbol> dtoTypeDict, bool asInterface)
    {
        return GetBaseTypeString(type, type.ClassCfg(this), dtoTypeDict, asInterface);
    }

    public string GetBaseTypeString(INamedTypeSymbol type, AutoGenerateDtoAttribute classAttr, Dictionary<string, INamedTypeSymbol> dtoTypeDict, bool asInterface)
    {
        return GetBaseTypeString(type, classAttr.Interfaces, dtoTypeDict, asInterface);
    }
    public string GetBaseTypeString(INamedTypeSymbol type, string[]? interfaces, Dictionary<string, INamedTypeSymbol> dtoTypeDict, bool asInterface)
    {
        var baseTypeStr = interfaces?.Any() ?? false ? $":{string.Join(", ", interfaces)}" : "";
        return GetBaseTypeString(type, baseTypeStr, dtoTypeDict, asInterface);
    }

    public string GetBaseTypeString(INamedTypeSymbol type, string baseTypeStr, Dictionary<string, INamedTypeSymbol> dtoTypeDict, bool asInterface)
    {
        var baseType = type.BaseType;
        if (baseType != null && dtoTypeDict.ContainsKey(baseType.ToDisplayString()))
        {
            var baseTypeAttr = baseType.ClassCfg(this);
            if (baseTypeAttr != null)
            {
                var ns = GetNamespacePrefixString(baseTypeAttr);
                var baseInterfaceName = ns + GetDtoClassName(baseType, baseTypeAttr, asInterface);
                if (string.IsNullOrWhiteSpace(baseTypeStr))
                    baseTypeStr = $": {baseInterfaceName}";
                else
                    baseTypeStr += $", {baseInterfaceName}";
            }
        }

        return baseTypeStr;
    }

    public string GetNamespaceForProperty(IPropertySymbol property, Dictionary<string, INamedTypeSymbol> dtoTypeDict)
    {
        //var type = dtoTypeDict.ContainsKey(property.Type.ToDisplayString())
        //    ? dtoTypeDict[property.Type.ToDisplayString()]
        //    : null;
        //return GetNamespacePrefixString(Namespace(type));
        return GetNamespacePrefixString(GetClassCfgForType(property.Type, dtoTypeDict));
    }

    internal string Namespace(INamedTypeSymbol? type, AutoGenerateDtoAttribute? cfg = null)
    {
        cfg ??= type?.ClassCfg(this);
        var ns = GetNamespacePrefixString(cfg);
        if (!string.IsNullOrWhiteSpace(ns))
            return ns.Substring(0, ns.Length - 1);
        if (!string.IsNullOrWhiteSpace(Config.Namespace))
            return Config.Namespace;
        if (type != null)
            return type.ContainingNamespace.ToDisplayString() + ".Generated";
        return "Nextended.CodeGen.Generated";
    }

    public string GetNamespacePrefixString(AutoGenerateDtoAttribute classCfg)
    {
        return GetNamespacePrefixString(classCfg?.Namespace);
    }

    public string GetNamespacePrefixString(string @namespace)
    {
        var result = @namespace;
        return string.IsNullOrWhiteSpace(result) ? string.Empty : $"{result}.";
    }


    public AutoGenerateDtoAttribute? GetClassCfgForType(ITypeSymbol type, Dictionary<string, INamedTypeSymbol> dtoTypeDict)
    {
        return dtoTypeDict.ContainsKey(type.ToDisplayString()) 
            ? dtoTypeDict[type.ToDisplayString()].ClassCfg(this) 
            : null;
    }

    public bool PropertyInBaseType(IPropertySymbol prop, INamedTypeSymbol? baseType, Dictionary<string, INamedTypeSymbol> dtoTypeDict)
    {
        while (baseType != null && dtoTypeDict.ContainsKey(baseType.ToDisplayString()))
        {
            var baseProps = GetDtoProperties(baseType, Ignore);
            if (baseProps.Any(bp => bp.Name == prop.Name))
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

}
