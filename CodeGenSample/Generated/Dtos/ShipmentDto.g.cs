/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:13:11) ---
/// --- ShipmentDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for Shipment

	/// <summary>IShipmentDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Base.Shipment"/></summary>
	public partial interface IShipmentDto : MyGenerated.Code.Test.IHubBaseGuidEntityDto 
	{
		string TrackingNumber { get; set; }
		System.DateTime ShippedDate { get; set; }
		System.DateTime? DeliveredDate { get; set; }
		System.Guid OrderId { get; set; }
	}


	/// <summary>ShipmentDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Base.Shipment"/></summary>
	public partial class ShipmentDto :  MyGenerated.Code.Test.HubBaseGuidEntityDto, IShipmentDto 
	{
		public string TrackingNumber { get; set; }
		public System.DateTime ShippedDate { get; set; }
		public System.DateTime? DeliveredDate { get; set; }
		public System.Guid OrderId { get; set; }
	}


	#endregion Dto class for Shipment

}
