using System;
using System.Globalization;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Konvertiert ein Binding an einem Brush zu dem passenden lesbaren Brush
    /// <example>
    ///  Foreground="{Binding ElementName=grid, Path=Background, Converter={StaticResource OptimalBrushConverter}}"
    /// </example>
    /// </summary>
    public class OptimalBrushConverter : GenericValueConverter<Brush, Brush>
    {
        /// <summary>
        ///  Konvertiert ein Binding an einer Farbe zu der passenden lesbaren farbe
        /// </summary>
        public override Brush Convert(Brush value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(ViewUtility.GetOptimalForegroundColor(ViewUtility.GetColor(value, GradientColorOption.LeastBrightness).ToMediaColor()));
        }

    }

    /// <summary>
    /// Konvertiert ein Binding an einer Farbe zu der passenden lesbaren farbe
    /// </summary>
    public class OptimalColorConverter : GenericValueConverter<Color, Color>
    {
        /// <summary>
        ///  Konvertiert ein Binding an einer Farbe zu der passenden lesbaren farbe
        /// </summary>
        public override Color Convert(Color value, Type targetType, object parameter, CultureInfo culture)
        {
            return ViewUtility.GetOptimalForegroundColor(value);
        }

    }
}