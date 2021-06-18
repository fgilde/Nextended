using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Konvertiert ein UIElement/Control zu einem Bild
	/// </summary>
	public class UIElementToImageConverter:GenericValueConverter<UIElement,ImageSource>
	{

		private readonly Dictionary<UIElement, ImageSource> lookup;

		/// <summary>
		/// Initializes a new instance of the <see cref="UIElementToImageConverter" /> class.
		/// </summary>
		public UIElementToImageConverter()
		{
			lookup = new Dictionary<UIElement, ImageSource>();
		}

		/// <summary>
		/// Cache leeren
		/// </summary>
		public void ClearCache()
		{
			lookup.Clear();
		}

		/// <summary>
		/// Gibt an, ob ein UIElement nur einmal als Bild erzeugt werden soll
		/// </summary>
		public bool UseCache
		{
			get { return (bool)GetValue(UseCacheProperty); }
			set { SetValue(UseCacheProperty, value); }
		}

		/// <summary>
		/// <see cref="UseCache"/>
		/// </summary>
		public static readonly DependencyProperty UseCacheProperty =
			DependencyProperty.Register("UseCache", typeof(bool), typeof(UIElementToImageConverter), new PropertyMetadata(false));

		/// <summary>
		/// Konvertiert ein UIElement/Control zu einem Bild
		/// </summary>
		public override ImageSource Convert(UIElement value, Type targetType, object parameter, CultureInfo culture)
		{
			if (UseCache && lookup.ContainsKey(value))
				return lookup[value];
			var result = value.ToImageSource();
			lookup.Add(value,result);
			return result;
		}


	}
}