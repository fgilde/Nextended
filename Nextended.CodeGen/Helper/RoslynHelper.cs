using Microsoft.CodeAnalysis;
using System.Reflection;
using Nextended.CodeGen.Attributes;
using Nextended.CodeGen.Generators.DtoGeneration;
using System.Text;


namespace Nextended.CodeGen.Helper;

internal static class RoslynHelper
{
    public static string ToCSharpString(this AttributeData attribute)
    {
        var sb = new StringBuilder();
        // Voll qualifizierter Name (inkl. Namespace), aber ohne das "Attribute"-Suffix.
        var attrType = attribute.AttributeClass;
        var attrName = attrType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
        if (attrName.EndsWith("Attribute"))
            attrName = attrName.Substring(0, attrName.Length - "Attribute".Length);

        sb.Append("[").Append(attrName);

        var argList = new List<string>();

        // Konstruktor-Argumente
        foreach (var arg in attribute.ConstructorArguments)
        {
            argList.Add(AttributeTypedConstantToCSharp(arg, attrType));
        }

        // Named-Argumente (benannte Properties)
        foreach (var na in attribute.NamedArguments)
        {
            argList.Add($"{na.Key} = {AttributeTypedConstantToCSharp(na.Value, attrType)}");
        }

        if (argList.Count > 0)
        {
            sb.Append("(").Append(string.Join(", ", argList)).Append(")");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private static string AttributeTypedConstantToCSharp(TypedConstant value, INamedTypeSymbol attrType = null)
    {
        if (value.IsNull)
            return "null";

        var type = value.Type;
        if (type == null) return value.Value?.ToString() ?? "null";

        if (type.SpecialType == SpecialType.System_String)
            return "\"" + value.Value?.ToString()?.Replace("\"", "\\\"") + "\"";
        if (type.SpecialType == SpecialType.System_Boolean)
            return ((bool)value.Value ? "true" : "false");
        if (type.TypeKind == TypeKind.Enum)
        {
            var enumType = type;
            var enumValue = value.Value;
            // Den Namen des Enum-Werts finden!
            var enumMember = enumType.GetMembers().OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, enumValue));
            var enumMemberName = enumMember != null ? enumMember.Name : enumValue?.ToString();

            var enumTypeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");
            return $"{enumTypeName}.{enumMemberName}";
        }
        if (type.SpecialType == SpecialType.System_Char)
            return $"'{value.Value}'";
        if (type.SpecialType == SpecialType.System_Double)
            return ((double)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (type.SpecialType == SpecialType.System_Single)
            return ((float)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
        if (type.SpecialType == SpecialType.System_Decimal)
            return ((decimal)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        if (type.SpecialType == SpecialType.System_Int64)
            return ((long)value.Value).ToString() + "L";
        if (type.SpecialType == SpecialType.System_UInt64)
            return ((ulong)value.Value).ToString() + "UL";
        if (type.SpecialType == SpecialType.System_Int32)
            return ((int)value.Value).ToString();
        if (type.SpecialType == SpecialType.System_UInt32)
            return ((uint)value.Value).ToString() + "U";
        if (type.SpecialType == SpecialType.System_Byte)
            return "(byte)" + value.Value;
        if (type.SpecialType == SpecialType.System_SByte)
            return "(sbyte)" + value.Value;
        if (type.SpecialType == SpecialType.System_Int16)
            return "(short)" + value.Value;
        if (type.SpecialType == SpecialType.System_UInt16)
            return "(ushort)" + value.Value;

        if (value.Kind == TypedConstantKind.Array && value.Values != null)
        {
            var vals = value.Values.Select(v => AttributeTypedConstantToCSharp(v, attrType));
            return $"new {type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")}[] {{ {string.Join(", ", vals)} }}";
        }

        return value.Value?.ToString() ?? "null";
    }
    
    public static string ConstraintClause(this ITypeParameterSymbol tp)
    {
        if (tp.HasReferenceTypeConstraint) return $"where {tp.Name} : class";
        if (tp.HasValueTypeConstraint) return $"where {tp.Name} : struct";        
        return "";
    }

    public static string GenerateGenericConstraintsWithDtoSubstitution(
        this INamedTypeSymbol type,
        INamedTypeSymbol autoGenerateSymbol, 
        Dictionary<string, INamedTypeSymbol> comTypeDict)
    {
        if (!type.IsGenericType || !type.TypeParameters.Any())
            return "";

        var constraints = new List<string>();
        foreach (var tp in type.TypeParameters)
        {
            var cons = tp.ConstraintTypes;
            if (cons.Length == 0 && tp is { HasReferenceTypeConstraint: false, HasValueTypeConstraint: false, HasUnmanagedTypeConstraint: false, HasNotNullConstraint: false, HasConstructorConstraint: false })
                continue;

            var consParts = new List<string>();
            if (tp.HasReferenceTypeConstraint)
                consParts.Add("class");
            if (tp.HasValueTypeConstraint)
                consParts.Add("struct");

            foreach (var ctype in cons)
            {
                var constraintTypeName = ctype.ToDisplayString();
                if (comTypeDict.TryGetValue(constraintTypeName, out var comType))
                {
                    var autoGenAttr = comType.GetAttributeInstance<AutoGenerateDtoAttribute>(autoGenerateSymbol);
                    var dtoTypeName = DtoGenerationSymbols.GetDtoClassName(comType, autoGenAttr, false)
                                      + DtoGenerationSymbols.GetGenericTypeParameters(comType);
                    consParts.Add(dtoTypeName);
                }
                else
                {
                    consParts.Add(constraintTypeName);
                }
            }

            if (tp.HasUnmanagedTypeConstraint)
                consParts.Add("unmanaged");
            if (tp.HasNotNullConstraint)
                consParts.Add("notnull");
            if (tp.HasConstructorConstraint)
                consParts.Add("new()");

            constraints.Add($"where {tp.Name} : {string.Join(", ", consParts)}");
        }

        return string.Join(" ", constraints);
    }

    public static T MapTo<T>(this AttributeData? attrData, Type? type) where T : new()
    {
        if (attrData == null) return default!;
        type ??= typeof(T);
        var result = (T)Activator.CreateInstance(type);

        var ctorParams = type.GetConstructors().FirstOrDefault()?.GetParameters();
        if (ctorParams != null && attrData.ConstructorArguments.Length == ctorParams.Length)
        {
            for (int i = 0; i < ctorParams.Length; i++)
            {
                var prop = type.GetProperty(ctorParams[i].Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    var arg = attrData.ConstructorArguments[i];
                    if (arg.Kind == TypedConstantKind.Array && prop.PropertyType.IsArray)
                    {
                        var elemType = prop.PropertyType.GetElementType()!;
                        var arr = Array.CreateInstance(elemType, arg.Values.Length);
                        for (int j = 0; j < arg.Values.Length; j++)
                            arr.SetValue(ConvertRoslynValue(arg.Values[j].Value, elemType), j);
                        prop.SetValue(result, arr);
                    }
                    else
                    {
                        prop.SetValue(result, ConvertRoslynValue(arg.Value, prop.PropertyType));
                    }
                }
            }
        }

        foreach (var namedArg in attrData.NamedArguments)
        {
            var prop = type.GetProperty(namedArg.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                var value = namedArg.Value;
                if (value.Kind == TypedConstantKind.Array && prop.PropertyType.IsArray)
                {
                    var elemType = prop.PropertyType.GetElementType()!;
                    var arr = Array.CreateInstance(elemType, value.Values.Length);
                    for (int j = 0; j < value.Values.Length; j++)
                        arr.SetValue(ConvertRoslynValue(value.Values[j].Value, elemType), j);
                    prop.SetValue(result, arr);
                }
                else
                {
                    prop.SetValue(result, ConvertRoslynValue(value.Value, prop.PropertyType));
                }
            }
        }

        return result;
    }

    // Wandelt Roslyn-Werte in echte CLR-Types um (besonders für Type, Enum etc.)
    private static object? ConvertRoslynValue(object? value, Type targetType)
    {
        if (value is null) return null;

        // Für Type-Properties (z.B. typeof(...))  
        if (value is INamedTypeSymbol ts && targetType == typeof(Type))
            return Type.GetType(ts.ToDisplayString()); // oder: ts.ToDisplayString(), je nach Anwendung

        // Enums
        if (targetType.IsEnum && value is int intVal)
            return Enum.ToObject(targetType, intVal);

        // Primitive/Direct
        return Convert.ChangeType(value, targetType);
    }


    public static ITypeSymbol UnwrapNullableTypeSymbol(this ITypeSymbol type)
    {
        return type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nts
            ? nts.TypeArguments[0]
            : type;
    }

    private static bool IsAttributeOrDerived(INamedTypeSymbol? actual, INamedTypeSymbol expectedBase)
    {
        while (actual != null)
        {
            if (SymbolEqualityComparer.Default.Equals(actual, expectedBase))
                return true;
            actual = actual.BaseType;
        }
        return false;
    }

    public static GenerationPropertySettingAttribute PropertyCfg(this ISymbol symbol, DtoGenerationSymbols symbols)
    {
        var res = symbol.GetAttributeInstance<GenerationPropertySettingAttribute>(symbols.PropertySetting);
        if (res != null)
        {
            res.MapWithClassMapper ??= symbols.Config.DefaultMappingSettings?.MapWithClassMapper;
        }

        return res;
    }
    
    
    public static AutoGenerateDtoAttribute ClassCfg(this ISymbol symbol, DtoGenerationSymbols symbols)
    {
        var cfg = symbols.Config;
        var res = symbol.GetAttributeInstance<AutoGenerateDtoAttribute>(symbols.AutoGenerateDto);
        res.Usings ??= cfg.Usings;
        res.Namespace ??= cfg.Namespace;
        res.Suffix ??= cfg.Suffix;
        res.Prefix ??= cfg.Prefix;
        res.AddReferencedNamespacesUsings ??= cfg.AddReferencedNamespacesUsings;
        res.AddContainingNamespaceUsings ??= cfg.AddContainingNamespaceUsings;
        //res.KeepAttributesOnGeneratedClass ??= cfg.KeepAttributesOnGeneratedClass;
        //res.KeepAttributesOnGeneratedInterface ??= cfg.KeepAttributesOnGeneratedInterface;
        res.Interfaces ??= cfg.Interfaces;
        res.BaseType ??= cfg.BaseType;
        res.PreClassString ??= cfg.PreClassString;
        res.PreInterfaceString ??= cfg.PreInterfaceString;
        res.ToSourceMethodName ??= cfg.DefaultMappingSettings?.ToSourceMethodName;
        res.ToDtoMethodName ??= cfg.DefaultMappingSettings?.ToDtoMethodName;
        return res;
    }
    
    private static T GetAttributeInstance<T>(this ISymbol symbol, INamedTypeSymbol roslynAttributeSymbol) where T : new()
    {
        var attrData = symbol
            .GetAttributes()
            .FirstOrDefault(a => IsAttributeOrDerived(a.AttributeClass, roslynAttributeSymbol));
        var clrType = attrData?.AttributeClass != null ? Type.GetType(attrData.AttributeClass.ToDisplayString()) : null;

        return attrData.MapTo<T>(clrType);
    }

    public static bool IsNullable(this IPropertySymbol prop)
        => prop.NullableAnnotation == NullableAnnotation.Annotated ||
           prop.Type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };
}