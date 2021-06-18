using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Shell;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// ProgressState zu boolean IsIndeterminate
	/// </summary>
	public class ProgressStateToIsIndeterminateConverter : IValueConverter
	{
		/// <summary>
		/// Konvertiert einen ProgressState zu einem boolean
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var progressState = value is TaskbarItemProgressState ? (TaskbarItemProgressState)value : TaskbarItemProgressState.None;
			return (progressState == TaskbarItemProgressState.Indeterminate);
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
			return TaskbarItemProgressState.None;
		}
	}
}
