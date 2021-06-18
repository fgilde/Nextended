using System;
using System.Globalization;
using System.Windows;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// VisibilityToBooleanConverter
	/// </summary>
	public class VisibilityToBooleanConverter:GenericValueConverter<Visibility,bool>
	{
		/// <summary>
		/// Converts the value of type Visibility to an value of type bool
		/// </summary>
		public override bool Convert(Visibility value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == Visibility.Visible)
				return true;
			return false;
		}

		/// <summary>
		/// Converts the value back.
		/// </summary>
		public override Visibility ConvertBack(bool value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value)
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

	}
}