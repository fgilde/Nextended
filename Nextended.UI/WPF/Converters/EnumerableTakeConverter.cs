using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    ///<summary>
    /// Gibt von einer Enumeration die ersten x items zurück (x muss als int als ConvertParam kommen)
    ///</summary>
    public class EnumerableTakeConverter:IValueConverter
    {
        /// <summary>
        /// Converts a value. 
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return value;
            try
            {
                int count = System.Convert.ToInt32(parameter);
                if(count > 0)
                {
                    var enumeration = value as IEnumerable<object>;
                    if(enumeration != null && enumeration.Count() > count)
                    {
                        return enumeration.Take(count);
                    }
                }
            }
            catch
            {
                return value;
            }
            return value;
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
        	return value;
        }
    }
}