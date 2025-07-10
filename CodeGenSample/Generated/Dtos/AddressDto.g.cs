/// <summary>
/// --- AUTO GENERATED CODE (10.07.2025 12:59:35) ---
/// --- AddressDto.g.cs ---
/// </summary>

using System;
using System.Runtime.InteropServices;
namespace MyGeneration {
	#region COM class for Address

	/// <summary>IAddressDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Address"/></summary>
	[ComVisible(true)]
	[Guid(MyGenerated.Code.Test.ComGuids.IdAddressDto)]
	[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)]
	public partial interface IAddressDto : MyGenerated.Code.Test.IEntityBaseDto 
	{
		[DispId(1)]
		string Street { get; set; }
		[DispId(2)]
		string City { get; set; }
		[DispId(3)]
		string Country { get; set; }
	}


	/// <summary>AddressDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Address"/></summary>
	[ComVisible(true)]
	[Guid(MyGenerated.Code.Test.ComGuids.IdAddressDto)]
	public partial class AddressDto :  MyGenerated.Code.Test.EntityBaseDto, IAddressDto 
	{
		public string Street { get; set; }
		public string City { get; set; }
		public string Country { get; set; }
	}


	#endregion COM class for Address

}
