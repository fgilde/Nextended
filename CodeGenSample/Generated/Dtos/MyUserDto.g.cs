/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:17:58) ---
/// --- MyUserDto.g.cs ---
/// </summary>

using System;
using CodeGenSample.Entities;
namespace MyGenerated.Code.Test {
	#region Dto class for User

	/// <summary>IMyUserDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.User"/></summary>
	public partial interface IMyUserDto : MyGenerated.Code.Test.IEntityBaseDto 
	{
		[System.ComponentModel.DataAnnotations.MaxLength(3)]
		string Name { get; }
		MyGeneration.IAddressDto ThatUserAddress { get; set; }
		MyGeneration.IAddressDto? AnotherAddress { get; }
		System.Collections.Generic.List<MyGeneration.AddressDto> AdditionalAddresses { get; }
		string XyZ { get; }
		ENUMS.UserLevelDto? UserLevel { get; set; }
		CodeGenSample.Entities.OtherInfos OtherInfos { get; }
		System.DateTimeOffset? LastOnline { get; }
		System.DateTime Birthday { get; }
		System.DateTime? OtherDate { get; }
	}


	/// <summary>MyUserDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.User"/></summary>
	[System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
	public partial class MyUserDto :  MyGenerated.Code.Test.EntityBaseDto, IMyUserDto 
	{
		public string Name { get; set; }
		public MyGeneration.AddressDto ThatUserAddress { get; set; }
		public MyGeneration.AddressDto? AnotherAddress { get; set; }
		public System.Collections.Generic.List<MyGeneration.AddressDto> AdditionalAddresses { get; set; }
		public string XyZ { get; set; }
		public ENUMS.UserLevelDto? UserLevel { get; set; }
		public CodeGenSample.Entities.OtherInfos OtherInfos { get; set; }
		public System.DateTimeOffset? LastOnline { get; set; }
		public System.DateTime Birthday { get; set; }
		public System.DateTime? OtherDate { get; set; }
		MyGeneration.IAddressDto IMyUserDto.ThatUserAddress { get => ThatUserAddress; set => ThatUserAddress = (MyGeneration.AddressDto)value; }
		MyGeneration.IAddressDto IMyUserDto.AnotherAddress { get => AnotherAddress; }
	}


	#endregion Dto class for User

}
