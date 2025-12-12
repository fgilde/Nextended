/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.SuperUnit src, MyGenerated.Code.Test.SuperUnitDto dest) 
		{
			if (src == null || dest == null) return;
			((CodeGenSample.Entities.Unit)src).AssignTo(( UnitDto )dest);
		}
		public static void AssignTo(this MyGenerated.Code.Test.SuperUnitDto src, CodeGenSample.Entities.SuperUnit dest) 
		{
			if (src == null || dest == null) return;
			((UnitDto)src).AssignTo(( CodeGenSample.Entities.Unit )dest);
		}
		public static MyGenerated.Code.Test.SuperUnitDto ToDto(this CodeGenSample.Entities.SuperUnit src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.SuperUnitDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.SuperUnit ToNet(this MyGenerated.Code.Test.SuperUnitDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.SuperUnit();
			src.AssignTo(result);
			return result;
		}
}
	}
