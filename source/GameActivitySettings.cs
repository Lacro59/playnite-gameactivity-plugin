using Playnite.SDK;
using CommonPluginsShared;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameActivity.Services;
using Playnite.SDK.Data;

namespace GameActivity
{
    public class GameActivitySettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;


        public bool EnableIntegrationButtonHeader { get; set; } = false;

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

        public bool _EnableIntegrationButtonDetails { get; set; } = false;
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

        public bool EnableLogging { get; set; } = false;
        public int TimeIntervalLogging { get; set; } = 5;

        public bool lvGamesIcon { get; set; } = true;
        public bool lvGamesName { get; set; } = true;
        public bool lvGamesSource { get; set; } = true;

        public bool lvAvgCpu { get; set; } = true;
        public bool lvAvgGpu { get; set; } = true;
        public bool lvAvgRam { get; set; } = true;
        public bool lvAvgFps { get; set; } = true;
        public bool lvAvgCpuT { get; set; } = true;
        public bool lvAvgGpuT { get; set; } = true;

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
        private readonly GameActivity Plugin;
        private GameActivitySettings EditingClone { get; set; }

        private GameActivitySettings _Settings;
        public GameActivitySettings Settings
        {
            get => _Settings;
            set
            {
                _Settings = value;
                OnPropertyChanged();
            }
        }


        public GameActivitySettingsViewModel(GameActivity plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<GameActivitySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new GameActivitySettings();
            }
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
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
    }
}
