using System;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	internal class CaptionButtonRectToMarginConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var rect = (Rect)value;
			return new Thickness(0, rect.Top, 0, 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
