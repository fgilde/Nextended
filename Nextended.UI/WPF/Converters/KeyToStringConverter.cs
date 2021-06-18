using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// KeyToStringConverter
    /// </summary>
    public class KeyToStringConverter : IValueConverter
    {

        /// <summary>
        /// Convert
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is Key ? (Key)value : Key.None;

            if (key == Key.D0 || key == Key.NumPad0)
                return "0";
            if (key == Key.D1 || key == Key.NumPad1)
                return "1";
            if (key == Key.D2 || key == Key.NumPad2)
                return "2";
            if (key == Key.D3 || key == Key.NumPad3)
                return "3";
            if (key == Key.D4 || key == Key.NumPad4)
                return "4";
            if (key == Key.D5 || key == Key.NumPad5)
                return "5";
            if (key == Key.D6 || key == Key.NumPad6)
                return "6";
            if (key == Key.D7 || key == Key.NumPad7)
                return "7";
            if (key == Key.D8 || key == Key.NumPad8)
                return "8";
            if (key == Key.D9 || key == Key.NumPad9)
                return "9";

            switch (key)
            {
                case Key.None:
                    return String.Empty;
                default:
                    return KeyLocalizer.TranslateKey(Enum.GetName(typeof(Key), key));
            }
        }

        /// <summary>
        /// ConvertBack
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new StringToKeyConverter().Convert(value, targetType, parameter, culture);
        }
    }
}