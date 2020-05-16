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
using Dashboard.Database.Collections;
using Dashboard.Models;

namespace Dashboard
{
    public class Dashboard : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static IGameDatabase DatabaseReference;

        private DashboardSettings settings { get; set; }

        // TODO Bad integration with structutre application
        private JArray activity { get; set; }
        private JObject activityDetails { get; set; }
        private JArray HWiNFO { get; set; }

        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        // Paths application data.
        public string pathActivityDB { get; set; }
        public string pathActivityDetailsDB { get; set; }

        // Variables timer function
        public Timer t { get; set; }


        #region Playnite GenericPlugin
        public Dashboard(IPlayniteAPI api) : base(api)
        {
            settings = new DashboardSettings(this);

            pathActivityDB = this.GetPluginUserDataPath() + "\\activity\\";
            pathActivityDetailsDB = this.GetPluginUserDataPath() + "\\activityDetails\\";

            if (!Directory.Exists(pathActivityDB))
                Directory.CreateDirectory(pathActivityDB);

            if (!Directory.Exists(pathActivityDetailsDB))
                Directory.CreateDirectory(pathActivityDetailsDB);

            // Procedure temporaire
            transformOldData_v01a(api.Paths.ConfigurationPath + "\\Extensions\\Dashboard\\database");
            transformOldData_v02a(api.Paths.ConfigurationPath + "\\Extensions\\Dashboard\\activity\\", api.Paths.ConfigurationPath + "\\Extensions\\Dashboard\\activityDetails\\");
            transformOldData_v03a(pathActivityDB, pathActivityDetailsDB, api.Paths.ConfigurationPath + "\\Extensions\\Dashboard\\activity\\", api.Paths.ConfigurationPath + "\\Extensions\\Dashboard\\activityDetails\\");
        }

        // Transform data v0.1a
        public void transformOldData_v01a(string pathActivityDB_old)
        {
            string pathFileActivityDB = pathActivityDB_old + "\\activity.json";

            if (File.Exists(pathFileActivityDB))
            {
                JObject activityDB = JObject.Parse(File.ReadAllText(pathFileActivityDB));

                try
                {
                    foreach (var item in activityDB)
                    {
                        string gameID = item.Key;
                        JArray gameDataArray = (JArray)item.Value;

                        if (!File.Exists(pathFileActivityDB))
                        {
                            using (StreamWriter sw = File.CreateText(pathActivityDB_old + item.Key + ".json"))
                            {
                                sw.WriteLine(JsonConvert.SerializeObject(JsonConvert.SerializeObject(gameDataArray)));
                            }
                        }
                    }
                    Directory.Delete(pathActivityDB_old, true);
                }
                catch (Exception ex)
                {
                    logger.Info("Dashboard plugin - transformOldData_v01a - " + ex.Message);
                }
            }
        }

        // Transform data v0.2a
        public void transformOldData_v02a(string pathActivityDB_old, string pathActivityDetailsDB_old)
        {
            if (Directory.Exists(pathActivityDB_old))
            {
                string[] filePaths = Directory.GetFiles(pathActivityDB_old, "*.json");
                for (int iFile = 0; iFile < filePaths.Length; iFile++)
                {
                    JArray gameDataArray = JArray.Parse(File.ReadAllText(filePaths[iFile]));
                    JObject gameDataLog = new JObject();
                    for (int iData = 0; iData < gameDataArray.Count; iData++)
                    {
                        if (gameDataArray[iData]["hwinfo"] != null)
                        {
                            string tempDate = Convert.ToDateTime(gameDataArray[iData]["dateSession"]).ToString("o");
                            gameDataLog.Add(new JProperty(tempDate, gameDataArray[iData]["hwinfo"]));
                        }
                    }

                    if (JsonConvert.SerializeObject(gameDataLog) != "{}")
                    {
                        string gameID = filePaths[iFile].Replace(pathActivityDB_old, "").Replace(".json", "");
                        if (!File.Exists(pathActivityDetailsDB_old + gameID + ".json"))
                        {
                            using (StreamWriter sw = File.CreateText(pathActivityDetailsDB_old + gameID + ".json"))
                            {
                                sw.WriteLine(JsonConvert.SerializeObject(gameDataLog));
                            }
                        }
                    }
                }
            }
        }

        // Transform data v0.3a
        public void transformOldData_v03a(string pathActivityDB, string pathActivityDetailsDB, string pathActivityDB_old, string pathActivityDetailsDB_old)
        {
            if (Directory.Exists(pathActivityDB_old))
            {
                if (Directory.Exists(pathActivityDB))
                {
                    Directory.Delete(pathActivityDB);
                }
                Directory.Move(pathActivityDB_old, pathActivityDB);
            }

            if (Directory.Exists(pathActivityDetailsDB_old))
            {
                if (Directory.Exists(pathActivityDetailsDB))
                {
                    Directory.Delete(pathActivityDetailsDB);
                }
                Directory.Move(pathActivityDetailsDB_old, pathActivityDetailsDB);
            }
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "Dashboard",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.
                        // PlayniteApi.Dialogs.ShowMessage("Code executed from a plugin!");
                        logger.Info("Dashboard plugin - DashboardMain");

                        DatabaseReference = PlayniteApi.Database;

                        // Show dashboard
                        new DashboardMain(settings, PlayniteApi.Database, PlayniteApi.Paths, this.GetPluginUserDataPath()).ShowDialog();
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
            if (settings.HWiNFO_enable)
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
            HWiNFO = new JArray();
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
            if (settings.HWiNFO_enable)
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
                if (JsonConvert.SerializeObject(HWiNFO) != "[]")
                {
                    activityDetails.Add(new JProperty(dateSession, HWiNFO));
                    File.WriteAllText(pathActivityDetailsDB + gameID + ".json", JsonConvert.SerializeObject(activityDetails));
                }
            }
            catch (Exception ex)
            {
                logger.Info("Dashboard plugin - OnGameStopped - " + ex.Message);
            }
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
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
            return new DashboardSettingsView();
        }
        #endregion


        #region Timer function
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void dataHWiNFO_start()
        {
            logger.Info("Dashboard plugin - dataHWiNFO_start");

            t = new Timer(settings.HWiNFO_timeLog * 60000);
            t.AutoReset = true;
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.Start();
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void dataHWiNFO_stop()
        {
            logger.Info("Dashboard plugin - dataHWiNFO_stop");

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
            HWiNFODumper HWinFO = new HWiNFODumper();
            List<HWiNFODumper.JsonObj> dataHWinfo = HWinFO.ReadMem();

            int fpsValue = 0;
            int cpuValue = GetCpuPercentage();
            int gpuValue = 0;
            int ramValue = GetRamPercentage();
            int gpuTValue = 0;
            int cpuTValue = 0;

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

            JObject HWiNFO_data = new JObject();
            HWiNFO_data["datelog"] = DateTime.Now.ToUniversalTime().ToString("o");
            HWiNFO_data["fps"] = fpsValue;
            HWiNFO_data["cpu"] = cpuValue;
            HWiNFO_data["gpu"] = gpuValue;
            HWiNFO_data["ram"] = ramValue;
            HWiNFO_data["gpuT"] = gpuTValue;
            HWiNFO_data["cpuT"] = cpuTValue;

            HWiNFO.Add(HWiNFO_data);
        }

        // http://technoblazze.blogspot.com/2015/07/get-ram-and-cpu-usage-in-c.html
        public int GetCpuPercentage()
        {
            PerformanceCounter cpuCounter;
            double cpuUsage = 0;
            int totalCpuUsage = 0;
            double returnLoopCount = 0;

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
            double returnLoopCount = 0;
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