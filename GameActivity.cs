using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MSIAfterburnerNET.HM.Interop;
using System.Reflection;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System.Windows;
using GameActivity.Views.Interface;
using Playnite.SDK.Events;
using GameActivity.Database.Collections;
using GameActivity.Models;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using GameActivity.Services;
using System.Globalization;
using GameActivity.Views;
using System.Threading.Tasks;

namespace GameActivity
{
    public class GameActivity : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private GameActivitySettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        public static IGameDatabase DatabaseReference;
        public static string pluginFolder;
        public static Game GameSelected { get; set; }
        public static GameActivityUI gameActivityUI { get; set; }
        public static GameActivityCollection GameActivityDatabases;
        public static GameActivityClass SelectedGameGameActivity = null;

        // TODO Bad integration with structutre application
        private JArray activity { get; set; }
        private JObject activityDetails { get; set; }
        private JArray LoggingData { get; set; }

        // Paths application data.
        public string pathActivityDB { get; set; }
        public string pathActivityDetailsDB { get; set; }

        // Variables timer function
        public Timer t { get; set; }
        public List<WarningData> WarningsMessage { get; set; }


        public GameActivity(IPlayniteAPI api) : base(api)
        {
            settings = new GameActivitySettings(this);

            // Get plugin's location 
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();

                if (cv.Check("GameActivity", pluginFolder))
                {
                    cv.ShowNotification(api, "GameActivity - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }

            // Init ui interagration
            gameActivityUI = new GameActivityUI(api, settings, this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(gameActivityUI.OnCustomThemeButtonClick));

            // Load database
            var TaskLoadDatabase = Task.Run(() =>
            {
                pathActivityDB = this.GetPluginUserDataPath() + "\\activity\\";
                pathActivityDetailsDB = this.GetPluginUserDataPath() + "\\activityDetails\\";

                // Verification & add necessary directory
                if (!Directory.Exists(pathActivityDB))
                {
                    Directory.CreateDirectory(pathActivityDB);
                }
                if (!Directory.Exists(pathActivityDetailsDB))
                {
                    Directory.CreateDirectory(pathActivityDetailsDB);
                }

                GameActivityDatabases = new GameActivityCollection();
                GameActivityDatabases.InitializeCollection(this.GetPluginUserDataPath());
            });
        }

        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    //MenuSection = "",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOCGameActivityViewGameActivity"),
                    Action = (gameMenuItem) =>
                    {
                        DatabaseReference = PlayniteApi.Database;
                        var ViewExtension = new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath(), args.Games.First());
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCGameActivity"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = resources.GetString("LOCGameActivityViewGamesActivities"),
                    Action = (mainMenuItem) =>
                    {
                        DatabaseReference = PlayniteApi.Database;
                        var ViewExtension = new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath());
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
        }

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.

            // start timer si HWiNFO log is enable.
            if (settings.EnableLogging)
            {
                dataHWiNFO_start();
            }

            // Create / get data.
            string gameID = game.Id.ToString();

            activity = new JArray();
            if (File.Exists(pathActivityDB + gameID + ".json"))
            {
                activity = JArray.Parse(File.ReadAllText(pathActivityDB + gameID + ".json"));
            }
            else
            {
                using (StreamWriter sw = File.CreateText(pathActivityDB + gameID + ".json"))
                {
                    sw.WriteLine("[]");
                }
            }

            activityDetails = new JObject();
            LoggingData = new JArray();
            if (File.Exists(pathActivityDetailsDB + gameID + ".json"))
            {
                activityDetails = JObject.Parse(File.ReadAllText(pathActivityDetailsDB + gameID + ".json"));
            }
            else
            {
                using (StreamWriter sw = File.CreateText(pathActivityDetailsDB + gameID + ".json"))
                {
                    sw.WriteLine("{}");
                }
            }
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.

            var TaskGameStopped = Task.Run(() =>
            {
                // Stop timer si HWiNFO log is enable.
                if (settings.EnableLogging)
                {
                    dataHWiNFO_stop();
                }

                // Infos
                string gameID = game.Id.ToString();
                string gameName = game.Name;
                //string dateSession = DateTime.Now.ToUniversalTime().ToString("o");
                string dateSession = DateTime.Now.ToUniversalTime().AddSeconds(-elapsedSeconds).ToString("o");
                string gameSourceID = game.SourceId.ToString();

                try
                {
                    // Write game activity.
                    string newActivityString = "{'sourceID':'" + gameSourceID + "', 'dateSession':'" + dateSession + "', 'elapsedSeconds':'" + elapsedSeconds + "'}";
                    JObject newActivity = (JObject.Parse(newActivityString));
                    activity.Add(newActivity);
                    File.WriteAllText(pathActivityDB + gameID + ".json", JsonConvert.SerializeObject(activity));

                    // Write game activity details.
                    if (JsonConvert.SerializeObject(LoggingData) != "[]")
                    {
                        activityDetails.Add(new JProperty(dateSession, LoggingData));
                        File.WriteAllText(pathActivityDetailsDB + gameID + ".json", JsonConvert.SerializeObject(activityDetails));
                    }
                }
                catch (Exception ex)
                {
                    logger.Info("GameActivity - OnGameStopped - " + ex.Message);
                }


                // Refresh integration interface
                var TaskIntegrationUI = Task.Run(() =>
                {
                    GameActivityDatabases = new GameActivityCollection();
                    GameActivityDatabases.InitializeCollection(this.GetPluginUserDataPath());

                    gameActivityUI.AddElements();
                    gameActivityUI.RefreshElements(GameSelected);
                });
            });
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.

            gameActivityUI.AddBtHeader();
            CheckGoodForLogging(true);
        }

        private bool CheckGoodForLogging(bool WithNotification = false)
        {
            if (settings.EnableLogging && settings.UseHWiNFO)
            {
                bool runHWiNFO = false;
                Process[] pname = Process.GetProcessesByName("HWiNFO32");
                if (pname.Length != 0)
                {
                    runHWiNFO = true;
                }
                else
                {
                    pname = Process.GetProcessesByName("HWiNFO64");
                    if (pname.Length != 0)
                    {
                        runHWiNFO = true;
                    }
                }

                if (!runHWiNFO && WithNotification)
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"GameActivity-runHWiNFO",
                        resources.GetString("LOCGameActivityNotificationHWiNFO"),
                        NotificationType.Error,
                        () => OpenSettingsView()
                    ));
                }

                if (!runHWiNFO)
                {
                    logger.Error("GameActivity - No HWiNFO running");
                }

                if (!WithNotification)
                {
                    return runHWiNFO;
                }
            }

            if (settings.EnableLogging && settings.UseMsiAfterburner)
            {
                bool runMSI = false;
                bool runRTSS = false;
                Process[] pname = Process.GetProcessesByName("MSIAfterburner");
                if (pname.Length != 0)
                {
                    runMSI = true;
                }
                pname = Process.GetProcessesByName("RTSS");
                if (pname.Length != 0)
                {
                    runRTSS = true;
                }

                if ((!runMSI || !runRTSS) && WithNotification)
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"GameActivity-runMSI",
                        resources.GetString("LOCGameActivityNotificationMSIAfterBurner"),
                        NotificationType.Error,
                        () => OpenSettingsView()
                    ));
                }

                if (!runMSI)
                {
                    logger.Warn("GameActivity - No MSI Afterburner running");
                }
                if (!runRTSS)
                {
                    logger.Warn("GameActivity - No RivaTunerStatisticsServer running");
                }

                if (!WithNotification)
                {
                    if ((!runMSI || !runRTSS))
                    {
                        return false;
                    }
                    return true;
                }
            }

            return false;
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
#if DEBUG
                    logger.Debug($"GameActivity - Game selected: {GameSelected.Name}");
#endif
                    if (settings.EnableIntegrationInCustomTheme || settings.EnableIntegrationInDescription)
                    {
                        PlayniteUiHelper.ResetToggle();
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            gameActivityUI.taskHelper.Check();
                            gameActivityUI.AddElements();
                            gameActivityUI.RefreshElements(GameSelected);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"Error on OnGameSelected()");
            }
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GameActivitySettingsView();
        }


        #region Timer function
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void dataHWiNFO_start()
        {
            logger.Info("GameActivity - dataLogging_start");

            WarningsMessage = new List<WarningData>();
            t = new Timer(settings.TimeIntervalLogging * 60000);
            t.AutoReset = true;
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.Start();
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void dataHWiNFO_stop()
        {
            logger.Info("GameActivity - dataLogging_stop");

            if (WarningsMessage.Count != 0)
            {
                var ViewExtension = new WarningsDialogs(WarningsMessage);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivityWarningCaption"), ViewExtension);
                windowExtension.ShowDialog();

                WarningsMessage = new List<WarningData>();
            }

            t.AutoReset = false;
            t.Stop();
        }

        /// <summary>
        /// Event excuted with the timer.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            int fpsValue = 0;
            int cpuValue = PerfCounter.GetCpuPercentage();
            int gpuValue = PerfCounter.GetGpuPercentage();
            int ramValue = PerfCounter.GetRamPercentage();
            int gpuTValue = PerfCounter.GetGpuTemperature();
            int cpuTValue = PerfCounter.GetCpuTemperature();


            if (settings.UseMsiAfterburner && CheckGoodForLogging())
            {
                var MSIAfterburner = new MSIAfterburnerNET.HM.HardwareMonitor();

                try
                {
                    fpsValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.FRAMERATE).Data;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Fail get fpsValue");
                }

                try
                {
                    gpuValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_USAGE).Data;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Fail get gpuValue");
                }

                try
                {
                    gpuTValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_TEMPERATURE).Data;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Fail get gpuTValue");
                }

                try
                {
                    cpuTValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.CPU_TEMPERATURE).Data;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Fail get cpuTValue");
                }
            }
            else if (settings.UseHWiNFO && CheckGoodForLogging())
            {
                HWiNFODumper HWinFO = new HWiNFODumper();
                List<HWiNFODumper.JsonObj> dataHWinfo = HWinFO.ReadMem();

                try
                {
                    foreach (var sensorItems in dataHWinfo)
                    {
                        JObject sensorItemsOBJ = JObject.Parse(JsonConvert.SerializeObject(sensorItems));

                        string sensorsID = "0x" + ((uint)sensorItemsOBJ["szSensorSensorID"]).ToString("X");

                        // Find sensors fps
                        if (sensorsID.ToLower() == settings.HWiNFO_fps_sensorsID.ToLower())
                        {
                            // Find data fps
                            foreach (var items in sensorItemsOBJ["sensors"])
                            {
                                JObject itemOBJ = JObject.Parse(JsonConvert.SerializeObject(items));
                                string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                if (dataID.ToLower() == settings.HWiNFO_fps_elementID.ToLower())
                                {
                                    fpsValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                }
                            }
                        }

                        // Find sensors gpu usage
                        if (sensorsID.ToLower() == settings.HWiNFO_gpu_sensorsID.ToLower())
                        {
                            // Find data gpu
                            foreach (var items in sensorItemsOBJ["sensors"])
                            {
                                JObject itemOBJ = JObject.Parse(JsonConvert.SerializeObject(items));
                                string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                if (dataID.ToLower() == settings.HWiNFO_gpu_elementID.ToLower())
                                {
                                    gpuValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                }
                            }
                        }

                        // Find sensors gpu temp
                        if (sensorsID.ToLower() == settings.HWiNFO_gpuT_sensorsID.ToLower())
                        {
                            // Find data gpu
                            foreach (var items in sensorItemsOBJ["sensors"])
                            {
                                JObject itemOBJ = JObject.Parse(JsonConvert.SerializeObject(items));
                                string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                if (dataID.ToLower() == settings.HWiNFO_gpuT_elementID.ToLower())
                                {
                                    gpuTValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                }
                            }
                        }

                        // Find sensors cpu temp
                        if (sensorsID.ToLower() == settings.HWiNFO_cpuT_sensorsID.ToLower())
                        {
                            // Find data gpu
                            foreach (var items in sensorItemsOBJ["sensors"])
                            {
                                JObject itemOBJ = JObject.Parse(JsonConvert.SerializeObject(items));
                                string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                if (dataID.ToLower() == settings.HWiNFO_cpuT_elementID.ToLower())
                                {
                                    cpuTValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Fail get HWiNFO");
                }
            }


            // Listing warnings
            bool WarningMinFps = false;
            bool WarningMaxCpuTemp = false;
            bool WarningMaxGpuTemp = false;
            bool WarningMaxCpuUsage = false;
            bool WarningMaxGpuUsage = false;
            bool WarningMaxRamUsage = false;

            if (settings.EnableWarning)
            {
                if (settings.MinFps != 0 && settings.MinFps >= fpsValue)
                {
                    WarningMinFps = true;
                }
                if (settings.MaxCpuTemp != 0 && settings.MaxCpuTemp <= cpuTValue)
                {
                    WarningMaxCpuTemp = true;
                }
                if (settings.MaxGpuTemp != 0 && settings.MaxGpuTemp <= gpuTValue)
                {
                    WarningMaxGpuTemp = true;
                }
                if (settings.MaxCpuUsage != 0 && settings.MaxCpuUsage <= cpuValue)
                {
                    WarningMaxCpuUsage = true;
                }
                if (settings.MaxGpuUsage != 0 && settings.MaxGpuUsage <= gpuValue)
                {
                    WarningMaxGpuUsage = true;
                }
                if (settings.MaxRamUsage != 0 && settings.MaxRamUsage <= ramValue)
                {
                    WarningMaxRamUsage = true;
                }

                WarningData Message = new WarningData
                {
                    At = resources.GetString("LOCGameActivityWarningAt") + " " + DateTime.Now.ToString("HH:mm"),
                    FpsData = new Data { Name = resources.GetString("LOCGameActivityFps"), Value = fpsValue, isWarm = WarningMinFps },
                    CpuTempData = new Data { Name = resources.GetString("LOCGameActivityCpuTemp"), Value = cpuTValue, isWarm = WarningMaxCpuTemp },
                    GpuTempData = new Data { Name = resources.GetString("LOCGameActivityGpuTemp"), Value = gpuTValue, isWarm = WarningMaxGpuTemp },
                    CpuUsageData = new Data { Name = resources.GetString("LOCGameActivityCpuUsage"), Value = cpuValue, isWarm = WarningMaxCpuUsage },
                    GpuUsageData = new Data { Name = resources.GetString("LOCGameActivityGpuUsage"), Value = gpuValue, isWarm = WarningMaxGpuUsage },
                    RamUsageData = new Data { Name = resources.GetString("LOCGameActivityRamUsage"), Value = ramValue, isWarm = WarningMaxRamUsage },
                };

                if (WarningMinFps || WarningMaxCpuTemp || WarningMaxGpuTemp || WarningMaxCpuUsage || WarningMaxGpuUsage)
                {
                    WarningsMessage.Add(Message);
                }
            }

            JObject Data = new JObject();
            Data["datelog"] = DateTime.Now.ToUniversalTime().ToString("o");
            Data["fps"] = fpsValue;
            Data["cpu"] = cpuValue;
            Data["gpu"] = gpuValue;
            Data["ram"] = ramValue;
            Data["gpuT"] = gpuTValue;
            Data["cpuT"] = cpuTValue;

            LoggingData.Add(Data);
        }
        #endregion
    }
}
