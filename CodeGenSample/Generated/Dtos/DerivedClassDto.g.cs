/// <summary>
/// --- AUTO GENERATED CODE (18.08.2025 09:20:47) ---
/// --- DerivedClassDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for DerivedClass

	/// <summary>IDerivedClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.DerivedClass"/></summary>
	public partial interface IDerivedClassDto : MyGenerated.Code.Test.IAbstractBaseClassDto 
	{
		string Name { get; set; }
	}


	/// <summary>DerivedClassDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.DerivedClass"/></summary>
	public partial class DerivedClassDto :  MyGenerated.Code.Test.AbstractBaseClassDto, IDerivedClassDto 
	{
		public string Name { get; set; }
	}


	#endregion Dto class for DerivedClass

}
