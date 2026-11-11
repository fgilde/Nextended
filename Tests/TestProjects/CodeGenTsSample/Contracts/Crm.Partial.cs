namespace Sample.Contracts.Crm;

// the other half of a partial DTO: its members are in the TypeScript too
public partial class CustomerDto
{
    public string? Email { get; set; }
    public IList<string> Phones { get; set; } = [];
}
