/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:17:58) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.Base.HubBaseGuidEntity src, MyGenerated.Code.Test.HubBaseGuidEntityDto dest) 
		{
			if (src == null || dest == null) return;
			dest.XName = src.XName;
			dest.HubBaseSubClass = src.HubBaseSubClass;
			dest.Id = src.Id;
			dest.OwnerId = src.OwnerId;
		}
		public static void AssignTo(this MyGenerated.Code.Test.HubBaseGuidEntityDto src, CodeGenSample.Entities.Base.HubBaseGuidEntity dest) 
		{
			if (src == null || dest == null) return;
			dest.XName = src.XName;
			dest.HubBaseSubClass = src.HubBaseSubClass;
			// skipped 'Id': destination not publicly writable
			dest.OwnerId = src.OwnerId;
		}
		public static MyGenerated.Code.Test.HubBaseGuidEntityDto ToDto(this CodeGenSample.Entities.Base.HubBaseGuidEntity src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.HubBaseGuidEntityDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.Base.HubBaseGuidEntity ToNet(this MyGenerated.Code.Test.HubBaseGuidEntityDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.Base.HubBaseGuidEntity();
			src.AssignTo(result);
			return result;
		}
}
	}
