using System;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// DoubleToGridLength
	/// </summary>
	[ValueConversion(typeof(Double), typeof(GridLength))]
	public class DoubleToGridLengthConverter : IValueConverter
	{
		/// <summary>
		/// DoubleToGridLength
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new GridLength((double)value);
		}

		/// <summary>
		/// DoubleToGridLength
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return ((GridLength)value).Value;
		}
	}
}