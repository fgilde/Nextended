/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:13:11) ---
/// --- ClassWithDimensionDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for ClassWithDimension

	/// <summary>IClassWithDimensionDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.ClassWithDimension"/></summary>
	public partial interface IClassWithDimensionDto  
	{
		string Name { get; set; }
		MyGenerated.Code.Test.DimensionDto<MyGenerated.Code.Test.SuperUnitDto> Dimension { get; set; }
	}


	/// <summary>ClassWithDimensionDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.ClassWithDimension"/></summary>
	public partial class ClassWithDimensionDto : IClassWithDimensionDto 
	{
		public string Name { get; set; }
		public MyGenerated.Code.Test.DimensionDto<MyGenerated.Code.Test.SuperUnitDto> Dimension { get; set; }
	}


	#endregion Dto class for ClassWithDimension

}
