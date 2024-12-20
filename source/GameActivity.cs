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
using System.Globalization;

namespace GameActivity
{
    public class GameActivity : PluginExtended<GameActivitySettingsViewModel, ActivityDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        internal TopPanelItem TopPanelItem { get; set; }
        internal SidebarItem SidebarItem { get; set; }
        internal SidebarItemControl SidebarItemControl { get; set; }

        private List<RunningActivity> RunningActivities => new List<RunningActivity>();


        public GameActivity(IPlayniteAPI api) : base(api)
        {
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

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                TopPanelItem = new GameActivityTopPanelItem(this);
                SidebarItem = new GameActivityViewSidebar(this);
            }
        }


        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomGameActivityButton")
                {
                    Common.LogDebug(true, $"OnCustomThemeButtonClick()");

                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true,
                        CanBeResizable = true,
                        Height = 740,
                        Width = 1280
                    };

                    GameActivityViewSingle ViewExtension = new GameActivityViewSingle(this, PluginDatabase.GameContext);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivity"), ViewExtension, windowOptions);
                    _ = windowExtension.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private bool CheckGoodForLogging(bool WithNotification = false)
        {
            if (PluginSettings.Settings.EnableLogging && (PluginSettings.Settings.UseHWiNFO || PluginSettings.Settings.UseHWiNFOGadget))
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
                        PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCGameActivityNotificationHWiNFO"),
                        NotificationType.Error,
                        () => OpenSettingsView()
                    ));
                }

                if (!runHWiNFO)
                {
                    Logger.Error("No HWiNFO running");
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
                        PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCGameActivityNotificationMSIAfterBurner"),
                        NotificationType.Error,
                        () => OpenSettingsView()
                    ));
                }

                if (!runMSI)
                {
                    Logger.Warn("No MSI Afterburner running");
                }
                if (!runRTSS)
                {
                    Logger.Warn("No RivaTunerStatisticsServer running");
                }

                if (!WithNotification)
                {
                    return runMSI && runRTSS;
                }
            }

            return false;
        }


        #region Timer functions
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void DataLogging_start(Guid Id)
        {
            Logger.Info($"DataLogging_start - {Id}");
            RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);

            runningActivity.timer = new System.Timers.Timer(PluginSettings.Settings.TimeIntervalLogging * 60000)
            {
                AutoReset = true
            };
            runningActivity.timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, Id);
            runningActivity.timer.Start();
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void DataLogging_stop(Guid Id)
        {
            Logger.Info($"DataLogging_stop - {Id}");
            RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);
            if (runningActivity.WarningsMessage.Count != 0 && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                try
                {
                    _= Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        WarningsDialogs ViewExtension = new WarningsDialogs(runningActivity.WarningsMessage);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivityWarningCaption"), ViewExtension);
                       _ = windowExtension.ShowDialog();
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on show WarningsMessage - {Id}", true, PluginDatabase.PluginName);
                }
            }

            runningActivity.timer.AutoReset = false;
            runningActivity.timer.Stop();
        }

        /// <summary>
        /// Event excuted with the timer.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e, Guid Id)
        {
            int fpsValue = 0;
            int cpuValue = 0;
            int gpuValue = 0;
            int ramValue = 0;
            int gpuTValue = 0;
            int cpuTValue = 0;
            int cpuPValue = 0;
            int gpuPValue = 0;

            double temp;

            if (PluginSettings.Settings.UsedLibreHardware && PluginSettings.Settings.WithRemoteServerWeb && !PluginSettings.Settings.IpRemoteServerWeb.IsNullOrEmpty())
            {
                LibreHardwareData libreHardwareMonitorData = LibreHardware.GetDataWeb(PluginSettings.Settings.IpRemoteServerWeb);
                if (libreHardwareMonitorData != null)
                {
                    string CpuPowers = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 3)?
                        .Children?.Find(x => x.Text == "Powers")?
                        .Children?.Find(x => x.Text == "CPU Package")?.Value;
                    CpuPowers = CpuPowers?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("W", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(CpuPowers, out temp);
                    cpuPValue = Convert.ToInt32(Math.Round(temp, 0));

                    string CpuLoad = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 3)?
                        .Children?.Find(x => x.Text == "Load")?
                        .Children?.Find(x => x.Text == "CPU Total")?.Value;
                    CpuLoad = CpuLoad?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("%", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(CpuLoad, out temp);
                    cpuValue = Convert.ToInt32(Math.Round(temp, 0));

                    string CpuTemperatures = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 3)?
                        .Children?.Find(x => x.Text == "Temperatures")?
                        .Children?.Find(x => x.Text == "CPU Package")?.Value;
                    CpuTemperatures = CpuTemperatures?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("°C", string.Empty)
                        ?.Replace("°F", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(CpuTemperatures, out temp);
                    cpuTValue = Convert.ToInt32(Math.Round(temp, 0));


                    string LoadMemory = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 43)?
                        .Children?.Find(x => x.Text == "Load")?
                        .Children?.Find(x => x.Text == "Memory")?.Value;
                    LoadMemory = LoadMemory?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("%", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(LoadMemory, out temp);
                    ramValue = Convert.ToInt32(Math.Round(temp, 0));


                    string GpuPowers = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 52)?
                        .Children?.Find(x => x.Text == "Powers")?
                        .Children?.Find(x => x.Text == "GPU Power")?.Value;
                    GpuPowers = GpuPowers?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("W", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(GpuPowers, out temp);
                    gpuPValue = Convert.ToInt32(Math.Round(temp, 0));

                    string GpuLoad = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 52)?
                        .Children?.Find(x => x.Text == "Load")?
                        .Children?.Find(x => x.Text == "D3D 3D")?.Value;
                    GpuLoad = GpuLoad?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("%", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(GpuLoad, out temp);
                    gpuValue = Convert.ToInt32(Math.Round(temp, 0));

                    string GpuTemperatures = libreHardwareMonitorData.Children[0]?.Children.Find(x => x.id == 52)?
                        .Children?.Find(x => x.Text == "Load")?
                        .Children?.Find(x => x.Text == "?")?.Value;
                    GpuTemperatures = GpuTemperatures?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                        ?.Replace("°C", string.Empty)
                        ?.Replace("°F", string.Empty)
                        ?.Trim();
                    _ = double.TryParse(GpuTemperatures, out temp);
                    gpuTValue = Convert.ToInt32(Math.Round(temp, 0));
                }
            }


            cpuValue = cpuValue == 0 ? PerfCounter.GetCpuPercentage() : cpuValue;
            gpuValue = gpuValue == 0 ? PerfCounter.GetGpuPercentage() : gpuValue;
            ramValue = ramValue == 0 ? PerfCounter.GetRamPercentage() : ramValue;
            gpuTValue = gpuTValue == 0 ? PerfCounter.GetGpuTemperature() : gpuTValue;
            cpuTValue = cpuTValue == 0 ? PerfCounter.GetCpuTemperature() : cpuTValue;
            cpuPValue = cpuPValue == 0 ? PerfCounter.GetCpuPower() : cpuPValue;
            gpuPValue = gpuPValue == 0 ? PerfCounter.GetGpuPower() : gpuPValue;


            if (PluginSettings.Settings.UseMsiAfterburner && CheckGoodForLogging())
            {
                MSIAfterburnerNET.HM.HardwareMonitor MSIAfterburner = null;

                try
                {
                    MSIAfterburner = new MSIAfterburnerNET.HM.HardwareMonitor();
                }
                catch (Exception ex)
                {
                    Logger.Warn("MSIAfterburnerNET - Fail initialize");
                    Common.LogError(ex, true, "MSIAfterburnerNET - Fail initialize");
                    MSIAfterburner = null;
                }

                if (MSIAfterburner != null)
                {
                    try
                    {
                        cpuPValue = cpuPValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.CPU_POWER).Data : cpuPValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSIAfterburnerNET - Fail get cpuPower");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get cpuPower");
                    }

                    try
                    {
                        gpuPValue = gpuPValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_POWER).Data : gpuPValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSIAfterburnerNET - Fail get gpuPower");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get gpuPower");
                    }

                    try
                    {
                        fpsValue = fpsValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.FRAMERATE).Data : fpsValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("FMSIAfterburnerNET - Fail get fpsValue");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get fpsValue");
                    }

                    try
                    {
                        gpuValue = gpuValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_USAGE).Data : gpuValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSIAfterburnerNET - Fail get gpuValue");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get gpuValue");
                    }

                    try
                    {
                        gpuTValue = gpuTValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.GPU_TEMPERATURE).Data : gpuTValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSIAfterburnerNET - Fail get gpuTValue");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get gpuTValue");
                    }

                    try
                    {
                        cpuTValue = cpuTValue == 0 ? (int)MSIAfterburner.GetEntry(MONITORING_SOURCE_ID.CPU_TEMPERATURE).Data : cpuTValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSIAfterburnerNET - Fail get cpuTValue");
                        Common.LogError(ex, true, "MSIAfterburnerNET - Fail get cpuTValue");
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
                    Logger.Error("HWiNFODumper - Fail initialize");
                    Common.LogError(ex, true, "HWiNFODumper - Fail initialize");
                }

                if (HWinFO != null && dataHWinfo != null)
                {
                    try
                    {
                        foreach (HWiNFODumper.JsonObj sensorItems in dataHWinfo)
                        {
                            dynamic sensorItemsOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(sensorItems));
                            string sensorsID = "0x" + ((uint)sensorItemsOBJ["szSensorSensorID"]).ToString("X");

                            // Find sensors fps
                            if (fpsValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_fps_sensorsID.ToLower())
                            {
                                // Find data fps
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
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
                            if (gpuValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_gpu_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
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
                            if (gpuTValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_gpuT_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
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
                            if (cpuTValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_cpuT_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_cpuT_elementID.ToLower())
                                    {
                                        cpuTValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }

                            // Find sensors gpu power
                            if (gpuPValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_gpuP_elementID.ToLower())
                            {
                                // Find data gpu
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_gpuP_elementID.ToLower())
                                    {
                                        gpuPValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }

                            // Find sensors cpu power
                            if (cpuPValue == 0 && sensorsID.ToLower() == PluginSettings.Settings.HWiNFO_cpuP_sensorsID.ToLower())
                            {
                                // Find data gpu
                                foreach (dynamic items in sensorItemsOBJ["sensors"])
                                {
                                    dynamic itemOBJ = Serialization.FromJson<dynamic>(Serialization.ToJson(items));
                                    string dataID = "0x" + ((uint)itemOBJ["dwSensorID"]).ToString("X");

                                    if (dataID.ToLower() == PluginSettings.Settings.HWiNFO_cpuP_sensorsID.ToLower())
                                    {
                                        cpuPValue = (int)Math.Round((Double)itemOBJ["Value"]);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("HWiNFODumper - Fail get HWiNFO");
                        Common.LogError(ex, true, "HWiNFODumper - Fail get HWiNFO");
                    }
                }
            }
            else if (PluginSettings.Settings.UseHWiNFOGadget && CheckGoodForLogging())
            {
                try
                {
                    if (fpsValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_fps_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        fpsValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                    if (gpuValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_gpu_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        gpuValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                    if (gpuTValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_gpuT_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        gpuTValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                    if (cpuTValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_cpuT_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        cpuTValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                    if (cpuPValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_cpuP_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        cpuPValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                    if (gpuPValue == 0)
                    {
                        _ = double.TryParse(HWiNFOGadget.GetData(PluginSettings.Settings.HWiNFO_gpuP_index)
                            ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            ?.Trim(), out temp);
                        gpuPValue = Convert.ToInt32(Math.Round(temp, 0));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("HWiNFOGadget - Fail initialize");
                    Common.LogError(ex, true, "HWiNFOGadget - Fail initialize");
                }
            }


            RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);
            if (runningActivity == null)
            {
                return;
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
                    At = ResourceProvider.GetString("LOCGameActivityWarningAt") + " " + DateTime.Now.ToString("HH:mm"),
                    FpsData = new Data { Name = ResourceProvider.GetString("LOCGameActivityFps"), Value = fpsValue, IsWarm = WarningMinFps },
                    CpuTempData = new Data { Name = ResourceProvider.GetString("LOCGameActivityCpuTemp"), Value = cpuTValue, IsWarm = WarningMaxCpuTemp },
                    GpuTempData = new Data { Name = ResourceProvider.GetString("LOCGameActivityGpuTemp"), Value = gpuTValue, IsWarm = WarningMaxGpuTemp },
                    CpuUsageData = new Data { Name = ResourceProvider.GetString("LOCGameActivityCpuUsage"), Value = cpuValue, IsWarm = WarningMaxCpuUsage },
                    GpuUsageData = new Data { Name = ResourceProvider.GetString("LOCGameActivityGpuUsage"), Value = gpuValue, IsWarm = WarningMaxGpuUsage },
                    RamUsageData = new Data { Name = ResourceProvider.GetString("LOCGameActivityRamUsage"), Value = ramValue, IsWarm = WarningMaxRamUsage },
                };

                if (WarningMinFps || WarningMaxCpuTemp || WarningMaxGpuTemp || WarningMaxCpuUsage || WarningMaxGpuUsage)
                {
                    runningActivity.WarningsMessage.Add(Message);
                }
            }

            List<ActivityDetailsData> ActivitiesDetailsData = runningActivity.GameActivitiesLog.ItemsDetails.Get(runningActivity.activityBackup.DateSession);
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
        public void DataBackup_start(Guid Id)
        {
            RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);
            if (runningActivity == null)
            {
                Logger.Warn($"No runningActivity find for {Id}");
                return;
            }

            runningActivity.timerBackup = new System.Timers.Timer(PluginSettings.Settings.TimeIntervalLogging * 60000);
            runningActivity.timerBackup.AutoReset = true;
            runningActivity.timerBackup.Elapsed += (sender, e) => OnTimedBackupEvent(sender, e, Id);
            runningActivity.timerBackup.Start();
        }

        public void DataBackup_stop(Guid Id)
        {
            RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);
            if (runningActivity == null)
            {
                Logger.Warn($"No runningActivity find for {Id}");
                return;
            }

            runningActivity.timerBackup.AutoReset = false;
            runningActivity.timerBackup.Stop();
        }

        private void OnTimedBackupEvent(object source, ElapsedEventArgs e, Guid Id)
        {
            try
            {
                RunningActivity runningActivity = RunningActivities.Find(x => x.Id == Id);

                ulong ElapsedSeconds = (ulong)(DateTime.Now.ToUniversalTime() - runningActivity.activityBackup.DateSession).TotalSeconds;
                runningActivity.activityBackup.ElapsedSeconds = ElapsedSeconds;
                runningActivity.activityBackup.ItemsDetailsDatas = runningActivity.GameActivitiesLog.ItemsDetails.Get(runningActivity.activityBackup.DateSession);

                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{Id}.json");
                FileSystem.WriteStringToFileSafe(PathFileBackup, Serialization.ToJson(runningActivity.activityBackup));
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
            yield return TopPanelItem;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton(this);
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

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            List<SidebarItem> items = new List<SidebarItem> { SidebarItem };
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
                new GameMenuItem
                {
                    //MenuSection = "",
                    Icon = Path.Combine(PluginFolder, "Resources", "chart-646.png"),
                    Description = ResourceProvider.GetString("LOCGameActivityViewGameActivity"),
                    Action = (gameMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            CanBeResizable = true,
                            Height = 740,
                            Width = 1280
                        };

                        GameActivityViewSingle ViewExtension = new GameActivityViewSingle(this, GameMenu);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivity"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCGameActivity"),
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
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCGameActivityViewGamesActivities"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            CanBeResizable = true,
                            Height = 740,
                            Width = 1280
                        };

                        GameActivityView ViewExtension = new GameActivityView(this);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGamesActivitiesTitle"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = "-"
                },

                // Show plugin view with all activities for all game in database
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonExportData"),
                    Action = (mainMenuItem) =>
                    {
                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"GameActivity - {ResourceProvider.GetString("LOCCommonProcessing")}") { Cancelable = false,
                        IsIndeterminate = true };

                        PlayniteApi.Dialogs.ActivateGlobalProgress((a) =>
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

                                        string Message = string.Format(ResourceProvider.GetString("LOCCommonExportDataResult"), ExportedDatasCsv.Count());
                                        MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage(Message, PluginDatabase.PluginName, MessageBoxButton.YesNo);
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
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonTransferPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        TransfertData ViewExtension = new TransfertData(PluginDatabase.GetDataGames().ToList(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCCommonSelectTransferData"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonIsolatedPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        ListDataWithoutGame ViewExtension = new ListDataWithoutGame(PluginDatabase.GetIsolatedDataGames().ToList(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCCommonIsolatedPluginData"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
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
            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
                else
                {
                    _ = Task.Run(() =>
                    {
                        SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);
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
                RunningActivity runningActivity = new RunningActivity();
                runningActivity.Id = args.Game.Id;
                runningActivity.PlaytimeOnStarted = args.Game.Playtime;

                RunningActivities.Add(runningActivity);

                DataBackup_start(args.Game.Id);

                // start timer si log is enable.
                if (PluginSettings.Settings.EnableLogging)
                {
                    DataLogging_start(args.Game.Id);
                }

                DateTime DateSession = DateTime.Now.ToUniversalTime();

                runningActivity.GameActivitiesLog = PluginDatabase.Get(args.Game);
                runningActivity.GameActivitiesLog.Items.Add(new Activity
                {
                    IdConfiguration = PluginDatabase?.LocalSystem?.GetIdConfiguration() ?? -1,
                    GameActionName = args.SourceAction?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction"),
                    DateSession = DateSession,
                    SourceID = args.Game.SourceId == null ? default : args.Game.SourceId,
                    PlatformIDs = args.Game.PlatformIds ?? new List<Guid>()
                });
                _ = runningActivity.GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, new List<ActivityDetailsData>());

                runningActivity.activityBackup = new ActivityBackup
                {
                    Id = runningActivity.GameActivitiesLog.Id,
                    Name = runningActivity.GameActivitiesLog.Name,
                    ElapsedSeconds = 0,
                    GameActionName = args.SourceAction?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction"),
                    IdConfiguration = PluginDatabase?.LocalSystem?.GetIdConfiguration() ?? -1,
                    DateSession = DateSession,
                    SourceID = args.Game.SourceId == null ? default : args.Game.SourceId,
                    PlatformIDs = args.Game.PlatformIds ?? new List<Guid>(),
                    ItemsDetailsDatas = new List<ActivityDetailsData>()
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);

                DataBackup_stop(args.Game.Id);
                if (PluginSettings.Settings.EnableLogging)
                {
                    DataLogging_stop(args.Game.Id);
                }
            }
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    RunningActivity runningActivity = RunningActivities.Find(x => x.Id == args.Game.Id);
                    DataBackup_stop(args.Game.Id);

                    // Stop timer si HWiNFO log is enable.
                    if (PluginSettings.Settings.EnableLogging)
                    {
                        DataLogging_stop(args.Game.Id);
                    }

                    if (runningActivity == null)
                    {
                        return;
                    }

                    ulong ElapsedSeconds = args.ElapsedSeconds;
                    if (ElapsedSeconds == 0)
                    {
                        Thread.Sleep(5000);
                        // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                        ElapsedSeconds = PluginSettings.Settings.SubstPlayStateTime && ExistsPlayStateInfoFile()
                            ? args.Game.Playtime - runningActivity.PlaytimeOnStarted - GetPlayStatePausedTimeInfo(args.Game)
                            : args.Game.Playtime - runningActivity.PlaytimeOnStarted;

                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}- noElapsedSeconds",
                            PluginDatabase.PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCGameActivityNoPlaytime"), args.Game.Name, ElapsedSeconds),
                            NotificationType.Info
                        ));
                    }
                    else if (PluginSettings.Settings.SubstPlayStateTime && ExistsPlayStateInfoFile()) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                    {
                        Thread.Sleep(10000); // Necessary since PlayState is executed after GameActivity.
                        ElapsedSeconds -= GetPlayStatePausedTimeInfo(args.Game);
                    }

                    // Infos
                    runningActivity.GameActivitiesLog.GetLastSessionActivity(false).ElapsedSeconds = ElapsedSeconds;
                    Common.LogDebug(true, Serialization.ToJson(runningActivity.GameActivitiesLog));
                    PluginDatabase.Update(runningActivity.GameActivitiesLog);

                    if (args.Game.Id == PluginDatabase.GameContext.Id)
                    {
                        PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                    }

                    // Delete running data
                    _ = RunningActivities.Remove(runningActivity);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Delete backup
            string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{args.Game.Id}.json");
            FileSystem.DeleteFile(PathFileBackup);
        }


        private bool ExistsPlayStateInfoFile() // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
        {
            // PlayState will write the Id and pausedTime to PlayState.txt file placed inside ExtensionsData Roaming Playnite folder
            // Check first if this file exists and if not return false to avoid executing unnecessary code.
            string PlayStateFile = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "PlayState.txt");
            return File.Exists(PlayStateFile);
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
            ulong PausedSeconds = ulong.TryParse(PlayStateInfo[1], out ulong number) ? number : 0;

            // After retrieving the info restart the file in order to avoid reusing the same txt if PlayState crash / gets uninstalled.
            string[] Info = { " ", " " };

            File.WriteAllLines(PlayStateFile, Info);

            // Check that the GameId is the same as the paused game. If so, return the paused time. If not, return 0.
            return game.Id.ToString() == Id ? PausedSeconds : 0;
        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // CheckGoodForLogging 
            _ = Task.Run(() =>
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
                _ = QuickSearch.QuickSearchSDK.AddCommand(GaCommand);
            }
            catch { }


            // Check backup
            try
            {
                _ = Task.Run(() =>
                {
                    Parallel.ForEach(Directory.EnumerateFiles(PluginDatabase.Paths.PluginUserDataPath, "SaveSession_*.json"), (objectFile) =>
                    {
                        // Wait extension database are loaded
                        _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                        _ = Serialization.TryFromJsonFile(objectFile, out ActivityBackup backupData);
                        if (backupData != null)
                        {
                            // If game is deleted...
                            Game game = API.Instance.Database.Games.Get(backupData.Id);
                            if (game == null)
                            {
                                try
                                {
                                    FileSystem.DeleteFileSafe(objectFile);
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                                }
                            }
                            // Otherwise...
                            else
                            {
                                _ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    PlayniteApi.Notifications.Add(new NotificationMessage(
                                        $"{PluginDatabase.PluginName}-backup-{Path.GetFileNameWithoutExtension(objectFile)}",
                                        PluginDatabase.PluginName + System.Environment.NewLine + string.Format(ResourceProvider.GetString("LOCGaBackupExist"), backupData.Name),
                                        NotificationType.Info,
                                        () =>
                                        {
                                            WindowOptions windowOptions = new WindowOptions
                                            {
                                                ShowMinimizeButton = false,
                                                ShowMaximizeButton = false,
                                                ShowCloseButton = true,
                                                CanBeResizable = true,
                                                Height = 350,
                                                Width = 800
                                            };

                                            GameActivityBackup ViewExtension = new GameActivityBackup(backupData);
                                            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGaBackupDataInfo"), ViewExtension, windowOptions);
                                            _ = windowExtension.ShowDialog();
                                        }
                                    ));
                                });
                            }
                        }
                    });
                });
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
