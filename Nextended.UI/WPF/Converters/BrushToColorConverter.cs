using System;
using System.Globalization;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Konvertiert einen Brush zu einer Farbe
	/// </summary>
	public class BrushToColorConverter : GenericValueConverter<Brush,Color>
	{
		#region Overrides of GenericValueConverter<Brush,Color>

		/// <summary>
		/// Konvertiert einen Brush zu einer Farbe
		/// </summary>
		public override Color Convert(Brush value, Type targetType, object parameter, CultureInfo culture)
		{
			return ViewUtility.GetColor(value, GradientColorOption.MostBrightness).ToMediaColor();
		}

		/// <summary>
		/// Konvertiert die Farbe zu einem brush
		/// </summary>
		public override Brush ConvertBack(Color value, Type targetType, object parameter, CultureInfo culture)
		{
			return new SolidColorBrush(value);
		}

		#endregion
	}
}