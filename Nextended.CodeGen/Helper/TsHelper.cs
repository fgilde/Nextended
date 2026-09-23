using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Generators.DtoGeneration;

namespace Nextended.CodeGen.Helper;

public class TsHelper
{

    public static bool HasBaseType(INamedTypeSymbol type)
    {
        return type.BaseType != null 
               && type.BaseType.SpecialType != SpecialType.System_Object 
               && type.BaseType.SpecialType != SpecialType.System_Enum
               && type.BaseType.SpecialType != SpecialType.System_ValueType;
    }
    
    public static bool RequiresImport(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.UnwrapNullableTypeSymbol() is not INamedTypeSymbol nts
            || DtoGenerationSymbols.IsDictionaryType(nts) 
            || DtoGenerationSymbols.IsSimpleEnumerableType(nts) 
            || nts.SpecialType == SpecialType.System_String 
            || nts.SpecialType == SpecialType.System_Enum 
            // || nts.BaseType?.SpecialType == SpecialType.System_ValueType 
            // || nts.SpecialType == SpecialType.System_ValueType 
            // || nts.SpecialType == SpecialType.System_Nullable_T
           )
        {
            return false;
        }
        
        return nts.IsReferenceType || nts.TypeKind == TypeKind.Enum;
    }
    
    public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbol(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol { ElementType: INamedTypeSymbol ats })
        {
            foreach (var symbol in GetNamedTypeSymbol(ats))
            {
                yield return symbol;
            }
        }
        else if (type is INamedTypeSymbol nts)
        {
            if (nts.IsGenericType && nts.TypeArguments.Length > 0)
            {
                foreach (var argument in nts.TypeArguments)
                {
                    foreach (var symbol in GetNamedTypeSymbol(argument))
                    {
                        if (TsHelper.RequiresImport(symbol))
                        {
                            yield return symbol;
                        }
                    }
                }
            }
            
            if (TsHelper.RequiresImport(nts))
            {
                yield return nts;
            }
        }
    }


}