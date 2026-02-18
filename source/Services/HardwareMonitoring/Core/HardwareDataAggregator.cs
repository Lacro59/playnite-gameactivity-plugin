using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameActivity.Services.HardwareMonitoring.Core
{
    /// <summary>
    /// Main orchestrator for hardware monitoring.
    /// Aggregates metrics from multiple providers with priority-based selection,
    /// automatic fallback on repeated failures, and short-lived caching.
    /// </summary>
    public class HardwareDataAggregator : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly MonitoringConfiguration _config;
        private readonly List<IHardwareDataProvider> _providers;

        /// <summary>Consecutive failure count per provider name.</summary>
        private readonly Dictionary<string, int> _providerFailures;

        /// <summary>Tracks whether a provider has been put in fallback (disabled) state.</summary>
        private readonly Dictionary<string, bool> _providerFallbackStatus;

        /// <summary>Last error message per provider, set on initialization failure or runtime exception.</summary>
        private readonly Dictionary<string, string> _providerLastErrors;

        private readonly ReaderWriterLockSlim _cacheLock;

        private HardwareMetrics _cachedMetrics;
        private DateTime _lastCacheUpdate;
        private bool _disposed;

        /// <param name="config">Optional monitoring configuration. Defaults are used if null.</param>
        public HardwareDataAggregator(MonitoringConfiguration config = null)
        {
            _config = config ?? new MonitoringConfiguration();
            _providers = new List<IHardwareDataProvider>();
            _providerFailures = new Dictionary<string, int>();
            _providerFallbackStatus = new Dictionary<string, bool>();
            _providerLastErrors = new Dictionary<string, string>();
            _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        /// Registers a hardware data provider into the aggregator.
        /// Providers must be registered before calling <see cref="Initialize"/>.
        /// </summary>
        /// <param name="provider">The provider to register. Ignored if null.</param>
        public void RegisterProvider(IHardwareDataProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            _providers.Add(provider);
            _providerFailures[provider.ProviderName] = 0;
            _providerFallbackStatus[provider.ProviderName] = false;
            _providerLastErrors[provider.ProviderName] = null;

            Logger.Info($"Registered provider: {provider.ProviderName}");
        }

        /// <summary>
        /// Initializes all registered providers ordered by priority (descending).
        /// Providers that fail initialization are tracked but not removed,
        /// so they can be retried or reported via <see cref="GetProviderStatus"/>.
        /// </summary>
        /// <returns>True if at least one provider initialized successfully.</returns>
        public bool Initialize()
        {
            Logger.Info("Initializing providers...");

            foreach (var provider in _providers.OrderByDescending(p => p.Capabilities.Priority))
            {
                try
                {
                    if (provider.Initialize())
                    {
                        Logger.Info($"Provider {provider.ProviderName} initialized successfully");
                    }
                    else
                    {
                        string msg = $"Provider {provider.ProviderName} returned false on Initialize()";
                        Logger.Warn(msg);
                        _providerLastErrors[provider.ProviderName] = msg;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error initializing provider {provider.ProviderName}");
                    _providerLastErrors[provider.ProviderName] = ex.Message;
                }
            }

            return _providers.Any(p => p.IsAvailable);
        }

        /// <summary>
        /// Returns the latest hardware metrics, using the cache if still valid.
        /// Each metric type is resolved independently from the highest-priority available provider.
        /// </summary>
        /// <param name="isCheck">
        /// If true, logs a warning when a metric cannot be obtained from any provider.
        /// Useful for diagnostics without polluting normal operation logs.
        /// </param>
        public HardwareMetrics GetMetrics(bool isCheck = false)
        {
            _cacheLock.EnterReadLock();
            try
            {
                if (_cachedMetrics != null &&
                    (DateTime.UtcNow - _lastCacheUpdate).TotalMilliseconds < _config.CacheDurationMs)
                {
                    return _cachedMetrics;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            var metrics = new HardwareMetrics();

            foreach (MetricType metricType in Enum.GetValues(typeof(MetricType)))
            {
                if (metricType == MetricType.None || metricType == MetricType.All)
                {
                    continue;
                }

                TryGetMetric(metrics, metricType, isCheck);
            }

            UpdateCache(metrics);
            return metrics;
        }

        /// <summary>
        /// Attempts to populate a single metric in <paramref name="metrics"/> by iterating
        /// providers in configured priority order, skipping unavailable or fallback providers.
        /// </summary>
        private void TryGetMetric(HardwareMetrics metrics, MetricType metricType, bool isCheck = false)
        {
            if (!_config.MetricPriorities.TryGetValue(metricType, out var priorities))
            {
                return;
            }

            foreach (var providerName in priorities)
            {
                if (_providerFallbackStatus.TryGetValue(providerName, out bool isFallback) && isFallback)
                {
                    continue;
                }

                var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName);
                if (provider == null || !provider.IsAvailable || !provider.Capabilities.Supports(metricType))
                {
                    continue;
                }

                try
                {
                    var providerMetrics = provider.GetMetrics();
                    if (SetMetricValue(metrics, metricType, providerMetrics, providerName))
                    {
                        _providerFailures[providerName] = 0;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    HandleProviderError(provider, ex);
                }
            }

            if (isCheck)
            {
                Logger.Warn($"Could not obtain {metricType} from any provider");
            }
        }

        /// <summary>
        /// Copies a single metric value from <paramref name="source"/> into <paramref name="metrics"/>
        /// and records the provider name as the source for that metric.
        /// </summary>
        /// <returns>True if the value was present and copied.</returns>
        private bool SetMetricValue(HardwareMetrics metrics, MetricType type, HardwareMetrics source, string providerName)
        {
            switch (type)
            {
                case MetricType.FPS:
                    if (source.FPS.HasValue) { metrics.FPS = source.FPS; metrics.Source.FPS = providerName; return true; }
                    break;
                case MetricType.CpuUsage:
                    if (source.CpuUsage.HasValue) { metrics.CpuUsage = source.CpuUsage; metrics.Source.CpuUsage = providerName; return true; }
                    break;
                case MetricType.CpuTemperature:
                    if (source.CpuTemperature.HasValue) { metrics.CpuTemperature = source.CpuTemperature; metrics.Source.CpuTemperature = providerName; return true; }
                    break;
                case MetricType.CpuPower:
                    if (source.CpuPower.HasValue) { metrics.CpuPower = source.CpuPower; metrics.Source.CpuPower = providerName; return true; }
                    break;
                case MetricType.GpuUsage:
                    if (source.GpuUsage.HasValue) { metrics.GpuUsage = source.GpuUsage; metrics.Source.GpuUsage = providerName; return true; }
                    break;
                case MetricType.GpuTemperature:
                    if (source.GpuTemperature.HasValue) { metrics.GpuTemperature = source.GpuTemperature; metrics.Source.GpuTemperature = providerName; return true; }
                    break;
                case MetricType.GpuPower:
                    if (source.GpuPower.HasValue) { metrics.GpuPower = source.GpuPower; metrics.Source.GpuPower = providerName; return true; }
                    break;
                case MetricType.RamUsage:
                    if (source.RamUsage.HasValue) { metrics.RamUsage = source.RamUsage; metrics.Source.RamUsage = providerName; return true; }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Increments the failure counter for a provider and disables it (fallback)
        /// if <see cref="MonitoringConfiguration.MaxFailuresBeforeFallback"/> is reached.
        /// Also stores the exception message for status reporting.
        /// </summary>
        private void HandleProviderError(IHardwareDataProvider provider, Exception ex)
        {
            _providerFailures[provider.ProviderName]++;
            _providerLastErrors[provider.ProviderName] = ex.Message;

            Logger.Error(ex, $"Error from provider {provider.ProviderName} " +
                            $"(failure {_providerFailures[provider.ProviderName]}/{_config.MaxFailuresBeforeFallback})");

            if (_config.EnableAutoFallback &&
                _providerFailures[provider.ProviderName] >= _config.MaxFailuresBeforeFallback)
            {
                _providerFallbackStatus[provider.ProviderName] = true;
                Logger.Warn($"Provider {provider.ProviderName} disabled due to repeated failures");
            }
        }

        /// <summary>
        /// Thread-safe cache update using write lock.
        /// </summary>
        private void UpdateCache(HardwareMetrics metrics)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cachedMetrics = metrics;
                _lastCacheUpdate = DateTime.UtcNow;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns a snapshot of the current status for all registered providers,
        /// including availability, failure count, fallback state, capabilities,
        /// and last error message (if any).
        /// </summary>
        public Dictionary<string, ProviderStatus> GetProviderStatus()
        {
            var status = new Dictionary<string, ProviderStatus>();

            foreach (var provider in _providers)
            {
                status[provider.ProviderName] = new ProviderStatus
                {
                    IsAvailable = provider.IsAvailable,
                    FailureCount = _providerFailures[provider.ProviderName],
                    IsInFallback = _providerFallbackStatus[provider.ProviderName],
                    Capabilities = provider.Capabilities,
                    LastErrorMessage = _providerLastErrors.TryGetValue(provider.ProviderName, out string err) ? err : null
                };
            }

            return status;
        }

        /// <summary>
        /// Resets a provider's failure count and fallback state, allowing it to be used again.
        /// Calls <see cref="IHardwareDataProvider.Reset"/> on the provider instance.
        /// </summary>
        /// <param name="providerName">The name of the provider to reset.</param>
        public void ResetProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName);
            if (provider != null)
            {
                provider.Reset();
                _providerFailures[providerName] = 0;
                _providerFallbackStatus[providerName] = false;
                _providerLastErrors[providerName] = null;
                Logger.Info($"Provider {providerName} reset");
            }
        }

        /// <summary>
        /// Disposes all registered providers and releases internal resources.
        /// Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var provider in _providers)
            {
                try
                {
                    provider?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error disposing provider {provider?.ProviderName}");
                }
            }

            _providers.Clear();
            _cacheLock?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Snapshot of a provider's runtime state, used for UI display, notifications, and diagnostics.
    /// </summary>
    public class ProviderStatus
    {
        /// <summary>True if the provider successfully initialized and is currently operational.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Number of consecutive runtime failures since last successful read or reset.</summary>
        public int FailureCount { get; set; }

        /// <summary>True if the provider was disabled due to exceeding the max failure threshold.</summary>
        public bool IsInFallback { get; set; }

        /// <summary>The metric types and priority this provider supports.</summary>
        public ProviderCapabilities Capabilities { get; set; }

        /// <summary>
        /// Last error message recorded, either from initialization failure or a runtime exception.
        /// Null if the provider has never failed.
        /// </summary>
        public string LastErrorMessage { get; set; }
    }
}