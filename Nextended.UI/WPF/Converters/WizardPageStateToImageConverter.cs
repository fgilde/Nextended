using System;
using System.Globalization;
using System.Windows.Media;
using Nextended.UI.Properties;
using Nextended.UI.WPF.Controls.Wizard;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// Konvertiert den Status einer Wizard Seite zu einem Bild
    /// </summary>
    public class WizardPageStateToImageConverter:GenericValueConverter<WizardPageState,ImageSource>
    {
        /// <summary>
        /// Converts the value of type "WizardPageState" to an value of type "ImageSource"/>. 
        /// </summary>
        public override ImageSource Convert(WizardPageState value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case WizardPageState.None:
                    return Images.transparent.ToImageSource();
                case WizardPageState.Valid:
                    return Images.bestaetigen_24.ToImageSource();
                case WizardPageState.InValid:
                    return Images.error256.ToImageSource();
				case WizardPageState.InProgress:
            		return Images.Warten_24.ToImageSource();
                default:
					return Images.transparent.ToImageSource();
            }
        }

		/// <summary>
		/// Converts the value back.
		/// </summary>
		public override WizardPageState ConvertBack(ImageSource value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == Images.transparent.ToImageSource())
				return WizardPageState.None;
			if (value == Images.bestaetigen_24.ToImageSource())
				return WizardPageState.Valid;
			if (value == Images.error256.ToImageSource())
				return WizardPageState.InValid;
			if (value == Images.Warten_24.ToImageSource())
				return WizardPageState.InProgress;
			return WizardPageState.None;
			
		}

    }
}