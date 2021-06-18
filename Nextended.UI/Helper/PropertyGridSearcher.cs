using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Nextended.Core.Properties;
using Nextended.UI.Classes;

namespace Nextended.UI.Helper
{
	/// <summary>
	/// PropertyGridSearcher
	/// </summary>
	public static class PropertyGridSearcher
	{
		/// <summary>
		/// Erweitert das Propertygrid um eine suche 
		/// </summary>
		public static void ExtendSearch(this PropertyGrid propertyGrid)
		{
			if (propertyGrid.Tag == null)
				propertyGrid.Tag = new PropertyGridSearchExtenter(propertyGrid);
			else
				new PropertyGridSearchExtenter(propertyGrid);
		}
	}

	internal class PropertyGridSearchExtenter
	{
		private readonly PropertyGrid propertyGrid;
		private object propObject;
		private string searchText => Resources.SearchContent;

        private bool frominternal;
		 //public void ExtendSearch(this PropertyGrid propertyGrid)
		 //{

		 //}

		public PropertyGridSearchExtenter(PropertyGrid propertyGrid)
		{
			this.propertyGrid = propertyGrid;
			propObject = propertyGrid.SelectedObject;
			propertyGrid.SelectedObjectsChanged += PropertyGridOnSelectedObjectsChanged;
			var fieldInfo = typeof(PropertyGrid).GetField("toolStrip", BindingFlags.Instance | BindingFlags.NonPublic);
			if (fieldInfo == null)
				return;
			var toolstrip = fieldInfo.GetValue(propertyGrid) as ToolStrip;
			if (toolstrip != null)
			{
				var tb = new ToolStripSpringTextBox { Margin = new Padding(10), ForeColor = Color.Gray, Text = searchText, Dock = DockStyle.Fill };
				tb.Enter += (sender, args) => SearchBoxEnter(tb);
				tb.Leave += (sender, args) => SearchBoxLeave(tb);
				tb.TextChanged += (sender, args) => SearchBoxTextChanged(propertyGrid, tb);

				toolstrip.Items.Add(new ToolStripSeparator());
				toolstrip.Items.Add(tb);
			}
		}

		private void PropertyGridOnSelectedObjectsChanged(object sender, EventArgs eventArgs)
		{
			if (!frominternal)
			{
				propObject = propertyGrid.SelectedObject;
			}
		}

		private void SearchBoxTextChanged(PropertyGrid control, ToolStripTextBox tb)
		 {
			 frominternal = true;
			 if (!string.IsNullOrWhiteSpace(tb.Text) && tb.Text != searchText)
			 {
				 if (propObject != null && !(propObject is CustomClass))
				 {
					 var customClass = new CustomClass();
					 foreach (
						 PropertyInfo propertyInfo in
							 propObject.GetType().GetProperties().Where(info => info.Name.ToLower().Contains(tb.Text.ToLower())))
					 {
						 try
						 {
							 object value = propertyInfo.GetValue(propObject, new object[] { });
							 string desc = string.Empty;
							 if (value != null)
								 desc = value.GetType().ToString();
							 customClass.AddProperty(propertyInfo.Name, value, desc, String.Empty, propObject.GetType());
						 }
						 catch (Exception e)
						 {
							 Trace.TraceError(e.Message);
						 }
					 }
					 propertyGrid.SelectedObject = customClass;
				 }
				 else
				 {
					 var customClass = new CustomClass();
					 var obj = propObject as CustomClass;
					 if (obj != null)
					 {
						 foreach (PropertyDescriptor prop in obj.GetProperties())
						 {
							 if (prop.Name.ToLower().Contains(tb.Text.ToLower()))
								 customClass.AddProperty(prop.Name, prop.GetValue(obj), prop.Description, prop.Category, prop.PropertyType);
						 }
					 }
					 propertyGrid.SelectedObject = customClass;
				 }
			 }
			 else
			 {
				 if (propObject != null)
					 propertyGrid.SelectedObject = propObject;
			 }
			 frominternal = false;
		 }

		 private void SearchBoxLeave(ToolStripTextBox tb)
		 {
			 if (String.IsNullOrWhiteSpace(tb.Text))
			 {
				 tb.Text = searchText;
				 tb.ForeColor = Color.Gray;
			 }
		 }

		 private void SearchBoxEnter(ToolStripTextBox tb)
		 {
			 if (tb.Text == searchText)
			 {
				 tb.ForeColor = Color.Black;
				 tb.Text = string.Empty;
			 }
		 }
	}

	/// <summary>
	/// ToolStripSpringTextBox
	/// Diese Klasse überschreibt die GetPreferredSize-Methode, 
	/// um die verfügbare Breite des übergeordneten ToolStrip-Steuerelements zu berechnen, 
	/// nachdem die Gesamtbreite aller anderen Elemente subtrahiert wurde.
	/// </summary>
	public class ToolStripSpringTextBox : ToolStripTextBox
	{
		/// <summary>
		/// </summary>
		/// <param name="constrainingSize">The custom-sized area for a control.</param>
		/// <returns>
		/// An ordered pair of type <see cref="T:System.Drawing.Size"/> representing the width and height of a rectangle.
		/// </returns>
		public override Size GetPreferredSize(Size constrainingSize)
		{
			// Use the default size if the text box is on the overflow menu
			// or is on a vertical ToolStrip.
			if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
			{
				return DefaultSize;
			}

			// Declare a variable to store the total available width as 
			// it is calculated, starting with the display width of the 
			// owning ToolStrip.
			Int32 width = Owner.DisplayRectangle.Width;

			// Subtract the width of the overflow button if it is displayed. 
			if (Owner.OverflowButton.Visible)
			{
				width = width - Owner.OverflowButton.Width -
					Owner.OverflowButton.Margin.Horizontal;
			}

			// Declare a variable to maintain a count of ToolStripSpringTextBox 
			// items currently displayed in the owning ToolStrip. 
			Int32 springBoxCount = 0;

			foreach (ToolStripItem item in Owner.Items)
			{
				// Ignore items on the overflow menu.
				if (item.IsOnOverflow) continue;

				if (item is ToolStripSpringTextBox)
				{
					// For ToolStripSpringTextBox items, increment the count and 
					// subtract the margin width from the total available width.
					springBoxCount++;
					width -= item.Margin.Horizontal;
				}
				else
				{
					// For all other items, subtract the full width from the total
					// available width.
					width = width - item.Width - item.Margin.Horizontal;
				}
			}

			// If there are multiple ToolStripSpringTextBox items in the owning
			// ToolStrip, divide the total available width between them. 
			if (springBoxCount > 1) width /= springBoxCount;

			// If the available width is less than the default width, use the
			// default width, forcing one or more items onto the overflow menu.
			if (width < DefaultSize.Width) width = DefaultSize.Width;

			// Retrieve the preferred size from the base class, but change the
			// width to the calculated width. 
			Size size = base.GetPreferredSize(constrainingSize);
			size.Width = width;
			return size;
		}
	}

}