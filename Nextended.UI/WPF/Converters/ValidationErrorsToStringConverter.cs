using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// ValidationErrorsToStringConverter
    /// </summary>
    [ValueConversion(typeof(ReadOnlyObservableCollection<ValidationError>), typeof(string))]
    public class ValidationErrorsToStringConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// When implemented in a derived class, returns an object that is set as the value of the target property for this markup extension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ValidationErrorsToStringConverter();
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            var errors = value as ReadOnlyObservableCollection<ValidationError>;

            if (errors == null)
            {
                return string.Empty;
            }

            return string.Join("\n", (from e in errors
                                      select e.ErrorContent as string).ToArray());
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
        	return null;
        }
    }
}