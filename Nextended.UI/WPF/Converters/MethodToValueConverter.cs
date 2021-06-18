using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Konvertiert eine Methode eines Objektes zu einem Wert
    /// </summary>
    public class MethodToValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value. 
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var methodName = parameter as string;
            if (value == null || methodName == null)
                return value;
            MethodInfo methodInfo = value.GetType().GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (methodInfo == null)
                return value;
            return methodInfo.Invoke(value, null);
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
        	var me = value as MethodInfo;
			if (me != null)
				return me.Name;
        	return value;
        }
    }
}