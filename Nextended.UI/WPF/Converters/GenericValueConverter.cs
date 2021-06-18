using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// GenericValueConverter 
    /// </summary>
	//[ValueConversion(typeof(TValue), typeof(TResult))]
    public abstract class GenericValueConverter<TValue,TResult>: DependencyObject, IValueConverter
    {
        /// <summary>
        /// Converts the value of type <typeparamref name="TValue"/> to an value of type <typeparamref name="TResult"/>. 
        /// </summary>
        public abstract TResult Convert(TValue value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Converts the value of type <typeparamref name="TResult"/> back to its old value of type <typeparamref name="TValue"/>. 
        /// </summary>
        public virtual TValue ConvertBack(TResult value, Type targetType, object parameter, CultureInfo culture)
        {
            return default(TValue);
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is TValue)
                return Convert((TValue)value, targetType,parameter, culture);
            return default(TResult);
        }

        /// <summary>
        /// Converts a value back. 
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TResult)
                return ConvertBack((TResult)value, targetType, parameter, culture);
            return default(TValue);
        }
    }
}