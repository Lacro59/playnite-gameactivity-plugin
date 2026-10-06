using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameActivity.Views
{
	/// <summary>
	/// Master-detail host for GameActivity settings tabs: grouped, optionally searchable left
	/// navigation and a detail pane showing the selected item's lazily created view.
	/// </summary>
	public partial class GaSettingsMasterDetailControl : UserControl
	{
		private ICollectionView _itemsView;

		/// <summary>
		/// Initializes a new instance of the <see cref="GaSettingsMasterDetailControl"/> class.
		/// </summary>
		public GaSettingsMasterDetailControl()
		{
			InitializeComponent();
		}

		/// <summary>Navigation items displayed in the left list.</summary>
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(
				nameof(ItemsSource),
				typeof(IEnumerable),
				typeof(GaSettingsMasterDetailControl),
				new PropertyMetadata(null, OnItemsSourceChanged));

		/// <summary>Navigation items displayed in the left list.</summary>
		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		/// <summary>Currently selected navigation item.</summary>
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(
				nameof(SelectedItem),
				typeof(GaSettingsNavigationItem),
				typeof(GaSettingsMasterDetailControl),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnSelectedItemChanged));

		/// <summary>Currently selected navigation item.</summary>
		public GaSettingsNavigationItem SelectedItem
		{
			get { return (GaSettingsNavigationItem)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		/// <summary>Filter text applied to navigation items.</summary>
		public static readonly DependencyProperty SearchTextProperty =
			DependencyProperty.Register(
				nameof(SearchText),
				typeof(string),
				typeof(GaSettingsMasterDetailControl),
				new PropertyMetadata(string.Empty, OnSearchTextChanged));

		/// <summary>Filter text applied to navigation items.</summary>
		public string SearchText
		{
			get { return (string)GetValue(SearchTextProperty); }
			set { SetValue(SearchTextProperty, value); }
		}

		/// <summary>Whether the search box is visible.</summary>
		public static readonly DependencyProperty ShowSearchProperty =
			DependencyProperty.Register(
				nameof(ShowSearch),
				typeof(bool),
				typeof(GaSettingsMasterDetailControl),
				new PropertyMetadata(true));

		/// <summary>Whether the search box is visible.</summary>
		public bool ShowSearch
		{
			get { return (bool)GetValue(ShowSearchProperty); }
			set { SetValue(ShowSearchProperty, value); }
		}

		/// <summary>Whether navigation items are grouped by <see cref="GaSettingsNavigationItem.GroupName"/>.</summary>
		public static readonly DependencyProperty EnableGroupingProperty =
			DependencyProperty.Register(
				nameof(EnableGrouping),
				typeof(bool),
				typeof(GaSettingsMasterDetailControl),
				new PropertyMetadata(false, OnEnableGroupingChanged));

		/// <summary>Whether navigation items are grouped by <see cref="GaSettingsNavigationItem.GroupName"/>.</summary>
		public bool EnableGrouping
		{
			get { return (bool)GetValue(EnableGroupingProperty); }
			set { SetValue(EnableGroupingProperty, value); }
		}

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GaSettingsMasterDetailControl control = d as GaSettingsMasterDetailControl;
			if (control != null)
			{
				control.ConfigureItemsView();
			}
		}

		private static void OnEnableGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GaSettingsMasterDetailControl control = d as GaSettingsMasterDetailControl;
			if (control != null)
			{
				control.ConfigureItemsView();
			}
		}

		private void ConfigureItemsView()
		{
			_itemsView = ItemsSource == null ? null : CollectionViewSource.GetDefaultView(ItemsSource);
			if (_itemsView == null)
			{
				return;
			}

			_itemsView.Filter = FilterItem;
			_itemsView.GroupDescriptions.Clear();
			if (EnableGrouping)
			{
				_itemsView.GroupDescriptions.Add(
					new PropertyGroupDescription(nameof(GaSettingsNavigationItem.GroupName)));
			}

			_itemsView.Refresh();
		}

		private bool FilterItem(object item)
		{
			GaSettingsNavigationItem navigationItem = item as GaSettingsNavigationItem;
			if (navigationItem == null)
			{
				return false;
			}

			string searchText = SearchText;
			if (string.IsNullOrWhiteSpace(searchText))
			{
				return true;
			}

			return ContainsSearchText(navigationItem.DisplayName, searchText) ||
				ContainsSearchText(navigationItem.Key, searchText) ||
				ContainsSearchText(navigationItem.GroupName, searchText) ||
				ContainsSearchText(navigationItem.Subtitle, searchText);
		}

		private static bool ContainsSearchText(string value, string searchText)
		{
			return !string.IsNullOrWhiteSpace(value) &&
				value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GaSettingsMasterDetailControl control = d as GaSettingsMasterDetailControl;
			if (control != null)
			{
				if (control._itemsView != null)
				{
					control._itemsView.Refresh();
				}

				control.SelectFirstVisibleItemIfNeeded();
			}
		}

		private void SelectFirstVisibleItemIfNeeded()
		{
			if (_itemsView == null)
			{
				return;
			}

			if (SelectedItem != null && _itemsView.Contains(SelectedItem))
			{
				return;
			}

			SelectedItem = _itemsView.Cast<GaSettingsNavigationItem>().FirstOrDefault();
		}

		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GaSettingsMasterDetailControl control = d as GaSettingsMasterDetailControl;
			GaSettingsNavigationItem item = e.NewValue as GaSettingsNavigationItem;
			if (control != null && item != null)
			{
				control.OnSelectedItemChangedInternal(item);
			}
		}

		private void OnSelectedItemChangedInternal(GaSettingsNavigationItem item)
		{
			if (item == null)
			{
				return;
			}

			if (item.IsRedirect)
			{
				GaSettingsNavigationItem redirectTarget = null;
				if (ItemsSource != null)
				{
					redirectTarget = ItemsSource
						.Cast<GaSettingsNavigationItem>()
						.FirstOrDefault(x => string.Equals(x.Key, item.RedirectKey, StringComparison.OrdinalIgnoreCase));
				}

				if (redirectTarget != null && !ReferenceEquals(item, redirectTarget))
				{
					SearchText = string.Empty;
					SelectedItem = redirectTarget;
				}

				return;
			}

			item.EnsureView();
		}
	}
}
