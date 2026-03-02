using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using GameActivity.Models.ExportData;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;

namespace GameActivity.Services
{
	/// <summary>
	/// Provides Playnite game context menu and main menu items for the GameActivity plugin.
	/// Inherits shared context (settings, database) from <see cref="PluginMenus"/>.
	/// </summary>
	public class GameActivityMenus : PluginMenus
	{
		private GenericPlugin _plugin;

		// Only used in DEBUG builds for the diagnostic test menu item.
		private GameActivityMonitoring _monitoring;

		/// <summary>
		/// Initializes a new instance of <see cref="GameActivityMenus"/>.
		/// </summary>
		/// <param name="pluginSettings">The plugin settings view model.</param>
		/// <param name="database">The GameActivity plugin database service.</param>
		public GameActivityMenus(PluginSettings settings, IPluginDatabase database) : base(settings, database)
		{
		}

		public void AddData(GenericPlugin plugin, GameActivityMonitoring monitoring)
		{
			_plugin = plugin;
			_monitoring = monitoring;
		}

		/// <summary>
		/// Resolves the absolute path to the chart icon displayed in the game context menu.
		/// </summary>
		private string ChartIconPath => Path.Combine(_database.Paths.PluginPath, "Resources", "chart-646.png");

		/// <summary>
		/// Computes the Playnite menu section label.
		/// Prepends <c>"@"</c> when <see cref="PluginSettings.MenuInExtensions"/> is <c>true</c>,
		/// which instructs Playnite to nest the section under the Extensions sub-menu.
		/// </summary>
		private string MenuSection
		{
			get
			{
				string prefix = _settings.MenuInExtensions ? "@" : string.Empty;
				return prefix + ResourceProvider.GetString("LOCGameActivity");
			}
		}

		/// <summary>
		/// Returns game context menu items shown when right-clicking a game in the Playnite library.
		/// </summary>
		/// <param name="args">Context arguments containing the list of selected games.</param>
		/// <returns>
		///     An enumerable of <see cref="GameMenuItem"/> instances injected into the game
		///     right-click menu.
		/// </returns>
		public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
		{
			// Guard: nothing to show if no game is selected.
			if (args?.Games == null || !args.Games.Any())
			{
				yield break;
			}

			// Capture before lambda to prevent unintended closure capture.
			Game selectedGame = args.Games.First();

			yield return new GameMenuItem
			{
				Icon = ChartIconPath,
				Description = ResourceProvider.GetString("LOCGameActivityViewGameActivity"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.PluginWindows.ShowPluginGameDataWindow(_plugin, selectedGame);
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, $"[GetGameMenuItems] Failed to open activity window for '{selectedGame?.Name}'.");
					}
				}
			};

#if DEBUG
			yield return new GameMenuItem
			{
				MenuSection = ResourceProvider.GetString("LOCGameActivity"),
				Description = "Test",
				Action = (menuArgs) => { }
			};
#endif
		}

		/// <summary>
		/// Returns main menu items shown in the Playnite Extensions menu.
		/// Covers: activity overview, CSV export, and database maintenance utilities.
		/// </summary>
		/// <param name="args">Context arguments (unused; present for Playnite API consistency).</param>
		/// <returns>
		///     An enumerable of <see cref="MainMenuItem"/> instances injected into the main menu.
		/// </returns>
		public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
		{
			// Compute once: avoids redundant string allocations across every yielded item.
			string section = MenuSection;

			// ── View ─────────────────────────────────────────────────────────────────
			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = ResourceProvider.GetString("LOCGameActivityViewGamesActivities"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.PluginWindows.ShowPluginGameDataWindow(_plugin);
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, "[GetMainMenuItems] Failed to open games activities window.");
					}
				}
			};

			yield return new MainMenuItem { MenuSection = section, Description = "-" };

			// ── CSV Export ───────────────────────────────────────────────────────────
			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = ResourceProvider.GetString("LOCCommonExtractToCsv"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.ExtractToCsv();
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, "[GetMainMenuItems] Failed to export current game to CSV.");
					}
				}
			};

			yield return new MainMenuItem { MenuSection = section, Description = "-" };

			// ── Database maintenance ─────────────────────────────────────────────────
			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = ResourceProvider.GetString("LOCGaGamesDataMismatch"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.PluginWindows.ShowPluginDataMismatch();
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, "[GetMainMenuItems] Failed to open data mismatch window.");
					}
				}
			};

			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = ResourceProvider.GetString("LOCCommonTransferPluginData"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.PluginWindows.ShowPluginTransfertData(_database.GetDataGames());
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, "[GetMainMenuItems] Failed to open transfer data window.");
					}
				}
			};

			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = ResourceProvider.GetString("LOCCommonIsolatedPluginData"),
				Action = (menuArgs) =>
				{
					try
					{
						_database.PluginWindows.ShowPluginDataWithoutGame(_database.GetIsolatedDataGames());
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, "[GetMainMenuItems] Failed to open isolated data window.");
					}
				}
			};

#if DEBUG
			yield return new MainMenuItem { MenuSection = section, Description = "-" };
			yield return new MainMenuItem
			{
				MenuSection = section,
				Description = "Test",
				Action = (menuArgs) =>
				{
					// Null-conditional: _monitoring is not guaranteed in all build configurations.
					_monitoring?.GetCurrentMetrics();
				}
			};
#endif
		}
	}
}