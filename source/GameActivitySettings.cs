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
        #region Settings variables
        public bool SaveColumnOrder { get; set; } = false;

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        private bool _enableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _enableIntegrationButton; set => SetValue(ref _enableIntegrationButton, value); }

        private bool _enableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails { get => _enableIntegrationButtonDetails; set => SetValue(ref _enableIntegrationButtonDetails, value); }

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


        public bool ShowLauncherIcons { get; set; } = true;
        public int ModeStoreIcon { get; set; } = 1;

        public bool CumulPlaytimeSession { get; set; } = false;
        public bool CumulPlaytimeStore { get; set; } = false;

        public bool SubstPlayStateTime { get; set; } = false; // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions

        public List<StoreColor> StoreColors { get; set; } = new List<StoreColor>();
        public SolidColorBrush ChartColors { get; set; } = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");

        public bool EnableLogging { get; set; } = false;
        public bool UsedLibreHardware { get; set; } = false;
        public bool WithRemoteServerWeb { get; set; } = false;
        public string IpRemoteServerWeb { get; set; } = string.Empty;
        public int TimeIntervalLogging { get; set; } = 5;

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

        public bool UseMsiAfterburner { get; set; } = false;
        public bool UseHWiNFO { get; set; } = false;
        public bool UseHWiNFOGadget { get; set; } = false;

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

        public long HWiNFO_gpu_index { get; set; } = 0;
        public long HWiNFO_fps_index { get; set; } = 0;
        public long HWiNFO_gpuT_index { get; set; } = 0;
        public long HWiNFO_cpuT_index { get; set; } = 0;
        public long HWiNFO_gpuP_index { get; set; } = 0;
        public long HWiNFO_cpuP_index { get; set; } = 0;

        public bool EnableWarning { get; set; } = false;
        public int MinFps { get; set; } = 0;
        public int MaxCpuTemp { get; set; } = 0;
        public int MaxGpuTemp { get; set; } = 0;
        public int MaxCpuUsage { get; set; } = 0;
        public int MaxGpuUsage { get; set; } = 0;
        public int MaxRamUsage { get; set; } = 0;


        public bool IgnoreSession { get; set; } = false;
        public int IgnoreSessionTime { get; set; } = 120;

        public int VariatorTime { get; set; } = 7;
        public int VariatorLog { get; set; } = 4;

        public int RecentActivityWeek { get; set; } = 2;


        public Dictionary<Guid, List<string>> CustomGameActions = new Dictionary<Guid, List<string>>();
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
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

        private string _recentActivity  = string.Empty;
        [DontSerialize]
        public string RecentActivity { get => _recentActivity; set => SetValue(ref _recentActivity, value); }
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
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);

            // Set default
            if (Settings.StoreColors.Count == 0)
            {
                Settings.StoreColors = GetDefaultStoreColors();
            }

            // Set missing
            List <Guid> sourceIds = GameActivity.PluginDatabase.Database.Items
                                                .Where(x => !Settings.StoreColors.Any(y => x.Value.SourceId == y.Id))
                                                .Select(x => x.Value.SourceId)
                                                .Distinct().ToList();
            List<Platform> platformIds = GameActivity.PluginDatabase.Database.Items
                                                .Where(x => x.Value != null && x.Value.Platforms != null)
                                                .Select(x => x.Value.Platforms)
                                                .SelectMany(x => x)
                                                .Where(x => !Settings.StoreColors.Any(y => x.Id == y.Id))
                                                .Distinct().ToList();

            Brush fill = null;
            foreach (Guid id in sourceIds)
            {
                string name = (id == default) ? "Playnite" : API.Instance.Database.Sources.Get(id)?.Name;
                if (name.IsNullOrEmpty())
                {
                    logger.Warn($"No name for SourceId {id}");
                }
                name = (name.IsEqual("PC (Windows)") || name.IsEqual("PC (Mac)") || name.IsEqual("PC (Linux)")) ? "Playnite" : name;

                if (Settings.StoreColors.FindAll(x => x.Name.Equals(name))?.Count == 0)
                {
                    fill = GetColor(name);
                    Settings.StoreColors.Add(new StoreColor
                    {
                        Name = name,
                        Fill = fill
                    });
                }
            }

            foreach (Platform platform in platformIds)
            {
                string name = platform.Name;
                name = (name.IsEqual("PC (Windows)") || name.IsEqual("PC (Mac)") || name.IsEqual("PC (Linux)")) ? "Playnite" : name;

                if (Settings.StoreColors.FindAll(x => x.Name.IsEqual(name)) == null)
                {
                    fill = GetColor(name);
                    Settings.StoreColors.Add(new StoreColor
                    {
                        Name = name,
                        Fill = fill
                    });
                }
            }

            Settings.StoreColors = Settings.StoreColors.Select(x => x).DistinctBy(x => x.Name).OrderBy(x => x.Name).ToList();
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
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
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }


        public static List<StoreColor> GetDefaultStoreColors()
        {
            List<StoreColor> storeColors = new List<StoreColor>();

            List<Guid> sourceIds = GameActivity.PluginDatabase.Database.Items.Select(x => x.Value.SourceId).Distinct().ToList();
            List<Platform> platformIds = GameActivity.PluginDatabase.Database.Items
                .Where(x => x.Value != null && x.Value.Platforms != null)
                .Select(x => x.Value.Platforms)
                .SelectMany(x => x).Distinct().ToList();

            Brush fill = null;
            foreach (Guid id in sourceIds)
            {
                string name = (id == default) ? "Playnite" : API.Instance.Database.Sources.Get(id)?.Name;
                if (name.IsNullOrEmpty())
                {
                    logger.Warn($"No name for SourceId {id}");
                }
                else
                {
                    name = (name.IsEqual("PC (Windows)") || name.IsEqual("PC (Mac)") || name.IsEqual("PC (Linux)")) ? "Playnite" : name;

                    if (storeColors.FindAll(x => x.Name.IsEqual(name)).Count() == 0)
                    {
                        fill = GetColor(name);
                        storeColors.Add(new StoreColor
                        {
                            Name = name,
                            Id = id,
                            Fill = fill
                        });
                    }
                }
            }

            foreach (Platform platform in platformIds)
            {
                string name = platform.Name;
                name = (name == "PC (Windows)" || name == "PC (Mac)" || name == "PC (Linux)") ? "Playnite" : name;

                if (storeColors.FindAll(x => x.Name.Equals(name)).Count() == 0)
                {
                    fill = GetColor(name);
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
