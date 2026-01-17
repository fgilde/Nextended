/// <summary>
/// --- AUTO GENERATED CODE (17.01.2026 23:55:55) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.Base.Shipment src, MyGenerated.Code.Test.ShipmentDto dest) 
		{
			if (src == null || dest == null) return;
			((CodeGenSample.Entities.Base.HubBaseGuidEntity)src).AssignTo(( HubBaseGuidEntityDto )dest);
			dest.TrackingNumber = src.TrackingNumber;
			dest.ShippedDate = src.ShippedDate;
			dest.DeliveredDate = src.DeliveredDate;
			dest.OrderId = src.OrderId;
		}
		public static void AssignTo(this MyGenerated.Code.Test.ShipmentDto src, CodeGenSample.Entities.Base.Shipment dest) 
		{
			if (src == null || dest == null) return;
			((HubBaseGuidEntityDto)src).AssignTo(( CodeGenSample.Entities.Base.HubBaseGuidEntity )dest);
			dest.TrackingNumber = src.TrackingNumber;
			dest.ShippedDate = src.ShippedDate;
			dest.DeliveredDate = src.DeliveredDate;
			dest.OrderId = src.OrderId;
		}
		public static MyGenerated.Code.Test.ShipmentDto ToDto(this CodeGenSample.Entities.Base.Shipment src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.ShipmentDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.Base.Shipment ToNet(this MyGenerated.Code.Test.ShipmentDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.Base.Shipment();
			src.AssignTo(result);
			return result;
		}
}
	}
