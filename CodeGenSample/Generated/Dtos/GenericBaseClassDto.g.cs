using System;
namespace MyGenerated.Code.Test {
	public partial interface IGenericBaseClassDto<T>  
	{
		T Id { get; set; }
	}


	public abstract partial class GenericBaseClassDto<T> : IGenericBaseClassDto<T> 
	{
		public T Id { get; set; }
	}


}
