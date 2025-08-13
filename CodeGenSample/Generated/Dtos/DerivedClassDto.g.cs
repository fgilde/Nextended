using System;
namespace MyGenerated.Code.Test {
	public partial interface IDerivedClassDto : MyGenerated.Code.Test.IAbstractBaseClassDto 
	{
		string Name { get; set; }
	}


	public partial class DerivedClassDto :  MyGenerated.Code.Test.AbstractBaseClassDto, IDerivedClassDto 
	{
		public string Name { get; set; }
	}


}
