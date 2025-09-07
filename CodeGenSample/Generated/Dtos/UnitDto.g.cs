using System;
namespace MyGenerated.Code.Test {
	#region Dto class for Unit

	/// <summary>IUnitDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Unit"/></summary>
	public partial interface IUnitDto  
	{
		decimal? BaseUnitFactor { get; set; }
		decimal? NullPointShift { get; set; }
	}


	/// <summary>UnitDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Unit"/></summary>
	public partial class UnitDto : IUnitDto 
	{
		public decimal? BaseUnitFactor { get; set; }
		public decimal? NullPointShift { get; set; }
	}


	#endregion Dto class for Unit

}
