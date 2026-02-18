using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ============================================================================
// HardwareDataAggregator.cs - Main orchestrator with fallback logic
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Core
{
	public class HardwareDataAggregator : IDisposable
	{
		private static readonly ILogger logger = LogManager.GetLogger();
		private readonly MonitoringConfiguration _config;
		private readonly List<IHardwareDataProvider> _providers;
		private readonly Dictionary<string, int> _providerFailures;
		private readonly Dictionary<string, bool> _providerFallbackStatus;
		private readonly ReaderWriterLockSlim _cacheLock;

		private HardwareMetrics _cachedMetrics;
		private DateTime _lastCacheUpdate;
		private bool _disposed;

		public HardwareDataAggregator(MonitoringConfiguration config = null)
		{
			_config = config ?? new MonitoringConfiguration();
			_providers = new List<IHardwareDataProvider>();
			_providerFailures = new Dictionary<string, int>();
			_providerFallbackStatus = new Dictionary<string, bool>();
			_cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		}

		public void RegisterProvider(IHardwareDataProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			_providers.Add(provider);
			_providerFailures[provider.ProviderName] = 0;
			_providerFallbackStatus[provider.ProviderName] = false;

			logger.Info($"Registered provider: {provider.ProviderName}");
		}

		public bool Initialize()
		{
			logger.Info("Initializing providers...");

			foreach (var provider in _providers.OrderByDescending(p => p.Capabilities.Priority))
			{
				try
				{
					if (provider.Initialize())
					{
						logger.Info($"Provider {provider.ProviderName} initialized successfully");
					}
					else
					{
						logger.Warn($"Provider {provider.ProviderName} failed to initialize");
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, $"Error initializing provider {provider.ProviderName}");
				}
			}

			return _providers.Any(p => p.IsAvailable);
		}

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

		private HardwareMetrics GetMetricsFromProvider(IHardwareDataProvider provider)
		{
			if (provider == null || !provider.IsAvailable)
			{
				return new HardwareMetrics();
			}

			try
			{
				var metrics = provider.GetMetrics();
				_providerFailures[provider.ProviderName] = 0;
				return metrics ?? new HardwareMetrics();
			}
			catch (Exception ex)
			{
				HandleProviderError(provider, ex);
				throw;
			}
		}

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
				logger.Warn($"Could not obtain {metricType} from any provider");
			}
		}

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

		private void HandleProviderError(IHardwareDataProvider provider, Exception ex)
		{
			_providerFailures[provider.ProviderName]++;

			logger.Error(ex, $"Error from provider {provider.ProviderName} " +
							$"(failure {_providerFailures[provider.ProviderName]}/{_config.MaxFailuresBeforeFallback})");

			if (_config.EnableAutoFallback &&
				_providerFailures[provider.ProviderName] >= _config.MaxFailuresBeforeFallback)
			{
				_providerFallbackStatus[provider.ProviderName] = true;
				logger.Warn($"Provider {provider.ProviderName} disabled due to repeated failures");
			}
		}

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
					Capabilities = provider.Capabilities
				};
			}

			return status;
		}

		public void ResetProvider(string providerName)
		{
			var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName);
			if (provider != null)
			{
				provider.Reset();
				_providerFailures[providerName] = 0;
				_providerFallbackStatus[providerName] = false;
				logger.Info($"Provider {providerName} reset");
			}
		}

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
					logger.Error(ex, $"Error disposing provider {provider?.ProviderName}");
				}
			}

			_providers.Clear();
			_cacheLock?.Dispose();
			_disposed = true;
		}
	}

	public class ProviderStatus
	{
		public bool IsAvailable { get; set; }
		public int FailureCount { get; set; }
		public bool IsInFallback { get; set; }
		public ProviderCapabilities Capabilities { get; set; }
	}
}