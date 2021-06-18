using System;
using System.Globalization;
using Nextended.UI.WPF.Controls.Wizard;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Konvertiert den Status einer Wizard Seite zu einem Bild
    /// </summary>
    public class WizardPageStateToBooleanConverter:GenericValueConverter<WizardPageState,bool>
    {
        /// <summary>
        /// Converts the value of type "WizardPageState" to an value of type "ImageSource"/>. 
        /// </summary>
        public override bool Convert(WizardPageState value, Type targetType, object parameter, CultureInfo culture)
        {
            bool res;
            try
            {
                res = System.Convert.ToBoolean(parameter);
            }
            catch 
            {
                res = false;
            }

            if (value == WizardPageState.InValid)
                return !res;
            return res;
        }

		/// <summary>
		/// Converts the bool value back to an WizardPageState
		/// </summary>
		public override WizardPageState ConvertBack(bool value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value)
				return WizardPageState.Valid;
			return WizardPageState.InValid;
		}
    }
}