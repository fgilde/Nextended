using System;
namespace MyGenerated.Code.Test {
	public static partial class MappingExtensions
	{
		static partial void BeforeAssignTo(CodeGenSample.Entities.Address src, MyGeneration.AddressDto dest) ;
		static partial void AfterAssignTo(CodeGenSample.Entities.Address src, MyGeneration.AddressDto dest) ;
		public static void AssignTo(this CodeGenSample.Entities.Address src, MyGeneration.AddressDto dest) 
		{
			if (src == null || dest == null) return;
			BeforeAssignTo(src, dest);
			((CodeGenSample.Entities.Base.EntityBase)src).AssignTo(( EntityBaseDto )dest);
			dest.Street = src.Street;
			dest.City = src.City;
			dest.Country = src.Country;
			dest.Number = src.Number;
			AfterAssignTo(src, dest);
		}
		static partial void BeforeAssignTo(MyGeneration.AddressDto src, CodeGenSample.Entities.Address dest) ;
		static partial void AfterAssignTo(MyGeneration.AddressDto src, CodeGenSample.Entities.Address dest) ;
		public static void AssignTo(this MyGeneration.AddressDto src, CodeGenSample.Entities.Address dest) 
		{
			if (src == null || dest == null) return;
			BeforeAssignTo(src, dest);
			((EntityBaseDto)src).AssignTo(( CodeGenSample.Entities.Base.EntityBase )dest);
			dest.Street = src.Street;
			dest.City = src.City;
			dest.Country = src.Country;
			dest.Number = src.Number;
			AfterAssignTo(src, dest);
		}
		public static MyGeneration.AddressDto ToMegaDto(this CodeGenSample.Entities.Address src) 
		{
			if (src == null) return null;
			var result = new MyGeneration.AddressDto();
			src.AssignTo(result);
			return result;
		}
		public static CodeGenSample.Entities.Address AsSrc(this MyGeneration.AddressDto src) 
		{
			if (src == null) return null;
			var result = new CodeGenSample.Entities.Address();
			src.AssignTo(result);
			return result;
		}
}
	}
