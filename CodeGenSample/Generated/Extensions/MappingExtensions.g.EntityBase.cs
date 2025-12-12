/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:15:37) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.Base.EntityBase src, MyGenerated.Code.Test.EntityBaseDto dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
			dest.ReferenceKey = src.ReferenceKey;
		}
		public static void AssignTo(this MyGenerated.Code.Test.EntityBaseDto src, CodeGenSample.Entities.Base.EntityBase dest) 
		{
			if (src == null || dest == null) return;
			dest.Id = src.Id;
			dest.ReferenceKey = src.ReferenceKey;
		}
		public static MyGenerated.Code.Test.EntityBaseDto ToDto(this CodeGenSample.Entities.Base.EntityBase src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.EntityBaseDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.Base.EntityBase ToNet(this MyGenerated.Code.Test.EntityBaseDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.Base.EntityBase();
			src.AssignTo(result);
			return result;
		}
}
	}
