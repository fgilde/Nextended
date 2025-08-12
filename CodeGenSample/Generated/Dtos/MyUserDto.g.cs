/// <summary>
/// --- AUTO GENERATED CODE (12.08.2025 11:40:30) ---
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
		CodeGenSample.Entities.Address? AnotherAddress { get; }
		string XyZ { get; }
		ENUMS.UserLevelDto? UserLevel { get; set; }
		CodeGenSample.Entities.OtherInfos OtherInfos { get; }
	}


	/// <summary>MyUserDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.User"/></summary>
	[System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
	public partial class MyUserDto :  MyGenerated.Code.Test.EntityBaseDto, IMyUserDto 
	{
		public string Name { get; set; }
		public MyGeneration.AddressDto ThatUserAddress { get; set; }
		public CodeGenSample.Entities.Address? AnotherAddress { get; set; }
		public string XyZ { get; set; }
		public ENUMS.UserLevelDto? UserLevel { get; set; }
		public CodeGenSample.Entities.OtherInfos OtherInfos { get; set; }
		MyGeneration.IAddressDto IMyUserDto.ThatUserAddress { get => ThatUserAddress; set => ThatUserAddress = (MyGeneration.AddressDto)value; }
	}


	#endregion Dto class for User

}
