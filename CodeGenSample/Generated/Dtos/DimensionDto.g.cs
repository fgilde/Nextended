/// <summary>
/// --- AUTO GENERATED CODE (16.11.2025 17:12:44) ---
/// --- DimensionDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for Dimension

	/// <summary>IDimensionDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Dimension<TUnit>"/></summary>
	public partial interface IDimensionDto<TUnit>  where TUnit : UnitDto
	{
		decimal? Value { get; set; }
		TUnit? Unit { get; set; }
	}


	/// <summary>DimensionDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Dimension<TUnit>"/></summary>
	public partial class DimensionDto<TUnit> : IDimensionDto<TUnit> where TUnit : UnitDto
	{
		public decimal? Value { get; set; }
		public TUnit? Unit { get; set; }
	}


	#endregion Dto class for Dimension

}
