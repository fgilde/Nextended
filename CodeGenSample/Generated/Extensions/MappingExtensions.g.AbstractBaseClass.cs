/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.AbstractBaseClass src, MyGenerated.Code.Test.AbstractBaseClassDto dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
		}
		public static void AssignTo(this MyGenerated.Code.Test.AbstractBaseClassDto src, CodeGenSample.Entities.AbstractBaseClass dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
		}
		public static MyGenerated.Code.Test.AbstractBaseClassDto ToDto(this CodeGenSample.Entities.AbstractBaseClass src, Func<MyGenerated.Code.Test.AbstractBaseClassDto> factory) 
		{
			if (src == null) return null;
			var result = factory != null ? factory() : null;
			if (result == null) return null;
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.AbstractBaseClass ToNet(this MyGenerated.Code.Test.AbstractBaseClassDto src, Func<CodeGenSample.Entities.AbstractBaseClass> factory) 
		{
			if (src == null) return null;
			var result = factory != null ? factory() : null;
			if (result == null) return null;
			src.AssignTo(result);
			return result;
		}
}
	}
