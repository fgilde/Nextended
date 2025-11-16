/// <summary>
/// --- AUTO GENERATED CODE (16.11.2025 17:22:27) ---
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
