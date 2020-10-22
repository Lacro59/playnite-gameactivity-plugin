using Newtonsoft.Json;
using Playnite.SDK;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameActivity
{
    public class GameActivitySettings : ISettings
    {
        private readonly GameActivity plugin;

        public bool EnableCheckVersion { get; set; } = true;
        public bool MenuInExtensions { get; set; } = true;
        public bool IgnoreSettings { get; set; } = false;

        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionOnlyIcon { get; set; } = true;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool EnableIntegrationButtonHeader { get; set; } = false;

        public bool IntegrationShowTitle { get; set; } = true;
        public bool IntegrationShowGraphic { get; set; } = true;
        public bool IntegrationShowGraphicLog { get; set; } = true;
        public bool IntegrationTopGameDetails { get; set; } = true;
        public bool IntegrationToggleDetails { get; set; } = true;

        public bool EnableIntegrationInCustomTheme { get; set; } = false;

        public double IntegrationShowGraphicHeight { get; set; } = 120;
        public double IntegrationShowGraphicLogHeight { get; set; } = 120;
        public bool EnableIntegrationAxisGraphic { get; set; } = true;
        public bool EnableIntegrationOrdinatesGraphic { get; set; } = true;
        public bool EnableIntegrationAxisGraphicLog { get; set; } = true;
        public bool EnableIntegrationOrdinatesGraphicLog { get; set; } = true;
        public int IntegrationGraphicOptionsCountAbscissa { get; set; } = 11;
        public int IntegrationGraphicLogOptionsCountAbscissa { get; set; } = 11;

        public bool EnableIntegrationButton { get; set; } = false;
        public bool EnableIntegrationButtonDetails { get; set; } = false;

        public bool showLauncherIcons { get; set; } = false;
        public bool CumulPlaytimeSession { get; set; } = false;
        public bool CumulPlaytimeStore { get; set; } = false;

        public bool EnableLogging { get; set; } = false;
        public int TimeIntervalLogging { get; set; } = 5;

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


        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonIgnore` ignore attribute.
        [JsonIgnore]
        public bool OptionThatWontBeSaved { get; set; } = false;

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public GameActivitySettings()
        {
        }

        public GameActivitySettings(GameActivity plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<GameActivitySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                EnableCheckVersion = savedSettings.EnableCheckVersion;
                MenuInExtensions = savedSettings.MenuInExtensions;

                EnableIntegrationInDescription = savedSettings.EnableIntegrationInDescription;
                EnableIntegrationInDescriptionOnlyIcon = savedSettings.EnableIntegrationInDescriptionOnlyIcon;
                EnableIntegrationInDescriptionWithToggle = savedSettings.EnableIntegrationInDescriptionWithToggle;

                EnableIntegrationButtonHeader = savedSettings.EnableIntegrationButtonHeader;

                IntegrationShowTitle = savedSettings.IntegrationShowTitle;
                IntegrationShowGraphic = savedSettings.IntegrationShowGraphic;
                IntegrationShowGraphicLog = savedSettings.IntegrationShowGraphicLog;
                IntegrationTopGameDetails = savedSettings.IntegrationTopGameDetails;
                IntegrationToggleDetails = savedSettings.IntegrationToggleDetails;

                EnableIntegrationInCustomTheme = savedSettings.EnableIntegrationInCustomTheme;

                IntegrationShowGraphicHeight = savedSettings.IntegrationShowGraphicHeight;
                IntegrationShowGraphicLogHeight = savedSettings.IntegrationShowGraphicLogHeight;
                EnableIntegrationAxisGraphic = savedSettings.EnableIntegrationAxisGraphic;
                EnableIntegrationOrdinatesGraphic = savedSettings.EnableIntegrationOrdinatesGraphic;
                EnableIntegrationAxisGraphicLog = savedSettings.EnableIntegrationAxisGraphicLog;
                EnableIntegrationOrdinatesGraphicLog = savedSettings.EnableIntegrationOrdinatesGraphicLog;
                IntegrationGraphicOptionsCountAbscissa = savedSettings.IntegrationGraphicOptionsCountAbscissa;
                IntegrationGraphicLogOptionsCountAbscissa = savedSettings.IntegrationGraphicLogOptionsCountAbscissa;

                EnableIntegrationButton = savedSettings.EnableIntegrationButton;
                EnableIntegrationButtonDetails = savedSettings.EnableIntegrationButtonDetails;

                showLauncherIcons = savedSettings.showLauncherIcons;
                CumulPlaytimeSession = savedSettings.CumulPlaytimeSession;
                CumulPlaytimeStore = savedSettings.CumulPlaytimeStore;

                EnableLogging = savedSettings.EnableLogging;
                TimeIntervalLogging = savedSettings.TimeIntervalLogging;

                UseMsiAfterburner = savedSettings.UseMsiAfterburner;
                UseHWiNFO = savedSettings.UseHWiNFO;

                HWiNFO_gpu_sensorsID = savedSettings.HWiNFO_gpu_sensorsID;
                HWiNFO_gpu_elementID = savedSettings.HWiNFO_gpu_elementID;
                HWiNFO_fps_sensorsID = savedSettings.HWiNFO_fps_sensorsID;
                HWiNFO_fps_elementID = savedSettings.HWiNFO_fps_elementID;
                HWiNFO_gpuT_sensorsID = savedSettings.HWiNFO_gpuT_sensorsID;
                HWiNFO_gpuT_elementID = savedSettings.HWiNFO_gpuT_elementID;
                HWiNFO_cpuT_sensorsID = savedSettings.HWiNFO_cpuT_sensorsID;
                HWiNFO_cpuT_elementID = savedSettings.HWiNFO_cpuT_elementID;

                EnableWarning = savedSettings.EnableWarning;
                MinFps = savedSettings.MinFps;
                MaxCpuTemp = savedSettings.MaxCpuTemp;
                MaxGpuTemp = savedSettings.MaxGpuTemp;
                MaxCpuUsage = savedSettings.MaxCpuUsage;
                MaxGpuUsage = savedSettings.MaxGpuUsage;
                MaxRamUsage = savedSettings.MaxRamUsage;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);

            GameActivity.gameActivityUI.RemoveElements();
            var TaskIntegrationUI = Task.Run(() =>
            {
                GameActivity.gameActivityUI.AddElements();
                GameActivity.gameActivityUI.RefreshElements(GameActivity.GameSelected);
            });
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}