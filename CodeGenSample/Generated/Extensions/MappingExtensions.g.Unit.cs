/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:13:11) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.Unit src, MyGenerated.Code.Test.UnitDto dest) 
		{
			if (src == null || dest == null) return;
			dest.BaseUnitFactor = src.BaseUnitFactor;
			dest.NullPointShift = src.NullPointShift;
		}
		public static void AssignTo(this MyGenerated.Code.Test.UnitDto src, CodeGenSample.Entities.Unit dest) 
		{
			if (src == null || dest == null) return;
			dest.BaseUnitFactor = src.BaseUnitFactor;
			dest.NullPointShift = src.NullPointShift;
		}
		public static MyGenerated.Code.Test.UnitDto ToDto(this CodeGenSample.Entities.Unit src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.UnitDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.Unit ToNet(this MyGenerated.Code.Test.UnitDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.Unit();
			src.AssignTo(result);
			return result;
		}
}
	}
