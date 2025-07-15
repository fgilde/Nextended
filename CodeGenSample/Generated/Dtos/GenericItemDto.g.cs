/// <summary>
/// --- AUTO GENERATED CODE (15.07.2025 10:47:02) ---
/// --- GenericItemDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for GenericItem

	/// <summary>IGenericItemDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.GenericItem<T, T2>"/></summary>
	public partial interface IGenericItemDto<T, T2> :CodeGenSample.Entities.Base.IItem where T : class where T2 : struct
	{
		T Value { get; set; }
		T2 Id { get; set; }
	}


	/// <summary>GenericItemDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.GenericItem<T, T2>"/></summary>
	internal partial class GenericItemDto<T, T2> : IGenericItemDto<T, T2> where T : class where T2 : struct
	{
		public T Value { get; set; }
		public T2 Id { get; set; }
	}


	#endregion Dto class for GenericItem

}
