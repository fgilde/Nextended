using System;

namespace Nextended.Core
{

	/// <summary>
	/// Base class for singleton pattern (everything that inherits from this class can be accessed via Type.Instance)
	/// </summary>
	/// <typeparam name="T">The type of the singleton</typeparam>
	public abstract class SingletonBase<T> : NotificationObject
		where T : SingletonBase<T>, new()
	{

		private static readonly Lazy<T> current = new Lazy<T>(() => new T());

		/// <summary>
		/// Gets the current instance
		/// </summary>
		public static T Instance => current.Value;
    }
}