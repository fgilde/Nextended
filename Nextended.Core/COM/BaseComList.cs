using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nextended.Core.COM
{
	/// <summary>
	/// Basis Callback Klasse
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[ComVisible(false)]
	public abstract class BaseComList<T> : IComList
	{
		/// <summary>
		/// Liste Liste
		/// </summary>
		protected readonly IList<T> List;

		/// <summary>
		/// Standard ctor
		/// </summary>
		protected BaseComList()
		{
			List = new List<T>();
		}

		/// <summary>
		/// Com Callback mit Listen Parameter
		/// </summary>
		/// <param name="list"></param>
		protected BaseComList(IList<T> list)
		{
			List = list ?? new List<T>();
		}

		/// <summary>
		/// Fügt Werte in die Liste
		/// </summary>
		/// <param name="aValue"></param>
		public abstract void Add(object aValue);

		/// <summary>
		/// Gets the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public object Get(int index)
		{
			return List[index];
		}

		/// <summary>
		/// Liefert die Liste der Elemente
		/// </summary>
		/// <returns></returns>
		public IEnumerable<T> Items()
		{
			return List;
		}

		/// <summary>
		/// Counts this instance.
		/// </summary>
		/// <returns></returns>
		public int Count()
		{
			return List.Count;
		}

		#region Implementation of IEnumerable

		public IEnumerator GetEnumerator()
		{
			return List.GetEnumerator();
		}

		#endregion
	}

	public class ComList<T> : BaseComList<T>
	{
	    public ComList() : base()
	    { }

	    public ComList(IList<T> list) : base(list)
	    {
	    }

	    public override void Add(object aValue)
		{
			var value = aValue is T ? (T)aValue : default(T);
			Check.NotNull(value, "value");
			List.Add(value);
		}
	}
}