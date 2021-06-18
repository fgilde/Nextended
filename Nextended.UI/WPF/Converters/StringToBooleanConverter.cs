using System;
using System.Globalization;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// StringToVisibilityConverter
    /// </summary>
    public class StringToBooleanConverter:GenericValueConverter<string, bool>
    {
        /// <summary>
        /// Converts the value of type TValue to an value of type"TResult". 
        /// </summary>
        public override bool Convert(string value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || (parameter is bool && !(bool)parameter))
                return !string.IsNullOrWhiteSpace(value);
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Converts the value of type "TResult" back to its old value of type "TValue". 
        /// </summary>
        public override string ConvertBack(bool value, Type targetType, object parameter, CultureInfo culture)
        {
        	return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}