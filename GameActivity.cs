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
using System.Windows;
using GameActivity.Views.Interface;
using Playnite.SDK.Events;
using GameActivity.Models;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using GameActivity.Services;
using System.Globalization;
using GameActivity.Views;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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

        public static Game GameSelected;
        public static ActivityDatabase PluginDatabase;
        public static GameActivityUI gameActivityUI;

        // Variables timer function
        public Timer t { get; set; }
        private GameActivities GameActivitiesLog;
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        private OldToNew oldToNew;


        public GameActivity(IPlayniteAPI api) : base(api)
        {
            settings = new GameActivitySettings(this);

            DatabaseReference = PlayniteApi.Database;

            // Old database            
            oldToNew = new OldToNew(this.GetPluginUserDataPath());

            // Loading plugin database 
            PluginDatabase = new ActivityDatabase(PlayniteApi, settings, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();

            // Get plugin's location 
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.PluginLocalization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
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

            // Add event fullScreen
            if (api.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(BtFullScreen_ClickEvent));
            }
        }


        #region Custom event
        private void BtFullScreen_ClickEvent(object sender, System.EventArgs e)
        {
            try
            {
                if (((Button)sender).Name == "PART_ButtonDetails")
                {
                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        gameActivityUI.Initial();
                        gameActivityUI.taskHelper.Check();
                        var dispatcherOp = gameActivityUI.AddElementsFS();
                        dispatcherOp.Completed += (s, ev) => { gameActivityUI.RefreshElements(GameSelected); };
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
        #endregion


        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                // Show plugin view with all activities for all game in database with data of selected game
                new GameMenuItem {
                    //MenuSection = "",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOCGameActivityViewGameActivity"),
                    Action = (gameMenuItem) =>
                    {
                        DatabaseReference = PlayniteApi.Database;
                        var ViewExtension = new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath(), GameMenu);
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
                // Show plugin view with all activities for all game in database
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
                Action = (mainMenuItem) =>
                {

                }
            });
#endif

            return mainMenuItems;
        }


        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            // Old database
            if (oldToNew.IsOld)
            {
                oldToNew.ConvertDB(PlayniteApi);
            }

            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
#if DEBUG
                    logger.Debug($"GameActivity - OnGameSelected() - {GameSelected.Name} - {GameSelected.Id.ToString()}");
#endif
                    if (settings.EnableIntegrationInCustomTheme || settings.EnableIntegrationInDescription)
                    {
                        PlayniteUiHelper.ResetToggle();
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            gameActivityUI.Initial();
                            gameActivityUI.taskHelper.Check();
                            var dispatcherOp = gameActivityUI.AddElements();
                            if (dispatcherOp != null)
                            {
                                dispatcherOp.Completed += (s, e) => { gameActivityUI.RefreshElements(args.NewValue[0]); };
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"Error on OnGameSelected()");
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(Game game)
        {
           
        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(Game game)
        {
            PlayniteUiHelper.ResetToggle();

            // start timer si log is enable.
            if (settings.EnableLogging)
            {
                dataHWiNFO_start();
            }

            DateTime DateSession = DateTime.Now.ToUniversalTime();

            GameActivitiesLog = PluginDatabase.Get(game);
            GameActivitiesLog.Items.Add(new Activity
            {
                DateSession = DateSession,
                SourceID = game.SourceId
            });
            GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, new List<ActivityDetailsData>());
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(Game game)
        {
            
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            var TaskGameStopped = Task.Run(() =>
            {
                // Stop timer si HWiNFO log is enable.
                if (settings.EnableLogging)
                {
                    dataHWiNFO_stop();
                }

                // Infos
                GameActivitiesLog.GetLastSessionActivity().ElapsedSeconds = elapsedSeconds;
                PluginDatabase.Update(GameActivitiesLog);

                // Refresh integration interface
                var TaskIntegrationUI = Task.Run(() =>
                {
                    var dispatcherOp = gameActivityUI.AddElements();
                    dispatcherOp.Completed += (s, e) => { gameActivityUI.RefreshElements(GameSelected); };
                });
            });
        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(Game game)
        {
            
        }


        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted()
        {
            gameActivityUI.AddBtHeader();
            CheckGoodForLogging(true);
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped()
        {
            
        }


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated()
        {
            
        }


        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GameActivitySettingsView();
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
                        "GameActivity" + Environment.NewLine + resources.GetString("LOCGameActivityNotificationHWiNFO"),
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
                        "GameActivity" + Environment.NewLine + resources.GetString("LOCGameActivityNotificationMSIAfterBurner"),
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
                try
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        var ViewExtension = new WarningsDialogs(WarningsMessage);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivityWarningCaption"), ViewExtension);
                        windowExtension.ShowDialog();

                        WarningsMessage = new List<WarningData>();
                    });
                }
                catch(Exception ex)
                {
                    Common.LogError(ex, "GameActivity", $"Error on show WarningsMessage");
                }
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
                MSIAfterburnerNET.HM.HardwareMonitor MSIAfterburner = null;

                try
                {
                    MSIAfterburner = new MSIAfterburnerNET.HM.HardwareMonitor();
                }
                catch (Exception ex)
                {
                    logger.Error("GameActivity - Fail initialize MSIAfterburnerNET");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail initialize MSIAfterburnerNET");
#endif
                }

                if (MSIAfterburner != null)
                {
                    try
                    {
                        fpsValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.FRAMERATE).Data;
                    }
                    catch (Exception ex)
                    {
                        logger.Error("GameActivity - Fail get fpsValue");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail get fpsValue");
#endif
                    }

                    try
                    {
                        if (gpuValue == 0)
                        {
                            gpuValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_USAGE).Data;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("GameActivity - Fail get gpuValue");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail get gpuValue");
#endif
                    }

                    try
                    {
                        if (gpuTValue == 0)
                        {
                            gpuTValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_TEMPERATURE).Data;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("GameActivity - Fail get gpuTValue");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail get gpuTValue");
#endif
                    }

                    try
                    {
                        if (cpuTValue == 0)
                        {
                            cpuTValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.CPU_TEMPERATURE).Data;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("GameActivity - Fail get cpuTValue");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail get cpuTValue");
#endif
                    }
                }
            }
            else if (settings.UseHWiNFO && CheckGoodForLogging())
            {
                HWiNFODumper HWinFO = null;
                List<HWiNFODumper.JsonObj> dataHWinfo = null;

                try
                {
                    HWinFO = new HWiNFODumper();
                    dataHWinfo = HWinFO.ReadMem();
                }
                catch (Exception ex)
                {
                    logger.Error("GameActivity - Fail initialize HWiNFODumper");
#if DEBUG
                    Common.LogError(ex, "GameActivity", "Fail initialize HWiNFODumper");
#endif
                }

                if (HWinFO != null && dataHWinfo != null)
                {
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
                    FpsData = new Data { Name = resources.GetString("LOCGameActivityFps"), Value = fpsValue, IsWarm = WarningMinFps },
                    CpuTempData = new Data { Name = resources.GetString("LOCGameActivityCpuTemp"), Value = cpuTValue, IsWarm = WarningMaxCpuTemp },
                    GpuTempData = new Data { Name = resources.GetString("LOCGameActivityGpuTemp"), Value = gpuTValue, IsWarm = WarningMaxGpuTemp },
                    CpuUsageData = new Data { Name = resources.GetString("LOCGameActivityCpuUsage"), Value = cpuValue, IsWarm = WarningMaxCpuUsage },
                    GpuUsageData = new Data { Name = resources.GetString("LOCGameActivityGpuUsage"), Value = gpuValue, IsWarm = WarningMaxGpuUsage },
                    RamUsageData = new Data { Name = resources.GetString("LOCGameActivityRamUsage"), Value = ramValue, IsWarm = WarningMaxRamUsage },
                };

                if (WarningMinFps || WarningMaxCpuTemp || WarningMaxGpuTemp || WarningMaxCpuUsage || WarningMaxGpuUsage)
                {
                    WarningsMessage.Add(Message);
                }
            }


            List<ActivityDetailsData> ActivitiesDetailsData = GameActivitiesLog.ItemsDetails.Get(GameActivitiesLog.GetLastSession());
            ActivitiesDetailsData.Add(new ActivityDetailsData
            {
                Datelog = DateTime.Now.ToUniversalTime(),
                FPS = fpsValue,
                CPU = cpuValue,
                CPUT = cpuTValue,
                GPU = gpuValue,
                GPUT = gpuTValue,
                RAM = ramValue
            });
        }
        #endregion
    }
}
