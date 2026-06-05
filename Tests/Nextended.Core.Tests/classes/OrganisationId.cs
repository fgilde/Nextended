using System;
using Nextended.Core.Types;

namespace Nextended.Core.Tests.classes
{
	/// <summary>
	/// OrganisationId
	/// </summary>
	public class OrganisationId : BaseId<OrganisationId, Guid>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrganisationId" /> class.
		/// </summary>
		/// <param name="id">The id.</param>
		public OrganisationId(Guid id)
			: base(id)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrganisationId" /> class.
		/// </summary>
		/// <param name="id">The id.</param>
		public OrganisationId(string id)
			: base(new Guid(id))
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrganisationId" /> class.
		/// </summary>
		public static OrganisationId NewId()
		{
			return new OrganisationId(Guid.NewGuid());
		}
	}
}