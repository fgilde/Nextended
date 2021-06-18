using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using Nextended.UI.Helper;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Convertiert einen KeyGesture zu einem String
    /// </summary>
    public class KeyGestureToStringConverter:IValueConverter
    {
        /// <summary>
        /// Convertiert einen KeyGesture zu einem String
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return String.Empty;

            var gesture = value as KeyGesture;
            if (gesture != null)
                return gesture.ConvertToString();
            
            return value.ToString();
        }

        /// <summary>
        /// string to gesure
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
        	return KeyGestureConvertHelper.CreateFrom(value.ToString());
        }
    }


}