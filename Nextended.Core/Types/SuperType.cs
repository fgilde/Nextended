using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Nextended.Core.Types
{
	/// <summary>
	/// SuperType base class for supertype pattern instead of enum
	/// </summary>
	[Serializable]
	public abstract class SuperType<T> : IEquatable<SuperType<T>>, IComparable<SuperType<T>>, IEquatable<T>
		where T : SuperType<T>
	{
		private readonly Type resourceType;

		/// <summary>
		/// Type
		/// </summary>
		protected readonly Type type;

		/// <summary>
		/// List of member
		/// </summary>
		protected static volatile HashSet<T> InnerList;

		private static readonly object lockObject = new object();


		/// <summary>
		/// Constructor
		/// </summary>
		protected SuperType(int id, string identifier, Type resourceType)
			: this(id, identifier, string.Empty)
		{
			// TypeResources
			this.resourceType = resourceType;
			Name = FormatName();
		}
		
		/// <summary>
		/// ResourceName
		/// </summary>
		/// <returns></returns>
		public string GetResourceName()
		{
			return $"{type.Name}_{Identifier}_Name";
		}

		/// <summary>
		/// Ctor
		/// </summary>
		protected SuperType(int id, string identifier, string name,
			string description = "")
		{
			Id = id;
			Identifier = identifier;

			type = GetType();
			while (type != null && type.BaseType != typeof(SuperType<T>))
				type = type.BaseType;

			if (!string.IsNullOrEmpty(name))
				Name = name;
			Description = description;
		}


		/// <summary>
		/// Id
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// Identifier
		/// </summary>
		public string Identifier { get; protected set; }

		/// <summary>
		/// Description
		/// </summary>
		public string Description { get; protected set; }

		/// <summary>
		/// Compares to.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public int CompareTo(SuperType<T> other)
		{
			Check.NotNull(() => other);
			if (other.GetType() != type)
				throw new ArgumentException(@"Can't compare different types", "other");
			return Id.CompareTo(other.Id);
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// List of all member
		/// </summary>
		public static T[] All
		{
			get
			{
				if (InnerList == null || InnerList.Count == 0)
				{
					lock (lockObject)
					{
						if (InnerList == null || InnerList.Count == 0)
						{
							InnerList = new HashSet<T>();
							Type type = typeof(T);
							MethodInfo addList = type.GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);
							if (addList != null)
								addList.Invoke(null, new object[] { });
							else
							{
								var list = new HashSet<T>();

								var properties = typeof(T).GetProperties();
								foreach (PropertyInfo property in
									properties.Where(property => property.PropertyType == typeof(T)))
								{
									if (property.CanRead)
									{
										try
										{
											var value = property.GetValue(null, null) as T;
											if (value != null)
												list.Add(value);
										}
										catch (Exception e)
										{
											Trace.TraceError(e.Message);
										}
									}
								}

								InnerList = list;
							}
						}
					}
				}
				return InnerList.ToArray();
			}
		}

		/// <summary>
		/// Indexer
		/// </summary>
		public static T Get(int id)
		{
			T find = All.SingleOrDefault(item => item.Id == id);

			if (find != null)
				return find;
			throw new ArgumentOutOfRangeException("id");
		}

		/// <summary>
		/// Identifier
		/// </summary>
		public static T Get(string identifier)
		{
			T find = All.SingleOrDefault(item => item.Identifier == identifier);

			if (find != null)
				return find;
			throw new ArgumentOutOfRangeException("identifier");
		}

		/// <summary>
		/// Equalses the specified other.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(T other)
		{
			return Equals(other as SuperType<T>);
		}

		/// <summary>
		/// Equalses the specified other.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(SuperType<T> other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.type == type && other.Id == Id;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(SuperType<T>)) return false;
			return Equals((SuperType<T>)obj);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (type.GetHashCode() * 397) ^ Id;
			}
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(SuperType<T> left, SuperType<T> right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(SuperType<T> left, SuperType<T> right)
		{
			return !Equals(left, right);
		}

		/// <summary>
		/// Equalses the specified other.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(int other)
		{
			SuperType<T> superType = other;
			return Equals(superType);
		}

		/// <summary>
		/// Implizite Konvertierung, durch die folgendes möglich ist:
		/// <code>
		/// FinancialStatementsState = id;
		/// </code>
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static implicit operator SuperType<T>(int id)
		{
			return Get(id);
		}

		/// <summary>
		/// Implizite Konvertierung, durch die folgendes möglich ist:
		/// <code>
		/// int i = FinancialStatementsState.Id;
		/// </code>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static implicit operator int(SuperType<T> type)
		{
			return type.Id;
		}

        private string FormatName()
        {
            return new ResourceManager(resourceType).GetString(GetResourceName());
        }

	}
}
