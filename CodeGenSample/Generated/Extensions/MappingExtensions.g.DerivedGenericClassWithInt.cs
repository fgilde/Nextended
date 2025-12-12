/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:17:58) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.DerivedGenericClassWithInt src, MyGenerated.Code.Test.DerivedGenericClassWithIntDto dest) 
		{
			if (src == null || dest == null) return;
		}
		public static void AssignTo(this MyGenerated.Code.Test.DerivedGenericClassWithIntDto src, CodeGenSample.Entities.DerivedGenericClassWithInt dest) 
		{
			if (src == null || dest == null) return;
		}
		public static MyGenerated.Code.Test.DerivedGenericClassWithIntDto ToDto(this CodeGenSample.Entities.DerivedGenericClassWithInt src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.DerivedGenericClassWithIntDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.DerivedGenericClassWithInt ToNet(this MyGenerated.Code.Test.DerivedGenericClassWithIntDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.DerivedGenericClassWithInt();
			src.AssignTo(result);
			return result;
		}
}
	}
