using System;
using System.Globalization;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// StringSuffixConverter
    /// </summary>
    public class StringPrefixConverter:GenericValueConverter<string, string>
    {
        /// <summary>
        /// Converts the value of type TValue to an value of type"TResult". 
        /// </summary>
        public override string Convert(string value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter+value;
        }

        /// <summary>
        /// Converts the value of type "TResult" back to its old value of type "TValue". 
        /// </summary>
        public override string ConvertBack(string value, Type targetType, object parameter, CultureInfo culture)
        {
            if(parameter == null)
                return value;
            return value.Remove(0, parameter.ToString().Length);
        }
    }
}