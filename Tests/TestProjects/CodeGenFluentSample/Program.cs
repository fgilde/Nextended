using FluentSample.Contracts;
using FluentSample.Domain;

var product = new Product
{
    Id = Guid.NewGuid(),
    TenantId = Guid.NewGuid(),
    Sku = "A-100",
    Name = "Chair",
    Price = 49.9m,
    State = ProductState.Active,
    Category = new Category { Name = "Furniture" }
};

ProductDto dto = product.ToDto();       // default DTO: ArticleNumber, no TenantId, no InternalNote
ProductRowDto row = product.ToRow();    // the view

Console.WriteLine($"{dto.ArticleNumber} {dto.Name} {dto.Category?.Name} {dto.State}");
Console.WriteLine($"{row.Id} {row.Name} {row.Price}");
