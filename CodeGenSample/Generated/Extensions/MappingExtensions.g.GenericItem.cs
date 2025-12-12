/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		internal static void AssignTo<T, T2>(this CodeGenSample.Entities.GenericItem<T, T2> src, MyGenerated.Code.Test.GenericItemDto<T, T2> dest) where T : class where T2 : struct
		{
			if (src == null || dest == null) return;
			dest.Value = src.Value;
			dest.Id = src.Id;
		}
		internal static void AssignTo<T, T2>(this MyGenerated.Code.Test.GenericItemDto<T, T2> src, CodeGenSample.Entities.GenericItem<T, T2> dest) where T : class where T2 : struct
		{
			if (src == null || dest == null) return;
			dest.Value = src.Value;
			dest.Id = src.Id;
		}
		internal static MyGenerated.Code.Test.GenericItemDto<T, T2> ToDto<T, T2>(this CodeGenSample.Entities.GenericItem<T, T2> src) where T : class where T2 : struct
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.GenericItemDto<T, T2>();
			src.AssignTo<T, T2>(result);
			return result;
		}
		internal static CodeGenSample.Entities.GenericItem<T, T2> ToNet<T, T2>(this MyGenerated.Code.Test.GenericItemDto<T, T2> src) where T : class where T2 : struct
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.GenericItem<T, T2>();
			src.AssignTo<T, T2>(result);
			return result;
		}
}
	}
