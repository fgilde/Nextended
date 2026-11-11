using System.Text.Json;
using System.Text.Json.Serialization;
using Nextended.Core.Attributes;
using Volo.Abp.Application.Dtos;

namespace Sample.Contracts.Shipping;

public enum ShipmentState
{
    Draft = 0,
    Booked = 1,
    InTransit = 8,
    Delivered = 16,
    Completed = 128
}

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Push = 2,
    All = Email | Push
}

// written as names wherever it is used
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    Low,
    High,
    [JsonStringEnumMemberName("very-high")] VeryHigh
}

public class ShipmentDto : CreationAuditedEntityDto<Guid>
{
    public string Number { get; set; } = "";
    public string? HouseBill { get; set; }
    public ShipmentState State { get; set; }
    public ShipmentState? PreviousState { get; set; }

    // this one property is written as names
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShipmentState StateName { get; set; }

    public Priority Priority { get; set; }
    public NotificationChannel Channels { get; set; }
    public decimal? ChargeableWeight { get; set; }
    public long Revision { get; set; }
    public DateOnly? PickupDay { get; set; }
    public TimeSpan TransitTime { get; set; }
    public Address? Origin { get; set; }
    public Crm.Address? BillTo { get; set; }
    public List<ShipmentLeg> Legs { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public List<string?> References { get; set; } = [];
    public Dictionary<ShipmentState, int> CountsByState { get; set; } = [];
    public IReadOnlyDictionary<string, decimal> Charges { get; set; } = new Dictionary<string, decimal>();
    public Dictionary<int, string> Codes { get; set; } = [];
    public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; } = [];
    public JsonElement? Raw { get; set; }
    public byte[]? Thumbnail { get; set; }

    [JsonPropertyName("cw1")]
    public bool HasCw1Number { get; set; }

    [JsonIgnore]
    public string InternalNote { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Remark { get; set; }

    [TypescriptIgnore]
    public string ServerOnly { get; set; } = "";

    [TypescriptProperty(Type = "`S-${string}`")]
    public string Display { get; set; } = "";
}

public class Address
{
    public string? Line1 { get; set; }
    public string? City { get; set; }
    public string CountryIsoAlpha2Code { get; set; } = "";
    public Coordinates? Coordinates { get; set; }
}

public readonly record struct Coordinates(double Latitude, double Longitude);

// [JsonPolymorphic] → a discriminated union "ShipmentLeg" and a base interface "ShipmentLegBase"
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(PickupLeg), "ShipmentPickupLeg")]
[JsonDerivedType(typeof(ConsolLeg), "ConsolLeg")]
[JsonDerivedType(typeof(DeliveryLeg), "ShipmentDeliveryLeg")]
public abstract class ShipmentLeg
{
    public int LegNumber { get; set; }
    public Address? Start { get; set; }
    public Address? End { get; set; }
    public virtual string? Vehicle { get; set; }
}

public class PickupLeg : ShipmentLeg
{
    public DateTime? PickedUp { get; set; }
}

public class ConsolLeg : ShipmentLeg
{
    public string? Vessel { get; set; }
    public override string? Vehicle { get; set; }
}

public class DeliveryLeg : ShipmentLeg
{
    // hides the base member with another type: the interface extends Omit<ShipmentLegBase, 'end'>
    public new Crm.Address? End { get; set; }
}
