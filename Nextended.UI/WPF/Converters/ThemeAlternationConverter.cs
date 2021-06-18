using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// AlternationConverter der den Theme berücksichtigt
	/// </summary>
	public class ThemeAlternationConverter : AlternationConverter, IValueConverter
	{
		/// <summary>
		/// Brush für even
		/// </summary>
		public Brush EvenRowBrush { get; set; }

		/// <summary>
		/// Brush für Odd
		/// </summary>
		public Brush OddRowBrush { get; set; }

		/// <summary>
		/// Konvertiert den AlternationIndex zu einem Brush
		/// </summary>
		public new object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (Values.Count <= 0 && Application.Current != null)
			{
				var index = System.Convert.ToInt32(value);
				if (index == 0)
					return EvenRowBrush ?? Application.Current.Resources["EvenRowBrush"];
				if (index == 1)
					return OddRowBrush ?? Application.Current.Resources["OddRowBrush"];
			}
			return base.Convert(value, targetType, parameter, culture);
		}
	}
}