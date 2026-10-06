using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using GameActivity.Services;
using GameActivity.Views;

namespace GameActivity
{
	/// <summary>
	/// Root GameActivity settings host: tab shell and master-detail navigation only.
	/// Section-specific UI lives in dedicated Settings section controls.
	/// </summary>
	public partial class GameActivitySettingsView : UserControl
	{
		private readonly GameActivitySettingsViewModel _viewModel;
		private GaSettingsMasterDetailControl _monitoringMasterDetail;
		private GaSettingsMasterDetailControl _displayMasterDetail;

		private GaGeneralSettingsSection _generalSection;
		private GaDisplayNavigationSettingsSection _displayNavigationSection;
		private GaDisplayControlsSettingsSection _displayControlsSection;
		private GaConfigurationSettingsSection _configurationSection;
		private GaExcludeTrackingSettingsSection _excludeTrackingSection;
		private GaIgnoreMismatchSettingsSection _ignoreMismatchSection;
		private GaMonitoringLoggingSettingsSection _loggingSection;
		private GaMonitoringProvidersSettingsSection _providersSection;
		private GaMonitoringThresholdsSettingsSection _thresholdsSection;
		private GaMonitoringWarningsSettingsSection _warningsSection;
		private GaMonitoringAlarmSettingsSection _alarmSection;

		/// <summary>
		/// Pending exclude-tracking game ids edited in the settings UI.
		/// Null when the Excluded games section was never opened; EndEdit then leaves tags unchanged.
		/// </summary>
		public static List<Guid> EditingExcludeTrackingGameIds { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GameActivitySettingsView"/> class.
		/// </summary>
		/// <param name="viewModel">Settings view model provided by the plugin.</param>
		public GameActivitySettingsView(GameActivitySettingsViewModel viewModel)
		{
			_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
			DataContext = viewModel;

			InitializeComponent();
			InitializeSectionContent();
		}

		/// <summary>
		/// Applies pending exclude-tracking list edits to Playnite tags when settings are saved.
		/// No-op when the Excluded games section was never opened.
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

		private void InitializeSectionContent()
		{
			_displayMasterDetail = CreateMasterDetailHost();
			PART_TabDisplay.Content = _displayMasterDetail;
			ConfigureDisplayNavigation();

			_monitoringMasterDetail = CreateMasterDetailHost();
			PART_TabMonitoring.Content = _monitoringMasterDetail;
			ConfigureMonitoringNavigation();

			PART_TabGeneral.Content = CreateGeneralSection();
			PART_TabConfiguration.Content = CreateConfigurationSection();
			PART_TabExcludeTracking.Content = CreateExcludeTrackingSection();
			PART_TabIgnoreMismatch.Content = CreateIgnoreMismatchSection();
		}

		private static GaSettingsMasterDetailControl CreateMasterDetailHost()
		{
			return new GaSettingsMasterDetailControl
			{
				ShowSearch = false
			};
		}

		private static void ConfigureMasterDetailNavigation(
			GaSettingsMasterDetailControl masterDetail,
			IList<GaSettingsNavigationItem> items)
		{
			masterDetail.ItemsSource = items;
			if (items.Count > 0)
			{
				masterDetail.SelectedItem = items[0];
			}
		}

		private void ConfigureDisplayNavigation()
		{
			// Same split as HLTB Display: navigation chrome vs in-game controls.
			List<GaSettingsNavigationItem> items = new List<GaSettingsNavigationItem>
			{
				new GaSettingsNavigationItem(
					"display-navigation",
					GetLoc("LOCGameActivitySettingsNavDisplayNavigation"),
					viewFactory: CreateDisplayNavigationSection),
				new GaSettingsNavigationItem(
					"display-controls",
					GetLoc("LOCGameActivitySettingsNavDisplayControls"),
					viewFactory: CreateDisplayControlsSection),
			};

			ConfigureMasterDetailNavigation(_displayMasterDetail, items);
		}

		private void ConfigureMonitoringNavigation()
		{
			// Hierarchy: Thresholds (foundation) → notification channels → session logging → providers.
			List<GaSettingsNavigationItem> items = new List<GaSettingsNavigationItem>
			{
				new GaSettingsNavigationItem(
					"mon-thresholds",
					GetLoc("LOCGameActivitySettingsNavThresholds"),
					viewFactory: CreateThresholdsSection),
				new GaSettingsNavigationItem(
					"mon-warnings",
					GetLoc("LOCGameActivitySettingsNavWarnings"),
					viewFactory: CreateWarningsSection),
				new GaSettingsNavigationItem(
					"mon-alarm",
					GetLoc("LOCGameActivitySettingsNavAlarm"),
					viewFactory: CreateAlarmSection),
				new GaSettingsNavigationItem(
					"mon-logging",
					GetLoc("LOCGameActivitySettingsNavLogging"),
					viewFactory: CreateLoggingSection),
				new GaSettingsNavigationItem(
					"mon-providers",
					GetLoc("LOCGameActivityHardwareMonitoringProvidersSection"),
					viewFactory: CreateProvidersSection),
			};

			ConfigureMasterDetailNavigation(_monitoringMasterDetail, items);
		}

		private static string GetLoc(string key)
		{
			return ResourceProvider.GetString(key);
		}

		private UserControl CreateGeneralSection()
		{
			if (_generalSection != null)
			{
				return _generalSection;
			}

			_generalSection = new GaGeneralSettingsSection();
			return _generalSection;
		}

		private UserControl CreateDisplayNavigationSection()
		{
			if (_displayNavigationSection != null)
			{
				return _displayNavigationSection;
			}

			_displayNavigationSection = new GaDisplayNavigationSettingsSection();
			return _displayNavigationSection;
		}

		private UserControl CreateDisplayControlsSection()
		{
			if (_displayControlsSection != null)
			{
				return _displayControlsSection;
			}

			_displayControlsSection = new GaDisplayControlsSettingsSection();
			return _displayControlsSection;
		}

		private UserControl CreateConfigurationSection()
		{
			if (_configurationSection != null)
			{
				return _configurationSection;
			}

			_configurationSection = new GaConfigurationSettingsSection();
			return _configurationSection;
		}

		private UserControl CreateExcludeTrackingSection()
		{
			if (_excludeTrackingSection != null)
			{
				_excludeTrackingSection.EnsureInitialized();
				return _excludeTrackingSection;
			}

			_excludeTrackingSection = new GaExcludeTrackingSettingsSection();
			_excludeTrackingSection.EnsureInitialized();
			return _excludeTrackingSection;
		}

		private UserControl CreateIgnoreMismatchSection()
		{
			if (_ignoreMismatchSection != null)
			{
				_ignoreMismatchSection.EnsureInitialized();
				return _ignoreMismatchSection;
			}

			_ignoreMismatchSection = new GaIgnoreMismatchSettingsSection();
			_ignoreMismatchSection.EnsureInitialized();
			return _ignoreMismatchSection;
		}

		private UserControl CreateLoggingSection()
		{
			if (_loggingSection != null)
			{
				return _loggingSection;
			}

			_loggingSection = new GaMonitoringLoggingSettingsSection();
			return _loggingSection;
		}

		private UserControl CreateProvidersSection()
		{
			if (_providersSection != null)
			{
				return _providersSection;
			}

			_providersSection = new GaMonitoringProvidersSettingsSection();
			return _providersSection;
		}

		private UserControl CreateThresholdsSection()
		{
			if (_thresholdsSection != null)
			{
				return _thresholdsSection;
			}

			_thresholdsSection = new GaMonitoringThresholdsSettingsSection();
			return _thresholdsSection;
		}

		private UserControl CreateWarningsSection()
		{
			if (_warningsSection != null)
			{
				return _warningsSection;
			}

			_warningsSection = new GaMonitoringWarningsSettingsSection();
			return _warningsSection;
		}

		private UserControl CreateAlarmSection()
		{
			if (_alarmSection != null)
			{
				return _alarmSection;
			}

			_alarmSection = new GaMonitoringAlarmSettingsSection();
			return _alarmSection;
		}
	}
}
