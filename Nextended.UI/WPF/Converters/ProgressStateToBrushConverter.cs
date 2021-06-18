using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shell;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// ProgressState zu brush
	/// </summary>
	public class ProgressStateToBrushConverter:IValueConverter 
	{
		/// <summary>
		/// Konvertiert einen ProgressState zu einem Brush
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
			switch (progressState)
			{
				case TaskbarItemProgressState.Error:
					return new SolidColorBrush(Colors.Red);
				case TaskbarItemProgressState.Paused:
					return new SolidColorBrush(Colors.Yellow);
				default:
					return new SolidColorBrush(Colors.LightGreen);
			}
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
			return TaskbarItemProgressState.Normal;
		}
	}
}
