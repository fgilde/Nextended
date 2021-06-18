using System;

namespace Nextended.Core.Extensions
{
	/// <summary>
	/// Extensions for easy expose of an object
	/// </summary>
	public static class ExposedObjectExtensions
	{
		/// <summary>
		/// Possibility to easy set private properties
		/// </summary>
		public static T SetExposed<T>(this T instance, 
			params Action<dynamic>[] setterActions)
		{
			dynamic dyn = ExposedObject.From(instance);
			foreach (var action in setterActions)
				action(dyn);
			return instance;
		}
	}
}