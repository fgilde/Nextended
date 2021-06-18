using System;
using System.Collections.ObjectModel;

namespace Nextended.Core.Attributes
{ 
	/// <summary>
	/// Attribute for Properties in Auto Object Editor Template with selectable items
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class SelectableItemAttribute : Attribute
	{
		/// <summary>
		/// Name of the Property that returns a list with possible values
		/// </summary>
		public string PossibleValuesPropertyName { get; set; } 

		/// <summary>
		/// SelectedIndex
		/// </summary>
		public int SelectedIndex { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [is editable].
		/// </summary>
		public bool IsEditable { get; set; }
		
		/// <summary>
		/// Gets or sets a value indicating whether [is read only].
		/// </summary>
		/// <value>
		///   <c>true</c> if [is read only]; otherwise, <c>false</c>.
		/// </value>
		public bool IsReadOnly { get; set; }

		/// <summary>
		/// If true template will use a listbox otherwise its a combo box
		/// </summary>
		public bool UseListBox { get; set; }

		/// <summary>
		/// Set to true if you want to have possibility to filter possible values
		/// </summary>
		public bool IsFilterable { get; set; }

		/// <summary>
		/// List of Possible Values
		/// </summary>
		public ObservableCollection<object> PossibleValues { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SelectableItemAttribute" /> class.
		/// </summary>
		public SelectableItemAttribute(string possibleValuesPropertyName)
		{
			IsFilterable = true;
			PossibleValuesPropertyName = possibleValuesPropertyName;
			//IsReadOnly = true;
			//IsEditable = true;
		}
	}

	/// <summary>
	/// Editierbare liste 
	/// </summary>
	public class EditableList : SelectableItemAttribute
	{
		/// <summary>
		/// Set to true to allow add and remove objects
		/// </summary>
		public bool CanAddAndRemoveEntries { get; set; }

		/// <summary>
		/// if true user must confirm the deletion
		/// </summary>
		public ConfirmationMode ConfirmRemoveObject { get; set; }

		/// <summary>
		/// typeof object in list
		/// </summary>
		public Type ObjectType { get; set; }

		/// <summary>
		/// Ctor
		/// </summary>
		public EditableList()
			: base(string.Empty)
		{
			//ConfirmRemoveObject = ConfirmationMode.ConfirmWithOptionDontShowAgain;
			ConfirmRemoveObject = ConfirmationMode.NoConfirmation;
		}
	}

	/// <summary>
	/// Art der Nachfrage 
	/// </summary>
	public enum ConfirmationMode
	{
		/// <summary>
		/// NoConfirmation
		/// </summary>
		NoConfirmation,

		/// <summary>
		/// Default question with option for do not ask again
		/// </summary>
		ConfirmWithOptionDontShowAgain,

		/// <summary>
		/// Ask always for a confirmation
		/// </summary>
		ConfirmAlways,
	}

}