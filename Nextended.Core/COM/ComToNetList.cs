#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nextended.Core.Extensions;

namespace Nextended.Core.COM
{
	/// <summary>
	/// Allgemeine Liste für COM Objekte
	/// </summary>
	[ComVisible(false)]
	public sealed class ComToNetList<TCom, TNet> : BaseComList<TNet>
	{
		private readonly Func<TCom, TNet> comToNetConverter;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComToNetList{TComInterface,TNetInterface}"/> class.
		/// </summary>
		/// <param name="comToNetConverter">The COM automatic net converter.</param>
		public ComToNetList(Func<TCom, TNet> comToNetConverter = null)
			: this(new List<TNet>(), comToNetConverter)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ComToNetList{TComInterface,TNetInterface}" /> class.
		/// </summary>
		/// <param name="list">Liste</param>
		/// <param name="comToNetConverter">COM nach .NET Converter</param>
		public ComToNetList(IList<TNet> list, Func<TCom, TNet> comToNetConverter = null)
			: base (list)
		{
			this.comToNetConverter = comToNetConverter ?? DefaultConverter;
		}

		/// <summary>
		/// Fügt Werte in die Liste
		/// </summary>
		/// <param name="aValue"></param>
		public override void Add(object aValue)
		{
			Check.Requires<ArgumentNullException>(aValue != null, "NULL-Value form COM");
			Check.Requires(aValue is TCom, () => new InvalidOperationException(string.Format("COM <-> .NET Conversion: {0} is not an instance of {1}", aValue.GetType().Name, typeof(TCom).Name)));

		    TNet target = comToNetConverter((TCom) aValue);
              if (target != null)
                  List.Add(target);           
		}

		private TNet DefaultConverter(TCom item)
		{
			Check.NotNull(item, "item");
			return item.MapTo<TNet>();
		}
	}
}
#endif