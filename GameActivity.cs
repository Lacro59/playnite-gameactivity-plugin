using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Timers;
using System.Diagnostics;
using MSIAfterburnerNET.HM.Interop;
using System.Reflection;
using CommonPluginsShared;
using System.Windows;
using Playnite.SDK.Events;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using System.Threading.Tasks;
using CommonPluginsShared.PlayniteExtended;
using System.Windows.Media;
using CommonPluginsShared.Controls;

namespace GameActivity
{
    public class GameActivity : PluginExtended<GameActivitySettingsViewModel, ActivityDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        // Variables timer function
        public Timer t { get; set; }
        private GameActivities GameActivitiesLog;
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        private OldToNew oldToNew;


        public GameActivity(IPlayniteAPI api) : base(api)
        {
            // Old database            
            oldToNew = new OldToNew(this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginButton", "PluginChartTime", "PluginChartLog" },
                SourceName = "GameActivity"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "GameActivity",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });
        }


        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            string ButtonName = string.Empty;
            try
            {
                ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomGameActivityButton")
                {
                    Common.LogDebug(true, $"OnCustomThemeButtonClick()");

                    var ViewExtension = new GameActivityViewSingle(PluginDatabase.GameContext);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
                    windowExtension.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        private bool CheckGoodForLogging(bool WithNotification = false)
        {
            if (PluginSettings.Settings.EnableLogging && PluginSettings.Settings.UseHWiNFO)
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
                    logger.Error("No HWiNFO running");
                }

                if (!WithNotification)
                {
                    return runHWiNFO;
                }
            }

            if (PluginSettings.Settings.EnableLogging && PluginSettings.Settings.UseMsiAfterburner)
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
                    logger.Warn("No MSI Afterburner running");
                }
                if (!runRTSS)
                {
                    logger.Warn("No RivaTunerStatisticsServer running");
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
        public void DataLogging_start()
        {
            logger.Info("DataLogging_start");

            WarningsMessage = new List<WarningData>();
            t = new Timer(PluginSettings.Settings.TimeIntervalLogging * 60000);
            t.AutoReset = true;
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.Start();
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void DataLogging_stop()
        {
            logger.Info("DataLogging_stop");

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
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on show WarningsMessage");
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


            if (PluginSettings.Settings.UseMsiAfterburner && CheckGoodForLogging())
            {
                MSIAfterburnerNET.HM.HardwareMonitor MSIAfterburner = null;

                try
                {
                    MSIAfterburner = new MSIAfterburnerNET.HM.HardwareMonitor();
                }
                catch (Exception ex)
                {
                    logger.Warn("Fail initialize MSIAfterburnerNET");
                    Common.LogError(ex, true, "Fail initialize MSIAfterburnerNET");
                }

                if (MSIAfterburner != null)
                {
                    try
                    {
                        fpsValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.FRAMERATE).Data;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Fail get fpsValue");
                        Common.LogError(ex, true, "Fail get fpsValue");
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
                        logger.Warn("Fail get gpuValue");
                        Common.LogError(ex, true, "Fail get gpuValue");
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
                        logger.Warn("Fail get gpuTValue");
                        Common.LogError(ex, true, "Fail get gpuTValue");
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
                        logger.Warn("Fail get cpuTValue");
                        Common.LogError(ex, true, "Fail get cpuTValue");
                    }
                }
            }
            else if (PluginSettings.Settings.UseHWiNFO && CheckGoodForLogging())
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
                    logger.Error("Fail initialize HWiNFODumper");
                    Common.LogError(ex, true, "Fail initialize HWiNFODumper");
                }

                if (HWinFO != null && dataHWinfo != null)
                {
                    try
                    {
                        foreach (var sensorItems in dataHWinfo)
                        {
                            dynamic sensorItemsOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(sensorItems));

                            string sensorsID = "0x" + ((uint)sensorItemsOBJ["szSensorSensorID"]).ToString("X");

                            // Find sensors fps
                            if (sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_fps_sensorsID.ToLower())
                            {
                                // Find data fps
                                foreach (var items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_fps_elementID.ToLower())
                                    {
                                        fpsValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }

                            // Find sensors gpu usage
                            if (sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_gpu_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (var items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_gpu_elementID.ToLower())
                                    {
                                        gpuValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }

                            // Find sensors gpu temp
                            if (sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_gpuT_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (var items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_gpuT_elementID.ToLower())
                                    {
                                        gpuTValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }

                            // Find sensors cpu temp
                            if (sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_cpuT_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (var items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_cpuT_elementID.ToLower())
                                    {
                                        cpuTValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Fail get HWiNFO");
                        Common.LogError(ex, true, "Fail get HWiNFO");
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

            if (PluginSettings.Settings.EnableWarning)
            {
                if (PluginSettings.Settings.MinFps != 0 && PluginSettings.Settings.MinFps >= fpsValue)
                {
                    WarningMinFps = true;
                }
                if (PluginSettings.Settings.MaxCpuTemp != 0 && PluginSettings.Settings.MaxCpuTemp <= cpuTValue)
                {
                    WarningMaxCpuTemp = true;
                }
                if (PluginSettings.Settings.MaxGpuTemp != 0 && PluginSettings.Settings.MaxGpuTemp <= gpuTValue)
                {
                    WarningMaxGpuTemp = true;
                }
                if (PluginSettings.Settings.MaxCpuUsage != 0 && PluginSettings.Settings.MaxCpuUsage <= cpuValue)
                {
                    WarningMaxCpuUsage = true;
                }
                if (PluginSettings.Settings.MaxGpuUsage != 0 && PluginSettings.Settings.MaxGpuUsage <= gpuValue)
                {
                    WarningMaxGpuUsage = true;
                }
                if (PluginSettings.Settings.MaxRamUsage != 0 && PluginSettings.Settings.MaxRamUsage <= ramValue)
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
        #endregion


        #region Theme integration
        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (PluginSettings.Settings.EnableIntegrationButtonHeader)
            {
                yield return new TopPanelItem()
                {
                    Icon = new TextBlock
                    {
                        Text = "\ue97f",
                        FontSize = 20,
                        FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                    },
                    Title = resources.GetString("LOCGameActivityViewGamesActivities"),
                    Activated = () =>
                    {
                        var ViewExtension = new GameActivityView();
                        ViewExtension.Height = 660;
                        ViewExtension.Width = 1290;
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGamesActivitiesTitle"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                };
            }

            yield break;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton();
            }

            if (args.Name == "PluginChartTime")
            {
                return new PluginChartTime { DisableAnimations = true };
            }

            if (args.Name == "PluginChartLog")
            {
                return new PluginChartLog { DisableAnimations = true };
            }
             
            return null;
        }

        // SidebarItem
        public class GameActivityViewSidebar : SidebarItem
        {
            public GameActivityViewSidebar()
            {
                Type = SiderbarItemType.View;
                Title = resources.GetString("LOCGameActivityViewGamesActivities");
                Icon = new TextBlock
                {
                    Text = "\ue97f",
                    FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                };
                Opened = () =>
                {
                    SidebarItemControl sidebarItemControl = new SidebarItemControl(PluginDatabase.PlayniteApi);
                    sidebarItemControl.SetTitle(resources.GetString("LOCGamesActivitiesTitle"));
                    sidebarItemControl.AddContent(new GameActivityView());

                    return sidebarItemControl;
                };
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var items = new List<SidebarItem>
            {
                new GameActivityViewSidebar()
            };
            return items;
        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                // Show plugin view with all activities for all game in database with data of selected game
                new GameMenuItem {
                    //MenuSection = "",
                    Icon = Path.Combine(PluginFolder, "Resources", "chart-646.png"),
                    Description = resources.GetString("LOCGameActivityViewGameActivity"),
                    Action = (gameMenuItem) =>
                    {
                        var ViewExtension = new GameActivityViewSingle(GameMenu);
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
                Action = (mainMenuItem) => 
                {

                }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
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
                        var ViewExtension = new GameActivityView();
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                Description = "-"
            });
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
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            // Old database
            if (oldToNew.IsOld)
            {
                oldToNew.ConvertDB(PlayniteApi);
            }

            // Old format
            var oldFormat = PluginDatabase.Database.Select(x => x).Where(x=> x.Items.FirstOrDefault() != null && x.Items.FirstOrDefault().PlatformIDs == null);
            if (oldFormat.Count() > 0)
            {
                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                    "GameActivity - Database migration",
                    false
                );
                globalProgressOptions.IsIndeterminate = true;

                PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                {
                    foreach (GameActivities gameActivities in PluginDatabase.Database)
                    {
                        foreach(Activity activity in gameActivities.Items)
                        {
                            activity.PlatformIDs = new List<Guid> { activity.PlatformID };
                        }

                        PluginDatabase.AddOrUpdate(gameActivities);
                    }
                }, globalProgressOptions);
            }

            try
            {
                if (args.NewValue?.Count == 1)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // start timer si log is enable.
            if (PluginSettings.Settings.EnableLogging)
            {
                DataLogging_start();
            }

            DateTime DateSession = DateTime.Now.ToUniversalTime();

            GameActivitiesLog = PluginDatabase.Get(args.Game);
            GameActivitiesLog.Items.Add(new Activity
            {
                IdConfiguration = PluginDatabase.LocalSystem.GetIdConfiguration(),
                DateSession = DateSession,
                SourceID = args.Game.SourceId,
                PlatformIDs = args.Game.PlatformIds
            });
            GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, new List<ActivityDetailsData>());
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            var TaskGameStopped = Task.Run(() =>
            {
                // Stop timer si HWiNFO log is enable.
                if (PluginSettings.Settings.EnableLogging)
                {
                    DataLogging_stop();
                }

                // Infos
                GameActivitiesLog.GetLastSessionActivity().ElapsedSeconds = args.ElapsedSeconds;
                PluginDatabase.Update(GameActivitiesLog);

                if (args.Game.Id == PluginDatabase.GameContext.Id)
                {
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            });
        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            CheckGoodForLogging(true);
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            
        }
        #endregion


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            
        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GameActivitySettingsView();
        }
        #endregion
    }
}
