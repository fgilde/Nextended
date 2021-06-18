using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Count > 0 = true
    /// </summary>
    public class CountToBooleanConverter : IValueConverter
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
            int lowerValue = 0;
            if (parameter != null)
            {
                try
                {lowerValue = System.Convert.ToInt32(parameter);}
                catch{lowerValue = 0;}
            }

            if (value is IEnumerable<object>)
                return ((IEnumerable<object>)value).Count() > lowerValue;
            if (value is int)
                return (int)value > lowerValue;
            return false;
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
            throw new InvalidOperationException("CountToBooleanConverter can only be used OneWay.");
        }
        
    }


    /// <summary>
    /// Count to visibility
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
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
            var converter = new CountToBooleanConverter();
            var res = (bool)converter.Convert(value, targetType, parameter, culture) ;
            return res ? Visibility.Visible : Visibility.Collapsed;
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
            throw new InvalidOperationException("CountToVisibilityConverter can only be used OneWay.");
        }

    }

}