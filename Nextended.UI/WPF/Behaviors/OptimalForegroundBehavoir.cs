using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace Nextended.UI.WPF.Behaviors
{
    /// <summary>
    /// OptimalForegroundBehavoir passt den Foreground entsprechend zum Background an
    /// </summary>
    public class OptimalForegroundBehavoir : DependencyObject
    {
        /// <summary>
        /// returns the IsActiveProperty as bool.
        /// </summary>
        public static bool GetIsActive(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsActiveProperty);
        }

        /// <summary>
        /// Sets the IsActiveProperty.
        /// </summary>
        public static void SetIsActive(DependencyObject obj, bool value)
        {
            obj.SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// IsActiveProperty
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(OptimalForegroundBehavoir), new UIPropertyMetadata(false, OnIsActiveChanged));

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                UpdateForeground(d);
            }
        }

        private static void UpdateForeground(DependencyObject d)
        {
            Brush bg = d.FindVisualBackground();
            if (bg != null)
            {
                Color color = ViewUtility.GetColor(bg, GradientColorOption.MostBrightness);
                TextBlock.SetForeground(d, new SolidColorBrush(ViewUtility.GetOptimalForegroundColor(color).ToMediaColor()));
            }
        }

    }
}