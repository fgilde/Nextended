using System;
using System.Runtime.InteropServices;
namespace MyGeneration {
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
		[DispId(4)]
		int Number { get; set; }
	}


	[ComVisible(true)]
	[Guid(MyGenerated.Code.Test.ComGuids.IdAddressDto)]
	public partial class AddressDto :  MyGenerated.Code.Test.EntityBaseDto, IAddressDto 
	{
		public string Street { get; set; }
		public string City { get; set; }
		public string Country { get; set; }
		public int Number { get; set; }
	}


}
