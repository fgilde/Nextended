using System.Drawing;
using System.Windows.Markup;

namespace Nextended.UI.WPF.MarkupExtensions
{
	/// <summary>
	/// Static Image extension für wpf
	/// </summary>
	public class StaticImage : StaticExtension
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StaticImage"/> class.
		/// </summary>
		/// <param name="member">A string that identifies the member to make a reference to. This string uses the format prefix:typeName.fieldOrPropertyName. prefix is the mapping prefix for a XAML namespace, and is only required to reference static values that are not mapped to the default XAML namespace.</param>
		public StaticImage(string member) : base(member)
		{}

		/// <summary>
		/// Returns an object value to set on the property where you apply this extension. For <see cref="T:System.Windows.Markup.StaticExtension" />, the return value is the static value that is evaluated for the requested static member.
		/// </summary>
		/// <param name="serviceProvider">An object that can provide services for the markup extension. The service provider is expected to provide a service that implements a type resolver (<see cref="T:System.Windows.Markup.IXamlTypeResolver" />).</param>
		/// <returns>
		/// The static value to set on the property where the extension is applied.
		/// </returns>
		public override object ProvideValue(System.IServiceProvider serviceProvider)
		{
			var result = base.ProvideValue(serviceProvider);
			if (result is Bitmap)
				return ((Bitmap) result).ToImageSource();
			if (result is Icon)
				return ((Icon)result).ToImageSource();
			return result;
		}
	}

}