/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.DerivedClass src, MyGenerated.Code.Test.DerivedClassDto dest) 
		{
			if (src == null || dest == null) return;
			((CodeGenSample.Entities.AbstractBaseClass)src).AssignTo(( AbstractBaseClassDto )dest);
			dest.Name = src.Name;
		}
		public static void AssignTo(this MyGenerated.Code.Test.DerivedClassDto src, CodeGenSample.Entities.DerivedClass dest) 
		{
			if (src == null || dest == null) return;
			((AbstractBaseClassDto)src).AssignTo(( CodeGenSample.Entities.AbstractBaseClass )dest);
			dest.Name = src.Name;
		}
		public static MyGenerated.Code.Test.DerivedClassDto ToDto(this CodeGenSample.Entities.DerivedClass src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.DerivedClassDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.DerivedClass ToNet(this MyGenerated.Code.Test.DerivedClassDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.DerivedClass();
			src.AssignTo(result);
			return result;
		}
}
	}
