//============================================================================================

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	//TODO: KLI & FG 2011-01-03 ConverterParameter logik umderehen und alle uses anpassen

    /// <summary>
    /// g
    /// </summary>
    public class BoolToVisibilityConverter:IValueConverter 
    {
        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (!(bool)value ? !System.Convert.ToBoolean(parameter) : System.Convert.ToBoolean(parameter)) ? Visibility.Visible : Visibility.Collapsed;
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
        	var vis = value is Visibility ? (Visibility) value : Visibility.Hidden;
			if (vis == Visibility.Visible)
				return true;
        	return false;
        }
    }
}