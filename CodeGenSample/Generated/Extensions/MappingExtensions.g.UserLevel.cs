/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:13:11) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static ENUMS.UserLevelDto? ToDto(this CodeGenSample.Entities.Enums.UserLevel? src) => src.HasValue ? (ENUMS.UserLevelDto)(int)src.Value : null;
		public static CodeGenSample.Entities.Enums.UserLevel? AsEntity(this ENUMS.UserLevelDto? src) => src.HasValue ? (CodeGenSample.Entities.Enums.UserLevel)(int)src.Value : null;
		public static ENUMS.UserLevelDto ToDto(this CodeGenSample.Entities.Enums.UserLevel src) => (ENUMS.UserLevelDto)(int)src;
		public static CodeGenSample.Entities.Enums.UserLevel AsEntity(this ENUMS.UserLevelDto src) => (CodeGenSample.Entities.Enums.UserLevel)(int)src;
}
	}
