using System;
using System.Globalization;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// 
    /// </summary>
    public class BoolToStringConverter:GenericValueConverter<bool,string>
    {
        /// <summary>
        /// Converts the specified value.
        /// </summary>
        public override string Convert(bool value, Type targetType, object parameter, CultureInfo culture)
        {
            if(parameter != null)
            {
                return value ? parameter.ToString() : string.Empty;
            }
            return System.Convert.ToString(value);
        }

        /// <summary>
        /// Converts the back.
        /// </summary>
        public override bool ConvertBack(string value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                return value == parameter.ToString();
            }
            try
            {
                return System.Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}