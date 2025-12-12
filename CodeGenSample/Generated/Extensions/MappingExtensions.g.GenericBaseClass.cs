/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:17:58) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo<T>(this CodeGenSample.Entities.GenericBaseClass<T> src, MyGenerated.Code.Test.GenericBaseClassDto<T> dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
		}
		public static void AssignTo<T>(this MyGenerated.Code.Test.GenericBaseClassDto<T> src, CodeGenSample.Entities.GenericBaseClass<T> dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
		}
		public static MyGenerated.Code.Test.GenericBaseClassDto<T> ToDto<T>(this CodeGenSample.Entities.GenericBaseClass<T> src, Func<MyGenerated.Code.Test.GenericBaseClassDto<T>> factory) 
		{
			if (src == null) return null;
			var result = factory != null ? factory() : null;
			if (result == null) return null;
			src.AssignTo<T>(result);
			return result;
		}
		public static CodeGenSample.Entities.GenericBaseClass<T> ToNet<T>(this MyGenerated.Code.Test.GenericBaseClassDto<T> src, Func<CodeGenSample.Entities.GenericBaseClass<T>> factory) 
		{
			if (src == null) return null;
			var result = factory != null ? factory() : null;
			if (result == null) return null;
			src.AssignTo<T>(result);
			return result;
		}
}
	}
