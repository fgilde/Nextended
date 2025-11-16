/// <summary>
/// --- AUTO GENERATED CODE (16.11.2025 17:12:44) ---
/// --- HubBaseGuidEntityDto.g.cs ---
/// </summary>

using System;
namespace MyGenerated.Code.Test {
	#region Dto class for HubBaseGuidEntity

	/// <summary>IHubBaseGuidEntityDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Base.HubBaseGuidEntity"/></summary>
	public partial interface IHubBaseGuidEntityDto  
	{
		string XName { get; set; }
		CodeGenSample.Entities.Base.HubBaseSubClass HubBaseSubClass { get; set; }
		System.Guid Id { get; set; }
		string OwnerId { get; set; }
	}


	/// <summary>HubBaseGuidEntityDto - GENERATED FROM <see cref="T:CodeGenSample.Entities.Base.HubBaseGuidEntity"/></summary>
	public partial class HubBaseGuidEntityDto : IHubBaseGuidEntityDto 
	{
		public string XName { get; set; }
		public CodeGenSample.Entities.Base.HubBaseSubClass HubBaseSubClass { get; set; }
		public System.Guid Id { get; set; }
		public string OwnerId { get; set; }
	}


	#endregion Dto class for HubBaseGuidEntity

}
