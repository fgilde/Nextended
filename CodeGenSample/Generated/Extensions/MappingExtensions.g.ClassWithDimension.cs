using Nextended.Core.Extensions;
/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.ClassWithDimension src, MyGenerated.Code.Test.ClassWithDimensionDto dest) 
		{
			if (src == null || dest == null) return;
			dest.Name = src.Name;
			dest.Dimension = src.Dimension.MapTo<MyGenerated.Code.Test.DimensionDto<MyGenerated.Code.Test.SuperUnitDto>>();
		}
		public static void AssignTo(this MyGenerated.Code.Test.ClassWithDimensionDto src, CodeGenSample.Entities.ClassWithDimension dest) 
		{
			if (src == null || dest == null) return;
			dest.Name = src.Name;
			dest.Dimension = src.Dimension.MapTo<CodeGenSample.Entities.Dimension<global::CodeGenSample.Entities.SuperUnit>>();
		}
		public static MyGenerated.Code.Test.ClassWithDimensionDto ToDto(this CodeGenSample.Entities.ClassWithDimension src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.ClassWithDimensionDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.ClassWithDimension ToNet(this MyGenerated.Code.Test.ClassWithDimensionDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.ClassWithDimension();
			src.AssignTo(result);
			return result;
		}
}
	}
