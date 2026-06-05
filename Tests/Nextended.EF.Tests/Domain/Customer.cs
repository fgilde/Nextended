using System.Collections.Generic;
using Nextended.Core.Attributes;

namespace Nextended.EF.Tests.Domain;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public decimal CreditLimit { get; set; }

    [IncludeInDetails]
    public virtual Address? Address { get; set; }

    [IncludeInDetails]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
