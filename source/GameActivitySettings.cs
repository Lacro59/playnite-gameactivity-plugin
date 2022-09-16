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

namespace GameActivity
{
    public class GameActivitySettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;

        public bool SaveColumnOrder { get; set; } = false;

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        private bool _EnableIntegrationButton { get; set; } = true;
        public bool EnableIntegrationButton
        {
            get => _EnableIntegrationButton;
            set
            {
                _EnableIntegrationButton = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationButtonDetails { get; set; } = false;
        public bool EnableIntegrationButtonDetails
        {
            get => _EnableIntegrationButtonDetails;
            set
            {
                _EnableIntegrationButtonDetails = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationChartTime { get; set; } = true;
        public bool EnableIntegrationChartTime
        {
            get => _EnableIntegrationChartTime;
            set
            {
                _EnableIntegrationChartTime = value;
                OnPropertyChanged();
            }
        }

        public bool ChartTimeTruncate { get; set; } = true;
        public bool ChartTimeVisibleEmpty { get; set; } = true;
        public double ChartTimeHeight { get; set; } = 120;
        public bool ChartTimeAxis { get; set; } = true;
        public bool ChartTimeOrdinates { get; set; } = true;
        public int ChartTimeCountAbscissa { get; set; } = 11;

        private bool _EnableIntegrationChartLog { get; set; } = true;
        public bool EnableIntegrationChartLog
        {
            get => _EnableIntegrationChartLog;
            set
            {
                _EnableIntegrationChartLog = value;
                OnPropertyChanged();
            }
        }

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
        public SolidColorBrush ChartColors { get; set; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2195f2"));

        public bool EnableLogging { get; set; } = false;
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

        public string HWiNFO_gpu_sensorsID { get; set; } = string.Empty;
        public string HWiNFO_gpu_elementID { get; set; } = string.Empty;
        public string HWiNFO_fps_sensorsID { get; set; } = string.Empty;
        public string HWiNFO_fps_elementID { get; set; } = string.Empty;
        public string HWiNFO_gpuT_sensorsID { get; set; } = string.Empty;
        public string HWiNFO_gpuT_elementID { get; set; } = string.Empty;
        public string HWiNFO_cpuT_sensorsID { get; set; } = string.Empty;
        public string HWiNFO_cpuT_elementID { get; set; } = string.Empty;

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


        public Dictionary<Guid, List<string>> CustomGameActions = new Dictionary<Guid, List<string>>();
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData { get; set; } = false;
        [DontSerialize]
        public bool HasData
        {
            get => _HasData;
            set
            {
                _HasData = value;
                OnPropertyChanged();
            }
        }

        private bool _HasDataLog { get; set; } = false;
        [DontSerialize]
        public bool HasDataLog
        {
            get => _HasDataLog;
            set
            {
                _HasDataLog = value;
                OnPropertyChanged();
            }
        }

        private string _LastDateSession { get; set; } = string.Empty;
        [DontSerialize]
        public string LastDateSession
        {
            get => _LastDateSession;
            set
            {
                _LastDateSession = value;
                OnPropertyChanged();
            }
        }

        private string _LastDateTimeSession { get; set; } = string.Empty;
        [DontSerialize]
        public string LastDateTimeSession
        {
            get => _LastDateTimeSession;
            set
            {
                _LastDateTimeSession = value;
                OnPropertyChanged();
            }
        }

        private string _LastPlaytimeSession { get; set; } = string.Empty;
        [DontSerialize]
        public string LastPlaytimeSession
        {
            get => _LastPlaytimeSession;
            set
            {
                _LastPlaytimeSession = value;
                OnPropertyChanged();
            }
        }

        private int _AvgFpsAllSession { get; set; } = 0;
        [DontSerialize]
        public int AvgFpsAllSession
        {
            get => _AvgFpsAllSession;
            set
            {
                _AvgFpsAllSession = value;
                OnPropertyChanged();
            }
        }
        #endregion  
    }


    public class GameActivitySettingsViewModel : ObservableObject, ISettings
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly GameActivity Plugin;
        private GameActivitySettings EditingClone { get; set; }

        private GameActivitySettings _Settings;
        public GameActivitySettings Settings { get => _Settings; set => SetValue(ref _Settings, value); }


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
            List<Guid> SourceIds = GameActivity.PluginDatabase.Database.Items
                                                .Where(x => !Settings.StoreColors.Any(y => x.Value.SourceId == y.Id))
                                                .Select(x => x.Value.SourceId)
                                                .Distinct().ToList();
            List<Platform> PlatformIds = GameActivity.PluginDatabase.Database.Items
                                                .Where(x => x.Value != null && x.Value.Platforms != null)
                                                .Select(x => x.Value.Platforms)
                                                .SelectMany(x => x)
                                                .Where(x => !Settings.StoreColors.Any(y => x.Id == y.Id))
                                                .Distinct().ToList();

            Brush Fill = null;
            foreach (Guid Id in SourceIds)
            {
                string Name = (Id == default(Guid)) ? "Playnite" : GameActivity.PluginDatabase.PlayniteApi.Database.Sources.Get(Id)?.Name;
                if (Name.IsNullOrEmpty())
                {
                    logger.Warn($"No name for SourceId {Id}");
                }
                Name = (Name.IsEqual("PC (Windows)") || Name.IsEqual("PC (Mac)") || Name.IsEqual("PC (Linux)")) ? "Playnite" : Name;

                if (Settings.StoreColors.FindAll(x => x.Name.Equals(Name)) == null)
                {
                    Fill = GetColor(Name);
                    Settings.StoreColors.Add(new StoreColor
                    {
                        Name = Name,
                        Fill = Fill
                    });
                }
            }

            foreach (Platform platform in PlatformIds)
            {
                string Name = platform.Name;
                Name = (Name.IsEqual("PC (Windows)") || Name.IsEqual("PC (Mac)") || Name.IsEqual("PC (Linux)")) ? "Playnite" : Name;

                if (Settings.StoreColors.FindAll(x => x.Name.Equals(Name)) == null)
                {
                    Fill = GetColor(Name);
                    Settings.StoreColors.Add(new StoreColor
                    {
                        Name = Name,
                        Fill = Fill
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
                Plugin.topPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.gameActivityViewSidebar.Visible = Settings.EnableIntegrationButtonSide;
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
            List<StoreColor> StoreColors = new List<StoreColor>();

            List<Guid> SourceIds = GameActivity.PluginDatabase.Database.Items.Select(x => x.Value.SourceId).Distinct().ToList();
            List<Platform> PlatformIds = GameActivity.PluginDatabase.Database.Items
                .Where(x => x.Value != null && x.Value.Platforms != null)
                .Select(x => x.Value.Platforms)
                .SelectMany(x => x).Distinct().ToList();

            Brush Fill = null;
            foreach (Guid Id in SourceIds)
            {
                string Name = (Id == default(Guid)) ? "Playnite" : GameActivity.PluginDatabase.PlayniteApi.Database.Sources.Get(Id)?.Name;
                if (Name.IsNullOrEmpty())
                {
                    logger.Warn($"No name for SourceId {Id}");
                }
                Name = (Name == "PC (Windows)" || Name == "PC (Mac)" || Name == "PC (Linux)") ? "Playnite" : Name;

                if (StoreColors.FindAll(x => x.Name.Equals(Name)).Count() == 0)
                {
                    Fill = GetColor(Name);
                    StoreColors.Add(new StoreColor
                    {
                        Name = Name,
                        Id = Id,
                        Fill = Fill
                    });
                }
            }

            foreach (Platform platform in PlatformIds)
            {
                string Name = platform.Name;
                Name = (Name == "PC (Windows)" || Name == "PC (Mac)" || Name == "PC (Linux)") ? "Playnite" : Name;

                if (StoreColors.FindAll(x => x.Name.Equals(Name)).Count() == 0)
                {
                    Fill = GetColor(Name);
                    StoreColors.Add(new StoreColor
                    {
                        Name = Name,
                        Id = platform.Id,
                        Fill = Fill
                    });
                }
            }

            return StoreColors;
        }

        private static Brush GetColor(string Name)
        {
            Brush Fill = null;
            switch (Name.ToLower())
            {
                case "steam":
                    Fill = new BrushConverter().ConvertFromString("#1b2838") as SolidColorBrush;
                    break;
                case "ps3":
                case "ps4":
                case "ps5":
                case "ps vita":
                    Fill = new BrushConverter().ConvertFromString("#296cc8") as SolidColorBrush;
                    break;
                case "playnite":
                    Fill = new BrushConverter().ConvertFromString("#ff5832") as SolidColorBrush;
                    break;
                case "xbox":
                case "xbox game pass":
                case "xbox one":
                case "xbox 360":
                case "xbox series":
                    Fill = new BrushConverter().ConvertFromString("#107c10") as SolidColorBrush;
                    break;
                case "origin":
                    Fill = new BrushConverter().ConvertFromString("#f56c2d") as SolidColorBrush;
                    break;
                case "blizzard":
                    Fill = new BrushConverter().ConvertFromString("#01b2f1") as SolidColorBrush;
                    break;
                case "gog":
                    Fill = new BrushConverter().ConvertFromString("#5c2f74") as SolidColorBrush;
                    break;
                case "ubisoft connect":
                case "ubisoft":
                case "uplay":
                    Fill = new BrushConverter().ConvertFromString("#0070ff") as SolidColorBrush;
                    break;
                case "epic":
                    Fill = new BrushConverter().ConvertFromString("#2a2a2a") as SolidColorBrush;
                    break;
                case "itch.io":
                    Fill = new BrushConverter().ConvertFromString("#ff244a") as SolidColorBrush;
                    break;
                case "indiegala":
                    Fill = new BrushConverter().ConvertFromString("#e41f27") as SolidColorBrush;
                    break;
                case "twitch":
                    Fill = new BrushConverter().ConvertFromString("#a970ff") as SolidColorBrush;
                    break;
                case "amazon":
                    Fill = new BrushConverter().ConvertFromString("#f99601") as SolidColorBrush;
                    break;
                case "battle.net":
                    Fill = new BrushConverter().ConvertFromString("#148eff") as SolidColorBrush;
                    break;
                case "bethesda":
                    Fill = new BrushConverter().ConvertFromString("#202020") as SolidColorBrush;
                    break;
                case "humble":
                    Fill = new BrushConverter().ConvertFromString("#3b3e48") as SolidColorBrush;
                    break;
            }

            return Fill;
        }
    }
}
