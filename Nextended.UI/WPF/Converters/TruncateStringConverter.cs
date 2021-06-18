using System;
using System.Globalization;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// TruncateStringConverter
    /// </summary>
    public class TruncateStringConverter : IValueConverter
	{
        /// <summary>
        /// Convert
        /// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int maxLength = 500;
            try
            {
                if (parameter != null)
                    maxLength = System.Convert.ToInt32(parameter);
            }
            catch (Exception)
            {
                maxLength = 500;
            }
            if (value is string)
            {
                var message = value as string;
                if (!string.IsNullOrEmpty(message) && message.Length > maxLength)
                {
                    string res = message.Substring(0, maxLength) + "...";
                    return res;
                }
                return message;
            }
            return value;
		}

        /// <summary>
        /// Not implemented
        /// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
	}
}
