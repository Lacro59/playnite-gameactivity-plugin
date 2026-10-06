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
	/// <summary>Ignore data-mismatch games settings section.</summary>
	public partial class GaIgnoreMismatchSettingsSection : UserControl
	{
		private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		private ObservableCollection<Game> _ignoreMismatchGames;
		private bool _ignoreMismatchListInitialized;

		/// <summary>Initializes a new instance of the <see cref="GaIgnoreMismatchSettingsSection"/> class.</summary>
		public GaIgnoreMismatchSettingsSection()
		{
			InitializeComponent();
			Loaded += GaIgnoreMismatchSettingsSection_Loaded;
		}

		/// <summary>Ensures the ignore-mismatch list is hydrated when the section becomes visible.</summary>
		public void EnsureInitialized()
		{
			EnsureIgnoreMismatchListInitialized();
		}

		private global::GameActivity.GameActivitySettings EditingSettings
		{
			get
			{
				global::GameActivity.GameActivitySettingsViewModel vm = DataContext as global::GameActivity.GameActivitySettingsViewModel;
				return vm?.Settings;
			}
		}

		private void GaIgnoreMismatchSettingsSection_Loaded(object sender, RoutedEventArgs e)
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

				global::GameActivity.GameActivitySettings settings = EditingSettings;
				if (settings == null)
				{
					return;
				}

				global::GameActivity.GameActivitySettingsViewModel.SanitizeIgnoredMismatchGameIds(settings);

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
			global::GameActivity.GameActivitySettings settings = EditingSettings;
			if (settings == null)
			{
				return;
			}

			global::GameActivity.GameActivitySettingsViewModel.SanitizeIgnoredMismatchGameIds(settings);
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
				WindowOptions windowOptions = new WindowOptions
				{
					EnableWindowPersistence = false
				};
				Window window = PlayniteUiHelper.CreateExtensionWindow(
					PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCGameActivityIgnoreMismatchAddDialogTitle"),
					view,
					windowOptions);
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
				Button button = sender as Button;
				if (_ignoreMismatchGames == null || button == null)
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
	}
}
