using Newtonsoft.Json;
using Playnite.SDK;
using System.Collections.Generic;

namespace GameActivity
{
    public class GameActivitySettings : ISettings
    {
        private readonly GameActivity plugin;

        public bool showLauncherIcons { get; set; } = false;

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
                showLauncherIcons = savedSettings.showLauncherIcons;

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