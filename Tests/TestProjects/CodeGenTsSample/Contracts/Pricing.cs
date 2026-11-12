using System.ComponentModel;

namespace Sample.Contracts.Pricing;

// no [JsonPolymorphic]: the union of the derived types is declared in the configuration (DerivedTypes)
public abstract class Place
{
    public Guid Id { get; set; }
}

public class AddressPlace : Place
{
    public string Street { get; set; } = "";
}

public class CountryPlace : Place
{
    public string IsoAlpha2Code { get; set; } = "";
}

public class PortPlace : Place
{
    public string UnLoCode { get; set; } = "";
}

public class MarginDto
{
    public decimal Rate { get; set; }
    public Place? Origin { get; set; }
    public Place? Destination { get; set; }
}

// a request: members with a default value may be left out by the client (OptionalDefaultValues)
public class MarginQueryDto
{
    public string? Filter { get; set; }
    public int MaxResultCount { get; set; } = 10;
    public string Sorting { get; set; } = "rate";
    [DefaultValue(false)] public bool IncludeDeleted { get; set; }
    public string Currency { get; set; } = null!;
    public int SkipCount { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PublicAttribute : Attribute;

// options with secrets: only the [Public] members reach the client (IncludeMembersWithAttribute)
public class PortalOptions
{
    [Public] public string Title { get; set; } = "";
    [Public] public string? LogoUrl { get; set; }
    public string ApiKey { get; set; } = "";
    public string ConnectionString { get; set; } = "";
}
