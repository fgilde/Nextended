using System;
using System.Globalization;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    ///<summary>
    /// Gibt true zurück, wenn der value null ist
    ///</summary>
    public class IsNullConverter : IValueConverter
    {
        /// <summary>
        /// Gibt true zurück, wenn der value null ist
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

}