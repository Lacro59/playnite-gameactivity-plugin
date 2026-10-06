using CommonPluginsShared;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
	/// <summary>Exclude-from-tracking games settings section.</summary>
	public partial class GaExcludeTrackingSettingsSection : UserControl
	{
		private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		private ObservableCollection<Game> _excludeTrackingGames;
		private bool _excludeTrackingListInitialized;

		/// <summary>Initializes a new instance of the <see cref="GaExcludeTrackingSettingsSection"/> class.</summary>
		public GaExcludeTrackingSettingsSection()
		{
			InitializeComponent();
			Loaded += GaExcludeTrackingSettingsSection_Loaded;
		}

		/// <summary>Ensures the exclude list is hydrated when the section becomes visible.</summary>
		public void EnsureInitialized()
		{
			EnsureExcludeTrackingListInitialized();
		}

		private void GaExcludeTrackingSettingsSection_Loaded(object sender, RoutedEventArgs e)
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
			global::GameActivity.GameActivitySettingsView.EditingExcludeTrackingGameIds =
				_excludeTrackingGames?.Select(g => g.Id).ToList() ?? new List<Guid>();
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
				WindowOptions windowOptions = new WindowOptions
				{
					EnableWindowPersistence = false
				};
				Window window = PlayniteUiHelper.CreateExtensionWindow(
					PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCGameActivityExcludeTrackingAddDialogTitle"),
					view,
					windowOptions);
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
				Button button = sender as Button;
				if (_excludeTrackingGames == null || button == null)
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
	}
}
