using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using GameActivity.Services.HardwareMonitoring.Providers;
using GameActivity.Services.HardwareMonitoring.Utilities;
using GameActivity.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameActivity.Services
{
    public class GameActivityMonitoring
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        internal GenericPlugin Plugin { get; set; }

        private HardwareDataAggregator _hardwareMonitor;
        private ProviderHealthMonitor _healthMonitor;
        private MonitoringDiagnostics _diagnostics;

        private List<RunningActivity> _runningActivities = new List<RunningActivity>();

        public GameActivityMonitoring(GenericPlugin plugin)
        {
            Plugin = plugin;
        }

        /// <summary>
        /// Initialize the monitoring system
        /// </summary>
        public void InitializeMonitoring()
        {
            Logger.Info("Initializing hardware monitoring system...");

            try
            {
                var config = new MonitoringConfiguration
                {
                    MaxFailuresBeforeFallback = 5,
                    CacheDurationMs = 500,
                    EnableAutoFallback = true,
                };

                _hardwareMonitor = new HardwareDataAggregator(config);
                RegisterProviders();

                if (_hardwareMonitor.Initialize())
                {
                    Logger.Info("Hardware monitoring initialized successfully");

                    _healthMonitor = new ProviderHealthMonitor(_hardwareMonitor);
                    _diagnostics = new MonitoringDiagnostics(_hardwareMonitor);

                    _diagnostics.LogDiagnostics();

                    ValidateExternalDependencies();
                }
                else
                {
                    Logger.Warn("No hardware monitoring providers available");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize hardware monitoring");
            }
        }

        /// <summary>
        /// Register all available hardware monitoring providers
        /// </summary>
        private void RegisterProviders()
        {
            if (PluginDatabase.PluginSettings.Settings.UseRivaTuner)
            {
                try
                {
                    _hardwareMonitor.RegisterProvider(new RivaTunerProvider());
                    Logger.Info("Registered RivaTuner provider");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to register RivaTuner provider");
                }
            }

            if (PluginDatabase.PluginSettings.Settings.UseLibreHardware)
            {
                try
                {
                    string remoteIp = null;
                    if (
                        PluginDatabase.PluginSettings.Settings.WithRemoteServerWeb
                        && !string.IsNullOrEmpty(
                            PluginDatabase.PluginSettings.Settings.IpRemoteServerWeb
                        )
                    )
                    {
                        remoteIp = PluginDatabase.PluginSettings.Settings.IpRemoteServerWeb;
                        Logger.Info($"Using LibreHardware remote server: {remoteIp}");
                    }

                    _hardwareMonitor.RegisterProvider(new LibreHardwareProvider(remoteIp));
                    Logger.Info("Registered LibreHardware provider");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to register LibreHardware provider");
                }
            }

            if (PluginDatabase.PluginSettings.Settings.UseHWiNFOSharedMemory)
            {
                try
                {
                    var hwinfoConfig = new HWiNFOConfiguration
                    {
                        FPS_SensorsID = PluginDatabase.PluginSettings.Settings.HWiNFO_fps_sensorsID,
                        FPS_ElementID = PluginDatabase.PluginSettings.Settings.HWiNFO_fps_elementID,
                        GPU_SensorsID = PluginDatabase.PluginSettings.Settings.HWiNFO_gpu_sensorsID,
                        GPU_ElementID = PluginDatabase.PluginSettings.Settings.HWiNFO_gpu_elementID,
                        GPUT_SensorsID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_gpuT_sensorsID,
                        GPUT_ElementID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_gpuT_elementID,
                        CPUT_SensorsID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_cpuT_sensorsID,
                        CPUT_ElementID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_cpuT_elementID,
                        GPUP_SensorsID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_gpuP_sensorsID,
                        GPUP_ElementID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_gpuP_elementID,
                        CPUP_SensorsID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_cpuP_sensorsID,
                        CPUP_ElementID = PluginDatabase
                            .PluginSettings
                            .Settings
                            .HWiNFO_cpuP_elementID,
                    };

                    _hardwareMonitor.RegisterProvider(new HWiNFOProvider(hwinfoConfig));
                    Logger.Info("Registered HWiNFO provider");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to register HWiNFO provider");
                }
            }

            if (PluginDatabase.PluginSettings.Settings.UseWMI)
            {
                try
                {
                    _hardwareMonitor.RegisterProvider(new WMIProvider());
                    Logger.Info("Registered WMI provider");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to register WMI provider");
                }
            }

            if (PluginDatabase.PluginSettings.Settings.UsePerformanceCounter)
            {
                try
                {
                    _hardwareMonitor.RegisterProvider(new PerformanceCounterProvider());
                    Logger.Info("Registered PerformanceCounter provider");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to register PerformanceCounter provider");
                }
            }
        }

        #region RunningActivity management

        public void AddRunningActivity(RunningActivity runningActivity)
        {
            _runningActivities.Add(runningActivity);
        }

        public RunningActivity GetRunningActivity(Guid id)
        {
            return _runningActivities.Find(x => x.Id == id);
        }

        public void RemoveRunningActivity(RunningActivity runningActivity)
        {
            _runningActivities.Remove(runningActivity);
        }

        #endregion

        #region Timer functions

        /// <summary>
        /// Start the monitoring timer
        /// </summary>
        public void DataLogging_start(Guid id)
        {
            Logger.Info($"DataLogging_start - {API.Instance.Database.Games.Get(id)?.Name} - {id}");
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            runningActivity.Timer = new System.Timers.Timer(
                PluginDatabase.PluginSettings.Settings.TimeIntervalLogging * 60000
            )
            {
                AutoReset = true,
            };
            runningActivity.Timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, id);
            runningActivity.Timer.Start();
        }

        /// <summary>
        /// Stop the monitoring timer
        /// </summary>
        public void DataLogging_stop(Guid id)
        {
            Logger.Info($"DataLogging_stop - {API.Instance.Database.Games.Get(id)?.Name} - {id}");
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);

            if (
                runningActivity.WarningsMessage.Count != 0
                && API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop
            )
            {
                try
                {
                    API.Instance.MainView.UIDispatcher.BeginInvoke(
                        (Action)
                            delegate
                            {
                                WarningsDialogs ViewExtension = new WarningsDialogs(
                                    runningActivity.WarningsMessage
                                );
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
                                    ResourceProvider.GetString("LOCGameActivityWarningCaption"),
                                    ViewExtension
                                );
                                windowExtension.ShowDialog();
                            }
                    );
                }
                catch (Exception ex)
                {
                    Common.LogError(
                        ex,
                        false,
                        $"Error on show WarningsMessage - {API.Instance.Database.Games.Get(id)?.Name} - {id}",
                        true,
                        PluginDatabase.PluginName
                    );
                }
            }

            runningActivity.Timer.AutoReset = false;
            runningActivity.Timer.Stop();

            if (_healthMonitor != null)
            {
                var recommendations = _healthMonitor.GetRecommendations();
                if (recommendations.Count > 0)
                {
                    Logger.Info("Monitoring recommendations:");
                    foreach (var rec in recommendations)
                    {
                        Logger.Info($"  - {rec}");
                    }
                }
            }
        }

        /// <summary>
        /// Event executed with the timer
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs e, Guid id)
        {
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            if (runningActivity == null)
            {
                Logger.Warn($"No runningActivity found for {id}");
                return;
            }

            try
            {
                HardwareMetrics metrics = _hardwareMonitor?.GetMetrics() ?? new HardwareMetrics();

                var validationWarnings = MetricsValidator.Validate(metrics);
                if (validationWarnings.Count > 0)
                {
                    foreach (var warning in validationWarnings)
                    {
                        Logger.Warn($"Metric validation: {warning}");
                    }
                    metrics = MetricsValidator.Sanitize(metrics);
                }

                _healthMonitor?.RecordCycle(metrics);

                Common.LogDebug(
                    true,
                    $"Metrics sources - "
                        + $"FPS:{metrics.Source.FPS} CPU:{metrics.Source.CpuUsage} "
                        + $"GPU:{metrics.Source.GpuUsage} RAM:{metrics.Source.RamUsage}"
                );

                CheckAndRecordWarnings(runningActivity, metrics);

                ActivityDetailsData activityDetailsData = new ActivityDetailsData
                {
                    Datelog = DateTime.UtcNow,
                    FPS = metrics.FPS ?? 0,
                    CPU = metrics.CpuUsage ?? 0,
                    CPUT = metrics.CpuTemperature ?? 0,
                    CPUP = metrics.CpuPower ?? 0,
                    GPU = metrics.GpuUsage ?? 0,
                    GPUT = metrics.GpuTemperature ?? 0,
                    GPUP = metrics.GpuPower ?? 0,
                    RAM = metrics.RamUsage ?? 0,
                };

                List<ActivityDetailsData> activitiesDetailsData =
                    runningActivity.GameActivitiesLog.ItemsDetails.Get(
                        runningActivity.ActivityBackup.DateSession
                    );
                activitiesDetailsData.Add(activityDetailsData);

                Common.LogDebug(
                    true,
                    $"Logged metrics: {Serialization.ToJson(activityDetailsData)}"
                );
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during monitoring tick");
            }
        }

        /// <summary>
        /// Check metrics against warning thresholds
        /// </summary>
        private void CheckAndRecordWarnings(
            RunningActivity runningActivity,
            HardwareMetrics metrics
        )
        {
            if (!PluginDatabase.PluginSettings.Settings.EnableWarning)
            {
                return;
            }

            var settings = PluginDatabase.PluginSettings.Settings;

            bool warningMinFps =
                settings.MinFps != 0 && metrics.FPS.HasValue && settings.MinFps >= metrics.FPS;
            bool warningMaxCpuTemp =
                settings.MaxCpuTemp != 0
                && metrics.CpuTemperature.HasValue
                && settings.MaxCpuTemp <= metrics.CpuTemperature;
            bool warningMaxGpuTemp =
                settings.MaxGpuTemp != 0
                && metrics.GpuTemperature.HasValue
                && settings.MaxGpuTemp <= metrics.GpuTemperature;
            bool warningMaxCpuUsage =
                settings.MaxCpuUsage != 0
                && metrics.CpuUsage.HasValue
                && settings.MaxCpuUsage <= metrics.CpuUsage;
            bool warningMaxGpuUsage =
                settings.MaxGpuUsage != 0
                && metrics.GpuUsage.HasValue
                && settings.MaxGpuUsage <= metrics.GpuUsage;
            bool warningMaxRamUsage =
                settings.MaxRamUsage != 0
                && metrics.RamUsage.HasValue
                && settings.MaxRamUsage <= metrics.RamUsage;

            if (
                warningMinFps
                || warningMaxCpuTemp
                || warningMaxGpuTemp
                || warningMaxCpuUsage
                || warningMaxGpuUsage
                || warningMaxRamUsage
            )
            {
                WarningData message = new WarningData
                {
                    At =
                        ResourceProvider.GetString("LOCGameActivityWarningAt")
                        + " "
                        + DateTime.Now.ToString("HH:mm"),
                    FpsData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityFps"),
                        Value = metrics.FPS ?? 0,
                        IsWarm = warningMinFps,
                    },
                    CpuTempData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityCpuTemp"),
                        Value = metrics.CpuTemperature ?? 0,
                        IsWarm = warningMaxCpuTemp,
                    },
                    GpuTempData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityGpuTemp"),
                        Value = metrics.GpuTemperature ?? 0,
                        IsWarm = warningMaxGpuTemp,
                    },
                    CpuUsageData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityCpuUsage"),
                        Value = metrics.CpuUsage ?? 0,
                        IsWarm = warningMaxCpuUsage,
                    },
                    GpuUsageData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityGpuUsage"),
                        Value = metrics.GpuUsage ?? 0,
                        IsWarm = warningMaxGpuUsage,
                    },
                    RamUsageData = new Data
                    {
                        Name = ResourceProvider.GetString("LOCGameActivityRamUsage"),
                        Value = metrics.RamUsage ?? 0,
                        IsWarm = warningMaxRamUsage,
                    },
                };

                runningActivity.WarningsMessage.Add(message);
            }
        }

        #endregion

        #region Backup functions

        public void DataBackup_start(Guid id)
        {
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            if (runningActivity == null)
            {
                Logger.Warn(
                    $"No runningActivity find for {API.Instance.Database.Games.Get(id)?.Name} - {id}"
                );
                return;
            }

            runningActivity.TimerBackup = new System.Timers.Timer(
                PluginDatabase.PluginSettings.Settings.TimeIntervalLogging * 60000 + 10000
            );
            runningActivity.TimerBackup.AutoReset = true;
            runningActivity.TimerBackup.Elapsed += (sender, e) => OnTimedBackupEvent(sender, e, id);
            runningActivity.TimerBackup.Start();
        }

        public void DataBackup_stop(Guid id)
        {
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            if (runningActivity == null)
            {
                Logger.Warn($"No runningActivity find for {id}");
                return;
            }

            runningActivity.TimerBackup.AutoReset = false;
            runningActivity.TimerBackup.Stop();
        }

        private void OnTimedBackupEvent(object source, ElapsedEventArgs e, Guid id)
        {
            try
            {
                RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);

                ulong elapsedSeconds = (ulong)
                    (DateTime.UtcNow - runningActivity.ActivityBackup.DateSession).TotalSeconds;
                runningActivity.ActivityBackup.ElapsedSeconds = elapsedSeconds;
                runningActivity.ActivityBackup.ItemsDetailsDatas =
                    runningActivity.GameActivitiesLog.ItemsDetails.Get(
                        runningActivity.ActivityBackup.DateSession
                    );

                string pathFileBackup = Path.Combine(
                    PluginDatabase.Paths.PluginUserDataPath,
                    $"SaveSession_{id}.json"
                );
                FileSystem.WriteStringToFileSafe(
                    pathFileBackup,
                    Serialization.ToJson(runningActivity.ActivityBackup)
                );
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        public void CheckBackup()
        {
            try
            {
                Task.Run(() =>
                {
                    Parallel.ForEach(
                        Directory.EnumerateFiles(
                            PluginDatabase.Paths.PluginUserDataPath,
                            "SaveSession_*.json"
                        ),
                        (objectFile) =>
                        {
                            SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                            Serialization.TryFromJsonFile(
                                objectFile,
                                out ActivityBackup backupData
                            );
                            if (backupData != null)
                            {
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
                                else
                                {
                                    API.Instance.MainView.UIDispatcher.BeginInvoke(
                                        (Action)
                                            delegate
                                            {
                                                API.Instance.Notifications.Add(
                                                    new NotificationMessage(
                                                        $"{PluginDatabase.PluginName}-backup-{Path.GetFileNameWithoutExtension(objectFile)}",
                                                        PluginDatabase.PluginName
                                                            + Environment.NewLine
                                                            + string.Format(
                                                                ResourceProvider.GetString(
                                                                    "LOCGaBackupExist"
                                                                ),
                                                                backupData.Name
                                                            ),
                                                        NotificationType.Info,
                                                        () =>
                                                        {
                                                            try
                                                            {
                                                                WindowOptions windowOptions =
                                                                    new WindowOptions
                                                                    {
                                                                        ShowMinimizeButton = false,
                                                                        ShowMaximizeButton = false,
                                                                        ShowCloseButton = true,
                                                                        CanBeResizable = true,
                                                                        Height = 350,
                                                                        Width = 800,
                                                                    };

                                                                GameActivityBackup ViewExtension =
                                                                    new GameActivityBackup(
                                                                        backupData
                                                                    );
                                                                Window windowExtension =
                                                                    PlayniteUiHelper.CreateExtensionWindow(
                                                                        ResourceProvider.GetString(
                                                                            "LOCGaBackupDataInfo"
                                                                        ),
                                                                        ViewExtension,
                                                                        windowOptions
                                                                    );
                                                                windowExtension.ShowDialog();
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Common.LogError(
                                                                    ex,
                                                                    false,
                                                                    true,
                                                                    PluginDatabase.PluginName
                                                                );
                                                            }
                                                        }
                                                    )
                                                );
                                            }
                                    );
                                }
                            }
                        }
                    );
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        #endregion

        #region External Dependencies Validation

        /// <summary>
        /// Validate that required external applications are running
        /// </summary>
        private void ValidateExternalDependencies()
        {
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                return;
            }

            var providerStatus = _hardwareMonitor.GetProviderStatus();
            var missingDependencies = new List<string>();

            foreach (var status in providerStatus)
            {
                if (!status.Value.IsAvailable && status.Value.Capabilities.RequiresExternalApp)
                {
                    string dependency = GetMissingDependencyMessage(status.Key);
                    if (!string.IsNullOrEmpty(dependency))
                    {
                        missingDependencies.Add(dependency);
                    }
                }
            }

            if (missingDependencies.Count > 0)
            {
                Logger.Warn(
                    $"Missing external dependencies: {string.Join(", ", missingDependencies)}"
                );
            }
        }

        /// <summary>
        /// Check if monitoring can proceed and optionally notify user
        /// </summary>
        public bool CheckMonitoringReadiness(bool withNotification = false)
        {
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                return false;
            }

            if (_hardwareMonitor == null)
            {
                Logger.Error("Hardware monitor not initialized");
                return false;
            }

            var providerStatus = _hardwareMonitor.GetProviderStatus();
            bool hasAvailableProvider = providerStatus.Any(p =>
                p.Value.IsAvailable && !p.Value.IsInFallback
            );

            if (!hasAvailableProvider)
            {
                if (withNotification)
                {
                    NotifyMissingDependencies(providerStatus);
                }

                Logger.Error("No available monitoring providers");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Notify user about missing dependencies
        /// </summary>
        private void NotifyMissingDependencies(Dictionary<string, ProviderStatus> providerStatus)
        {
            var missingDeps = new List<string>();

            foreach (var status in providerStatus)
            {
                if (!status.Value.IsAvailable && status.Value.Capabilities.RequiresExternalApp)
                {
                    string message = GetMissingDependencyMessage(status.Key);
                    if (!string.IsNullOrEmpty(message))
                    {
                        missingDeps.Add(message);
                    }
                }
            }

            if (missingDeps.Count == 0)
            {
                return;
            }

            string notificationMessage = string.Join(Environment.NewLine, missingDeps);

            API.Instance.Notifications.Add(
                new NotificationMessage(
                    $"{PluginDatabase.PluginName}-monitoring-dependencies",
                    $"{PluginDatabase.PluginName}{Environment.NewLine}{notificationMessage}",
                    NotificationType.Error,
                    () => Plugin.OpenSettingsView()
                )
            );
        }

        /// <summary>
        /// Get user-friendly message for missing dependency
        /// </summary>
        private string GetMissingDependencyMessage(string providerName)
        {
            switch (providerName)
            {
                case "HWiNFO":
                    if (IsProcessRunning("HWiNFO32") || IsProcessRunning("HWiNFO64"))
                    {
                        return null;
                    }
                    return ResourceProvider.GetString("LOCGameActivityNotificationHWiNFO")
                        ?? "HWiNFO is not running";

                case "RivaTuner":
                    if (IsProcessRunning("RTSS"))
                    {
                        return null;
                    }
                    return ResourceProvider.GetString("LOCGameActivityNotificationMSIAfterBurner")
                        ?? "RivaTuner Statistics Server (RTSS) is not running";

                case "LibreHardware":
                    return "LibreHardware Monitor is not accessible";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Check if a process is running
        /// </summary>
        private bool IsProcessRunning(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to check process: {processName}");
                return false;
            }
        }

        #endregion

        #region Additional utility methods

        /// <summary>
        /// Get current monitoring status for UI display
        /// </summary>
        public string GetMonitoringStatus()
        {
            if (_hardwareMonitor == null)
            {
                return "Monitoring not initialized";
            }

            var status = _hardwareMonitor.GetProviderStatus();
            int available = status.Count(p => p.Value.IsAvailable);
            int fallback = status.Count(p => p.Value.IsInFallback);

            return $"{available} providers active, {fallback} in fallback";
        }

        /// <summary>
        /// Manually trigger diagnostics (useful for settings/debug UI)
        /// </summary>
        public string RunDiagnostics()
        {
            if (_diagnostics == null)
            {
                return "Diagnostics not available";
            }

            return _diagnostics.GenerateTextReport();
        }

        /// <summary>
        /// Reset a specific provider
        /// </summary>
        public void ResetProvider(string providerName)
        {
            _hardwareMonitor?.ResetProvider(providerName);
            Logger.Info($"Reset provider: {providerName}");
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            Logger.Info("Disposing monitoring system");

            _hardwareMonitor?.Dispose();
            _hardwareMonitor = null;
            _healthMonitor = null;
            _diagnostics = null;
        }

        #endregion

        #region Tests

        /// <summary>
        /// Test method to retrieve current hardware metrics with detailed diagnostics
        /// </summary>
        public HardwareMetrics GetCurrentMetrics()
        {
            if (_hardwareMonitor == null)
            {
                Logger.Warn("Hardware monitor not initialized");
                return new HardwareMetrics();
            }

            try
            {
                Logger.Info("=== GetCurrentMetrics - Starting diagnostic ===");

                var providerStatus = _hardwareMonitor.GetProviderStatus();
                Logger.Info($"Total providers registered: {providerStatus.Count}");

                foreach (var status in providerStatus)
                {
                    Logger.Info(
                        $"Provider: {status.Key} | Available: {status.Value.IsAvailable} | "
                            + $"Fallback: {status.Value.IsInFallback} | "
                            + $"Failures: {status.Value.FailureCount}"
                    );

                    var caps = status.Value.Capabilities;
                    Logger.Info(
                        $"  Capabilities - FPS: {caps.Supports(MetricType.FPS)}, CPU: {caps.Supports(MetricType.CpuUsage)}, "
                            + $"GPU: {caps.Supports(MetricType.GpuUsage)}, RAM: {caps.Supports(MetricType.RamUsage)}, "
                            + $"CPU Temps: {caps.Supports(MetricType.CpuTemperature)}, GPU Temps: {caps.Supports(MetricType.GpuTemperature)}, "
                            + $"CPU Power: {caps.Supports(MetricType.CpuPower)}, GPU Power: {caps.Supports(MetricType.GpuPower)}"
                    );
                }

                Logger.Info("=== Testing Individual Providers ===");
                foreach (
                    var provider in _hardwareMonitor
                        .GetType()
                        .GetField(
                            "_providers",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        )
                        ?.GetValue(_hardwareMonitor) as List<IHardwareDataProvider>
                        ?? new List<IHardwareDataProvider>()
                )
                {
                    if (provider.IsAvailable)
                    {
                        try
                        {
                            Logger.Info($"Testing provider: {provider.ProviderName}");
                            var testMetrics = provider.GetMetrics();
                            if (testMetrics != null)
                            {
                                Logger.Info(
                                    $"  {provider.ProviderName} returned - FPS:{testMetrics.FPS?.ToString() ?? "NULL"}, "
                                        + $"CPU:{testMetrics.CpuUsage?.ToString() ?? "NULL"}%, "
                                        + $"GPU:{testMetrics.GpuUsage?.ToString() ?? "NULL"}%, "
                                        + $"RAM:{testMetrics.RamUsage?.ToString() ?? "NULL"}%, "
                                        + $"CPUT:{testMetrics.CpuTemperature?.ToString() ?? "NULL"}°C, "
                                        + $"GPUT:{testMetrics.GpuTemperature?.ToString() ?? "NULL"}°C"
                                );
                            }
                            else
                            {
                                Logger.Warn($"  {provider.ProviderName} returned NULL metrics");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"  {provider.ProviderName} threw exception");
                        }
                    }
                }

                HardwareMetrics metrics = _hardwareMonitor.GetMetrics(true) ?? new HardwareMetrics();

                Logger.Info("=== Metrics Sources ===");
                Logger.Info($"FPS Source: {metrics.Source.FPS ?? "NULL"}");
                Logger.Info($"CPU Usage Source: {metrics.Source.CpuUsage ?? "NULL"}");
                Logger.Info($"GPU Usage Source: {metrics.Source.GpuUsage ?? "NULL"}");
                Logger.Info($"RAM Usage Source: {metrics.Source.RamUsage ?? "NULL"}");
                Logger.Info($"CPU Temp Source: {metrics.Source.CpuTemperature ?? "NULL"}");
                Logger.Info($"GPU Temp Source: {metrics.Source.GpuTemperature ?? "NULL"}");
                Logger.Info($"CPU Power Source: {metrics.Source.CpuPower ?? "NULL"}");
                Logger.Info($"GPU Power Source: {metrics.Source.GpuPower ?? "NULL"}");

                Logger.Info("=== Metrics Values ===");
                Logger.Info($"FPS: {metrics.FPS?.ToString() ?? "NULL"}");
                Logger.Info($"CPU Usage: {metrics.CpuUsage?.ToString() ?? "NULL"}%");
                Logger.Info($"GPU Usage: {metrics.GpuUsage?.ToString() ?? "NULL"}%");
                Logger.Info($"RAM Usage: {metrics.RamUsage?.ToString() ?? "NULL"}%");
                Logger.Info($"CPU Temp: {metrics.CpuTemperature?.ToString() ?? "NULL"}°C");
                Logger.Info($"GPU Temp: {metrics.GpuTemperature?.ToString() ?? "NULL"}°C");
                Logger.Info($"CPU Power: {metrics.CpuPower?.ToString() ?? "NULL"}W");
                Logger.Info($"GPU Power: {metrics.GpuPower?.ToString() ?? "NULL"}W");

                var validationWarnings = MetricsValidator.Validate(metrics);
                if (validationWarnings.Count > 0)
                {
                    Logger.Warn("=== Validation Warnings ===");
                    foreach (var warning in validationWarnings)
                    {
                        Logger.Warn($"  - {warning}");
                    }
                    metrics = MetricsValidator.Sanitize(metrics);
                }

                _healthMonitor?.RecordCycle(metrics);

                Logger.Info("=== GetCurrentMetrics - Diagnostic complete ===");

                return metrics;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error retrieving metrics");
                return new HardwareMetrics();
            }
        }

        #endregion
    }
}
