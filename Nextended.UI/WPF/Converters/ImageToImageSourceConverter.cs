using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Class ImageToImageSourceConverter
	/// </summary>
	public class ImageToImageSourceConverter:GenericValueConverter<Image, ImageSource>
	{
		/// <summary>
		/// Converts the value of type System.Drawing.Image to an value of type ImageSource. 
		/// </summary>
		public override ImageSource Convert(Image value, Type targetType, object parameter, CultureInfo culture)
		{
			int width;
			if(parameter != null && int.TryParse(parameter.ToString(), out width))
			{
				value = ViewUtility.ResizeImage(value, width, width);
			}
			var bitmap = new Bitmap(value);

			return bitmap.ToImageSource();
		}

		/// <summary>
		/// Konvertiert den wert zurück
		/// </summary>
		public override Image ConvertBack(ImageSource value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.ToImage();
		}
	}
}