namespace FluentSample.Domain;

// Plain entities: no attribute of Nextended.Core. Everything about their DTOs is in CodeGen/.

public abstract class Entity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}

public class Product : Entity
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? InternalNote { get; set; }
    public ProductState State { get; set; }
    public Category? Category { get; set; }
    public List<string> Tags { get; set; } = [];
}

public class Category : Entity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public enum ProductState { Draft, Active, Discontinued, Legacy }
