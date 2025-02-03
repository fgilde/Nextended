#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nextended.Core.Extensions;

namespace Nextended.Core.COM
{
	/// <summary>
	/// COM Liste für die Übergabe von .NET an COM
	/// </summary>
	[ComVisible(false)]
	public sealed class NetToComList<TNet, TCom> : BaseComList<TCom>
	{
		private readonly Func<TNet, TCom> netToComConverter;

		/// <summary>
		/// Initializes a new instance of the <see cref="NetToComList{TNet, TCom}"/> class.
		/// </summary>
		public NetToComList(IList<TCom> list, Func<TNet, TCom> netToComConverter = null)
			: base (list)
		{
			this.netToComConverter = netToComConverter ?? DefaultConverter;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NetToComList{TNet, TCom}"/> class.
		/// </summary>
		public NetToComList(Func<TNet, TCom> netToComConverter = null)
			: this(new List<TCom>(), netToComConverter)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="NetToComList{TNet, TCom}"/> class.
		/// </summary>
		public NetToComList(IList<TNet> collection, Func<TNet, TCom> netToComConverter = null)
			: this(netToComConverter)
		{
			collection.Apply(netObj => Add(netObj));
		}

		/// <summary>
		/// Fügt Werte in die Liste
		/// </summary>
		/// <param name="aValue"></param>
		public override void Add(object aValue)
		{
			Check.NotNull(aValue, nameof(aValue));
			Check.Requires(aValue is TNet, () => new InvalidOperationException($".NET <-> COM Conversion: {aValue.GetType().Name} is not an instance of {typeof (TCom).Name}"));

			TCom target = netToComConverter((TNet)aValue);
			if (target != null)
				List.Add(target); 			
		}

		private TCom DefaultConverter(TNet item)
		{
			Check.NotNull(item, nameof(item));
			return item.MapTo<TCom>();
		}
	}

}
#endif