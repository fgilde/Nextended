using System;
namespace MyGenerated.Code.Test {
	public partial interface IDerivedGenericClassDto<T> : MyGenerated.Code.Test.IGenericBaseClassDto<T> 
	{
		string Name { get; set; }
	}


	public partial class DerivedGenericClassDto<T> :  MyGenerated.Code.Test.GenericBaseClassDto<T>, IDerivedGenericClassDto<T> 
	{
		public string Name { get; set; }
	}


}
