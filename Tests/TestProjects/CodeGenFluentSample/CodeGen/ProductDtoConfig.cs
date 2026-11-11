using FluentSample.Domain;
using Nextended.Core.CodeGen;

namespace FluentSample.CodeGen;

/// <summary>The DTOs of one entity, like a ResponseFilter&lt;T&gt; or a FluentValidation validator.</summary>
public sealed class ProductDtoConfig : DtoConfig<Product>
{
    public ProductDtoConfig()
    {
        Ignore(x => x.InternalNote);
        Property(x => x.Sku).PropertyName("ArticleNumber");

        // a second DTO of the same entity for lists: flat, base members first, then Name and Price, without the heavy members
        View("ProductRow", v => v
            .Ignore(x => x.Category, x => x.Tags, x => x.UpdatedUtc)
            .BaseMembersFirst()
            .Order(x => x.Name, x => x.Price)
            .ToDtoMethodName("ToRow"));
    }
}
