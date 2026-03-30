using GameActivity.Models;
using GameActivity.Models.ExportData;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.SystemInfo;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GameActivity.Services
{
	/// <summary>
	/// Provides the GameActivity-specific database layer on top of
	/// <see cref="PluginDatabaseObject{TSettings,TDatabase,TItem,TDetails}"/>.
	/// Manages <see cref="GameActivities"/> entries, theme-resource binding,
	/// game-info synchronisation, and CSV export.
	/// </summary>
	public class GameActivityDatabase : PluginDatabaseObject<GameActivitySettings, GameActivities, Activity>
	{
		#region Fields

		/// <summary>
		/// Backing store for the lazily-initialised <see cref="SystemConfigurationManager"/>.
		/// </summary>
		private readonly Lazy<SystemConfigurationManager> _systemConfigurationManager;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the <see cref="SystemConfigurationManager"/> responsible for reading and writing
		/// the hardware-configuration file (<c>Configurations.json</c>).
		/// Initialised lazily on first access to avoid unnecessary I/O at startup.
		/// </summary>
		public SystemConfigurationManager SystemConfigurationManager => _systemConfigurationManager.Value;

		#endregion

		#region Constructor

		/// <summary>
		/// Initialises a new <see cref="GameActivityDatabase"/> instance.
		/// </summary>
		/// <param name="pluginSettings">View-model that owns the plugin settings.</param>
		/// <param name="pluginUserDataPath">Root directory for all user data written by this plugin.</param>
		public GameActivityDatabase(GameActivitySettings pluginSettings, string pluginUserDataPath)
			: base(pluginSettings, "GameActivity", pluginUserDataPath)
		{
			PluginWindows = new GameActivityWindows(PluginName, this);
			PluginExportCsv = new GameActivityExport();

			// Defer file I/O until the manager is actually needed.
			_systemConfigurationManager = new Lazy<SystemConfigurationManager>(() =>
				new SystemConfigurationManager(
					Path.Combine(Paths.PluginUserDataPath, "Configurations.json"),
					false));
		}

		#endregion

		#region Core Overrides

		/// <summary>
		/// Returns the <see cref="GameActivities"/> entry for <paramref name="id"/>.
		/// If no entry exists yet, an empty default record is created and persisted automatically.
		/// </summary>
		/// <remarks>
		/// <paramref name="onlyCache"/> and <paramref name="force"/> are intentionally ignored:
		/// activity data is always local — no remote source is involved.
		/// </remarks>
		/// <param name="id">Playnite game identifier.</param>
		/// <param name="onlyCache">Unused — kept for interface compatibility.</param>
		/// <param name="force">Unused — kept for interface compatibility.</param>
		/// <returns>
		/// The existing or newly-created <see cref="GameActivities"/>;
		/// <c>null</c> if the game no longer exists in the Playnite database.
		/// </returns>
		public override GameActivities Get(Guid id, bool onlyCache = false, bool force = false)
		{
			GameActivities gameActivities = GetOnlyCache(id);

			if (gameActivities == null)
			{
				Game game = API.Instance.Database.Games.Get(id);
				if (game != null)
				{
					gameActivities = GetDefault(game);
					Add(gameActivities);
				}
			}

			return gameActivities;
		}

		/// <summary>
		/// Pushes activity-related values into <see cref="GameActivitySettingsViewModel.Settings"/>
		/// so theme bindings update automatically when the active game changes.
		/// All properties are reset to their empty/zero defaults when no data exists.
		/// </summary>
		/// <param name="game">Game whose activity data should be reflected in the theme.</param>
		public override void SetThemesResources(Game game)
		{
			GameActivities gameActivities = Get(game, true);

			if (gameActivities == null)
			{
				ResetThemeResources();
				return;
			}

			// Cache the converted timestamp to avoid three separate UTC→local conversions.
			DateTime lastSession = gameActivities.GetLastSession().ToLocalTime();

			PlayTimeToStringConverter converter = new PlayTimeToStringConverter();
			string playtime = (string)converter.Convert(
				gameActivities.GetLastSessionActivity().ElapsedSeconds,
				null, null, CultureInfo.CurrentCulture);

			PluginSettings.HasData = gameActivities.HasData;
			PluginSettings.HasDataLog = gameActivities.HasDataDetails();
			PluginSettings.LastDateSession = lastSession.ToString(Constants.DateUiFormat);
			PluginSettings.LastDateTimeSession = lastSession.ToString(Constants.DateUiFormat)
														+ " " + lastSession.ToString(Constants.TimeUiFormat);
			PluginSettings.LastPlaytimeSession = playtime;
			PluginSettings.AvgFpsAllSession = gameActivities.ItemsDetails.AvgFpsAllSession;
			PluginSettings.RecentActivity = gameActivities.GetRecentActivity();
		}

		/*
		/// <summary>
		/// Handles Playnite's <c>Games.ItemUpdated</c> event.
		/// Keeps the plugin database in sync with updated game metadata and ensures a default
		/// activity entry exists for games that have never been launched.
		/// </summary>
		public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			if (e?.UpdatedItems == null)
			{
				return;
			}

			foreach (ItemUpdateEvent<Game> gameUpdated in e.UpdatedItems)
			{
				Guid id = gameUpdated.NewData.Id;
				_database.SetGameInfoDetails<Activity, ActivityDetails>(gameUpdated.NewData.Id);

				// Side-effect: auto-creates a default entry for games with no activity record.
				Get(id);
			}
		}
		*/

		/// <summary>
		/// Merges activity sessions and detail logs from <paramref name="fromId"/> into
		/// <paramref name="toId"/>. Existing detail-log keys in the target are preserved
		/// (no overwrite on collision).
		/// </summary>
		/// <param name="fromId">Source game whose data is merged away.</param>
		/// <param name="toId">Target game that receives the merged data.</param>
		/// <returns>
		/// Updated <see cref="GameActivities"/> for <paramref name="toId"/>,
		/// or <c>null</c> on error.
		/// </returns>
		public override PluginGameEntry MergeData(Guid fromId, Guid toId)
		{
			try
			{
				GameActivities fromData = Get(fromId, true);
				GameActivities toData = Get(toId, true);

				// Merge flat session list.
				toData.Items.AddRange(fromData.Items);

				// Merge detail-log dictionary; existing keys are preserved.
				foreach (var entry in fromData.ItemsDetails.Items)
				{
					toData.ItemsDetails.Items.TryAdd(entry.Key, entry.Value);
				}

				return toData;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		#endregion

		#region Query

		/// <summary>
		/// Returns a snapshot list of all <see cref="GameActivities"/> entries in the database.
		/// </summary>
		/// <remarks>
		/// For streaming or large datasets, prefer iterating <see cref="Database"/> directly
		/// to avoid the allocation of a full copy.
		/// </remarks>
		public List<GameActivities> GetListGameActivity()
		{
			LiteDbItemCollection<GameActivities> db = GetDatabaseSafe();
			if (db == null)
			{
				return new List<GameActivities>();
			}

			List<GameActivities> listGameActivity = db.FindAll()?.ToList() ?? new List<GameActivities>();

			return listGameActivity;
		}

		/// <summary>
		/// Returns all <see cref="GameActivities"/> whose recorded playtime or play-count
		/// diverges from the values stored in Playnite, signalling a synchronisation mismatch.
		/// </summary>
		/// <param name="withHidden">
		/// <c>true</c> to include hidden games in the results.
		/// </param>
		/// <returns>A (possibly empty) sequence of mismatched entries.</returns>
		public IEnumerable<GameActivities> GetGamesDataMismatch(bool withHidden)
		{
			try
			{
				LiteDbItemCollection<GameActivities> db = GetDatabaseSafe();
				if (db == null)
				{
					return Enumerable.Empty<GameActivities>();
				}

				IEnumerable<GameActivities> mismatchData = db.FindAll()
					.Where(x => x.GameExist
							  && (x.SessionPlaytime != x.Game.Playtime
								  || x.Game.PlayCount != (ulong)x.Count)
							  && (!x.Game.Hidden || withHidden)) ?? Enumerable.Empty<GameActivities>();

				return mismatchData;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return Enumerable.Empty<GameActivities>();
			}
		}

		#endregion

		#region Export / CSV



		#endregion

		#region Private Helpers

		/// <summary>
		/// Resets all theme-bound settings properties to their empty/zero defaults.
		/// Called when no <see cref="GameActivities"/> entry is found for the active game.
		/// </summary>
		private void ResetThemeResources()
		{
			PluginSettings.HasData = false;
			PluginSettings.HasDataLog = false;
			PluginSettings.LastDateSession = string.Empty;
			PluginSettings.LastDateTimeSession = string.Empty;
			PluginSettings.LastPlaytimeSession = string.Empty;
			PluginSettings.AvgFpsAllSession = 0;
		}

		/// <summary>Returns localised column headers for the aggregated (minimum) CSV export.</summary>
		private static List<string> BuildMinimumCsvHeader()
		{
			return new List<string>
			{
				ResourceProvider.GetString("LOCGameNameTitle"),
				ResourceProvider.GetString("LOCSourceLabel"),
				ResourceProvider.GetString("LOCPlayCountLabel"),
				ResourceProvider.GetString("LOCStatsTotalPlayTime"),
				ResourceProvider.GetString("LOCStatsTotalPlayTime"),
				ResourceProvider.GetString("LOCGameActivityLvGamesLastActivity"),
				ResourceProvider.GetString("LOCGameActivityAvgCpu"),
				ResourceProvider.GetString("LOCGameActivityAvgRam"),
				ResourceProvider.GetString("LOCGameActivityAvgGpu"),
				ResourceProvider.GetString("LOCGameActivityAvgFps"),
				ResourceProvider.GetString("LOCGameActivityAvgCpuT"),
				ResourceProvider.GetString("LOCGameActivityAvgGpuT"),
				ResourceProvider.GetString("LOCGameActivityAvgCpuP"),
				ResourceProvider.GetString("LOCGameActivityAvgGpuP"),
			};
		}

		/// <summary>Returns localised column headers for the full (per-log-entry) CSV export.</summary>
		private static List<string> BuildFullCsvHeader()
		{
			return new List<string>
			{
				ResourceProvider.GetString("LOCGameNameTitle"),
				ResourceProvider.GetString("LOCSourceLabel"),
				ResourceProvider.GetString("LOCGameActivityDateSession"),
				ResourceProvider.GetString("LOCGameActivityDateLog"),
				ResourceProvider.GetString("LOCTimePlayed"),
				ResourceProvider.GetString("LOCTimePlayed"),
				ResourceProvider.GetString("LOCGameActivityPCName"),
				ResourceProvider.GetString("LOCGameActivityCpuUsage"),
				ResourceProvider.GetString("LOCGameActivityRamUsage"),
				ResourceProvider.GetString("LOCGameActivityGpuUsage"),
				ResourceProvider.GetString("LOCGameActivityFps"),
				ResourceProvider.GetString("LOCGameActivityCpuTemp"),
				ResourceProvider.GetString("LOCGameActivityGpuTemp"),
				ResourceProvider.GetString("LOCGameActivityCpuPower"),
				ResourceProvider.GetString("LOCGameActivityGpuPower"),
			};
		}

		/// <summary>
		/// Builds a single aggregated <see cref="ExportData"/> row for the given
		/// <paramref name="activities"/> and updates the progress display.
		/// </summary>
		/// <param name="activities">Source activity entry.</param>
		/// <param name="a">Progress context for UI feedback.</param>
		private ExportData BuildMinimumExportRow(GameActivities activities, GlobalProgressActionArgs a)
		{
			// Cache once — used nine times below for all Avg* calculations.
			DateTime lastSession = activities.GetLastSession();

			TimeSpan ts = TimeSpan.FromSeconds((int)activities.Game.Playtime);
			UpdateProgressText(a, activities);

			return new ExportData
			{
				Name = activities.Name,
				SourceName = activities.Source?.Name ?? activities.Platforms?.FirstOrDefault()?.Name ?? "Playnite",
				PlayCount = activities.Count,
				Playtime = activities.Playtime,
				PlaytimeFormat = FormatTimeSpan(ts),
				LastSession = lastSession,
				AvgCPU = activities.AvgCPU(lastSession),
				AvgRAM = activities.AvgRAM(lastSession),
				AvgGPU = activities.AvgGPU(lastSession),
				AvgFPS = activities.AvgFPS(lastSession),
				AvgCPUT = activities.AvgCPUT(lastSession),
				AvgGPUT = activities.AvgGPUT(lastSession),
				AvgCPUP = activities.AvgCPUP(lastSession),
				AvgGPUP = activities.AvgGPUP(lastSession),
			};
		}

		/// <summary>
		/// Appends one <see cref="ExportDataAll"/> row per hardware-log entry across all
		/// sessions of <paramref name="activities"/>.
		/// </summary>
		/// <param name="activities">Source activity entry.</param>
		/// <param name="target">List that receives the generated rows.</param>
		/// <param name="a">Progress context for cancellation checks and UI feedback.</param>
		private void BuildFullExportRows(
			GameActivities activities,
			List<ExportDataAll> target,
			GlobalProgressActionArgs a)
		{
			foreach (Activity session in activities.Items)
			{
				if (a.CancelToken.IsCancellationRequested)
				{
					return;
				}

				UpdateProgressText(a, activities);

				TimeSpan ts = TimeSpan.FromSeconds((int)session.ElapsedSeconds);
				List<ActivityDetailsData> details = activities.ItemsDetails.Get((DateTime)session.DateSession);

				foreach (ActivityDetailsData z in details)
				{
					target.Add(new ExportDataAll
					{
						Name = activities.Name,
						SourceName = activities.Source?.Name ?? activities.Platforms?.FirstOrDefault()?.Name ?? "Playnite",
						Session = session.DateSession,
						DateTimeValue = z.Datelog,
						Playtime = session.ElapsedSeconds,
						PlaytimeFormat = FormatTimeSpan(ts),
						PC = session.Configuration.Name,
						CPU = z.CPU,
						RAM = z.RAM,
						GPU = z.GPU,
						FPS = z.FPS,
						CPUT = z.CPUT,
						GPUT = z.GPUT,
						CPUP = z.CPUP,
						GPUP = z.GPUP,
					});
				}

				a.CurrentProgressValue++;
			}
		}

		/// <summary>
		/// Formats a <see cref="TimeSpan"/> as <c>HH:mm:ss</c>.
		/// </summary>
		/// <param name="ts">Duration to format.</param>
		/// <returns>Zero-padded string in <c>HH:mm:ss</c> format.</returns>
		private static string FormatTimeSpan(TimeSpan ts)
		{
			// {0} = total hours (can exceed 23), {1} = minutes, {2} = seconds.
			return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}

		/// <summary>
		/// Updates <see cref="GlobalProgressActionArgs.Text"/> with the current extraction
		/// progress counter and the active game name / source.
		/// </summary>
		/// <param name="a">Progress context to mutate.</param>
		/// <param name="activities">Activity entry whose game name is displayed.</param>
		private void UpdateProgressText(GlobalProgressActionArgs a, GameActivities activities)
		{
			string sourceSuffix = activities.Game?.Source == null
				? string.Empty
				: $" ({activities.Game.Source.Name})";

			a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessingExtraction")}"
				   + $"\n\n{a.CurrentProgressValue}/{a.ProgressMaxValue}"
				   + $"\n{activities.Game?.Name}{sourceSuffix}";
		}

		#endregion
	}
}