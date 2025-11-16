/// <summary>
/// --- AUTO GENERATED CODE (16.11.2025 17:12:44) ---
/// --- DerivedGenericClassDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for DerivedGenericClass

	/// <summary>IDerivedGenericClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.DerivedGenericClass<T>"/></summary>
	public partial interface IDerivedGenericClassDto<T> : MyGenerated.Code.Test.IGenericBaseClassDto<T> 
	{
		string Name { get; set; }
	}


	/// <summary>DerivedGenericClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.DerivedGenericClass<T>"/></summary>
	public partial class DerivedGenericClassDto<T> :  MyGenerated.Code.Test.GenericBaseClassDto<T>, IDerivedGenericClassDto<T> 
	{
		public string Name { get; set; }
	}


	#endregion Dto class for DerivedGenericClass

}
