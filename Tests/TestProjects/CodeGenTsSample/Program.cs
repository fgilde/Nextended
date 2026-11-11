using System.Text.Json;
using Sample.Contracts.Shipping;

// the JSON the generated TypeScript in ts/ describes (ABP style: camelCase, enums as numbers)
var shipment = new ShipmentDto { Number = "S-1", State = ShipmentState.Booked };
Console.WriteLine(JsonSerializer.Serialize(shipment, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
