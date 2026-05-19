namespace Nextended.EF.Tests.Domain;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public decimal Price { get; set; }
}
