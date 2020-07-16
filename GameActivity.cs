using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;
using System;
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
using GameActivity.Database.Collections;
using GameActivity.Models;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace GameActivity
{
    public class GameActivity : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();
        public static IGameDatabase DatabaseReference;

        private GameActivitySettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        private readonly IntegrationUI ui = new IntegrationUI();
        private GameActivityCollection GameActivityDatabases;

        // TODO Bad integration with structutre application
        private JArray activity { get; set; }
        private JObject activityDetails { get; set; }
        private JArray LoggingData { get; set; }

        // Paths application data.
        public string pathActivityDB { get; set; }
        public string pathActivityDetailsDB { get; set; }

        // Variables timer function
        public Timer t { get; set; }
        public List<JArray> WarningsMessage { get; set; }


        #region Playnite GenericPlugin
        public GameActivity(IPlayniteAPI api) : base(api)
        {
            settings = new GameActivitySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);

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
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    resources.GetString("LOCGameActivity"),
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        // Show GameActivity
                        DatabaseReference = PlayniteApi.Database;
                        new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
                    })
            };
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

            // Stop timer si HWiNFO log is enable.
            if (settings.EnableLogging)
            {
                dataHWiNFO_stop();
            }

            // Infos
            string gameID = game.Id.ToString();
            string gameName = game.Name;
            string dateSession = DateTime.Now.ToUniversalTime().ToString("o");
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
            GameActivity.isFirstLoad = true;
            Integration();
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        private void OnBtHeaderClick(object sender, RoutedEventArgs e)
        {
            // Show GameActivity
            DatabaseReference = PlayniteApi.Database;
            new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.

            if (settings.EnableIntegrationButtonHeader)
            {
                logger.Info("SuccessStory - Add Header button");
                Button btHeader = new GameActivityButtonHeader(TransformIcon.Get("GameActivity"));
                btHeader.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                btHeader.Click += OnBtHeaderClick;
                ui.AddButtonInWindowsHeader(btHeader);
            }
        }


        private Game GameSelected { get; set; }
        private StackPanel PART_ElemDescription = null;

        public static bool isFirstLoad = true;

        private void OnGameSelectedToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (PART_ElemDescription != null)
            {
                if ((bool)((ToggleButton)sender).IsChecked)
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_GameActivity")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integratio ToggleButton
                            foreach (ToggleButton sp in Tools.FindVisualChildren<ToggleButton>(Application.Current.MainWindow))
                            {
                                if (sp.Name == "PART_ScToggleButton")
                                {
                                    sp.IsChecked = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_GameActivity")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            if (((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_Achievements")
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            else
            {
                logger.Error("GameActivity - PART_ElemDescription not found in OnGameSelectedToggleButtonClick()");
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];

                    // Reset view visibility
                    if (PART_ElemDescription != null)
                    {
                        for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                        {
                            if ((((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_GameActivity") && (((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_Achievements"))
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }

                    Integration();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"OnGameSelected() ");
            }
        }

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            // Show GameActivity
            DatabaseReference = PlayniteApi.Database;
            new GameActivityView(settings, PlayniteApi, this.GetPluginUserDataPath(), GameSelected).ShowDialog();
        }

        private void Integration()
        {
            try
            {
                // Refresh database
                if (GameActivity.isFirstLoad)
                {
                    GameActivityDatabases = new GameActivityCollection();
                    GameActivityDatabases.InitializeCollection(this.GetPluginUserDataPath());
                    GameActivity.isFirstLoad = false;
                }


                GameActivityClass SelectedGameGameActivity = GameActivityDatabases.Get(GameSelected.Id);


                // Search game description
                if (PART_ElemDescription == null)
                {
                    foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                    {
                        if (sp.Name == "PART_ElemDescription")
                        {
                            PART_ElemDescription = sp;
                            break;
                        }
                    }
                }


                // Delete
                logger.Info("GameActivity - Delete");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_GaButton");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_GaToggleButton");
                ui.RemoveElementInGameSelectedDescription("PART_GameActivity");
                ui.ClearElementInCustomTheme("PART_GameActivty_Graphic");
                ui.ClearElementInCustomTheme("PART_GameActivty_GraphicLog");


                // Reset resources
                //List<ResourcesList> resourcesLists = new List<ResourcesList>();
                //ui.AddResources(resourcesLists);


                // No game activity
                if (SelectedGameGameActivity == null)
                {
                    logger.Info("GameActivity - No activity for " + GameSelected.Name);
                    return;
                }


                // Add resources
                //ui.AddResources(resourcesLists);


                // Auto integration
                if (settings.EnableIntegrationInDescription || settings.EnableIntegrationInDescriptionWithToggle)
                {
                    if (settings.EnableIntegrationInDescriptionWithToggle)
                    {
                        ToggleButton tb = new ToggleButton();
                        if (settings.IntegrationToggleDetails)
                        {
                            tb = new GameActivityToggleButtonDetails(SelectedGameGameActivity.GetLastSessionActivity().ElapsedSeconds);
                        }
                        else
                        {
                            tb = new GameActivityToggleButton();
                            tb.Content = resources.GetString("LOCGameActivityTitle");
                        }
                        
                        tb.IsChecked = false;
                        tb.Name = "PART_GaToggleButton";
                        tb.Width = 150;
                        tb.HorizontalAlignment = HorizontalAlignment.Right;
                        tb.VerticalAlignment = VerticalAlignment.Stretch;
                        tb.Margin = new Thickness(10, 0, 0, 0);
                        tb.Click += OnGameSelectedToggleButtonClick;
                        
                        ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(tb);
                    }


                    // Add game activity elements
                    StackPanel GaSp = CreateGa(SelectedGameGameActivity, settings.IntegrationShowTitle, settings.IntegrationShowGraphic, settings.IntegrationShowGraphicLog, false);

                    if (settings.EnableIntegrationInDescriptionWithToggle)
                    {
                        GaSp.Visibility = Visibility.Collapsed;
                    }

                    ui.AddElementInGameSelectedDescription(GaSp, settings.IntegrationTopGameDetails);
                }


                // Auto adding button
                if (settings.EnableIntegrationButton || settings.EnableIntegrationButtonDetails)
                {
                    Button bt = new Button();
                    if (settings.EnableIntegrationButton)
                    {
                        bt.Content = resources.GetString("LOCGameActivityTitle");
                    }

                    if (settings.EnableIntegrationButtonDetails)
                    {
                        bt = new GameActivityButtonDetails(SelectedGameGameActivity.GetLastSessionActivity().ElapsedSeconds);
                    }

                    bt.Name = "PART_GaButton";
                    bt.Width = 150;
                    bt.HorizontalAlignment = HorizontalAlignment.Right;
                    bt.VerticalAlignment = VerticalAlignment.Stretch;
                    bt.Margin = new Thickness(10, 0, 0, 0);
                    bt.Click += OnBtGameSelectedActionBarClick;

                    ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(bt);
                }


                // Custom theme
                if (settings.EnableIntegrationInCustomTheme)
                {
                    // Create 
                    StackPanel spGaG = CreateGa(SelectedGameGameActivity, false, true, false, true);
                    ui.AddElementInCustomTheme(spGaG, "PART_GameActivty_Graphic");

                    StackPanel spGaGL = CreateGa(SelectedGameGameActivity, false, false, true, true);
                    ui.AddElementInCustomTheme(spGaGL, "PART_GameActivty_GraphicLog");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"Impossible integration");
            }
        }

        // Create FrameworkElement with game activity datas
        public StackPanel CreateGa(GameActivityClass gameActivity, bool ShowTitle, bool ShowGraphic, bool ShowGraphicLog, bool IsCustom = false)
        {
            StackPanel spGa = new StackPanel();
            spGa.Name = "PART_GameActivity";

            if (ShowTitle)
            {
                TextBlock tbGa = new TextBlock();
                tbGa.Name = "PART_GameActivity_TextBlock";
                tbGa.Text = resources.GetString("LOCGameActivityTitle");
                tbGa.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                tbGa.Margin = new Thickness(0, 15, 0, 10);

                Separator sep = new Separator();
                sep.Name = "PART_GameActivity_Separator";
                sep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");

                spGa.Children.Add(tbGa);
                spGa.Children.Add(sep);
                spGa.UpdateLayout();
            }

            if (ShowGraphic)
            {
                StackPanel spGaG = new StackPanel();
                if (!IsCustom)
                {
                    spGaG.Name = "PART_GameActivty_Graphic";
                    spGaG.Height = 120;
                    spGaG.MaxHeight = 120;
                    spGaG.Margin = new Thickness(0, 5, 0, 5);
                }

                spGaG.Children.Add(new GameActivityGameGraphicTime(settings, gameActivity));

                spGa.Children.Add(spGaG);
                spGa.UpdateLayout();
            }

            if (ShowGraphicLog)
            {
                StackPanel spGaGL = new StackPanel();
                if (!IsCustom)
                {
                    spGaGL.Name = "PART_GameActivty_GraphicLog";
                    spGaGL.Height = 120;
                    spGaGL.MaxHeight = 120;
                    spGaGL.Margin = new Thickness(0, 5, 0, 5);
                }

                spGaGL.Children.Add(new GameActivityGameGraphicLog(settings, gameActivity));

                spGa.Children.Add(spGaGL);
                spGa.UpdateLayout();
            }

            return spGa;
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
        #endregion


        #region Timer function
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void dataHWiNFO_start()
        {
            logger.Info("GameActivity - dataLogging_start");

            WarningsMessage = new List<JArray>();
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
                new WarningsDialogs(resources.GetString("LOCGameActivityWarningCaption"), WarningsMessage).ShowDialog();
                WarningsMessage = new List<JArray>();
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
            int cpuValue = GetCpuPercentage();
            int gpuValue = 0;
            int ramValue = GetRamPercentage();
            int gpuTValue = 0;
            int cpuTValue = 0;


            if (settings.UseMsiAfterburner)
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
            else if (settings.UseHWiNFO)
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

                                //logger.Info("----- " + dataID.ToLower() + " - " + settings.HWiNFO_fps_elementID.ToLower());

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

                                //logger.Info("----- " + dataID.ToLower() + " - " + settings.HWiNFO_gpu_elementID.ToLower());

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

                                //logger.Info("----- " + dataID.ToLower() + " - " + settings.HWiNFO_gpu_elementID.ToLower());

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

                                //logger.Info("----- " + dataID.ToLower() + " - " + settings.HWiNFO_gpu_elementID.ToLower());

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

                JArray Message = new JArray
                {
                    new JObject (new JProperty("At", resources.GetString("LOCGameActivityWarningAt") + " " + DateTime.Now.ToString("HH:mm"))),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityFps")), new JProperty("Value", fpsValue), new JProperty("isWarm", WarningMinFps)),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityCpuTemp")), new JProperty("Value", cpuTValue), new JProperty("isWarm", WarningMaxCpuTemp)),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityGpuTemp")), new JProperty("Value", gpuTValue), new JProperty("isWarm", WarningMaxGpuTemp)),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityCpuUsage")), new JProperty("Value", cpuValue), new JProperty("isWarm", WarningMaxCpuUsage)),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityGpuUsage")), new JProperty("Value", gpuValue), new JProperty("isWarm", WarningMaxGpuUsage)),
                    new JObject (new JProperty("Name", resources.GetString("LOCGameActivityRamUsage")), new JProperty("Value", ramValue), new JProperty("isWarm", WarningMaxRamUsage))
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

        // http://technoblazze.blogspot.com/2015/07/get-ram-and-cpu-usage-in-c.html
        public int GetCpuPercentage()
        {
            PerformanceCounter cpuCounter;
            double cpuUsage = 0;
            int totalCpuUsage = 0;
            //double returnLoopCount = 0;

            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            for (int i = 0; i < 5; i++)
            {
                cpuUsage += cpuCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
            }
            totalCpuUsage = Convert.ToInt32(Math.Ceiling(cpuUsage / 5));
            return totalCpuUsage;
        }

        public int GetRamPercentage()
        {
            PerformanceCounter ramCounter;
            double ramUsage = 0;
            int TotalRamMemory = 0;
            int AvailableRamMemory = 0;
            int UsedRamMemory = 0;
            int RamUsagePercentage = 0;
            //double returnLoopCount = 0;
            MEMORYSTATUSEX statEX = new MEMORYSTATUSEX();
            statEX.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref statEX);

            double ram = (double)statEX.ullTotalPhys;
            //float ram = (float)stat.TotalPhysical;
            ram /= 1024;
            ram /= 1024;

            TotalRamMemory = Convert.ToInt32(Math.Round(ram, 0));
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            for (int i = 0; i < 5; i++)
            {
                ramUsage += ramCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
            }
            AvailableRamMemory = Convert.ToInt32(Math.Round((ramUsage / 5), 0));
            UsedRamMemory = TotalRamMemory - AvailableRamMemory;
            RamUsagePercentage = ((UsedRamMemory * 100) / TotalRamMemory);

            return RamUsagePercentage;

        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }
        #endregion
    }
}
