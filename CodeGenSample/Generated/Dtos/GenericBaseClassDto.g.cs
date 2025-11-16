/// <summary>
/// --- AUTO GENERATED CODE (16.11.2025 17:12:44) ---
/// --- GenericBaseClassDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for GenericBaseClass

	/// <summary>IGenericBaseClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.GenericBaseClass<T>"/></summary>
	public partial interface IGenericBaseClassDto<T>  
	{
		T Id { get; set; }
	}


	/// <summary>GenericBaseClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.GenericBaseClass<T>"/></summary>
	public abstract partial class GenericBaseClassDto<T> : IGenericBaseClassDto<T> 
	{
		public T Id { get; set; }
	}


	#endregion Dto class for GenericBaseClass

}
