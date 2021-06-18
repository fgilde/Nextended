using System;
using System.Globalization;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Konvertiert einen Bool zu einer farbe 
	/// </summary>
	public class BoolToColorConverter:GenericValueConverter<bool,Color>
	{
		#region Overrides of GenericValueConverter<bool,Color>

		/// <summary>
		///  Konvertiert einen Bool zu einer farbe 
		/// </summary>
		public override Color Convert(bool value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!value)
				return Colors.Transparent;
			Color result = Colors.Black;
			if (parameter is Color)
				return (Color) parameter;
			if (parameter is Brush)
				return ViewUtility.GetColor((Brush) parameter, GradientColorOption.MostBrightness).ToMediaColor();
			return result;
		}

		/// <summary>
		/// Konvertiert eine Farbe zu bool 
		/// </summary>
		public override bool ConvertBack(Color value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == Colors.Transparent)
				return false;
			return true;
		}

		#endregion
	}

	/// <summary>
	/// Konvertiert einen Bool zu einer farbe 
	/// </summary>
	public class BoolToBrushConverter : GenericValueConverter<bool, Brush>
	{
		#region Overrides of GenericValueConverter<bool,Color>

		/// <summary>
		///  Konvertiert einen Bool zu einer farbe 
		/// </summary>
		public override Brush Convert(bool value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!value)
				return Brushes.Transparent;
			if (parameter is Brush)
				return (Brush) parameter;
			return  Brushes.Black;
		}

		/// <summary>
		/// Konvertiert eine Farbe zu bool 
		/// </summary>
		public override bool ConvertBack(Brush value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == Brushes.Transparent || ViewUtility.GetColor(value).ToMediaColor() == Colors.Transparent)
				return false;
			return true;
		}

		#endregion
	}
}