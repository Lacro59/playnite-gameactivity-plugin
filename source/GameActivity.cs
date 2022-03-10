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
using CommonPlayniteShared.Common;
using CommonPluginsShared.Extensions;
using System.Threading;
using QuickSearch.SearchItems;
using MoreLinq;
using CommonPluginsControls.Views;

namespace GameActivity
{
    public class GameActivity : PluginExtended<GameActivitySettingsViewModel, ActivityDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        internal TopPanelItem topPanelItem;
        internal GameActivityViewSidebar gameActivityViewSidebar;

        // Variables timer functions
        public System.Timers.Timer t { get; set; }
        private GameActivities GameActivitiesLog;
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        public System.Timers.Timer tBackup { get; set; }
        private ActivityBackup activityBackup { get; set; }

        private ulong PlaytimeOnStarted { get; set; }

        //private OldToNew oldToNew;


        public GameActivity(IPlayniteAPI api) : base(api)
        {
            // Old database            
            //oldToNew = new OldToNew(this.GetPluginUserDataPath());

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

            // Remove duplicate
            Task.Run(() =>
            {
                if (!PluginSettings.Settings.HasRemovingDuplicate)
                {
                    System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                    GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                        $"GameActivity - Database updating...",
                        false
                    );
                    globalProgressOptions.IsIndeterminate = false;

                    PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                    {
                        activateGlobalProgress.ProgressMaxValue = PluginDatabase.Database.Count;

                        foreach (GameActivities gameActivities in PluginDatabase.Database)
                        {
                            Thread.Sleep(10);
                            try
                            {
                                double countBefore = gameActivities.Items.Count();
                                gameActivities.Items = gameActivities.Items.DistinctBy(x => new { x.DateSession, x.ElapsedSeconds }).ToList();
                                double countAfter = gameActivities.Items.Count();

                                if (countBefore > countAfter)
                                {
                                    logger.Warn($"Duplicate items ({countBefore - countAfter}) in {gameActivities.Name}");
                                    PluginDatabase.Update(gameActivities);
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            }

                            activateGlobalProgress.CurrentProgressValue++;
                        }

                        PluginSettings.Settings.HasRemovingDuplicate = true;
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            this.SavePluginSettings(PluginSettings.Settings);
                        });
                    }, globalProgressOptions);
                }
            });

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                topPanelItem = new TopPanelItem()
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
                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            Width = 1280,
                            Height = 740
                        };

                        var ViewExtension = new GameActivityView();
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGamesActivitiesTitle"), ViewExtension, windowOptions);
                        windowExtension.ResizeMode = ResizeMode.CanResize;
                        windowExtension.ShowDialog();
                    },
                    Visible = PluginSettings.Settings.EnableIntegrationButtonHeader
                };

                gameActivityViewSidebar = new GameActivityViewSidebar(this);
            }
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

                    var windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true,
                        Width = 1280,
                        Height = 740
                    };

                    var ViewExtension = new GameActivityViewSingle(PluginDatabase.GameContext);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension, windowOptions);
                    windowExtension.ResizeMode = ResizeMode.CanResize;
                    windowExtension.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
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
                        $"{PluginDatabase.PluginName}-runHWiNFO",
                        PluginDatabase.PluginName + Environment.NewLine + resources.GetString("LOCGameActivityNotificationHWiNFO"),
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
                        $"{PluginDatabase.PluginName }- runMSI",
                        PluginDatabase.PluginName + Environment.NewLine + resources.GetString("LOCGameActivityNotificationMSIAfterBurner"),
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


        #region Timer functions
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void DataLogging_start()
        {
            logger.Info("DataLogging_start");

            WarningsMessage = new List<WarningData>();
            t = new System.Timers.Timer(PluginSettings.Settings.TimeIntervalLogging * 60000);
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

            if (WarningsMessage.Count != 0 && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
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
                    Common.LogError(ex, false, $"Error on show WarningsMessage", true, PluginDatabase.PluginName);
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
            int cpuPValue = 0;
            int gpuPValue = 0;


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
                    MSIAfterburner = null;
                }

                if (MSIAfterburner != null)
                {
                    try
                    {
                        cpuPValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.CPU_POWER).Data;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Fail get cpuPower");
                        Common.LogError(ex, true, "Fail get cpuPower");
                    }

                    try
                    {
                        gpuPValue = (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_POWER).Data;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Fail get gpuPower");
                        Common.LogError(ex, true, "Fail get gpuPower");
                    }

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
            ActivityDetailsData activityDetailsData = new ActivityDetailsData
            {
                Datelog = DateTime.Now.ToUniversalTime(),
                FPS = fpsValue,
                CPU = cpuValue,
                CPUT = cpuTValue,
                CPUP = cpuPValue,
                GPU = gpuValue,
                GPUT = gpuTValue,
                GPUP = gpuPValue,
                RAM = ramValue
            };
            Common.LogDebug(true, Serialization.ToJson(activityDetailsData));
            ActivitiesDetailsData.Add(activityDetailsData);
        }
        #endregion


        #region Backup functions
        public void DataBackup_start()
        {
            tBackup = new System.Timers.Timer(60000);
            tBackup.AutoReset = true;
            tBackup.Elapsed += new ElapsedEventHandler(OnTimedBackupEvent);
            tBackup.Start();            
        }

        public void DataBackup_stop()
        {
            tBackup.AutoReset = false;
            tBackup.Stop();
        }

        private async void OnTimedBackupEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                ulong ElapsedSeconds = (ulong)(DateTime.Now.ToUniversalTime() - (DateTime)activityBackup.DateSession).TotalSeconds;
                activityBackup.ElapsedSeconds = ElapsedSeconds;
                activityBackup.ItemsDetailsDatas = GameActivitiesLog.ItemsDetails.Get((DateTime)activityBackup.DateSession);

                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "SaveSession.json");
                FileSystem.WriteStringToFileSafe(PathFileBackup, Serialization.ToJson(activityBackup));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        #endregion

        #endregion



        #region Theme integration
        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield return topPanelItem;
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
                return new PluginChartTime { DisableAnimations = true, LabelsRotation = true, Truncate = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate };
            }

            if (args.Name == "PluginChartLog")
            {
                return new PluginChartLog { DisableAnimations = true, LabelsRotation = true };
            }
             
            return null;
        }

        // SidebarItem
        public class GameActivityViewSidebar : SidebarItem
        {
            public GameActivityViewSidebar(GameActivity plugin)
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
                Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide;
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var items = new List<SidebarItem>
            {
                gameActivityViewSidebar
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
                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            Width = 1280,
                            Height = 740
                        };

                        var ViewExtension = new GameActivityView();
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGamesActivitiesTitle"), ViewExtension, windowOptions);
                        windowExtension.ResizeMode = ResizeMode.CanResize;
                        windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = "-"
                },

                // Show plugin view with all activities for all game in database
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = resources.GetString("LOCCommonExportData"),
                    Action = (mainMenuItem) =>
                    {
                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            $"GameActivity - {resources.GetString("LOCCommonProcessing")}",
                            false
                        );
                        globalProgressOptions.IsIndeterminate = true;

                        PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                        {
                            try
                            {
                                List<ExportedData> ExportedDatas = new List<ExportedData>();

                                foreach(GameActivities gameActivities in PluginDatabase.Database)
                                {
                                    List<ExportedData> GameExportedDatas = gameActivities.Items.Select(x => new ExportedData
                                    {
                                        Id = gameActivities.Id,
                                        Name = gameActivities.Name,
                                        LastActivity = gameActivities.LastActivity,

                                        SourceName = x.SourceName,
                                        DateSession = x.DateSession,
                                        ElapsedSeconds = x.ElapsedSeconds
                                    }).ToList();


                                    for(int i = 0; i < GameExportedDatas.Count; i++)
                                    {
                                        List<ActivityDetailsData> ActivityDetailsDatas = gameActivities.GetSessionActivityDetails(GameExportedDatas[i].DateSession);

                                        if (ActivityDetailsDatas.Count > 0)
                                        {
                                            ActivityDetailsDatas.ForEach(x => ExportedDatas.Add(new ExportedData
                                            {
                                                Id = GameExportedDatas[i].Id,
                                                Name = GameExportedDatas[i].Name,
                                                LastActivity = GameExportedDatas[i].LastActivity,

                                                SourceName = GameExportedDatas[i].SourceName,
                                                DateSession = GameExportedDatas[i].DateSession,
                                                ElapsedSeconds = GameExportedDatas[i].ElapsedSeconds,

                                                FPS = x.FPS,
                                                CPU = x.CPU,
                                                GPU = x.GPU,
                                                RAM = x.RAM,
                                                CPUT = x.CPUT,
                                                GPUT = x.GPUT
                                            }));
                                        }
                                        else
                                        {
                                            ExportedDatas.Add(new ExportedData
                                            {
                                                Id = GameExportedDatas[i].Id,
                                                Name = GameExportedDatas[i].Name,
                                                LastActivity = GameExportedDatas[i].LastActivity,

                                                SourceName = GameExportedDatas[i].SourceName,
                                                DateSession = GameExportedDatas[i].DateSession,
                                                ElapsedSeconds = GameExportedDatas[i].ElapsedSeconds
                                            });
                                        }
                                    }
                                }


                                string ExportedDatasCsv = ExportedDatas.ToCsv();
                                string SavPath = PlayniteApi.Dialogs.SaveFile("CSV|*.csv");

                                if (!SavPath.IsNullOrEmpty())
                                {
                                    try
                                    {
                                        FileSystem.WriteStringToFileSafe(SavPath, ExportedDatasCsv);

                                        string Message = string.Format(resources.GetString("LOCCommonExportDataResult"), ExportedDatasCsv.Count());
                                        var result = PlayniteApi.Dialogs.ShowMessage(Message, PluginDatabase.PluginName, MessageBoxButton.YesNo);
                                        if (result == MessageBoxResult.Yes)
                                        {
                                            Process.Start(Path.GetDirectoryName(SavPath));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, false, true, PluginDatabase.PluginName, PluginDatabase.PluginName + Environment.NewLine + ex.Message);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, true);
                            }

                        }, globalProgressOptions);
                    }
                },


                // Database management
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = resources.GetString("LOCCommonTransferPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        var ViewExtension = new TransfertData(PluginDatabase.GetDataGames(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCCommonSelectTransferData"), ViewExtension, windowOptions);
                        windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCGameActivity"),
                    Description = resources.GetString("LOCCommonIsolatedPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        var ViewExtension = new ListDataWithoutGame(PluginDatabase.GetIsolatedDataGames(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCCommonIsolatedPluginData"), ViewExtension, windowOptions);
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
            //if (oldToNew.IsOld)
            //{
            //    oldToNew.ConvertDB(PlayniteApi);
            //}

                       
            // Old format
            //var oldFormat = PluginDatabase.Database?.Select(x => x).Where(x => x.Items.FirstOrDefault() != null && x.Items.FirstOrDefault().PlatformIDs == null);
            var oldFormat = PluginDatabase.Database?.Select(x => x).Where(x => x.Items.Where(y => y.PlatformID != default(Guid)).Count() > 0);
            if (oldFormat?.Count() > 0)
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
                            activity.PlatformID = default(Guid);
                        }

                        PluginDatabase.AddOrUpdate(gameActivities);
                    }
                }, globalProgressOptions);
            }

            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
                else
                {
                    Task.Run(() =>
                    {
                        System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            if (args.NewValue?.Count == 1)
                            {
                                PluginDatabase.GameContext = args.NewValue[0];
                                PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
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
            try
            {
                PlaytimeOnStarted = args.Game.Playtime;

                DataBackup_start();

                // start timer si log is enable.
                if (PluginSettings.Settings.EnableLogging)
                {
                    DataLogging_start();
                }

                DateTime DateSession = DateTime.Now.ToUniversalTime();

                GameActivitiesLog = PluginDatabase.Get(args.Game);
                GameActivitiesLog.Items.Add(new Activity
                {
                    IdConfiguration = PluginDatabase?.LocalSystem?.GetIdConfiguration() ?? -1,
                    GameActionName = args.SourceAction?.Name ?? resources.GetString("LOCGameActivityDefaultAction"),
                    DateSession = DateSession,
                    SourceID = args.Game.SourceId == null ? default : args.Game.SourceId,
                    PlatformIDs = args.Game.PlatformIds ?? new List<Guid>()
                });
                GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, new List<ActivityDetailsData>());


                activityBackup = new ActivityBackup();
                activityBackup.Id = GameActivitiesLog.Id;
                activityBackup.Name = GameActivitiesLog.Name;
                activityBackup.ElapsedSeconds = 0;
                activityBackup.GameActionName = args.SourceAction?.Name ?? resources.GetString("LOCGameActivityDefaultAction");
                activityBackup.IdConfiguration = PluginDatabase?.LocalSystem?.GetIdConfiguration() ?? -1;
                activityBackup.DateSession = DateSession;
                activityBackup.SourceID = args.Game.SourceId == null ? default : args.Game.SourceId;
                activityBackup.PlatformIDs = args.Game.PlatformIds ?? new List<Guid>();
                activityBackup.ItemsDetailsDatas = new List<ActivityDetailsData>();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);

                DataBackup_stop();
                if (PluginSettings.Settings.EnableLogging)
                {
                    DataLogging_stop();
                }
            }
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            var TaskGameStopped = Task.Run(() =>
            {
                try
                {
                    DataBackup_stop();

                    // Stop timer si HWiNFO log is enable.
                    if (PluginSettings.Settings.EnableLogging)
                    {
                        DataLogging_stop();
                    }

                    ulong ElapsedSeconds = args.ElapsedSeconds;
                    if (ElapsedSeconds == 0)
                    {
                        Thread.Sleep(5000);
                        if (ExistsPlayStateInfoFile()) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                        {
                            ElapsedSeconds = args.Game.Playtime - PlaytimeOnStarted - GetPlayStatePausedTimeInfo(args.Game);
                        }
                        else
                        {
                            ElapsedSeconds = args.Game.Playtime - PlaytimeOnStarted;
                        }

                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}- noElapsedSeconds",
                            PluginDatabase.PluginName + System.Environment.NewLine + string.Format(resources.GetString("LOCGameActivityNoPlaytime"), args.Game.Name, ElapsedSeconds),
                            NotificationType.Info
                        ));
                    }
                    else if (ExistsPlayStateInfoFile()) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                    {
                        Thread.Sleep(5000); // Necessary since PlayState is executed after GameActivity.
                        ElapsedSeconds -= GetPlayStatePausedTimeInfo(args.Game);
                    }

                    // Infos
                    GameActivitiesLog.GetLastSessionActivity(false).ElapsedSeconds = ElapsedSeconds;
                    Common.LogDebug(true, Serialization.ToJson(GameActivitiesLog));
                    PluginDatabase.Update(GameActivitiesLog);

                    if (args.Game.Id == PluginDatabase.GameContext.Id)
                    {
                        PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Delete backup
            string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "SaveSession.json");
            FileSystem.DeleteFile(PathFileBackup);
        }

        private bool ExistsPlayStateInfoFile() // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
        {
            // PlayState will write the Id and pausedTime to PlayState.txt file placed inside ExtensionsData Roaming Playnite folder
            // Check first if this file exists and if not return false to avoid executing unnecessary code.
            string PlayStateFile = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "PlayState.txt");
            if (File.Exists(PlayStateFile))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private ulong GetPlayStatePausedTimeInfo(Game game) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
        {
            // PlayState will write the Id and pausedTime to PlayState.txt file placed inside ExtensionsData Roaming Playnite folder
            // Check first if this file exists and if not return 0 as pausedTime.
            // This check is redundant with ExistsPlayStateInfoFile, but it's because the PlayState file will be modified after the first check, so added as a fallback to avoid exceptions.
            string PlayStateFile = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "PlayState.txt");
            if (!File.Exists(PlayStateFile))
            {
                return 0;
            }

            // The file is a simple txt, first line is GameId and second line the paused time.
            string[] PlayStateInfo = File.ReadAllLines(PlayStateFile);
            string Id = PlayStateInfo[0];
            ulong PausedSeconds = Convert.ToUInt64(PlayStateInfo[1]);

            // After retrieving the info restart the file in order to avoid reusing the same txt if PlayState crash / gets uninstalled.
            string[] Info = { " ", " " };

            File.WriteAllLines(PlayStateFile, Info);

            // Check that the GameId is the same as the paused game. If so, return the paused time. If not, return 0.
            if (game.Id.ToString() == Id)
            {
                return PausedSeconds;
            }
            else
            {
                return 0;
            }
        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // CheckGoodForLogging 
            Task.Run(() =>
            {
                Common.LogDebug(true, "CheckGoodForLogging_1");
                if (!CheckGoodForLogging(false))
                {
                    Thread.Sleep(10000);
                    Common.LogDebug(true, "CheckGoodForLogging_2");
                    if (!CheckGoodForLogging(false))
                    {
                        Thread.Sleep(10000);
                        Common.LogDebug(true, "CheckGoodForLogging_3");
                        if (!CheckGoodForLogging(false))
                        {
                            Thread.Sleep(10000);
                            Common.LogDebug(true, "CheckGoodForLogging_4");
                            Application.Current.Dispatcher.BeginInvoke((Action)delegate
                            {
                                CheckGoodForLogging(true);
                            });
                        }
                    }
                }
            });


            // QuickSearch support
            try
            {
                string icon = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "chart-646.png");
                SubItemsAction GaSubItemsAction = new SubItemsAction() { Action = () => { }, Name = "", CloseAfterExecute = false, SubItemSource = new QuickSearchItemSource() };
                CommandItem GaCommand = new CommandItem(PluginDatabase.PluginName, new List<CommandAction>(), ResourceProvider.GetString("LOCGaQuickSearchDescription"), icon);
                GaCommand.Keys.Add(new CommandItemKey() { Key = "ga", Weight = 1 });
                GaCommand.Actions.Add(GaSubItemsAction);
                QuickSearch.QuickSearchSDK.AddCommand(GaCommand);
            }
            catch { }


            // Check backup
            try
            {
                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "SaveSession.json");
                if (File.Exists(PathFileBackup))
                {
                    Serialization.TryFromJsonFile<ActivityBackup>(PathFileBackup, out ActivityBackup backupData);
                    if (backupData != null)
                    {
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-backup",
                            PluginDatabase.PluginName + System.Environment.NewLine + string.Format(resources.GetString("LOCGaBackupExist"), backupData.Name),
                            NotificationType.Info,
                            () =>
                            {
                                WindowOptions windowOptions = new WindowOptions
                                {
                                    ShowMinimizeButton = false,
                                    ShowMaximizeButton = false,
                                    ShowCloseButton = true,
                                };

                                GameActivityBackup ViewExtension = new GameActivityBackup(backupData);
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCGaBackupDataInfo"), ViewExtension, windowOptions);
                                windowExtension.ShowDialog();
                            }
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
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
