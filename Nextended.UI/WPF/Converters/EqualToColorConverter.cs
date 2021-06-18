using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// 
    /// </summary>
    public class EqualToColorConverter:DependencyObject,IValueConverter
    {


        /// <summary>
        /// FallbackBrush
        /// </summary>
        public Brush FallbackBrush
        {
            get { return (Brush)GetValue(FallBackBrushProperty); }
            set { SetValue(FallBackBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="FallbackBrush"/>
        /// </summary>
        public static readonly DependencyProperty FallBackBrushProperty =
            DependencyProperty.Register("FallbackBrush", typeof(Brush), typeof(EqualToColorConverter), new UIPropertyMetadata(Brushes.White));

        

        /// <summary>
        /// Farbe die zurückgegeben wird, wenn das ReferenceObject dem value gleicht
        /// </summary>
        public Brush Brush
        {
            get { return (Brush)GetValue(BrushProperty); }
            set { SetValue(BrushProperty, value); }
        }

        /// <summary>
        /// <see cref="Brush"/>
        /// </summary>
        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register("Brush", typeof(Brush), typeof(EqualToColorConverter), new UIPropertyMetadata(Brushes.Black));

        

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object referenceObject = parameter;
            if (parameter is FrameworkElement)
                 referenceObject = ((FrameworkElement) parameter).DataContext;
            if (referenceObject != null && value != null)
            {
                if (referenceObject.Equals(value))
                    return Brush;
            }
            
            return FallbackBrush;
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
        	return value;
        }
    }
}