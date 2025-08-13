using System;
using System.Runtime.InteropServices;
namespace ENUMS {
	[ComVisible(true)]
	[Guid(MyGenerated.Code.Test.ComGuids.IdUserLevelDto)]
	public enum UserLevelDto
	{
		[DispId(1)]
		Guest = 0,
		[DispId(2)]
		User = 1,
		[DispId(3)]
		Admin = 2,
	}


}
