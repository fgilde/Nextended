using System;

namespace Nextended.Core.Attributes
{
	/// <summary>
	/// Attribute für Eigenschaften, die innerhalb einer BaseOptionPage automatisch geladen und gespeichert werden sollen
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class SettingsPropertyAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsPropertyAttribute" /> class.
		/// </summary>
		/// <param name="defaultValue">The default value.</param>
		/// <param name="settingsKey">The settings key.</param>
		public SettingsPropertyAttribute(string settingsKey, object defaultValue)
		{
			DefaultValue = defaultValue;
			SettingsKey = settingsKey;
			AllowResetValue = true;
		}

		/// <summary>
		/// Funktion, die beim zurücksetzen aufgerufen werden soll (wenn keine vorhanden wird nur der wert auf defaultvalue gesetzt)
		/// </summary>
		public string ResetValueConverterFunc { get; set; }

		/// <summary>
		/// Gibt an ob der wert zurückgesetzt werden kann
		/// </summary>
		public bool AllowResetValue { get; set; }

		/// <summary>
		/// Der defaultwert
		/// </summary>
		public object DefaultValue { get; set; }

		/// <summary>
		/// Der Key
		/// </summary>
		public string SettingsKey { get; set; }
	}
}