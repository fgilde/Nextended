using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Generators.DtoGeneration;
using Nextended.CodeGen.Helper;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;
using StringExtensions = Nextended.CodeGen.Helper.StringExtensions;

namespace Nextended.CodeGen.Generators.TypescriptGenerator;

public class TypescriptGenerator  : ISourceSubGenerator
{
    public bool RequireConfig { get; }
    
    public IEnumerable<GeneratedFile> Execute(GenerationContext context)
    {
        DtoGenerationSymbols symbols = DtoGenerationSymbols.Collect(context.Compilation!, context.Config?.DtoGeneration);

        HashSet<INamedTypeSymbol> discoveredTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        Dictionary<ITypeSymbol, string> cachedTypeMapping = new Dictionary<ITypeSymbol, string>(SymbolEqualityComparer.Default);
        
        var types = symbols.FindTypesWithAttribute(context.Compilation);
        
        if (context.Config.Typescript.UnmappedTypeHandling == UnmappedTypeHandling.ForceMapping)
        {
            foreach (var type in types)
            {
                GetDiscoveredTypes(type, discoveredTypes, symbols);
            }
        }
        else
        {
            discoveredTypes.AddRange(types);
        }

        if (context.Config.DtoGeneration.OneFilePerClass)
        {
            foreach (var type in discoveredTypes)
            {
                yield return new TypescriptCodeGenerator(type, symbols, context.Config, cachedTypeMapping).Write();
            }
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var type in discoveredTypes)
            {
                sb.Append(new TypescriptCodeGenerator(type, symbols, context.Config, cachedTypeMapping).Write().Content).Append("\n\n");
            }

            yield return new GeneratedFile("AllDtos.g.ts", "", sb.ToString(), context.Config.Typescript.OutputPath);
        }
    }

    private void GetDiscoveredTypes(INamedTypeSymbol typeSymbol, HashSet<INamedTypeSymbol> discoveredTypes, DtoGenerationSymbols symbols)
    {
        var type = typeSymbol.OriginalDefinition;
        
        if (type.IsGenericType)
        {
            foreach (var arg in type.TypeArguments)
            {
                if (arg is INamedTypeSymbol namedTypeSymbol)
                {
                    GetDiscoveredTypes(namedTypeSymbol, discoveredTypes, symbols);
                }
                else if (arg is IArrayTypeSymbol { ElementType: INamedTypeSymbol elem })
                {
                    GetDiscoveredTypes(elem, discoveredTypes, symbols);
                }
            }
        }
        
        if (TsHelper.RequiresImport(type) && discoveredTypes.Add(type))
        {
            if (TsHelper.HasBaseType(type))
            {
                GetDiscoveredTypes(type.BaseType!, discoveredTypes, symbols);
            }
                
            var props = symbols.GetDtoPropertiesDeep(type, symbols.Ignore, true);

            foreach (var prop in props)
            {
                if (prop.Type is INamedTypeSymbol namedTypeSymbol)
                {
                    GetDiscoveredTypes(namedTypeSymbol, discoveredTypes, symbols);
                }
                else if (prop.Type is IArrayTypeSymbol { ElementType: INamedTypeSymbol elem })
                {
                    GetDiscoveredTypes(elem, discoveredTypes, symbols);
                }
            }
        }
        
    }

}