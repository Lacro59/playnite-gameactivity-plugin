using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Plugins;
using GameActivity.Models;
using MoreLinq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GameActivity
{
    internal static class BrushCache
    {
        private static readonly BrushConverter Converter = new BrushConverter();
        private static readonly Dictionary<string, Brush> Cache = new Dictionary<string, Brush>();

        /// <summary>Returns a frozen <see cref="SolidColorBrush"/> for <paramref name="hex"/>, creating it only once.</summary>
        internal static Brush FromHex(string hex)
        {
            if (!Cache.TryGetValue(hex, out Brush brush))
            {
                brush = (Brush)Converter.ConvertFromString(hex);
                brush.Freeze(); // Safe cross-thread use
                Cache[hex] = brush;
            }
            return brush;
        }
    }

    /// <summary>
    /// Persisted settings for the GameActivity plugin.
    /// All properties without <see cref="DontSerializeAttribute"/> are written to disk by Playnite.
    /// </summary>
    public class GameActivitySettings : PluginSettings
    {
        #region UI Integration

        /// <summary>Whether column order in the list view is persisted between sessions.</summary>
        public bool SaveColumnOrder { get; set; } = false;

        /// <summary>Show the activity button in the top panel header.</summary>
        public bool EnableIntegrationButtonHeader { get; set; } = false;

        /// <summary>Show the activity button in the sidebar.</summary>
        public bool EnableIntegrationButtonSide { get; set; } = true;

        private bool _enableIntegrationButton = true;
        /// <summary>Show the activity button inside game details.</summary>
        public bool EnableIntegrationButton
        {
            get => _enableIntegrationButton;
            set => SetValue(ref _enableIntegrationButton, value);
        }

        private bool _enableIntegrationButtonDetails = false;
        /// <summary>Show extra detail controls alongside the activity button.</summary>
        public bool EnableIntegrationButtonDetails
        {
            get => _enableIntegrationButtonDetails;
            set => SetValue(ref _enableIntegrationButtonDetails, value);
        }

        #endregion

        #region Chart Display

        private bool _enableIntegrationChartTime = true;
        /// <summary>Display the playtime chart in the game details panel.</summary>
        public bool EnableIntegrationChartTime
        {
            get => _enableIntegrationChartTime;
            set => SetValue(ref _enableIntegrationChartTime, value);
        }

        /// <summary>Truncate long session labels on the time axis.</summary>
        public bool ChartTimeTruncate { get; set; } = true;

        /// <summary>Show the time chart even when there is no session data.</summary>
        public bool ChartTimeVisibleEmpty { get; set; } = true;

        /// <summary>Height in pixels of the time chart.</summary>
        public double ChartTimeHeight { get; set; } = 120;

        /// <summary>Show the X axis on the time chart.</summary>
        public bool ChartTimeAxis { get; set; } = true;

        /// <summary>Show Y-axis ordinate labels on the time chart.</summary>
        public bool ChartTimeOrdinates { get; set; } = true;

        /// <summary>Number of X-axis data points visible on the time chart.</summary>
        public int ChartTimeCountAbscissa { get; set; } = 11;

        private bool _enableIntegrationChartLog = true;
        /// <summary>Display the hardware-log chart in the game details panel.</summary>
        public bool EnableIntegrationChartLog
        {
            get => _enableIntegrationChartLog;
            set => SetValue(ref _enableIntegrationChartLog, value);
        }

        /// <summary>Show the log chart even when there is no data.</summary>
        public bool ChartLogVisibleEmpty { get; set; } = true;

        /// <summary>Height in pixels of the log chart.</summary>
        public double ChartLogHeight { get; set; } = 120;

        /// <summary>Show the X axis on the log chart.</summary>
        public bool ChartLogAxis { get; set; } = true;

        /// <summary>Show Y-axis ordinate labels on the log chart.</summary>
        public bool ChartLogOrdinates { get; set; } = true;

        /// <summary>Number of X-axis data points visible on the log chart.</summary>
        public int ChartLogCountAbscissa { get; set; } = 11;

        /// <summary>Show interactive controls (zoom, filter) on the charts.</summary>
        public bool UseControls { get; set; } = true;

        /// <summary>Include CPU usage series in the log chart.</summary>
        public bool DisplayCpu { get; set; } = true;

        /// <summary>Include GPU usage series in the log chart.</summary>
        public bool DisplayGpu { get; set; } = true;

        /// <summary>Include RAM usage series in the log chart.</summary>
        public bool DisplayRam { get; set; } = true;

        /// <summary>Include FPS series in the log chart.</summary>
        public bool DisplayFps { get; set; } = true;

        #endregion

        #region Visual Customization

        /// <summary>Show store/platform launcher icons in the list view.</summary>
        public bool ShowLauncherIcons { get; set; } = true;

        /// <summary>Icon display mode: 0 = none, 1 = icon, 2 = text.</summary>
        public int ModeStoreIcon { get; set; } = 1;

        /// <summary>Per-store / per-platform colour mappings used in the charts.</summary>
        public List<StoreColor> StoreColors { get; set; } = new List<StoreColor>();

        /// <summary>Default accent colour for chart series that have no store mapping.</summary>
        public SolidColorBrush ChartColors { get; set; } =
            (SolidColorBrush)BrushCache.FromHex("#2195f2");

        #endregion

        #region Session Management

        /// <summary>Add each session's playtime on top of the existing total instead of replacing it.</summary>
        public bool CumulPlaytimeSession { get; set; } = false;

        /// <summary>Cumulate playtime from store data into the session total.</summary>
        public bool CumulPlaytimeStore { get; set; } = false;

        /// <summary>Subtract PlayState paused time from the recorded session duration.</summary>
        public bool SubstPlayStateTime { get; set; } = false;

        /// <summary>Discard sessions that are shorter than <see cref="IgnoreSessionTime"/>.</summary>
        public bool IgnoreSession { get; set; } = false;

        /// <summary>Minimum session length in seconds before a session is recorded.</summary>
        public int IgnoreSessionTime { get; set; } = 120;

        #endregion

        #region Hardware Monitoring — General

        /// <summary>Enable hardware metrics logging during gameplay.</summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>How often metrics are sampled, in minutes.</summary>
        public int TimeIntervalLogging { get; set; } = 5;

        /// <summary>
        /// Provider selection strategy.
        /// <c>0</c> = Automatic (try all in order); <c>1</c> = Manual (use only the explicitly enabled provider).
        /// </summary>
        public int MonitoringMode { get; set; } = 0;

        /// <summary>How many consecutive read failures trigger a fall-back to the next provider.</summary>
        public int MaxFailuresBeforeFallback { get; set; } = 5;

        /// <summary>How long (ms) a metrics snapshot is considered fresh before the provider is queried again.</summary>
        public int MetricsCacheDurationMs { get; set; } = 500;

        #endregion

        #region Hardware Monitoring — Providers

        // ── RivaTuner Statistics Server ───────────────────────────────────────
        /// <summary>Use RTSS as the FPS data source.</summary>
        public bool UseRivaTuner { get; set; } = false;

        // ── LibreHardware Monitor ─────────────────────────────────────────────
        /// <summary>Use LibreHardwareMonitor for comprehensive CPU/GPU/RAM data.</summary>
        public bool UseLibreHardware { get; set; } = false;

        /// <summary>
        /// Connect to a remote LibreHardware web server instead of the local instance.
        /// Not persisted — reset to <c>true</c> on every load.
        /// </summary>
        [DontSerialize]
        public bool WithRemoteServerWeb { get; set; } = true;

        /// <summary>IP address (or hostname) of the remote LibreHardware server.</summary>
        public string IpRemoteServerWeb { get; set; } = string.Empty;

        // ── HWiNFO ────────────────────────────────────────────────────────────
        /// <summary>Use HWiNFO shared-memory as the data source.</summary>
        public bool UseHWiNFOSharedMemory { get; set; } = false;

        /// <summary>Use the HWiNFO Gadget registry method instead of shared memory.</summary>
        public bool UseHWiNFOGadget { get; set; } = false;

        // Shared-memory sensor identifiers
        /// <summary>HWiNFO shared-memory sensor ID for GPU load.</summary>
        public string HWiNFO_gpu_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for GPU load.</summary>
        public string HWiNFO_gpu_elementID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory sensor ID for FPS.</summary>
        public string HWiNFO_fps_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for FPS.</summary>
        public string HWiNFO_fps_elementID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory sensor ID for GPU temperature.</summary>
        public string HWiNFO_gpuT_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for GPU temperature.</summary>
        public string HWiNFO_gpuT_elementID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory sensor ID for GPU power.</summary>
        public string HWiNFO_gpuP_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for GPU power.</summary>
        public string HWiNFO_gpuP_elementID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory sensor ID for CPU temperature.</summary>
        public string HWiNFO_cpuT_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for CPU temperature.</summary>
        public string HWiNFO_cpuT_elementID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory sensor ID for CPU power.</summary>
        public string HWiNFO_cpuP_sensorsID { get; set; } = string.Empty;
        /// <summary>HWiNFO shared-memory element ID for CPU power.</summary>
        public string HWiNFO_cpuP_elementID { get; set; } = string.Empty;

        // Gadget (registry) sensor indices
        /// <summary>Registry index for GPU load (Gadget mode).</summary>
        public long HWiNFO_gpu_index { get; set; } = 0;
        /// <summary>Registry index for FPS (Gadget mode).</summary>
        public long HWiNFO_fps_index { get; set; } = 0;
        /// <summary>Registry index for GPU temperature (Gadget mode).</summary>
        public long HWiNFO_gpuT_index { get; set; } = 0;
        /// <summary>Registry index for CPU temperature (Gadget mode).</summary>
        public long HWiNFO_cpuT_index { get; set; } = 0;
        /// <summary>Registry index for GPU power (Gadget mode).</summary>
        public long HWiNFO_gpuP_index { get; set; } = 0;
        /// <summary>Registry index for CPU power (Gadget mode).</summary>
        public long HWiNFO_cpuP_index { get; set; } = 0;

        // ── MSI Afterburner ───────────────────────────────────────────────────
        /// <summary>Use MSI Afterburner as a data source (legacy — prefer RivaTuner).</summary>
        public bool UseMsiAfterburner { get; set; } = false;

        // ── Built-in Windows providers ────────────────────────────────────────
        /// <summary>Use Windows Management Instrumentation for hardware data.</summary>
        public bool UseWMI { get; set; } = true;

        /// <summary>Use Windows Performance Counters for hardware data.</summary>
        public bool UsePerformanceCounter { get; set; } = true;

        #endregion

        #region Performance Warnings

        /// <summary>Enable in-game pop-up warnings when thresholds are exceeded.</summary>
        public bool EnableWarning { get; set; } = false;

        /// <summary>Warn when FPS drops below this value. <c>0</c> = disabled.</summary>
        public int MinFps { get; set; } = 0;

        /// <summary>Warn when CPU temperature exceeds this value in °C. <c>0</c> = disabled.</summary>
        public int MaxCpuTemp { get; set; } = 0;

        /// <summary>Warn when GPU temperature exceeds this value in °C. <c>0</c> = disabled.</summary>
        public int MaxGpuTemp { get; set; } = 0;

        /// <summary>Warn when CPU usage exceeds this percentage. <c>0</c> = disabled.</summary>
        public int MaxCpuUsage { get; set; } = 0;

        /// <summary>Warn when GPU usage exceeds this percentage. <c>0</c> = disabled.</summary>
        public int MaxGpuUsage { get; set; } = 0;

        /// <summary>Warn when RAM usage exceeds this percentage. <c>0</c> = disabled.</summary>
        public int MaxRamUsage { get; set; } = 0;

        #endregion

        #region List View Columns

        /// <summary>Show game icon column.</summary>
        public bool lvGamesIcon { get; set; } = true;
        /// <summary>Show PC name column.</summary>
        public bool lvGamesPcName { get; set; } = true;
        /// <summary>Show source column.</summary>
        public bool lvGamesSource { get; set; } = true;
        /// <summary>Show play action column.</summary>
        public bool lvGamesPlayAction { get; set; } = true;
        /// <summary>Show average CPU usage column.</summary>
        public bool lvAvgCpu { get; set; } = true;
        /// <summary>Show average GPU usage column.</summary>
        public bool lvAvgGpu { get; set; } = true;
        /// <summary>Show average RAM usage column.</summary>
        public bool lvAvgRam { get; set; } = true;
        /// <summary>Show average FPS column.</summary>
        public bool lvAvgFps { get; set; } = true;
        /// <summary>Show average CPU temperature column.</summary>
        public bool lvAvgCpuT { get; set; } = true;
        /// <summary>Show average GPU temperature column.</summary>
        public bool lvAvgGpuT { get; set; } = true;
        /// <summary>Show average CPU power column.</summary>
        public bool lvAvgCpuP { get; set; } = true;
        /// <summary>Show average GPU power column.</summary>
        public bool lvAvgGpuP { get; set; } = true;

        #endregion

        #region Analysis & Statistics

        /// <summary>Number of days used as the time-grouping window for the playtime chart.</summary>
        public int VariatorTime { get; set; } = 7;

        /// <summary>Number of sessions shown per page in the log chart.</summary>
        public int VariatorLog { get; set; } = 4;

        /// <summary>Number of weeks considered "recent" in the activity summary.</summary>
        public int RecentActivityWeek { get; set; } = 2;

        #endregion

        #region Custom Game Actions

        /// <summary>
        /// Per-game custom launch actions keyed by game ID.
        /// Each game can have multiple named actions.
        /// </summary>
        public Dictionary<Guid, List<string>> CustomGameActions { get; set; } = new Dictionary<Guid, List<string>>();

        #endregion

        #region Non-Serialized UI State

        private bool _hasDataLog = false;
        /// <summary><c>true</c> when the currently selected game has at least one hardware log entry.</summary>
        [DontSerialize]
        public bool HasDataLog
        {
            get => _hasDataLog;
            set => SetValue(ref _hasDataLog, value);
        }

        private string _lastDateSession = string.Empty;
        /// <summary>Date of the last recorded session (display string).</summary>
        [DontSerialize]
        public string LastDateSession
        {
            get => _lastDateSession;
            set => SetValue(ref _lastDateSession, value);
        }

        private string _lastDateTimeSession = string.Empty;
        /// <summary>Date and time of the last recorded session (display string).</summary>
        [DontSerialize]
        public string LastDateTimeSession
        {
            get => _lastDateTimeSession;
            set => SetValue(ref _lastDateTimeSession, value);
        }

        private string _lastPlaytimeSession = string.Empty;
        /// <summary>Formatted duration of the last recorded session.</summary>
        [DontSerialize]
        public string LastPlaytimeSession
        {
            get => _lastPlaytimeSession;
            set => SetValue(ref _lastPlaytimeSession, value);
        }

        private int _avgFpsAllSession = 0;
        /// <summary>Average FPS across all recorded sessions for the selected game.</summary>
        [DontSerialize]
        public int AvgFpsAllSession
        {
            get => _avgFpsAllSession;
            set => SetValue(ref _avgFpsAllSession, value);
        }

        private string _recentActivity = string.Empty;
        /// <summary>Formatted recent-activity summary string shown in the UI.</summary>
        [DontSerialize]
        public string RecentActivity
        {
            get => _recentActivity;
            set => SetValue(ref _recentActivity, value);
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// <c>true</c> when logging is enabled AND at least one hardware provider is active.
        /// Used by <see cref="GameActivitySettingsViewModel.VerifySettings"/> to catch misconfiguration.
        /// </summary>
        [DontSerialize]
        public bool HasAnyMonitoringProviderEnabled =>
            EnableLogging && (
                UseMsiAfterburner ||
                UseRivaTuner ||
                UseLibreHardware ||
                UseHWiNFOSharedMemory ||
                UseHWiNFOGadget ||
                UseWMI ||
                UsePerformanceCounter
            );

        /// <summary>
        /// <c>true</c> when the active HWiNFO method has at least one sensor identifier configured.
        /// </summary>
        [DontSerialize]
        public bool IsHWiNFOConfigured
        {
            get
            {
                if (UseHWiNFOGadget)
                {
                    return HWiNFO_fps_index > 0 || HWiNFO_gpu_index > 0;
                }
                if (UseHWiNFOSharedMemory)
                {
                    return !string.IsNullOrEmpty(HWiNFO_fps_sensorsID) ||
                           !string.IsNullOrEmpty(HWiNFO_gpu_sensorsID);
                }
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Playnite settings ViewModel for the GameActivity plugin.
    /// Handles the edit lifecycle (BeginEdit / CancelEdit / EndEdit / VerifySettings)
    /// and orchestrates store-colour initialisation.
    /// </summary>
    public class GameActivitySettingsViewModel : PluginSettingsViewModel, ISettings
    { 
        private readonly GameActivity _plugin;

        private GameActivitySettings _editingClone;

        private GameActivitySettings _settings;
        /// <summary>The live settings object bound to the settings UI.</summary>
        public GameActivitySettings Settings
        {
            get => _settings;
            set => SetValue(ref _settings, value);
        }

        /// <summary>
        /// Loads persisted settings (or creates defaults) and applies backward-compatibility patches.
        /// </summary>
        /// <param name="plugin">The owning plugin instance — required for Save/Load.</param>
        public GameActivitySettingsViewModel(GameActivity plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException("plugin");
            Settings = plugin.LoadPluginSettings<GameActivitySettings>() ?? new GameActivitySettings();
        }

        /// <inheritdoc/>
        public void BeginEdit()
        {
            // Snapshot current state so CancelEdit can restore it exactly.
            _editingClone = Serialization.GetClone(Settings);

            InitializeCommands(GameActivity.PluginName, GameActivity.PluginDatabase);

            if (Settings.StoreColors.Count == 0)
            {
                Settings.StoreColors = GetDefaultStoreColors();
            }
            else
            {
                UpdateMissingStoreColors();
            }
        }

        /// <inheritdoc/>
        public void CancelEdit()
        {
            Settings = _editingClone;
        }

        /// <inheritdoc/>
        public void EndEdit()
        {
            _plugin.SavePluginSettings(Settings);
            GameActivity.PluginDatabase.PluginSettings = this;

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                _plugin.TopPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                _plugin.SidebarItem.Visible = Settings.EnableIntegrationButtonSide;
            }

            OnPropertyChanged();
        }

        /// <inheritdoc/>
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (Settings.EnableLogging)
            {
                if (!Settings.HasAnyMonitoringProviderEnabled)
                {
                    errors.Add("Hardware monitoring is enabled but no provider is configured. " +
                               "Enable at least one (RivaTuner, LibreHardware, HWiNFO).");
                }
                if (Settings.TimeIntervalLogging < 1)
                {
                    errors.Add("Logging interval must be at least 1 minute.");
                }
                if (Settings.UseHWiNFOSharedMemory && !Settings.IsHWiNFOConfigured)
                {
                    errors.Add("HWiNFO Shared Memory is enabled but no sensor IDs are configured.");
                }
                if (Settings.UseLibreHardware && Settings.WithRemoteServerWeb &&
                    string.IsNullOrEmpty(Settings.IpRemoteServerWeb))
                {
                    errors.Add("LibreHardware remote server is enabled but no IP address is specified.");
                }
            }

            if (Settings.EnableWarning)
            {
                bool hasThreshold = Settings.MinFps > 0 ||
                                    Settings.MaxCpuTemp > 0 ||
                                    Settings.MaxGpuTemp > 0 ||
                                    Settings.MaxCpuUsage > 0 ||
                                    Settings.MaxGpuUsage > 0 ||
                                    Settings.MaxRamUsage > 0;
                if (!hasThreshold)
                {
                    errors.Add("Performance warnings are enabled but no threshold is configured.");
                }
            }

            return errors.Count == 0;
        }

        #region Store colours 

        /// <summary>
        /// Adds <see cref="StoreColor"/> entries for any source or platform that exists in the
        /// database but is not yet present in <see cref="GameActivitySettings.StoreColors"/>.
        /// Guarantees no duplicate names and re-sorts alphabetically when done.
        /// </summary>
        private void UpdateMissingStoreColors()
        {
            var items = GameActivity.PluginDatabase.Database.Items;

            // Build a lookup of already-known names (case-insensitive) for O(1) existence checks.
            // This replaces the repeated All() calls that were O(n) per iteration.
            var existingByName = new HashSet<string>(
                Settings.StoreColors.Select(c => c.Name),
                StringComparer.OrdinalIgnoreCase);

            // ── Missing sources ───────────────────────────────────────────────────
            var missingSources = items
                .Where(x => !Settings.StoreColors.Any(c => c.Id == x.Value.SourceId))
                .Select(x => x.Value.SourceId)
                .Distinct()
                .ToList();

            foreach (Guid id in missingSources)
            {
                string name = PlayniteTools.GetSourceName(id);
                if (string.IsNullOrEmpty(name) || existingByName.Contains(name))
                {
                    continue;
                }

                Settings.StoreColors.Add(new StoreColor { Name = name, Id = id, Fill = GetColor(name) });
                existingByName.Add(name); // Keep the set in sync for the platforms loop below
            }

            // ── Missing platforms ─────────────────────────────────────────────────
            var missingPlatforms = items
                .Where(x => x.Value?.Platforms != null)
                .SelectMany(x => x.Value.Platforms)
                .Where(p => !Settings.StoreColors.Any(c => c.Id == p.Id))
                .DistinctBy(p => p.Id)
                .ToList();

            foreach (Platform platform in missingPlatforms)
            {
                string name = PlayniteTools.GetSourceBySourceIdOrPlatformId(
                    default, new List<Guid> { platform.Id });

                if (string.IsNullOrEmpty(name) || existingByName.Contains(name))
                {
                    continue;
                }

                Settings.StoreColors.Add(new StoreColor { Name = name, Id = platform.Id, Fill = GetColor(name) });
                existingByName.Add(name);
            }

            // Final dedup + sort as safety net (covers pre-existing duplicates in persisted data).
            Settings.StoreColors = Settings.StoreColors
                .DistinctBy(c => c.Name)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Builds the initial <see cref="StoreColor"/> list from all sources and platforms
        /// currently in the database. Guarantees no duplicate names in the result.
        /// </summary>
        public static List<StoreColor> GetDefaultStoreColors()
        {
            var items = GameActivity.PluginDatabase.Database.Items;

            // Use a dict keyed by normalised name to prevent any duplicate, regardless
            // of whether the collision comes from two sources, two platforms, or a
            // source/platform pair that resolves to the same display name.
            var byName = new Dictionary<string, StoreColor>(StringComparer.OrdinalIgnoreCase);

            // ── Sources ───────────────────────────────────────────────────────────
            var sourceIds = items
                .Select(x => x.Value.SourceId)
                .Distinct()
                .ToList();

            foreach (Guid id in sourceIds)
            {
                string name = PlayniteTools.GetSourceBySourceIdOrPlatformId(id, null);
                if (string.IsNullOrEmpty(name) || byName.ContainsKey(name))
                {
                    continue;
                }
                byName[name] = new StoreColor { Name = name, Id = id, Fill = GetColor(name) };
            }

            // ── Platforms ─────────────────────────────────────────────────────────
            var platforms = items
                .Where(x => x.Value?.Platforms != null)
                .SelectMany(x => x.Value.Platforms)
                .DistinctBy(p => p.Id)
                .ToList();

            foreach (Platform platform in platforms)
            {
                string name = PlayniteTools.GetSourceBySourceIdOrPlatformId(
                    default, new List<Guid> { platform.Id });

                if (string.IsNullOrEmpty(name) || byName.ContainsKey(name))
                {
                    continue;
                }
                byName[name] = new StoreColor { Name = name, Id = platform.Id, Fill = GetColor(name) };
            }

            return byName.Values
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Returns the official brand colour for a known store or platform name.
        /// Brushes are cached via <see cref="BrushCache"/> — conversion occurs only once per hex value.
        /// Returns <c>null</c> for unrecognised names (caller renders a neutral fallback).
        /// </summary>
        private static Brush GetColor(string name)
        {
            switch (name?.ToLowerInvariant())
            {
                // ── Mobile / Nintendo ─────────────────────────────────────────────
                case "android":
                    return BrushCache.FromHex("#068962");
                case "switch":
                    return BrushCache.FromHex("#e60012");

                // ── PC / Indie ────────────────────────────────────────────────────
                case "legacy games":
                    return BrushCache.FromHex("#0f2a53");
                case "playnite":
                    return BrushCache.FromHex("#ff5832");
                case "itch.io":
                    return BrushCache.FromHex("#ff244a");
                case "indiegala":
                    return BrushCache.FromHex("#e41f27");

                // ── Valve ─────────────────────────────────────────────────────────
                case "steam":
                    return BrushCache.FromHex("#1b2838");

                // ── Sony PlayStation ──────────────────────────────────────────────
                case "ps3":
                case "ps4":
                case "ps5":
                case "ps vita":
                case "playstation":
                    return BrushCache.FromHex("#296cc8");

                // ── Microsoft / Xbox ──────────────────────────────────────────────
                case "xbox":
                case "xbox game pass":
                case "xbox one":
                case "xbox 360":
                case "xbox series":
                case "microsoft store":
                    return BrushCache.FromHex("#107c10");

                // ── EA ────────────────────────────────────────────────────────────
                // Origin (legacy orange) and EA App (new purple brand) are distinct.
                case "origin":
                    return BrushCache.FromHex("#f56c2d");
                case "ea app":
                case "ea":
                    return BrushCache.FromHex("#6e34eb"); // EA rebrand 2022

                // ── Ubisoft ───────────────────────────────────────────────────────
                case "ubisoft connect":
                case "ubisoft":
                case "uplay":
                    return BrushCache.FromHex("#0070ff");

                // ── Blizzard / Battle.net ─────────────────────────────────────────
                case "blizzard":
                case "battle.net":
                    return BrushCache.FromHex("#148eff");

                // ── Epic Games ────────────────────────────────────────────────────
                case "epic":
                case "epic games":
                    return BrushCache.FromHex("#2a2a2a");

                // ── GOG ───────────────────────────────────────────────────────────
                case "gog":
                    return BrushCache.FromHex("#5c2f74");

                // ── Twitch / Amazon ───────────────────────────────────────────────
                case "twitch":
                    return BrushCache.FromHex("#9146ff"); // Official Twitch purple
                case "amazon":
                case "amazon games":
                    return BrushCache.FromHex("#f99601");

                // ── Rockstar Games ────────────────────────────────────────────────
                case "rockstar":
                case "rockstar games":
                    return BrushCache.FromHex("#ffab00"); // Official Rockstar yellow

                // ── Bethesda ──────────────────────────────────────────────────────
                case "bethesda":
                    return BrushCache.FromHex("#202020");

                // ── Humble Bundle ─────────────────────────────────────────────────
                case "humble":
                case "humble bundle":
                    return BrushCache.FromHex("#3b3e48");

                // ── Riot Games ────────────────────────────────────────────────────
                case "riot games":
                case "riot":
                    return BrushCache.FromHex("#d13639");

                // ── Netflix Games ─────────────────────────────────────────────────
                case "netflix":
                case "netflix games":
                    return BrushCache.FromHex("#e50914"); // Official Netflix red

                // ── Inconnu : le caller applique sa couleur neutre ─────────────────
                default:
                    return null;
            }
        }

        #endregion
    }
}