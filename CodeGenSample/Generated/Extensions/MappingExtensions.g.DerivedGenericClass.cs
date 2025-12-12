/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo<T>(this CodeGenSample.Entities.DerivedGenericClass<T> src, MyGenerated.Code.Test.DerivedGenericClassDto<T> dest) 
		{
			if (src == null || dest == null) return;
			((CodeGenSample.Entities.GenericBaseClass<T>)src).AssignTo<T>(( GenericBaseClassDto<T> )dest);
			dest.Name = src.Name;
		}
		public static void AssignTo<T>(this MyGenerated.Code.Test.DerivedGenericClassDto<T> src, CodeGenSample.Entities.DerivedGenericClass<T> dest) 
		{
			if (src == null || dest == null) return;
			((GenericBaseClassDto<T>)src).AssignTo<T>(( CodeGenSample.Entities.GenericBaseClass<T> )dest);
			dest.Name = src.Name;
		}
		public static MyGenerated.Code.Test.DerivedGenericClassDto<T> ToDto<T>(this CodeGenSample.Entities.DerivedGenericClass<T> src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.DerivedGenericClassDto<T>();
			src.AssignTo<T>(result);
			return result;
		}
		public static CodeGenSample.Entities.DerivedGenericClass<T> ToNet<T>(this MyGenerated.Code.Test.DerivedGenericClassDto<T> src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.DerivedGenericClass<T>();
			src.AssignTo<T>(result);
			return result;
		}
}
	}
