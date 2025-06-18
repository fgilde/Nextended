using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Nextended.CodeGen.Helper;

internal static class RoslynHelper
{

    public static T MapTo<T>(this AttributeData? attrData) where T : new()
    {
        if (attrData == null) return default!;
        var result = new T();
        var type = typeof(T);

        var ctorParams = type.GetConstructors().FirstOrDefault()?.GetParameters();
        if (ctorParams != null && attrData.ConstructorArguments.Length == ctorParams.Length)
        {
            for (int i = 0; i < ctorParams.Length; i++)
            {
                var prop = type.GetProperty(ctorParams[i].Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                    prop.SetValue(result, attrData.ConstructorArguments[i].Value);
            }
        }

        foreach (var namedArg in attrData.NamedArguments)
        {
            var prop = type.GetProperty(namedArg.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
                prop.SetValue(result, namedArg.Value.Value);
        }

        return result;
    }


    public static T GetAttributeInstance<T>(
        this ISymbol symbol, INamedTypeSymbol roslynAttributeSymbol
    ) where T : new()
    {
        var attrData = symbol
            .GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, roslynAttributeSymbol));
        return attrData.MapTo<T>();
    }
}