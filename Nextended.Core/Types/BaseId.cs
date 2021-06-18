using System;
using System.Runtime.Serialization;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Interface für typisierte Ids
	/// </summary>
	public interface ITypeBasedId<out TIdType>
	{
		/// <summary>
		/// Die enthaltene echte (konkrete) Id
		/// </summary>
		TIdType Id { get;}
	}

	/// <summary>
	///  Interface für typisierte Ids
	/// </summary>
	public interface ITypeBasedId : ITypeBasedId<object>
	{ }
	
	/// <summary>
	/// Zusammenfassung für BaseId.
	/// </summary>
	[DataContract]
	public abstract class BaseId<T, TIdType> : IEquatable<T>, ITypeBasedId<TIdType>
		where T : BaseId<T, TIdType>
	{
        /// <summary>
        /// Protected BaseID Konstruktor
        /// </summary>
		protected BaseId()
		{}

		/// <summary>
		/// Konstruktor
		/// </summary>
		protected BaseId(TIdType id)
		{
			if (typeof(TIdType) == typeof(Guid))
			{
				Guid localId = (Guid)(object)id;
				if (localId.Equals(Guid.Empty))
					throw new ArgumentException("Argument of type GUID must not be EMPTY");
			}
			if (typeof (TIdType) == typeof (int))
			{
				int localId = (int) (object) id;
				if (localId < 0)
					throw new ArgumentException("Argument of type INT must be >= 0");
			}

			Id = id;
		}

		/// <summary>
		/// Identifier
		/// </summary>
		[DataMember]
		public TIdType Id { get; private set; }

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return Id.ToString();
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as T);
		}

		/// <summary>
		/// Equalses the specified other.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(T other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id.Equals(other.Id);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		/// <summary>
		/// ==s the specified id1.
		/// </summary>
		/// <param name="id1">The id1.</param>
		/// <param name="id2">The id2.</param>
		/// <returns></returns>
		public static bool operator ==(BaseId<T, TIdType> id1, BaseId<T, TIdType> id2)
		{
			if ((object)id1 == null)
			{
				if ((object)id2 == null)
					return true;
				return false;
			}
			return id1.Equals(id2);
		}

		/// <summary>
		/// !=s the specified id1.
		/// </summary>
		/// <param name="id1">The id1.</param>
		/// <param name="id2">The id2.</param>
		/// <returns></returns>
		public static bool operator !=(BaseId<T, TIdType> id1, BaseId<T, TIdType> id2)
		{
			return !(id1 == id2);
		}

		/// <summary>
		/// Ts the specified id.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public static implicit operator TIdType(BaseId<T, TIdType> id)
		{
			return id.Id;
		}
	}
}