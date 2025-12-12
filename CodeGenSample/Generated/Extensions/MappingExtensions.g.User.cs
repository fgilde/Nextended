using Nextended.Core.Extensions;
/// <summary>
/// --- AUTO GENERATED CODE (12.12.2025 14:13:11) ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		public static void AssignTo(this CodeGenSample.Entities.User src, MyGenerated.Code.Test.MyUserDto dest) 
		{
			if (src == null || dest == null) return;
			((CodeGenSample.Entities.Base.EntityBase)src).AssignTo(( EntityBaseDto )dest);
			dest.Name = src.Name;
			dest.ThatUserAddress = src.Address.MapTo<MyGeneration.AddressDto>();
			dest.AnotherAddress = src.AnotherAddress?.ToMegaDto();
			dest.AdditionalAddresses = src.AdditionalAddresses != null ? System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(src.AdditionalAddresses, x => x?.ToMegaDto())) : null;
			dest.XyZ = src.XyZ;
			dest.UserLevel = src.Level?.ToDto();
			dest.OtherInfos = src.OtherInfos;
			dest.LastOnline = src.LastOnline;
			dest.Birthday = src.Birthday;
			dest.OtherDate = src.OtherDate;
		}
		public static void AssignTo(this MyGenerated.Code.Test.MyUserDto src, CodeGenSample.Entities.User dest) 
		{
			if (src == null || dest == null) return;
			((EntityBaseDto)src).AssignTo(( CodeGenSample.Entities.Base.EntityBase )dest);
			dest.Name = src.Name;
			dest.Address = src.ThatUserAddress.MapTo<global::CodeGenSample.Entities.Address>();
			dest.AnotherAddress = src.AnotherAddress?.AsSrc();
			dest.AdditionalAddresses = src.AdditionalAddresses != null ? System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(src.AdditionalAddresses, x => x?.AsSrc())) : null;
			dest.XyZ = src.XyZ;
			dest.Level = src.UserLevel?.AsEntity();
			dest.OtherInfos = src.OtherInfos;
			dest.LastOnline = src.LastOnline;
			dest.Birthday = src.Birthday;
			dest.OtherDate = src.OtherDate;
		}
		public static MyGenerated.Code.Test.MyUserDto ToMyDto(this CodeGenSample.Entities.User src) 
		{
			if (src == null) return null;
			var result = new MyGenerated.Code.Test.MyUserDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.User ToNet(this MyGenerated.Code.Test.MyUserDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.User();
			src.AssignTo(result);
			return result;
		}
}
	}
