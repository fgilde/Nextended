using System;
using System.Globalization;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Invertiert einen boolschen Wert
	/// </summary>
	public class InvertBooleanConverter : IValueConverter
	{
		///<summary>
		///Converts a value. The data binding engine calls this method when it propagates a value from the binding source to the binding target.
		///</summary>
		///
		///<returns>
		///A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty"></see>.<see cref="F:System.Windows.DependencyProperty.UnsetValue"></see> indicates that the converter produced no value and that the binding uses the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"></see>, if available, or the default value instead.A return value of <see cref="T:System.Windows.Data.Binding"></see>.<see cref="F:System.Windows.Data.Binding.DoNothing"></see> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"></see> or default value.
		///</returns>
		///
		///<param name="culture">The culture to use in the converter.</param>
		///<param name="targetType">The type of the binding target property.</param>
		///<param name="parameter">The converter parameter to use.</param>
		///<param name="value">The value produced by the binding source.</param>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
				return !System.Convert.ToBoolean(value);
			return null;
		}

		///<summary>
		///Converts a value. The data binding engine calls this method when it propagates a value from the binding target to the binding source.
		///</summary>
		///
		///<returns>
		///A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty"></see>.<see cref="F:System.Windows.DependencyProperty.UnsetValue"></see> indicates that the converter produced no value and that to the binding uses the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"></see>, if available, or the default value instead.A return value of <see cref="T:System.Windows.Data.Binding"></see>.<see cref="F:System.Windows.Data.Binding.DoNothing"></see> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"></see> or default value.
		///</returns>
		///
		///<param name="culture">The culture to use in the converter.</param>
		///<param name="targetType">The type to convert to.</param>
		///<param name="parameter">The converter parameter to use.</param>
		///<param name="value">The value that is produced by the binding target.</param>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
				return !System.Convert.ToBoolean(value);
			return null;
		}
	}
}
