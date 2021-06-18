using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    internal class LevelToIndentConverter : IValueConverter
    {
        private const double indentSize = 19.0;

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return new Thickness((int)o * indentSize, 0, 0, 0);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
