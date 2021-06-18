using System;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Generic Eventargs
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EventArgs<T> : EventArgs
	{
		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		public T Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventArgs&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		public EventArgs(T value)
		{
			Value = value;
		}
	}
}