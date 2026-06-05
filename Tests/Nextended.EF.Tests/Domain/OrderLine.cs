using Nextended.Core.Attributes;

namespace Nextended.EF.Tests.Domain;

public class OrderLine
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    public int ProductId { get; set; }

    [IncludeInDetails]
    public virtual Product? Product { get; set; }
}
