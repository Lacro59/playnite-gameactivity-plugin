using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using CommonPluginsShared;
using GameActivity.Services;
using GameActivity.Views;

namespace GameActivity
{
	public partial class GameActivitySettingsView : UserControl
	{
		private static ILogger Logger => LogManager.GetLogger();

		private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		private StackPanel SpControl { get; set; }

		private ObservableCollection<Game> _excludeTrackingGames;
		private bool _excludeTrackingListInitialized;

		private ObservableCollection<Game> _ignoreMismatchGames;
		private bool _ignoreMismatchListInitialized;

		/// <summary>
		/// Pending exclude-tracking game ids edited in the settings UI.
		/// Null when the Excluded games tab was never opened; EndEdit then leaves tags unchanged.
		/// </summary>
		public static List<Guid> EditingExcludeTrackingGameIds { get; set; }

		public GameActivitySettingsView()
		{
			InitializeComponent();

			labelIntervalLabel_text.Content = "(5 " + ResourceProvider.GetString("LOCGameActivityTimeLabel") + ")";
			Slider_ValueChanged(hwSlider, null);

			PART_SelectorColorPicker.OnlySimpleColor = true;
			PART_SelectorColorPicker.IsSimpleColor = true;
		}

		/// <summary>
		/// Applies pending exclude-tracking list edits to Playnite tags when settings are saved.
		/// No-op when the Excluded games tab was never opened.
		/// </summary>
		public static void ApplyEditingExcludeTrackingChanges()
		{
			if (EditingExcludeTrackingGameIds == null || GameActivity.PluginDatabase == null)
			{
				return;
			}

			GameActivityDatabase database = GameActivity.PluginDatabase;

			try
			{
				HashSet<Guid> pendingIds = new HashSet<Guid>(EditingExcludeTrackingGameIds);
				HashSet<Guid> currentIds = new HashSet<Guid>(
					database.GetGamesExcludedFromTracking().Select(g => g.Id));

				List<Guid> toAdd = pendingIds.Where(id => !currentIds.Contains(id)).ToList();
				List<Guid> toRemove = currentIds.Where(id => !pendingIds.Contains(id)).ToList();

				foreach (Guid gameId in toAdd)
				{
					Game game = API.Instance?.Database?.Games?.Get(gameId);
					if (game != null)
					{
						database.AddExcludeTrackingTag(game);
					}
				}

				foreach (Guid gameId in toRemove)
				{
					Game game = API.Instance?.Database?.Games?.Get(gameId);
					if (game != null)
					{
						database.RemoveExcludeTrackingTag(game);
					}
				}

				if (toAdd.Count > 0 || toRemove.Count > 0)
				{
					Common.LogDebug($"Exclude tracking settings apply: +{toAdd.Count} / -{toRemove.Count}");
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, database.PluginName);
			}
			finally
			{
				EditingExcludeTrackingGameIds = null;
			}
		}

		/// <summary>
		/// Discards pending exclude-tracking list edits when settings are cancelled.
		/// </summary>
		public static void CancelEditingExcludeTrackingChanges()
		{
			EditingExcludeTrackingGameIds = null;
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (sender != null)
			{
				Slider slider = sender as Slider;

				if (labelIntervalLabel_text != null)
				{
					labelIntervalLabel_text.Content = "(" + slider.Value + " " + ResourceProvider.GetString("LOCGameActivityTimeLabel") + ")";
				}
			}
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		#region Exclude tracking

		private void TabExcludeTracking_GotFocus(object sender, RoutedEventArgs e)
		{
			EnsureExcludeTrackingListInitialized();
		}

		private void EnsureExcludeTrackingListInitialized()
		{
			try
			{
				if (_excludeTrackingListInitialized || PluginDatabase == null)
				{
					return;
				}

				_excludeTrackingGames = new ObservableCollection<Game>(PluginDatabase.GetGamesExcludedFromTracking());
				PART_ExcludeTrackingList.ItemsSource = _excludeTrackingGames;
				SyncEditingExcludeTrackingGameIds();
				_excludeTrackingListInitialized = true;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase?.PluginName);
			}
		}

		private void SyncEditingExcludeTrackingGameIds()
		{
			EditingExcludeTrackingGameIds = _excludeTrackingGames?.Select(g => g.Id).ToList() ?? new List<Guid>();
		}

		private void ButtonExcludeTrackingAddGame_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureExcludeTrackingListInitialized();
				if (_excludeTrackingGames == null)
				{
					return;
				}

				ExcludeTrackingAddGamesView view = new ExcludeTrackingAddGamesView(
					PluginDatabase,
					_excludeTrackingGames.Select(g => g.Id));
				Window window = PlayniteUiHelper.CreateExtensionWindow(
					PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCGameActivityExcludeTrackingAddDialogTitle"),
					view);
				_ = window.ShowDialog();

				if (!view.Confirmed)
				{
					return;
				}

				foreach (Game game in view.GetSelectedGames())
				{
					if (game == null || _excludeTrackingGames.Any(g => g.Id == game.Id))
					{
						continue;
					}

					_excludeTrackingGames.Add(game);
				}

				List<Game> ordered = _excludeTrackingGames.OrderBy(g => g.Name).ToList();
				_excludeTrackingGames.Clear();
				foreach (Game game in ordered)
				{
					_excludeTrackingGames.Add(game);
				}

				SyncEditingExcludeTrackingGameIds();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		private void ButtonExcludeTrackingRemoveItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureExcludeTrackingListInitialized();
				if (_excludeTrackingGames == null || !(sender is Button button))
				{
					return;
				}

				Game game = button.Tag as Game ?? button.DataContext as Game;
				if (game == null)
				{
					return;
				}

				Game toRemove = _excludeTrackingGames.FirstOrDefault(g => g.Id == game.Id);
				if (toRemove != null)
				{
					_ = _excludeTrackingGames.Remove(toRemove);
					SyncEditingExcludeTrackingGameIds();
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		#endregion

		#region Ignore mismatch

		private GameActivitySettings EditingSettings
		{
			get
			{
				GameActivitySettingsViewModel vm = DataContext as GameActivitySettingsViewModel;
				return vm?.Settings;
			}
		}

		private void TabIgnoreMismatch_GotFocus(object sender, RoutedEventArgs e)
		{
			EnsureIgnoreMismatchListInitialized();
		}

		private void EnsureIgnoreMismatchListInitialized()
		{
			try
			{
				if (_ignoreMismatchListInitialized)
				{
					return;
				}

				GameActivitySettings settings = EditingSettings;
				if (settings == null)
				{
					return;
				}

				GameActivitySettingsViewModel.SanitizeIgnoredMismatchGameIds(settings);

				List<Game> games = new List<Game>();
				if (API.Instance?.Database?.Games != null)
				{
					foreach (Guid id in settings.IgnoredMismatchGameIds)
					{
						Game game = API.Instance.Database.Games.Get(id);
						if (game != null)
						{
							games.Add(game);
						}
					}
				}

				_ignoreMismatchGames = new ObservableCollection<Game>(games.OrderBy(g => g.Name));
				PART_IgnoreMismatchList.ItemsSource = _ignoreMismatchGames;
				SyncIgnoredMismatchGameIds();
				_ignoreMismatchListInitialized = true;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase?.PluginName);
			}
		}

		private void SyncIgnoredMismatchGameIds()
		{
			GameActivitySettings settings = EditingSettings;
			if (settings == null)
			{
				return;
			}

			GameActivitySettingsViewModel.SanitizeIgnoredMismatchGameIds(settings);
			settings.IgnoredMismatchGameIds = _ignoreMismatchGames?.Select(g => g.Id).ToList() ?? new List<Guid>();
			Common.LogDebug($"Ignore mismatch settings sync count={settings.IgnoredMismatchGameIds.Count}");
		}

		private void ButtonIgnoreMismatchAddGame_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureIgnoreMismatchListInitialized();
				if (_ignoreMismatchGames == null)
				{
					return;
				}

				ExcludeTrackingAddGamesView view = new ExcludeTrackingAddGamesView(
					PluginDatabase,
					_ignoreMismatchGames.Select(g => g.Id));
				Window window = PlayniteUiHelper.CreateExtensionWindow(
					PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCGameActivityIgnoreMismatchAddDialogTitle"),
					view);
				_ = window.ShowDialog();

				if (!view.Confirmed)
				{
					return;
				}

				foreach (Game game in view.GetSelectedGames())
				{
					if (game == null || _ignoreMismatchGames.Any(g => g.Id == game.Id))
					{
						continue;
					}

					_ignoreMismatchGames.Add(game);
				}

				List<Game> ordered = _ignoreMismatchGames.OrderBy(g => g.Name).ToList();
				_ignoreMismatchGames.Clear();
				foreach (Game game in ordered)
				{
					_ignoreMismatchGames.Add(game);
				}

				SyncIgnoredMismatchGameIds();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		private void ButtonIgnoreMismatchRemoveItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureIgnoreMismatchListInitialized();
				if (_ignoreMismatchGames == null || !(sender is Button button))
				{
					return;
				}

				Game game = button.Tag as Game ?? button.DataContext as Game;
				if (game == null)
				{
					return;
				}

				Game toRemove = _ignoreMismatchGames.FirstOrDefault(g => g.Id == game.Id);
				if (toRemove != null)
				{
					_ = _ignoreMismatchGames.Remove(toRemove);
					SyncIgnoredMismatchGameIds();
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		#endregion

		#region SetColors
		private void BtPickColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SpControl = ((Grid)((FrameworkElement)sender).Parent).Children.OfType<StackPanel>().FirstOrDefault();

				if (SpControl.Background is SolidColorBrush brush)
				{
					Color color = brush.Color;
					PART_SelectorColorPicker.SetColors(color);
				}

				PART_SelectorColor.Visibility = Visibility.Visible;
				PART_ColorListContener.Visibility = Visibility.Collapsed;
				PART_ChartColor.Visibility = Visibility.Collapsed;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		private void PART_ColorOK_Click(object sender, RoutedEventArgs e)
		{
			if (SpControl != null)
			{
				if (PART_SelectorColorPicker.IsSimpleColor)
				{
					Color color = PART_SelectorColorPicker.SimpleColor;
					SpControl.Background = new SolidColorBrush(color);

					if (SpControl.Tag != null)
					{
						int.TryParse((string)SpControl.Tag, out int index);
						PluginDatabase.PluginSettings.StoreColors[index].Fill = new SolidColorBrush(color);
					}
					else
					{
						PluginDatabase.PluginSettings.ChartColors = new SolidColorBrush(color);
					}
				}
			}
			else
			{
				Logger.Warn("One control is undefined");
			}

			PART_SelectorColor.Visibility = Visibility.Collapsed;
			PART_ColorListContener.Visibility = Visibility.Visible;
			PART_ChartColor.Visibility = Visibility.Visible;
		}

		private void PART_ColorCancel_Click(object sender, RoutedEventArgs e)
		{
			PART_SelectorColor.Visibility = Visibility.Collapsed;
			PART_ColorListContener.Visibility = Visibility.Visible;
			PART_ChartColor.Visibility = Visibility.Visible;
		}



		private void BtPickChartColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SpControl = ((Grid)((FrameworkElement)sender).Parent).Children.OfType<StackPanel>().FirstOrDefault();

				if (SpControl.Background is SolidColorBrush brush)
				{
					Color color = brush.Color;
					PART_SelectorColorPicker.SetColors(color);
				}

				PART_SelectorColor.Visibility = Visibility.Visible;
				PART_ColorListContener.Visibility = Visibility.Collapsed;
				PART_ChartColor.Visibility = Visibility.Collapsed;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			PART_Color.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");
			PluginDatabase.PluginSettings.ChartColors = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");
		}
		#endregion
	}
}
