using Nextended.Core.CodeGen;
using Nextended.Core.Enums;
using Sample.Contracts.Crm;
using Volo.Abp.Application.Dtos;

namespace Sample.CodeGen;

/// <summary>Two TypeScript generations of the same contracts. Read by Nextended.CodeGen at compile time, never executed.</summary>
public sealed class TsCodeGen : CodeGenConfig
{
    public TsCodeGen()
    {
        // ABP: enums as numbers, folders per namespace below Sample.Contracts, ABP paging from a hand-written module
        Typescript(t => t
            .OutputPath("ts")
            .Preset(TypescriptPreset.Abp)
            .IncludeNamespaces("Sample.Contracts")
            .RootNamespace("Sample.Contracts")
            .TypeMap(typeof(PagedResultDto<>), "PagedResult from '../ts-support/abp'")
            .Type<CustomerDto>(c => c.Property(x => x.Email).Optional()));

        // Coworkee: enums as strings, const objects instead of enums (erasableSyntaxOnly), one flat folder, aliases for ids and dates
        Typescript(t => t
            .OutputPath("ts-const")
            .Preset(TypescriptPreset.Coworkee)
            .EnumStyle(TypescriptEnumStyle.Const)
            .Layout(TypescriptLayout.Flat)
            .RootNamespace("Sample.Contracts")
            .TypeAliases()
            .IncludeDerivedFrom(typeof(EntityDto<>))
            .Exclude<CustomerPage>()
            .Type<CustomerDto>(c => c.Name("Customer")));
    }
}
