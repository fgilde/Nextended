using System.Collections.Generic;

namespace Nextended.ResponseFilters.Tests.TestSupport;

public class OrderDto
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public string? CreditCard { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal Subtotal { get; set; }
    public double Score { get; set; }
    public bool IsActive { get; set; }
    public CustomerDto? Customer { get; set; }
    public List<LineItemDto>? Lines { get; set; }
    public string[]? Tags { get; set; }
}

public class CustomerDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public decimal? CreditLimit { get; set; }
}

public class LineItemDto
{
    public string? Sku { get; set; }
    public decimal? UnitCost { get; set; }
    public int Quantity { get; set; }
}

public class CyclicDto
{
    public string? Name { get; set; }
    public CyclicDto? Self { get; set; }
}

public class IndexerDto
{
    private readonly Dictionary<string, string> _bag = new();
    public string? Name { get; set; }
    public string this[string key]
    {
        get => _bag[key];
        set => _bag[key] = value;
    }
}
