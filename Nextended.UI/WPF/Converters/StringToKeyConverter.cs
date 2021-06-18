using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// StringToKeyConverter
    /// </summary>
    public class StringToKeyConverter:IValueConverter
    {
        /// <summary>
        ///Konvertiert ein String zu einem Key (für AccessKey) 
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value.ToString();
            char c = s[0];
            if (s.Contains("_"))
                c = s.Split('_')[1][0];

            var converter = new KeyConverter();
            var key = converter.ConvertFrom(c.ToString());
            Key result = key is Key ? (Key) key : Key.None;
            return result;
            
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return  new KeyToStringConverter().Convert(value, targetType, parameter, culture);
        }
    }
}