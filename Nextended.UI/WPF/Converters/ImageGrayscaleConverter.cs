using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Konvertiert ein Bild zu einem Bild mit Graustufen (ohne Farbe)
	/// </summary>
	public class ImageGrayscaleConverter : IValueConverter
	{
		#region Implementation of IValueConverter

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;
			var image = value as Image;
			if (image == null && value is ImageSource)
				image = ((ImageSource) value).ToImage();
			if(image == null)
				throw new NotSupportedException("Only Image and ImageSource type are allowed as source type");
			image = image.ToDisabledGrayscaleImage();
			if (targetType.IsAssignableFrom(typeof(Image)))
				return image;
			return new Bitmap(image).ToImageSource();
		}

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}