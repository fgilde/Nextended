using System;
namespace MyGenerated.Code.Test {
	public partial interface IGenericItemDto<T, T2> :CodeGenSample.Entities.Base.IItem where T : class where T2 : struct
	{
		T Value { get; set; }
		T2 Id { get; set; }
	}


	internal partial class GenericItemDto<T, T2> : IGenericItemDto<T, T2> where T : class where T2 : struct
	{
		public T Value { get; set; }
		public T2 Id { get; set; }
	}


}
