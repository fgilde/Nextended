using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
	/// <summary>
	/// Creates a CollectionView for databinding to a HierarchicalTemplate ItemSource
	/// </summary>
	[ValueConversion(typeof(IList), typeof(IEnumerable))]
	public class CollectionViewFactoryConverter : GenericValueConverter<IList, IEnumerable>
	{
		/// <summary>
		/// Converts the specified value.
		/// </summary>
		public override IEnumerable Convert(IList value, Type targetType, object parameter, CultureInfo culture)
		{
			var collection = value;

			var view = new ListCollectionView(collection);

			if (parameter != null)
			{
				var sort = new SortDescription(parameter.ToString(), ListSortDirection.Ascending);
				view.SortDescriptions.Add(sort);
			}

			return view;
		}
	}
}