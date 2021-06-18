using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    ///  Konvertiert einen Boolean (AllowMultiSelect) zu dem SelectionMode für ListViews
    /// </summary>
    public class BooleanToSelectionModeConverter:IValueConverter
    {
        /// <summary>
        /// Konvertiert einen Boolean (AllowMultiSelect) zu dem SelectionMode für ListViews
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
                return SelectionMode.Extended;
            return SelectionMode.Single;
        }

        /// <summary>
        /// Gibt zurück, ob der übergebene SelectionMode MultiSelect ist
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SelectionMode 
                && ((SelectionMode)value == SelectionMode.Multiple || (SelectionMode)value == SelectionMode.Extended ))
                return true;
            return false;
        }
    }
}