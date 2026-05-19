using System;
using System.Collections.Generic;
using Nextended.Core.Attributes;

namespace Nextended.EF.Tests.Domain;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public decimal TotalCost { get; set; }

    public int CustomerId { get; set; }

    [IncludeInDetails]
    public virtual Customer? Customer { get; set; }

    [IncludeInDetails]
    public virtual ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
