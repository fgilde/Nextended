using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Nextended.UI.Properties;
using Nextended.Core.Properties;
using Nextended.UI.ViewModels;
using Nextended.UI.WPF.Converters;

namespace Nextended.UI.WPF.Behaviors
{
	/// <summary>
	/// ItemSourceFilterBehavior ist ein behavoir um ein ItemsControl zu Filtern, 
	/// und funtkiniert automatisch bei ItemsControl und ItemsPresentern für templates
	/// </summary>
	public class ItemSourceFilterBehavior : Behavior<FrameworkElement> // von Framework element, da auch in Templates gehen soll
	{

		private Predicate<object> oldFilter;
		private ComboBox comboBox;

		private readonly static KeyGesture searchFocusGesture = new KeyGesture(Key.F, ModifierKeys.Control);

		#region Static / Dependency


		/// <summary>
		/// returns the SearchInAllPropertiesProperty as bool.
		/// </summary>
		public static bool GetSearchInAllProperties(DependencyObject obj)
		{
			return (bool)obj.GetValue(SearchInAllPropertiesProperty);
		}

		/// <summary>
		/// Sets the SearchInAllPropertiesProperty.
		/// </summary>
		public static void SetSearchInAllProperties(DependencyObject obj, bool value)
		{
			obj.SetValue(SearchInAllPropertiesProperty, value);
		}

		/// <summary>
		/// SearchInAllPropertiesProperty
		/// </summary>
		public static readonly DependencyProperty SearchInAllPropertiesProperty =
			DependencyProperty.RegisterAttached("SearchInAllProperties", typeof(bool), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(false));


		/// <summary>
		/// returns the PropertyNamesToFilterProperty as ObservableCollection
		/// </summary>
		public static ObservableCollection<string> GetPropertyNamesToFilter(DependencyObject obj)
		{
			return (ObservableCollection<string>)obj.GetValue(PropertyNamesToFilterProperty);
		}

		/// <summary>
		/// Sets the PropertyNamesToFilterProperty.
		/// </summary>
		public static void SetPropertyNamesToFilter(DependencyObject obj, ObservableCollection<string> value)
		{
			obj.SetValue(PropertyNamesToFilterProperty, value);
		}

		/// <summary>
		/// PropertyNamesToFilterProperty
		/// </summary>
		public static readonly DependencyProperty PropertyNamesToFilterProperty =
			DependencyProperty.RegisterAttached("PropertyNamesToFilter", typeof(ObservableCollection<string>), typeof(ItemSourceFilterBehavior),
			new UIPropertyMetadata(new ObservableCollection<string>()));


		/// <summary>
		/// returns the WaterMarkTextProperty as string.
		/// </summary>
		public static string GetWaterMarkText(DependencyObject obj)
		{
			return (string)obj.GetValue(WaterMarkTextProperty);
		}

		/// <summary>
		/// returns the FocusSearchboxKeyGestureProperty as KeyGesture.
		/// </summary>
		public static KeyGesture GetFocusSearchboxKeyGesture(DependencyObject obj)
		{
			return (KeyGesture)obj.GetValue(FocusSearchboxKeyGestureProperty);
		}

		/// <summary>
		/// Sets the FocusSearchboxKeyGestureProperty.
		/// </summary>
		public static void SetFocusSearchboxKeyGesture(DependencyObject obj, KeyGesture value)
		{
			obj.SetValue(FocusSearchboxKeyGestureProperty, value);
		}

		/// <summary>
		/// Shortcut, um direkt in die Suchbox zu springen
		/// </summary>
		public static readonly DependencyProperty FocusSearchboxKeyGestureProperty =
			DependencyProperty.RegisterAttached("FocusSearchboxKeyGesture", typeof(KeyGesture), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(searchFocusGesture, KeyGestureChanged));

		private static void KeyGestureChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			SetWaterMarkText(dependencyObject, GetWatermarkTextSuggestion(dependencyObject));
		}

		private static string GetWatermarkTextSuggestion()
		{
			return GetWatermarkTextForGesture(searchFocusGesture);
		}

		private static string GetWatermarkTextSuggestion(DependencyObject dependencyObject)
		{
			var gesture = GetFocusSearchboxKeyGesture(dependencyObject);
			return GetWatermarkTextForGesture(gesture);
		}

		private static string GetWatermarkTextForGesture(KeyGesture gesture)
		{
			if (gesture == null)
				return Resources.SearchContent;
			var shortCut = new KeyGestureToStringConverter().Convert(gesture, typeof(string), null, CultureInfo.CurrentUICulture);
			return $"{Resources.SearchContent} ({shortCut})";
		}

		/// <summary>
		/// Sets the WaterMarkTextProperty.
		/// </summary>
		public static void SetWaterMarkText(DependencyObject obj, string value)
		{
			obj.SetValue(WaterMarkTextProperty, value);
		}


		/// <summary>
		/// WaterMarkTextProperty
		/// </summary>
		public static readonly DependencyProperty WaterMarkTextProperty =
			DependencyProperty.RegisterAttached("WaterMarkText", typeof(string), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(GetWatermarkTextSuggestion()));


		/// <summary>
		/// returns the IsEnabledProperty as bool.
		/// </summary>
		public static bool GetIsFilterEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsFilterEnabledProperty);
		}

		/// <summary>
		/// Sets the IsEnabledProperty.
		/// </summary>
		public static void SetIsFilterEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsFilterEnabledProperty, value);
		}

		/// <summary>
		/// <see cref="IsCaseSensitive"/>
		/// </summary>
		public static readonly DependencyProperty IsCaseSensitiveProperty =
			DependencyProperty.Register("IsCaseSensitive", typeof(bool), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(false));

		/// <summary>
		/// IsEnabledProperty
		/// </summary>
		public static readonly DependencyProperty IsFilterEnabledProperty =
			DependencyProperty.RegisterAttached("IsFilterEnabled", typeof(bool), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(true));

		/// <summary>
		/// <see cref="ItemsControl"/>
		/// </summary>
		public static readonly DependencyProperty ItemsControlProperty =
			DependencyProperty.Register("ItemsControl", typeof(ItemsControl), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null, ItemsControlChanged));


		/// <summary>
		/// <see cref="TextBox"/>
		/// </summary>
		public static readonly DependencyProperty TextBoxProperty =
			DependencyProperty.Register("TextBox", typeof(TextBox), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null, TextBoxControlChanged));

		/// <summary>
		/// returns the PropertyNameToFilterProperty as string.
		/// </summary>
		public static string GetPropertyNameToFilter(DependencyObject obj)
		{
			return (string)obj.GetValue(PropertyNameToFilterProperty);
		}

		/// <summary>
		/// Sets the PropertyNameToFilterProperty.
		/// </summary>
		public static void SetPropertyNameToFilter(DependencyObject obj, string value)
		{
			obj.SetValue(PropertyNameToFilterProperty, value);
		}

		/// <summary>
		/// PropertyNameToFilterProperty
		/// </summary>
		public static readonly DependencyProperty PropertyNameToFilterProperty =
			DependencyProperty.RegisterAttached("PropertyNameToFilter", typeof(string), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(string.Empty));


		private static void ItemsControlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			var oldComboBox = e.OldValue as ComboBox;
			if (oldComboBox != null)
				oldComboBox.DropDownClosed -= ((ItemSourceFilterBehavior)dependencyObject).ComboBoxOnDropDownClosed;
			var newComboBox = e.NewValue as ComboBox;
			if (newComboBox != null)
				newComboBox.DropDownClosed += ((ItemSourceFilterBehavior)dependencyObject).ComboBoxOnDropDownClosed;
		}

		private static void TextBoxControlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue != null)
			{
				((TextBox)e.OldValue).TextChanged -= ((ItemSourceFilterBehavior)dependencyObject).TextBoxOnTextChanged;
				((TextBox)e.OldValue).PreviewKeyDown -= ((ItemSourceFilterBehavior)dependencyObject).TextBoxOnKeyDown;
			}
			if (e.NewValue != null)
			{
				((TextBox)e.NewValue).PreviewKeyDown += ((ItemSourceFilterBehavior)dependencyObject).TextBoxOnKeyDown;
				((TextBox)e.NewValue).TextChanged += ((ItemSourceFilterBehavior)dependencyObject).TextBoxOnTextChanged;
			}
		}
		#endregion

		#region AdditionalFilter




		/// <summary>
		/// returns the AdditionalItemFiltersProperty as ObservableCollection.
		/// </summary>
		public static ObservableCollection<ItemFilterModel<object>> GetAdditionalItemFilters(DependencyObject obj)
		{
			return (ObservableCollection<ItemFilterModel<object>>)obj.GetValue(AdditionalItemFiltersProperty);
		}

		/// <summary>
		/// Sets the AdditionalItemFiltersProperty.
		/// </summary>
		public static void SetAdditionalItemFilters(DependencyObject obj, ObservableCollection<ItemFilterModel<object>> value)
		{
			obj.SetValue(AdditionalItemFiltersProperty, value);
		}


		private static void UpdateHasFilter(DependencyObject obj)
		{
			if (obj != null)
			{
				var collection = obj.GetValue(AdditionalItemFiltersProperty) as ObservableCollection<ItemFilterModel<object>>;
				if (collection != null && collection.Any())
					obj.SetValue(HasAdditionalFiltersProperty, true);
				else
					obj.SetValue(HasAdditionalFiltersProperty, false);
				UpdateFilerButtonVisibility(obj);
			}
		}

		private static void FilterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateHasFilter(sender as DependencyObject);
		}

		/// <summary>
		/// AdditionalItemFiltersProperty
		/// </summary>
		public static readonly DependencyProperty AdditionalItemFiltersProperty =
			DependencyProperty.RegisterAttached("AdditionalItemFilters", typeof(ObservableCollection<ItemFilterModel<object>>), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null, AdditionalFiltersChanged));

		private static void AdditionalFiltersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var value = e.NewValue as ObservableCollection<ItemFilterModel<object>>;
			var old = e.OldValue as ObservableCollection<ItemFilterModel<object>>;
			if (old != null)
				old.CollectionChanged -= FilterCollectionChanged;
			if (value != null)
				value.CollectionChanged += FilterCollectionChanged;
			UpdateHasFilter(obj);
			UpdateContextMenu(obj);
		}


		/// <summary>
		/// Zusätzliche Filter die dann ausgewählt werden können
		/// </summary>
		public ObservableCollection<ItemFilterModel<object>> AdditionalItemFilters
		{
			get { return GetAdditionalItemFilters(ItemsControl); }
			set { SetAdditionalItemFilters(ItemsControl, value); }
		}


		/// <summary>
		/// returns the HasAdditionalFiltersProperty as bool.
		/// </summary>
		public static bool GetHasAdditionalFilters(DependencyObject obj)
		{
			return (bool)obj.GetValue(HasAdditionalFiltersProperty);
		}

		/// <summary>
		/// Sets the HasAdditionalFiltersProperty.
		/// </summary>
		public static void SetHasAdditionalFilters(DependencyObject obj, bool value)
		{
			obj.SetValue(HasAdditionalFiltersProperty, value);
		}

		/// <summary>
		/// HasAdditionalFiltersProperty
		/// </summary>
		public static readonly DependencyProperty HasAdditionalFiltersProperty =
			DependencyProperty.RegisterAttached("HasAdditionalFilters", typeof(bool), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(false, HasFilterChanged));

		private static void HasFilterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			UpdateFilerButtonVisibility(dependencyObject);
		}

		private static void UpdateFilerButtonVisibility(DependencyObject dependencyObject)
		{
			bool hasFilter = GetHasAdditionalFilters(dependencyObject);
			ItemSourceFilterBehavior behavior = GetItemSourceFilterBehavior(dependencyObject);
			if (behavior != null && behavior.FilterButton != null)
				behavior.FilterButton.Visibility = hasFilter ? Visibility.Visible : Visibility.Collapsed;
		}


		/// <summary>
		/// returns the ItemSourceFilterBehaviorProperty as ItemSourceFilterBehavior.
		/// </summary>
		public static ItemSourceFilterBehavior GetItemSourceFilterBehavior(DependencyObject obj)
		{
			return (ItemSourceFilterBehavior)obj.GetValue(ItemSourceFilterBehaviorProperty);
		}

		/// <summary>
		/// Sets the ItemSourceFilterBehaviorProperty.
		/// </summary>
		public static void SetItemSourceFilterBehavior(DependencyObject obj, ItemSourceFilterBehavior value)
		{
			obj.SetValue(ItemSourceFilterBehaviorProperty, value);
		}

		/// <summary>
		/// ItemSourceFilterBehaviorProperty
		/// </summary>
		public static readonly DependencyProperty ItemSourceFilterBehaviorProperty =
			DependencyProperty.RegisterAttached("ItemSourceFilterBehavior", typeof(ItemSourceFilterBehavior), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null));

		/// <summary>
		/// Text der als Platzhalter benutzt wird
		/// </summary>
		public bool HasAdditionalFilters
		{
			get { return GetHasAdditionalFilters(ItemsControl); }
			set { SetHasAdditionalFilters(ItemsControl, value); }
		}


		/// <summary>
		/// returns the CurrentFilterProperty as ItemFilterModel
		/// </summary>
		public static ItemFilterModel<object> GetCurrentFilter(DependencyObject obj)
		{
			return (ItemFilterModel<object>)obj.GetValue(CurrentFilterProperty);
		}

		/// <summary>
		/// Sets the CurrentFilterProperty.
		/// </summary>
		public static void SetCurrentFilter(DependencyObject obj, ItemFilterModel<object> value)
		{
			obj.SetValue(CurrentFilterProperty, value);
		}

		/// <summary>
		/// CurrentFilterProperty
		/// </summary>
		public static readonly DependencyProperty CurrentFilterProperty =
			DependencyProperty.RegisterAttached("CurrentFilter", typeof(ItemFilterModel<object>), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null, CurrentFilterChanged));

		private static void CurrentFilterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			ExecuteCurrentFilter(dependencyObject);
		}

		/// <summary>
		/// Aktueller filter
		/// </summary>
		public ItemFilterModel<object> CurrentFilter
		{
			get { return GetCurrentFilter(ItemsControl); }
			set { SetCurrentFilter(ItemsControl, value); }
		}


		private static void ExecuteCurrentFilter(DependencyObject dependencyObject)
		{
			var bhv = GetItemSourceFilterBehavior(dependencyObject);
			ItemFilterModel<object> currentFilter = GetCurrentFilter(dependencyObject);
			if (currentFilter == null || currentFilter.Expression == null)
				ResetFiter(dependencyObject);
			else
			{
				bhv.WaterMarkText = currentFilter.Description;
				bhv.FilterButton.FindDescendant<Image>().Source = currentFilter.Image ?? Images.BaumfilterAktivieren_24.ToImageSource();
				ItemSourceFilterBehavior behavior = GetItemSourceFilterBehavior(dependencyObject);
				behavior.UpdateItemsControlCollectionFilter(true);
			}
		}

		private static void ResetFiter(DependencyObject dependencyObject)
		{
			ItemSourceFilterBehavior behavior = GetItemSourceFilterBehavior(dependencyObject);
			behavior.WaterMarkText = GetWatermarkTextSuggestion();
			behavior.FilterButton.FindDescendant<Image>().Source = Images.BaumfilterAktivieren_24.ToImageSource();
			behavior.UpdateItemsControlCollectionFilter(false);
		}


		private static void UpdateContextMenu(DependencyObject obj)
		{
			ItemFilterModel<object> currentFilter = GetCurrentFilter(obj);
			var filterList = obj.GetValue(AdditionalItemFiltersProperty) as ObservableCollection<ItemFilterModel<object>>;
			if (filterList != null && filterList.Any())
			{
				// Imer einen filter bereitstellen der die auswahl zurücksetzt
				if (filterList.All(model => model.Expression != null))
					filterList.Insert(0, new ItemFilterModel<object>(null, Resources.EmptyFilter));

				bool shouldSetContextMenuProperty = false;
				var menu = obj.GetValue(AdditionalFilterContextMenuProperty) as ContextMenu;
				if (menu == null)
				{
					shouldSetContextMenuProperty = true;
					menu = new ContextMenu();
				}
				menu.Items.Clear();
				foreach (ItemFilterModel<object> filter in filterList)
				{
					var menuItem = new MenuItem { Header = filter.Caption, IsChecked = filter == currentFilter, Icon = new Image { Source = filter.Image, Height = 16 } };
					var filter1 = filter;
					menuItem.Click += (o, args) => SetFilter(obj, filter1);
					menu.Items.Add(menuItem);
				}
				if (shouldSetContextMenuProperty && menu.Items.Count > 0)
					obj.SetValue(AdditionalFilterContextMenuProperty, menu);
			}
			else
			{
				obj.SetValue(AdditionalFilterContextMenuProperty, null);
			}
		}

		private static void SetFilter(DependencyObject obj, ItemFilterModel<object> filter)
		{
			SetCurrentFilter(obj, filter);
		}

		/// <summary>
		/// returns the AdditionalFilterContextMenuProperty as ContextMenu.
		/// </summary>
		public static ContextMenu GetAdditionalFilterContextMenu(DependencyObject obj)
		{
			if (obj == null)
				return null;
			return (ContextMenu)obj.GetValue(AdditionalFilterContextMenuProperty);
		}

		/// <summary>
		/// Sets the AdditionalFilterContextMenuProperty.
		/// </summary>
		public static void SetAdditionalFilterContextMenu(DependencyObject obj, ContextMenu value)
		{
			obj.SetValue(AdditionalFilterContextMenuProperty, value);
		}

		/// <summary>
		/// AdditionalFilterContextMenuProperty
		/// </summary>
		public static readonly DependencyProperty AdditionalFilterContextMenuProperty =
			DependencyProperty.RegisterAttached("AdditionalFilterContextMenu", typeof(ContextMenu), typeof(ItemSourceFilterBehavior), new UIPropertyMetadata(null));


		/// <summary>
		/// Contextmenu der filter
		/// </summary>
		public ContextMenu AdditionalFilterContextMenu
		{
			get { return GetAdditionalFilterContextMenu(ItemsControl); }
			//private set { SetAdditionalFilterContextMenu(ItemsControl, value); }
		}


		/// <summary>
		/// Filterbutton
		/// </summary>
		public Button FilterButton
		{
			get { return (Button)GetValue(FilterButtonProperty); }
			set { SetValue(FilterButtonProperty, value); }
		}

		/// <summary>
		/// <see cref="FilterButton"/>
		/// </summary>
		public static readonly DependencyProperty FilterButtonProperty =
			DependencyProperty.Register("FilterButton", typeof(Button), typeof(ItemSourceFilterBehavior), new PropertyMetadata(null, FilterButtonChanged));

		private static void FilterButtonChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue != null)
			{
				((Button)e.OldValue).Click -= ((ItemSourceFilterBehavior)dependencyObject).FilterButtonClick;
				((Button)e.OldValue).Visibility = ((ItemSourceFilterBehavior)dependencyObject).AdditionalFilterContextMenu != null ? Visibility.Visible : Visibility.Collapsed;
			}
			if (e.NewValue != null)
			{
				((Button)e.NewValue).Click += ((ItemSourceFilterBehavior)dependencyObject).FilterButtonClick;
				((Button)e.NewValue).Visibility = ((ItemSourceFilterBehavior)dependencyObject).AdditionalFilterContextMenu != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private void FilterButtonClick(object sender, RoutedEventArgs e)
		{
			UpdateContextMenu(ItemsControl);
			AdditionalFilterContextMenu.IsOpen = !AdditionalFilterContextMenu.IsOpen;
		}




		#endregion


		/// <summary>
		/// Text der als Platzhalter benutzt wird
		/// </summary>
		public string WaterMarkText
		{
			get { return GetWaterMarkText(ItemsControl); }
			set { SetWaterMarkText(ItemsControl, value); }
		}

		/// <summary>
		/// Shortcut um den Focus zu setzen
		/// </summary>
		public KeyGesture FocusSearchboxKeyGesture
		{
			get { return GetFocusSearchboxKeyGesture(ItemsControl); }
			set { SetFocusSearchboxKeyGesture(ItemsControl, value); }
		}

		/// <summary>
		/// Gibt an, ob der filter groß und klein schreibung beachtet
		/// </summary>
		public bool IsCaseSensitive
		{
			get { return (bool)GetValue(IsCaseSensitiveProperty); }
			set { SetValue(IsCaseSensitiveProperty, value); }
		}

		/// <summary>
		/// Gibt an ob in allen Properties gesucht werden soll
		/// </summary>
		public bool SearchInAllProperties
		{
			get { return GetSearchInAllProperties(ItemsControl); }
			set { SetSearchInAllProperties(ItemsControl, value); }
		}

		/// <summary>
		/// Gibt an ob der filter aktiv ist
		/// </summary>
		public bool IsFilterEnabled
		{
			get { return GetIsFilterEnabled(ItemsControl); }
			set { SetIsFilterEnabled(ItemsControl, value); }
		}


		/// <summary>
		/// Eigenschaft, auf die der Filter greift
		/// </summary>
		public string PropertyNameToFilter
		{
			get { return GetPropertyNameToFilter(ItemsControl); }
			set { SetPropertyNameToFilter(ItemsControl, value); }
		}

		/// <summary>
		/// Eigenschaft, auf die der Filter greift
		/// </summary>
		public ObservableCollection<string> PropertyNamesToFilter
		{
			get { return GetPropertyNamesToFilter(ItemsControl); }
			set { SetPropertyNamesToFilter(ItemsControl, value); }
		}

		/// <summary>
		/// Das ItemsControl, welches fürs Filtern benutzt werden soll
		/// </summary>
		public ItemsControl ItemsControl
		{
			get { return (ItemsControl)GetValue(ItemsControlProperty); }
			set { SetValue(ItemsControlProperty, value); }
		}



		/// <summary>
		/// Die Textbox, die zum Filtern benutzt werden soll
		/// </summary>
		public TextBox TextBox
		{
			get { return (TextBox)GetValue(TextBoxProperty); }
			set { SetValue(TextBoxProperty, value); }
		}

		/// <summary>
		/// Called when [attached].
		/// </summary>
		protected override void OnAttached()
		{
			base.OnAttached();
			if (ItemsControl == null)
			{
				if (AssociatedObject is ItemsControl)
					ItemsControl = (ItemsControl)AssociatedObject;
				else if (AssociatedObject is ItemsPresenter)
					ItemsControl = AssociatedObject.TemplatedParent as ItemsControl;
				if (ItemsControl != null && ItemsControl.Items != null)
				{
					oldFilter = ItemsControl.Items.Filter;
				}
				comboBox = TextBox.TemplatedParent as ComboBox;
				if (comboBox != null)
				{
					comboBox.DropDownOpened += ComboBoxOnDropDownOpened;
					if (FocusSearchboxKeyGesture != null)
					{
						WaterMarkText = GetWatermarkTextSuggestion(ItemsControl);
						comboBox.KeyDown += CheckFocusShortcut;
					}
				}
				if (ItemsControl != null)
					ItemsControl.KeyDown += CheckFocusShortcut;

				SetItemSourceFilterBehavior(ItemsControl, this);
			}
		}

		private void CheckFocusShortcut(object sender, KeyEventArgs e)
		{
			if (e.Key == FocusSearchboxKeyGesture.Key && Keyboard.Modifiers == FocusSearchboxKeyGesture.Modifiers)
			{
				SetFocusToSearchElement();
				e.Handled = true;
			}
		}

		private void ComboBoxOnDropDownOpened(object sender, EventArgs eventArgs)
		{
			SetFocusToSearchElement();
		}

		private void SetFocusToSearchElement()
		{
			if (TextBox != null)
				TextBox.Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(TextBox)));
		}

		/// <summary>
		/// Called when [detaching].
		/// </summary>
		protected override void OnDetaching()
		{
			base.OnDetaching();
			TextBox.TextChanged -= TextBoxOnTextChanged;
			TextBox.PreviewKeyDown -= TextBoxOnKeyDown;
			if (comboBox != null)
			{
				comboBox.DropDownOpened -= ComboBoxOnDropDownOpened;
			}
		}


		private void ComboBoxOnDropDownClosed(object sender, EventArgs eventArgs)
		{
			TextBox.Text = string.Empty;
		}

		private void TextBoxOnKeyDown(object sender, KeyEventArgs e)
		{
			bool selectionChanged = false;
			if (e.Key == Key.Escape && !String.IsNullOrEmpty(TextBox.Text))
			{
				TextBox.Text = string.Empty;
				e.Handled = true;
			}
			if (e.Key == Key.Down && ItemsControl.Items.Count > 0)
			{
				var selector = ItemsControl as Selector;
				if (selector != null)
				{
					selectionChanged = true;
					if (selector.SelectedIndex == -1)
						selector.SelectedIndex = 0;
					else
						selector.SelectedIndex++;
				}
				e.Handled = true;
			}
			if (e.Key == Key.Up && ItemsControl.Items.Count > 0)
			{
				var selector = ItemsControl as Selector;
				if (selector != null)
				{
					selectionChanged = true;
					if (selector.SelectedIndex == -1)
						selector.SelectedIndex = ItemsControl.Items.Count - 1;
					else if (selector.SelectedIndex > 0)
						selector.SelectedIndex--;
				}
				e.Handled = true;
			}
			if (selectionChanged)
			{
				var listBox = ItemsControl as ListBox;
				if (listBox != null)
					listBox.ScrollIntoView(listBox.SelectedItem);
			}
		}

		private void TextBoxOnTextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
		{
			UpdateItemsControlCollectionFilter(false);
		}

		private void UpdateItemsControlCollectionFilter(bool isValidAdditionalFilter)
		{
			try
			{
				if (!IsFilterEnabled || ItemsControl == null || ItemsControl.Items == null)
					return;

				if (!String.IsNullOrEmpty(TextBox.Text) || isValidAdditionalFilter)
				{
					ItemsControl.Items.Filter = GetFilter();
				}
				else
				{
					if (CurrentFilter != null && CurrentFilter.Expression != null && string.IsNullOrEmpty(TextBox.Text))
					{
						ItemsControl.Items.Filter = o => CurrentFilter.Expression(o);
					}
					else
					{
						ItemsControl.Items.Filter = oldFilter;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private Predicate<object> GetFilter()
		{
			if (string.IsNullOrEmpty(PropertyNameToFilter))
			{
				PropertyNameToFilter = ItemsControl.DisplayMemberPath;
			}

			if (CurrentFilter == null || CurrentFilter.Expression == null)
			{
				if (oldFilter != null)
					return o => oldFilter(o) && o != null && ExecuteFilter(o);
				return o => o != null && ExecuteFilter(o);
			}
			if (oldFilter != null)
				return o => oldFilter(o) && o != null && ExecuteFilter(o) && CurrentFilter.Expression(o);
			return o => o != null && ExecuteFilter(o) && CurrentFilter.Expression(o);
		}

		private string GetPropertyValue(object o, string propertyName)
		{
			try
			{
				var propertyInfo = o.GetType().GetProperty(propertyName);
				if (propertyInfo != null)
				{
					object value = propertyInfo.GetValue(o, null);
					if (value != null)
						return value.ToString();
				}
			}
			catch (Exception)
			{
				return string.Empty;
			}
			return string.Empty;
		}

		private bool ExecuteFilter(object o)
		{
			string valueString = o.ToString();
			string searchString = TextBox.Text.ToLowerInvariant();

			if (SearchInAllProperties)
			{
				PropertyNamesToFilter = new ObservableCollection<string>(o.GetType().GetProperties().Where(info => info.PropertyType == typeof(string)).Select(info => info.Name));
			}

			if (PropertyNamesToFilter != null && PropertyNamesToFilter.Any())
			{
				return !IsCaseSensitive
					? PropertyNamesToFilter.Any(s => GetPropertyValue(o, s).ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
					: PropertyNamesToFilter.Any(s => GetPropertyValue(o, s).Contains(searchString));
			}


			if (!String.IsNullOrEmpty(PropertyNameToFilter))
			{
				valueString = GetPropertyValue(o, PropertyNameToFilter);
			}

			if (!IsCaseSensitive)
			{
				valueString = valueString.ToLowerInvariant();
				searchString = searchString.ToLowerInvariant();
			}

			return valueString.Contains(searchString);
		}
	}


}