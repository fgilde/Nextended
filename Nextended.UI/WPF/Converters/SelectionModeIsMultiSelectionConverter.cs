using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Konverter um die selectionmode als boolean zu binden (IsMultiSelect)
	/// </summary>
	[ValueConversion(typeof(SelectionMode), typeof(bool))]
	public class SelectionModeIsMultiSelectionConverter:IValueConverter
	{
		/// <summary>
		/// Gibt true zurück, wenn ein Multiselect möglich ist
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			SelectionMode mode = value is SelectionMode ? (SelectionMode) value : SelectionMode.Single;
			return mode == SelectionMode.Multiple || mode == SelectionMode.Extended;
		}

		/// <summary>
		/// Convertback
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
				return SelectionMode.Multiple;
			return SelectionMode.Single;
		}
	}
}