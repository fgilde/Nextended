using System;
using System.Globalization;
using System.Windows;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// StringToVisibilityConverter
    /// </summary>
    public class StringToVisibilityConverter:GenericValueConverter<string, Visibility>
    {
        /// <summary>
        /// Converts the value of type TValue to an value of type"TResult". 
        /// </summary>
        public override Visibility Convert(string value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility) new BoolToVisibilityConverter().Convert(string.IsNullOrWhiteSpace(value), targetType, parameter, culture);
        }

    }
}