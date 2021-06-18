using System;

namespace Nextended.Core
{

	/// <summary>
	/// Basisklasse für Singelton (alles was von dieser klasse erbt kann mit Type.Instance aufgerufen werden)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class SingletonBase<T> : NotificationObject
		where T : SingletonBase<T>, new()
	{

		private static readonly Lazy<T> current = new Lazy<T>(() => new T());

		/// <summary>
		/// Aktuelle Instanz
		/// </summary>
		public static T Instance => current.Value;
    }
}