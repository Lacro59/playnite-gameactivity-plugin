using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Data;
using GameActivity.Models;
using System;
using Playnite.SDK.Models;
using System.Windows.Media;
using MoreLinq;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Plugins;

namespace GameActivity
{
	public class GameActivitySettings : PluginSettings
	{
		#region UI Integration Settings

		public bool SaveColumnOrder { get; set; } = false;

		public bool EnableIntegrationButtonHeader { get; set; } = false;
		public bool EnableIntegrationButtonSide { get; set; } = true;

		private bool _enableIntegrationButton = true;
		public bool EnableIntegrationButton { get => _enableIntegrationButton; set => SetValue(ref _enableIntegrationButton, value); }

		private bool _enableIntegrationButtonDetails = false;
		public bool EnableIntegrationButtonDetails { get => _enableIntegrationButtonDetails; set => SetValue(ref _enableIntegrationButtonDetails, value); }

		#endregion

		#region Chart Display Settings

		private bool _enableIntegrationChartTime = true;
		public bool EnableIntegrationChartTime { get => _enableIntegrationChartTime; set => SetValue(ref _enableIntegrationChartTime, value); }

		public bool ChartTimeTruncate { get; set; } = true;
		public bool ChartTimeVisibleEmpty { get; set; } = true;
		public double ChartTimeHeight { get; set; } = 120;
		public bool ChartTimeAxis { get; set; } = true;
		public bool ChartTimeOrdinates { get; set; } = true;
		public int ChartTimeCountAbscissa { get; set; } = 11;

		private bool _enableIntegrationChartLog = true;
		public bool EnableIntegrationChartLog { get => _enableIntegrationChartLog; set => SetValue(ref _enableIntegrationChartLog, value); }

		public bool ChartLogVisibleEmpty { get; set; } = true;
		public double ChartLogHeight { get; set; } = 120;
		public bool ChartLogAxis { get; set; } = true;
		public bool ChartLogOrdinates { get; set; } = true;
		public int ChartLogCountAbscissa { get; set; } = 11;

		public bool UseControls { get; set; } = true;
		public bool DisplayCpu { get; set; } = true;
		public bool DisplayGpu { get; set; } = true;
		public bool DisplayRam { get; set; } = true;
		public bool DisplayFps { get; set; } = true;

		#endregion

		#region Visual Customization

		public bool ShowLauncherIcons { get; set; } = true;
		public int ModeStoreIcon { get; set; } = 1;

		public List<StoreColor> StoreColors { get; set; } = new List<StoreColor>();
		public SolidColorBrush ChartColors { get; set; } = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");

		#endregion

		#region Session Management

		public bool CumulPlaytimeSession { get; set; } = false;
		public bool CumulPlaytimeStore { get; set; } = false;
		public bool SubstPlayStateTime { get; set; } = false; // Temporary workaround for PlayState paused time

		public bool IgnoreSession { get; set; } = false;
		public int IgnoreSessionTime { get; set; } = 120; // In seconds

		#endregion

		#region Hardware Monitoring - General Settings

		public bool EnableLogging { get; set; } = false;
		public int TimeIntervalLogging { get; set; } = 5; // In minutes

		/// <summary>
		/// Monitoring mode: 0 = Automatic (try all providers), 1 = Manual (user selects specific provider)
		/// </summary>
		public int MonitoringMode { get; set; } = 0;

		/// <summary>
		/// Maximum number of consecutive failures before switching to fallback provider
		/// </summary>
		public int MaxFailuresBeforeFallback { get; set; } = 5;

		/// <summary>
		/// Cache duration for metrics in milliseconds (reduces redundant calls)
		/// </summary>
		public int MetricsCacheDurationMs { get; set; } = 500;

		#endregion

		#region Hardware Monitoring - Provider Specific Settings

		// ═══════════════════════════════════════════════════════════════
		// RivaTuner Statistics Server (RTSS)
		// ═══════════════════════════════════════════════════════════════
		/// <summary>
		/// Enable RivaTuner Statistics Server provider (recommended for FPS)
		/// Replaces MSI Afterburner - lighter and more direct
		/// </summary>
		public bool UseRivaTuner { get; set; } = true;

		// ═══════════════════════════════════════════════════════════════
		// LibreHardware Monitor
		// ═══════════════════════════════════════════════════════════════
		/// <summary>
		/// Enable LibreHardware Monitor provider (comprehensive hardware data)
		/// </summary>
		public bool UseLibreHardware { get; set; } = false;

		/// <summary>
		/// Use remote LibreHardware web server instead of local monitoring
		/// </summary>
		[DontSerialize]
		public bool WithRemoteServerWeb { get; set; } = true;

		/// <summary>
		/// IP address of remote LibreHardware server (e.g., "192.168.1.100")
		/// </summary>
		public string IpRemoteServerWeb { get; set; } = string.Empty;

		// ═══════════════════════════════════════════════════════════════
		// HWiNFO
		// ═══════════════════════════════════════════════════════════════
		/// <summary>
		/// Enable HWiNFO shared memory provider
		/// </summary>
		public bool UseHWiNFOSharedMemory { get; set; } = false;

		/// <summary>
		/// Use HWiNFO Gadget registry method (alternative to shared memory)
		/// </summary>
		public bool UseHWiNFOGadget { get; set; } = false;

		// HWiNFO Sensor Configuration (Shared Memory Method)
		public string HWiNFO_gpu_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_gpu_elementID { get; set; } = string.Empty;
		public string HWiNFO_fps_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_fps_elementID { get; set; } = string.Empty;
		public string HWiNFO_gpuT_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_gpuT_elementID { get; set; } = string.Empty;
		public string HWiNFO_gpuP_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_gpuP_elementID { get; set; } = string.Empty;
		public string HWiNFO_cpuT_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_cpuT_elementID { get; set; } = string.Empty;
		public string HWiNFO_cpuP_sensorsID { get; set; } = string.Empty;
		public string HWiNFO_cpuP_elementID { get; set; } = string.Empty;

		// HWiNFO Gadget Configuration (Registry Method)
		public long HWiNFO_gpu_index { get; set; } = 0;
		public long HWiNFO_fps_index { get; set; } = 0;
		public long HWiNFO_gpuT_index { get; set; } = 0;
		public long HWiNFO_cpuT_index { get; set; } = 0;
		public long HWiNFO_gpuP_index { get; set; } = 0;
		public long HWiNFO_cpuP_index { get; set; } = 0;

		// ═══════════════════════════════════════════════════════════════
		// MSI Afterburner
		// ═══════════════════════════════════════════════════════════════
		public bool UseMsiAfterburner { get; set; } = false;

		// ═══════════════════════════════════════════════════════════════
		// Integrated
		// ═══════════════════════════════════════════════════════════════
		public bool UseWMI { get; set; } = true;
		public bool UsePerformanceCounter { get; set; } = true;

		#endregion

		#region Performance Warnings

		public bool EnableWarning { get; set; } = false;

		/// <summary>
		/// Minimum FPS threshold - warn if FPS drops below this value (0 = disabled)
		/// </summary>
		public int MinFps { get; set; } = 0;

		/// <summary>
		/// Maximum CPU temperature threshold in °C (0 = disabled)
		/// </summary>
		public int MaxCpuTemp { get; set; } = 0;

		/// <summary>
		/// Maximum GPU temperature threshold in °C (0 = disabled)
		/// </summary>
		public int MaxGpuTemp { get; set; } = 0;

		/// <summary>
		/// Maximum CPU usage threshold in % (0 = disabled)
		/// </summary>
		public int MaxCpuUsage { get; set; } = 0;

		/// <summary>
		/// Maximum GPU usage threshold in % (0 = disabled)
		/// </summary>
		public int MaxGpuUsage { get; set; } = 0;

		/// <summary>
		/// Maximum RAM usage threshold in % (0 = disabled)
		/// </summary>
		public int MaxRamUsage { get; set; } = 0;

		#endregion

		#region List View Display Settings

		public bool lvGamesIcon { get; set; } = true;
		public bool lvGamesPcName { get; set; } = true;
		public bool lvGamesSource { get; set; } = true;
		public bool lvGamesPlayAction { get; set; } = true;

		public bool lvAvgCpu { get; set; } = true;
		public bool lvAvgGpu { get; set; } = true;
		public bool lvAvgRam { get; set; } = true;
		public bool lvAvgFps { get; set; } = true;
		public bool lvAvgCpuT { get; set; } = true;
		public bool lvAvgGpuT { get; set; } = true;
		public bool lvAvgCpuP { get; set; } = true;
		public bool lvAvgGpuP { get; set; } = true;

		#endregion

		#region Analysis and Statistics

		public int VariatorTime { get; set; } = 7;
		public int VariatorLog { get; set; } = 4;
		public int RecentActivityWeek { get; set; } = 2;

		#endregion

		#region Custom Game Actions

		public Dictionary<Guid, List<string>> CustomGameActions { get; set; } = new Dictionary<Guid, List<string>>();

		#endregion

		#region Non-Serialized Properties (UI State)

		private bool _hasDataLog = false;
		[DontSerialize]
		public bool HasDataLog { get => _hasDataLog; set => SetValue(ref _hasDataLog, value); }

		private string _lastDateSession = string.Empty;
		[DontSerialize]
		public string LastDateSession { get => _lastDateSession; set => SetValue(ref _lastDateSession, value); }

		private string _lastDateTimeSession = string.Empty;
		[DontSerialize]
		public string LastDateTimeSession { get => _lastDateTimeSession; set => SetValue(ref _lastDateTimeSession, value); }

		private string _lastPlaytimeSession = string.Empty;
		[DontSerialize]
		public string LastPlaytimeSession { get => _lastPlaytimeSession; set => SetValue(ref _lastPlaytimeSession, value); }

		private int _avgFpsAllSession = 0;
		[DontSerialize]
		public int AvgFpsAllSession { get => _avgFpsAllSession; set => SetValue(ref _avgFpsAllSession, value); }

		private string _recentActivity = string.Empty;
		[DontSerialize]
		public string RecentActivity { get => _recentActivity; set => SetValue(ref _recentActivity, value); }

		#endregion

		#region Helper Methods

		/// <summary>
		/// Check if any hardware monitoring provider is enabled
		/// </summary>
		[DontSerialize]
		public bool HasAnyMonitoringProviderEnabled
		{
			get
			{
				return EnableLogging && (
					UseMsiAfterburner ||
					UseRivaTuner ||
					UseLibreHardware ||
					UseHWiNFOSharedMemory ||
					UseHWiNFOGadget
				);
			}
		}

		/// <summary>
		/// Check if HWiNFO is properly configured
		/// </summary>
		[DontSerialize]
		public bool IsHWiNFOConfigured
		{
			get
			{
				if (UseHWiNFOGadget)
				{
					// For gadget mode, check if at least one index is configured
					return HWiNFO_fps_index > 0 || HWiNFO_gpu_index > 0;
				}
				else if (UseHWiNFOSharedMemory)
				{
					// For shared memory mode, check if at least one sensor is configured
					return !string.IsNullOrEmpty(HWiNFO_fps_sensorsID) ||
						   !string.IsNullOrEmpty(HWiNFO_gpu_sensorsID);
				}
				return false;
			}
		}

		#endregion
	}


	public class GameActivitySettingsViewModel : ObservableObject, ISettings
	{
		private static readonly ILogger logger = LogManager.GetLogger();

		private readonly GameActivity Plugin;
		private GameActivitySettings EditingClone { get; set; }

		private GameActivitySettings _settings;
		public GameActivitySettings Settings { get => _settings; set => SetValue(ref _settings, value); }


		public GameActivitySettingsViewModel(GameActivity plugin)
		{
			// Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
			Plugin = plugin;

			// Load saved settings.
			GameActivitySettings savedSettings = plugin.LoadPluginSettings<GameActivitySettings>();

			// LoadPluginSettings returns null if not saved data is available.
			Settings = savedSettings ?? new GameActivitySettings();

			// Ensure default values for new settings
			ApplyDefaultsForNewSettings();
		}

		/// <summary>
		/// Apply default values for newly added settings to maintain backward compatibility
		/// </summary>
		private void ApplyDefaultsForNewSettings()
		{
			// Set defaults for monitoring configuration if not already set
			if (Settings.MaxFailuresBeforeFallback == 0)
			{
				Settings.MaxFailuresBeforeFallback = 5;
			}

			if (Settings.MetricsCacheDurationMs == 0)
			{
				Settings.MetricsCacheDurationMs = 500;
			}
		}

		// Code executed when settings view is opened and user starts editing values.
		public void BeginEdit()
		{
			EditingClone = Serialization.GetClone(Settings);

			// Set default store colors
			if (Settings.StoreColors.Count == 0)
			{
				Settings.StoreColors = GetDefaultStoreColors();
			}

			// Set missing store colors
			UpdateMissingStoreColors();
		}

		/// <summary>
		/// Update store colors for new sources and platforms
		/// </summary>
		private void UpdateMissingStoreColors()
		{
			// Find sources without colors
			List<Guid> sourceIds = GameActivity.PluginDatabase.Database.Items
				.Where(x => !Settings.StoreColors.Any(y => x.Value.SourceId == y.Id))
				.Select(x => x.Value.SourceId)
				.Distinct()
				.ToList();

			// Find platforms without colors
			List<Platform> platformIds = GameActivity.PluginDatabase.Database.Items
				.Where(x => x.Value != null && x.Value.Platforms != null)
				.SelectMany(x => x.Value.Platforms)
				.Where(x => !Settings.StoreColors.Any(y => x.Id == y.Id))
				.Distinct()
				.ToList();

			// Add missing source colors
			foreach (Guid id in sourceIds)
			{
				string name = GetSourceName(id);
				if (!name.IsNullOrEmpty() && Settings.StoreColors.All(x => !x.Name.Equals(name)))
				{
					Brush fill = GetColor(name);
					Settings.StoreColors.Add(new StoreColor
					{
						Name = name,
						Id = id,
						Fill = fill
					});
				}
			}

			// Add missing platform colors
			foreach (Platform platform in platformIds)
			{
				string name = NormalizePlatformName(platform.Name);
				if (Settings.StoreColors.All(x => !x.Name.Equals(name)))
				{
					Brush fill = GetColor(name);
					Settings.StoreColors.Add(new StoreColor
					{
						Name = name,
						Id = platform.Id,
						Fill = fill
					});
				}
			}

			// Remove duplicates and sort
			Settings.StoreColors = Settings.StoreColors
				.DistinctBy(x => x.Name)
				.OrderBy(x => x.Name)
				.ToList();
		}

		/// <summary>
		/// Get normalized source name
		/// </summary>
		private string GetSourceName(Guid id)
		{
			if (id == default)
			{
				return "Playnite";
			}

			string name = API.Instance.Database.Sources.Get(id)?.Name;
			if (name.IsNullOrEmpty())
			{
				logger.Warn($"No name found for SourceId {id}");
				return null;
			}

			return NormalizePlatformName(name);
		}

		/// <summary>
		/// Normalize platform/source names (PC variants -> Playnite)
		/// </summary>
		private string NormalizePlatformName(string name)
		{
			if (name.IsEqual("PC (Windows)") ||
				name.IsEqual("PC (Mac)") ||
				name.IsEqual("PC (Linux)"))
			{
				return "Playnite";
			}
			return name;
		}

		// Code executed when user decides to cancel any changes made since BeginEdit was called.
		public void CancelEdit()
		{
			Settings = EditingClone;
		}

		// Code executed when user decides to confirm changes made since BeginEdit was called.
		public void EndEdit()
		{
			Plugin.SavePluginSettings(Settings);
			GameActivity.PluginDatabase.PluginSettings = this;

			if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
			{
				Plugin.TopPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
				Plugin.SidebarItem.Visible = Settings.EnableIntegrationButtonSide;
			}

			this.OnPropertyChanged();
		}

		// Code execute when user decides to confirm changes made since BeginEdit was called.
		// Executed before EndEdit is called and EndEdit is not called if false is returned.
		public bool VerifySettings(out List<string> errors)
		{
			errors = new List<string>();

			// Verify monitoring configuration
			if (Settings.EnableLogging)
			{
				if (!Settings.HasAnyMonitoringProviderEnabled)
				{
					errors.Add("Hardware monitoring is enabled but no providers are configured. " +
							  "Enable at least one provider (RivaTuner, LibreHardware, HWiNFO) to collect metrics.");
				}

				if (Settings.TimeIntervalLogging < 1)
				{
					errors.Add("Logging interval must be at least 1 minute.");
				}

				if (Settings.UseHWiNFOSharedMemory && !Settings.IsHWiNFOConfigured)
				{
					errors.Add("HWiNFO is enabled but not properly configured. " +
							  "Please configure sensor IDs or switch to HWiNFO Gadget mode.");
				}

				if (Settings.UseLibreHardware && Settings.WithRemoteServerWeb &&
					string.IsNullOrEmpty(Settings.IpRemoteServerWeb))
				{
					errors.Add("LibreHardware remote server is enabled but no IP address is specified.");
				}
			}

			// Verify warning thresholds
			if (Settings.EnableWarning)
			{
				bool hasAnyThreshold = Settings.MinFps > 0 ||
									  Settings.MaxCpuTemp > 0 ||
									  Settings.MaxGpuTemp > 0 ||
									  Settings.MaxCpuUsage > 0 ||
									  Settings.MaxGpuUsage > 0 ||
									  Settings.MaxRamUsage > 0;

				if (!hasAnyThreshold)
				{
					errors.Add("Performance warnings are enabled but no thresholds are configured.");
				}
			}

			return errors.Count == 0;
		}


		public static List<StoreColor> GetDefaultStoreColors()
		{
			List<StoreColor> storeColors = new List<StoreColor>();

			List<Guid> sourceIds = GameActivity.PluginDatabase.Database.Items
				.Select(x => x.Value.SourceId)
				.Distinct()
				.ToList();

			List<Platform> platformIds = GameActivity.PluginDatabase.Database.Items
				.Where(x => x.Value != null && x.Value.Platforms != null)
				.SelectMany(x => x.Value.Platforms)
				.Distinct()
				.ToList();

			// Add source colors
			foreach (Guid id in sourceIds)
			{
				string name = (id == default) ? "Playnite" : API.Instance.Database.Sources.Get(id)?.Name;
				if (!name.IsNullOrEmpty())
				{
					name = (name.IsEqual("PC (Windows)") || name.IsEqual("PC (Mac)") || name.IsEqual("PC (Linux)"))
						? "Playnite"
						: name;

					if (storeColors.All(x => !x.Name.Equals(name)))
					{
						Brush fill = GetColor(name);
						storeColors.Add(new StoreColor
						{
							Name = name,
							Id = id,
							Fill = fill
						});
					}
				}
			}

			// Add platform colors
			foreach (Platform platform in platformIds)
			{
				string name = platform.Name;
				name = (name == "PC (Windows)" || name == "PC (Mac)" || name == "PC (Linux)")
					? "Playnite"
					: name;

				if (storeColors.All(x => !x.Name.Equals(name)))
				{
					Brush fill = GetColor(name);
					storeColors.Add(new StoreColor
					{
						Name = name,
						Id = platform.Id,
						Fill = fill
					});
				}
			}

			return storeColors;
		}

		private static Brush GetColor(string name)
		{
			Brush fill = null;
			switch (name?.ToLower())
			{
				case "android":
					fill = new BrushConverter().ConvertFromString("#068962") as SolidColorBrush;
					break;
				case "switch":
					fill = new BrushConverter().ConvertFromString("#e60012") as SolidColorBrush;
					break;
				case "legacy games":
					fill = new BrushConverter().ConvertFromString("#0f2a53") as SolidColorBrush;
					break;
				case "steam":
					fill = new BrushConverter().ConvertFromString("#1b2838") as SolidColorBrush;
					break;
				case "riot games":
					fill = new BrushConverter().ConvertFromString("#d13639") as SolidColorBrush;
					break;
				case "ps3":
				case "ps4":
				case "ps5":
				case "ps vita":
				case "playstation":
					fill = new BrushConverter().ConvertFromString("#296cc8") as SolidColorBrush;
					break;
				case "playnite":
					fill = new BrushConverter().ConvertFromString("#ff5832") as SolidColorBrush;
					break;
				case "xbox":
				case "xbox game pass":
				case "xbox one":
				case "xbox 360":
				case "xbox series":
				case "microsoft store":
					fill = new BrushConverter().ConvertFromString("#107c10") as SolidColorBrush;
					break;
				case "origin":
				case "ea app":
					fill = new BrushConverter().ConvertFromString("#f56c2d") as SolidColorBrush;
					break;
				case "blizzard":
					fill = new BrushConverter().ConvertFromString("#01b2f1") as SolidColorBrush;
					break;
				case "gog":
					fill = new BrushConverter().ConvertFromString("#5c2f74") as SolidColorBrush;
					break;
				case "ubisoft connect":
				case "ubisoft":
				case "uplay":
					fill = new BrushConverter().ConvertFromString("#0070ff") as SolidColorBrush;
					break;
				case "epic":
					fill = new BrushConverter().ConvertFromString("#2a2a2a") as SolidColorBrush;
					break;
				case "itch.io":
					fill = new BrushConverter().ConvertFromString("#ff244a") as SolidColorBrush;
					break;
				case "indiegala":
					fill = new BrushConverter().ConvertFromString("#e41f27") as SolidColorBrush;
					break;
				case "twitch":
					fill = new BrushConverter().ConvertFromString("#a970ff") as SolidColorBrush;
					break;
				case "amazon":
					fill = new BrushConverter().ConvertFromString("#f99601") as SolidColorBrush;
					break;
				case "battle.net":
					fill = new BrushConverter().ConvertFromString("#148eff") as SolidColorBrush;
					break;
				case "bethesda":
					fill = new BrushConverter().ConvertFromString("#202020") as SolidColorBrush;
					break;
				case "humble":
					fill = new BrushConverter().ConvertFromString("#3b3e48") as SolidColorBrush;
					break;
				default:
					break;
			}

			return fill;
		}
	}
}