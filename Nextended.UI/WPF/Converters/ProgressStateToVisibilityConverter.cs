using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// ProgressState zu Visibility
	/// </summary>
	public class ProgressStateToVisibilityConverter: IValueConverter
	{
		/// <summary>
		/// Konvertiert einen Progressstate zu einem Sichtbarkeitsstatus
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var progressState = value is TaskbarItemProgressState
						? (TaskbarItemProgressState)value
						: TaskbarItemProgressState.None;
			return progressState == TaskbarItemProgressState.None ? Visibility.Collapsed : Visibility.Visible;
		}

		/// <summary>
		/// Macht nix
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var vis = value is Visibility ? (Visibility) value : Visibility.Visible;
			if (vis == Visibility.Visible)
				return TaskbarItemProgressState.Normal;
			return TaskbarItemProgressState.None;
		}
	}
}
