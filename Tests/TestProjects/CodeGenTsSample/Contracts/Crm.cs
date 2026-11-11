using Volo.Abp.Application.Dtos;

namespace Sample.Contracts.Crm;

// the same name as Sample.Contracts.Shipping.Address: another folder, imported under an alias where both meet
public class Address
{
    public string? Company { get; set; }
    public string? VatId { get; set; }
}

public partial class CustomerDto : EntityDto<Guid>
{
    public string Name { get; set; } = "";
    public Address? Billing { get; set; }
}

// a page of anything with an id: generic, with a constraint
public class Page<T> where T : EntityDto<Guid>
{
    public List<T> Items { get; set; } = [];
    public int? Skip { get; set; }
    public PagedResultDto<T>? Original { get; set; }
}

public class CustomerPage : Page<CustomerDto>
{
    public Dictionary<Guid, List<CustomerDto>> ByOwner { get; set; } = [];
}
