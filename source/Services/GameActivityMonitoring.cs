using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Manages the lifecycle of hardware monitoring during game sessions.
    /// Handles provider registration, per-game timers, metric logging,
    /// warning detection, session backup, and user notifications.
    /// </summary>
    public class GameActivityMonitoring
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        internal GenericPlugin Plugin { get; set; }

        private HardwareDataAggregator _hardwareMonitor;
        private ProviderHealthMonitor _healthMonitor;
        private MonitoringDiagnostics _diagnostics;

        private readonly List<RunningActivity> _runningActivities = new List<RunningActivity>();

        public GameActivityMonitoring(GenericPlugin plugin)
        {
            Plugin = plugin;
        }

        #region Initialization

        /// <summary>
        /// Initializes the hardware monitoring system asynchronously to avoid blocking the UI thread.
        /// Registers configured providers, runs diagnostics, validates external dependencies,
        /// and notifies the user of any provider that failed to initialize.
        /// </summary>
        public void InitializeMonitoring()
        {
            Logger.Info("Initializing hardware monitoring system...");

            Task.Run(() =>
            {
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

                        // LogDiagnostics can be slow; already in background thread
                        _diagnostics.LogDiagnostics();

                        ValidateExternalDependencies();
                    }
                    else
                    {
                        Logger.Warn("No hardware monitoring providers available");
                    }


                    NotifyFailedProviders();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to initialize hardware monitoring");
                }
            });
        }

        /// <summary>
        /// Registers all hardware providers enabled in plugin settings.
        /// Each provider is registered independently so a failure on one does not block others.
        /// </summary>
        private void RegisterProviders()
        {
            if (PluginDatabase.PluginSettings.UseRivaTuner)
            {
                TryRegisterProvider(() => new RivaTunerProvider(), "RivaTuner");
            }

            if (PluginDatabase.PluginSettings.UseLibreHardware)
            {
                TryRegisterProvider(() =>
                {
                    string remoteIp = null;
                    if (PluginDatabase.PluginSettings.WithRemoteServerWeb
                        && !string.IsNullOrEmpty(PluginDatabase.PluginSettings.IpRemoteServerWeb))
                    {
                        remoteIp = PluginDatabase.PluginSettings.IpRemoteServerWeb;
                        Logger.Info($"Using LibreHardware remote server: {remoteIp}");
                    }
                    return new LibreHardwareProvider(remoteIp);
                }, "LibreHardware");
            }

            if (PluginDatabase.PluginSettings.UseMsiAfterburner)
            {
                TryRegisterProvider(() =>
                {
					return new MsiAfterburnerProvider();
				}, "MsiAfterburner");
            }

            if (PluginDatabase.PluginSettings.UseHWiNFOSharedMemory)
            {
                TryRegisterProvider(() =>
                {
                    var hwinfoConfig = new HWiNFOConfiguration
                    {
                        FPS_SensorsID = PluginDatabase.PluginSettings.HWiNFO_fps_sensorsID,
                        FPS_ElementID = PluginDatabase.PluginSettings.HWiNFO_fps_elementID,
                        GPU_SensorsID = PluginDatabase.PluginSettings.HWiNFO_gpu_sensorsID,
                        GPU_ElementID = PluginDatabase.PluginSettings.HWiNFO_gpu_elementID,
                        GPUT_SensorsID = PluginDatabase.PluginSettings.HWiNFO_gpuT_sensorsID,
                        GPUT_ElementID = PluginDatabase.PluginSettings.HWiNFO_gpuT_elementID,
                        CPUT_SensorsID = PluginDatabase.PluginSettings.HWiNFO_cpuT_sensorsID,
                        CPUT_ElementID = PluginDatabase.PluginSettings.HWiNFO_cpuT_elementID,
                        GPUP_SensorsID = PluginDatabase.PluginSettings.HWiNFO_gpuP_sensorsID,
                        GPUP_ElementID = PluginDatabase.PluginSettings.HWiNFO_gpuP_elementID,
                        CPUP_SensorsID = PluginDatabase.PluginSettings.HWiNFO_cpuP_sensorsID,
                        CPUP_ElementID = PluginDatabase.PluginSettings.HWiNFO_cpuP_elementID,
                    };
                    return new HWiNFOProvider(hwinfoConfig);
                }, "HWiNFO");
            }

            if (PluginDatabase.PluginSettings.UseWMI)
            {
                TryRegisterProvider(() => new WMIProvider(), "WMI");
            }

            if (PluginDatabase.PluginSettings.UsePerformanceCounter)
            {
                TryRegisterProvider(() => new PerformanceCounterProvider(), "PerformanceCounter");
            }
        }

        /// <summary>
        /// Safely instantiates and registers a provider via a factory delegate.
        /// Logs a warning if the factory or registration throws.
        /// </summary>
        /// <param name="factory">Factory that creates the provider instance.</param>
        /// <param name="providerLabel">Human-readable name used in log messages.</param>
        private void TryRegisterProvider(Func<IHardwareDataProvider> factory, string providerLabel)
        {
            try
            {
                _hardwareMonitor.RegisterProvider(factory());
                Logger.Info($"Registered {providerLabel} provider");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to register {providerLabel} provider");
            }
        }

        /// <summary>
        /// Sends a Playnite error notification for each provider that is unavailable after initialization.
        /// Clicking the notification opens the plugin settings view.
        /// Must be called after <see cref="HardwareDataAggregator.Initialize"/>.
        /// </summary>
        private void NotifyFailedProviders()
        {
            if (_hardwareMonitor == null)
            {
                return;
            }

            var providerStatus = _hardwareMonitor.GetProviderStatus();

            foreach (var kvp in providerStatus)
            {
                string providerName = kvp.Key;
                ProviderStatus status = kvp.Value;

                if (status.IsAvailable)
                {
                    continue;
                }

                string baseMessage = string.Format(
                    ResourceProvider.GetString("LOCGameActivityProviderError") ?? "{0} provider failed to initialize.",
                    providerName
                );

                // Append root cause if available to help the user diagnose the issue
                string fullMessage = string.IsNullOrEmpty(status.LastErrorMessage)
                    ? baseMessage
                    : $"{baseMessage} {status.LastErrorMessage}";

                Logger.Warn($"Provider unavailable: {providerName} — {status.LastErrorMessage}");

                // Dispatch to UI thread; notifications API must be called from the main thread
                API.Instance.MainView.UIDispatcher.BeginInvoke((Action)delegate
                {
                    API.Instance.Notifications.Add(
                        new NotificationMessage(
                            $"{PluginDatabase.PluginName}-provider-error-{providerName}",
                            $"{PluginDatabase.PluginName}{Environment.NewLine}{fullMessage}",
                            NotificationType.Error,
                            () => Plugin.OpenSettingsView()
                        )
                    );
                });
            }
        }

        #endregion

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
        /// Starts the metric logging timer for a game session.
        /// The interval is driven by <see cref="PluginSettings.TimeIntervalLogging"/> (in minutes).
        /// </summary>
        public void DataLogging_start(Guid id)
        {
            Logger.Info($"DataLogging_start - {API.Instance.Database.Games.Get(id)?.Name} - {id}");

            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            runningActivity.Timer = new Timer(PluginDatabase.PluginSettings.TimeIntervalLogging * 60000)
            {
                AutoReset = true,
            };
            runningActivity.Timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, id);
            runningActivity.Timer.Start();
        }

        /// <summary>
        /// Stops the metric logging timer and shows any accumulated warnings to the user.
        /// Also logs provider health recommendations collected during the session.
        /// </summary>
        public void DataLogging_stop(Guid id)
        {
            Logger.Info($"DataLogging_stop - {API.Instance.Database.Games.Get(id)?.Name} - {id}");

            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);

            if (runningActivity.WarningsMessage.Count != 0
                && API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                try
                {
                    API.Instance.MainView.UIDispatcher.BeginInvoke((Action)delegate
                    {
                        WarningsDialogs viewExtension = new WarningsDialogs(runningActivity.WarningsMessage);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
                            ResourceProvider.GetString("LOCGameActivityWarningCaption"),
                            viewExtension
                        );
                        windowExtension.ShowDialog();
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(
                        ex, false,
                        $"Error on show WarningsMessage - {API.Instance.Database.Games.Get(id)?.Name} - {id}",
                        true, PluginDatabase.PluginName
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
        /// Periodic timer callback: collects hardware metrics, validates them,
        /// checks warning thresholds, and appends a data point to the session log.
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

                Common.LogDebug(true,
                    $"Metrics sources - FPS:{metrics.Source.FPS} CPU:{metrics.Source.CpuUsage} " +
                    $"GPU:{metrics.Source.GpuUsage} RAM:{metrics.Source.RamUsage}");

                CheckAndRecordWarnings(runningActivity, metrics);

                var activityDetailsData = new ActivityDetailsData
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

                Common.LogDebug(true, $"Logged metrics: {Serialization.ToJson(activityDetailsData)}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during monitoring tick");
            }
        }

        /// <summary>
        /// Compares current metrics against user-configured thresholds.
        /// Appends a <see cref="WarningData"/> entry if any threshold is breached.
        /// </summary>
        private void CheckAndRecordWarnings(RunningActivity runningActivity, HardwareMetrics metrics)
        {
            if (!PluginDatabase.PluginSettings.EnableWarning)
            {
                return;
            }

            var settings = PluginDatabase.PluginSettings;

            bool warningMinFps = settings.MinFps != 0 && metrics.FPS.HasValue && settings.MinFps >= metrics.FPS;
            bool warningMaxCpuTemp = settings.MaxCpuTemp != 0 && metrics.CpuTemperature.HasValue && settings.MaxCpuTemp <= metrics.CpuTemperature;
            bool warningMaxGpuTemp = settings.MaxGpuTemp != 0 && metrics.GpuTemperature.HasValue && settings.MaxGpuTemp <= metrics.GpuTemperature;
            bool warningMaxCpuUsage = settings.MaxCpuUsage != 0 && metrics.CpuUsage.HasValue && settings.MaxCpuUsage <= metrics.CpuUsage;
            bool warningMaxGpuUsage = settings.MaxGpuUsage != 0 && metrics.GpuUsage.HasValue && settings.MaxGpuUsage <= metrics.GpuUsage;
            bool warningMaxRamUsage = settings.MaxRamUsage != 0 && metrics.RamUsage.HasValue && settings.MaxRamUsage <= metrics.RamUsage;

            if (!warningMinFps && !warningMaxCpuTemp && !warningMaxGpuTemp
                && !warningMaxCpuUsage && !warningMaxGpuUsage && !warningMaxRamUsage)
            {
                return;
            }

            var message = new WarningData
            {
                At = ResourceProvider.GetString("LOCGameActivityWarningAt") + " " + DateTime.Now.ToString("HH:mm"),
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

        #endregion

        #region Backup functions

        /// <summary>
        /// Starts the session backup timer. The backup fires slightly after the logging timer
        /// to ensure fresh data is captured (logging interval + 10 s).
        /// </summary>
        public void DataBackup_start(Guid id)
        {
            RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);
            if (runningActivity == null)
            {
                Logger.Warn($"No runningActivity find for {API.Instance.Database.Games.Get(id)?.Name} - {id}");
                return;
            }

            runningActivity.TimerBackup = new Timer(
                PluginDatabase.PluginSettings.TimeIntervalLogging * 60000 + 10000
            )
            {
                AutoReset = true,
            };
            runningActivity.TimerBackup.Elapsed += (sender, e) => OnTimedBackupEvent(sender, e, id);
            runningActivity.TimerBackup.Start();
        }

        /// <summary>
        /// Stops the session backup timer.
        /// </summary>
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

        /// <summary>
        /// Periodic backup callback: writes current session state to a JSON file
        /// so it can be recovered if the application crashes.
        /// </summary>
        private void OnTimedBackupEvent(object source, ElapsedEventArgs e, Guid id)
        {
            try
            {
                RunningActivity runningActivity = _runningActivities.Find(x => x.Id == id);

                ulong elapsedSeconds = (ulong)(DateTime.UtcNow - runningActivity.ActivityBackup.DateSession).TotalSeconds;
                runningActivity.ActivityBackup.ElapsedSeconds = elapsedSeconds;
                runningActivity.ActivityBackup.ItemsDetailsDatas =
                    runningActivity.GameActivitiesLog.ItemsDetails.Get(
                        runningActivity.ActivityBackup.DateSession
                    );

                string pathFileBackup = Path.Combine(
                    PluginDatabase.Paths.PluginUserDataPath,
                    $"SaveSession_{id}.json"
                );
                FileSystem.WriteStringToFileSafe(pathFileBackup, Serialization.ToJson(runningActivity.ActivityBackup));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Scans for orphaned backup files (from crashed sessions) and notifies the user
        /// via Playnite notifications so they can review or discard the data.
        /// Runs asynchronously to avoid blocking startup.
        /// </summary>
        public void CheckBackup()
        {
            try
            {
                Task.Run(() =>
                {
                    System.Threading.Tasks.Parallel.ForEach(
                        Directory.EnumerateFiles(PluginDatabase.Paths.PluginUserDataPath, "SaveSession_*.json"),
                        (objectFile) =>
                        {
                            System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                            Serialization.TryFromJsonFile(objectFile, out ActivityBackup backupData);
                            if (backupData == null)
                            {
                                return;
                            }

                            Game game = API.Instance.Database.Games.Get(backupData.Id);
                            if (game == null)
                            {
                                // Game no longer exists; discard the orphaned backup
                                try { FileSystem.DeleteFileSafe(objectFile); }
                                catch (Exception ex) { Common.LogError(ex, false, true, PluginDatabase.PluginName); }
                                return;
                            }

                            API.Instance.MainView.UIDispatcher.BeginInvoke((Action)delegate
                            {
                                API.Instance.Notifications.Add(
                                    new NotificationMessage(
                                        $"{PluginDatabase.PluginName}-backup-{Path.GetFileNameWithoutExtension(objectFile)}",
                                        PluginDatabase.PluginName + Environment.NewLine +
                                        string.Format(ResourceProvider.GetString("LOCGaBackupExist"), backupData.Name),
                                        NotificationType.Info,
                                        () =>
                                        {
                                            try
                                            {
                                                WindowOptions windowOptions = new WindowOptions
                                                {
                                                    ShowMinimizeButton = false,
                                                    ShowMaximizeButton = false,
                                                    ShowCloseButton = true,
                                                    CanBeResizable = false,
                                                    Height = 380,
                                                    Width = 800,
                                                };

                                                GameActivityBackup viewExtension = new GameActivityBackup(backupData);
                                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
                                                    ResourceProvider.GetString("LOCGaBackupDataInfo"),
                                                    viewExtension,
                                                    windowOptions
                                                );
                                                windowExtension.ShowDialog();
                                            }
                                            catch (Exception ex)
                                            {
                                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                                            }
                                        }
                                    )
                                );
                            });
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

        #region External dependencies validation

        /// <summary>
        /// Checks whether required external applications (e.g. RTSS, HWiNFO) are running
        /// for providers that depend on them, and logs warnings for any that are missing.
        /// Only runs when logging is enabled.
        /// </summary>
        private void ValidateExternalDependencies()
        {
            if (!PluginDatabase.PluginSettings.EnableLogging)
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
                Logger.Warn($"Missing external dependencies: {string.Join(", ", missingDependencies)}");
            }
        }

        /// <summary>
        /// Checks whether at least one monitoring provider is operational.
        /// Optionally notifies the user if none are available.
        /// </summary>
        /// <param name="withNotification">If true, sends a Playnite notification when no provider is available.</param>
        /// <returns>True if monitoring can proceed; false otherwise.</returns>
        public bool CheckMonitoringReadiness(bool withNotification = false)
        {
            if (!PluginDatabase.PluginSettings.EnableLogging)
            {
                return false;
            }

            if (_hardwareMonitor == null)
            {
                Logger.Error("Hardware monitor not initialized");
                return false;
            }

            var providerStatus = _hardwareMonitor.GetProviderStatus();
            bool hasAvailableProvider = providerStatus.Any(p => p.Value.IsAvailable && !p.Value.IsInFallback);

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
        /// Sends a single aggregated Playnite error notification listing all providers
        /// that require an external application which is not currently running.
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

            API.Instance.Notifications.Add(
                new NotificationMessage(
                    $"{PluginDatabase.PluginName}-monitoring-dependencies",
                    $"{PluginDatabase.PluginName}{Environment.NewLine}{string.Join(Environment.NewLine, missingDeps)}",
                    NotificationType.Error,
                    () => Plugin.OpenSettingsView()
                )
            );
        }

        /// <summary>
        /// Returns a localised user-facing message when a provider's required external process
        /// is not detected. Returns null if the process is running or no message applies.
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
                           ?? "HWiNFO is not running.";

                case "MsiAfterburner":
                    if (IsProcessRunning("MSIAfterburner"))
                    {
                        return null;
                    }
                    return ResourceProvider.GetString("LOCGameActivityNotificationMSIAfterBurner")
                           ?? "MSI AfterBurner is not running.";

                case "RivaTuner":
                    if (IsProcessRunning("RTSS"))
                    {
                        return null;
                    }
                    return ResourceProvider.GetString("LOCGameActivityNotificationRivaTuner")
                           ?? "RivaTuner Statistics Server (RTSS) is not running.";

                case "LibreHardware":
                    return ResourceProvider.GetString("LOCGameActivityNotificationLibreHardware")
                           ?? "LibreHardware Monitor remote server is not accessible.";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns true if at least one process with the given name is currently running.
        /// </summary>
        private bool IsProcessRunning(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to check process: {processName}");
                return false;
            }
        }

        #endregion

        #region Diagnostics and utilities

        /// <summary>
        /// Returns a brief human-readable summary of active providers for UI display.
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
        /// Generates a full text diagnostic report. Useful for settings/debug UI.
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
        /// Resets a provider by name, clearing its failure and fallback state.
        /// </summary>
        public void ResetProvider(string providerName)
        {
            _hardwareMonitor?.ResetProvider(providerName);
            Logger.Info($"Reset provider: {providerName}");
        }

        #endregion

        #region Tests

        /// <summary>
        /// Retrieves current hardware metrics with verbose diagnostic logging.
        /// Intended for debugging via the settings UI; not called during normal game sessions.
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
                        $"Provider: {status.Key} | Available: {status.Value.IsAvailable} | " +
                        $"Fallback: {status.Value.IsInFallback} | Failures: {status.Value.FailureCount}"
                    );

                    var caps = status.Value.Capabilities;
                    Logger.Info(
                        $"  Capabilities - FPS: {caps.Supports(MetricType.FPS)}, CPU: {caps.Supports(MetricType.CpuUsage)}, " +
                        $"GPU: {caps.Supports(MetricType.GpuUsage)}, RAM: {caps.Supports(MetricType.RamUsage)}, " +
                        $"CPU Temps: {caps.Supports(MetricType.CpuTemperature)}, GPU Temps: {caps.Supports(MetricType.GpuTemperature)}, " +
                        $"CPU Power: {caps.Supports(MetricType.CpuPower)}, GPU Power: {caps.Supports(MetricType.GpuPower)}"
                    );
                }

                Logger.Info("=== Testing Individual Providers ===");

                // Access internal provider list via reflection for diagnostic purposes only
                var providers = _hardwareMonitor
                    .GetType()
                    .GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_hardwareMonitor) as List<IHardwareDataProvider>
                    ?? new List<IHardwareDataProvider>();

                foreach (var provider in providers)
                {
                    if (!provider.IsAvailable)
                    {
                        continue;
                    }

                    try
                    {
                        Logger.Info($"Testing provider: {provider.ProviderName}");
                        var testMetrics = provider.GetMetrics();
                        if (testMetrics != null)
                        {
                            Logger.Info(
                                $"  {provider.ProviderName} returned - " +
                                $"FPS:{testMetrics.FPS?.ToString() ?? "NULL"}, " +
                                $"CPU:{testMetrics.CpuUsage?.ToString() ?? "NULL"}%, " +
                                $"GPU:{testMetrics.GpuUsage?.ToString() ?? "NULL"}%, " +
                                $"RAM:{testMetrics.RamUsage?.ToString() ?? "NULL"}%, " +
                                $"CPUT:{testMetrics.CpuTemperature?.ToString() ?? "NULL"}°C, " +
                                $"GPUT:{testMetrics.GpuTemperature?.ToString() ?? "NULL"}°C"
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

        #region Cleanup

        /// <summary>
        /// Releases all monitoring resources. Should be called when the plugin is unloaded.
        /// </summary>
        public void Dispose()
        {
            Logger.Info("Disposing monitoring system");

            _hardwareMonitor?.Dispose();
            _hardwareMonitor = null;
            _healthMonitor = null;
            _diagnostics = null;
        }

        #endregion
    }
}