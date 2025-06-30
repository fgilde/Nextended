using Microsoft.CodeAnalysis;
using System.Reflection;


namespace Nextended.CodeGen.Helper;

internal static class RoslynHelper
{
    public static string ConstraintClause(this ITypeParameterSymbol tp)
    {
        if (tp.HasReferenceTypeConstraint) return $"where {tp.Name} : class";
        if (tp.HasValueTypeConstraint) return $"where {tp.Name} : struct";        
        return "";
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

    public static T GetAttributeInstance<T>(this ISymbol symbol, INamedTypeSymbol roslynAttributeSymbol) where T : new()
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