using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// MonitoringConfiguration.cs - Monitoring system configuration
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Core
{
	public class MonitoringConfiguration
	{
		// Maximum number of errors before permanent fallback
		public int MaxFailuresBeforeFallback { get; set; } = 5;

		// Cache duration in milliseconds
		public int CacheDurationMs { get; set; } = 500;

		/// <summary>
		/// Provider attempt order per metric: fastest sources first (shared memory), then direct sensor APIs,
		/// then WMI, then performance counters (multi-sample, slowest). Every registered provider that can
		/// supply a metric should appear here; <see cref="HardwareDataAggregator"/> skips names that are not
		/// registered or do not <see cref="ProviderCapabilities.Supports"/> the metric.
		/// </summary>
		public Dictionary<MetricType, List<string>> MetricPriorities { get; set; }

		// Enable/disable automatic fallback
		public bool EnableAutoFallback { get; set; } = true;

		public MonitoringConfiguration()
		{
			MetricPriorities = new Dictionary<MetricType, List<string>>
			{
				{ MetricType.FPS, new List<string> { "RivaTuner", "HWiNFO", "HWiNFOGadget", "MsiAfterburner" } },
				{ MetricType.CpuUsage, new List<string> { "HWiNFO", "MsiAfterburner", "LibreHardware", "WMI", "PerformanceCounter" } },
				{ MetricType.CpuTemperature, new List<string> { "HWiNFO", "HWiNFOGadget", "MsiAfterburner", "LibreHardware" } },
				{ MetricType.CpuPower, new List<string> { "HWiNFO", "HWiNFOGadget", "MsiAfterburner", "LibreHardware" } },
				{ MetricType.GpuUsage, new List<string> { "HWiNFO", "HWiNFOGadget", "MsiAfterburner", "LibreHardware", "WMI", "PerformanceCounter" } },
				{ MetricType.GpuTemperature, new List<string> { "HWiNFO", "HWiNFOGadget", "MsiAfterburner", "LibreHardware" } },
				{ MetricType.GpuPower, new List<string> { "HWiNFO", "HWiNFOGadget", "MsiAfterburner", "LibreHardware" } },
				{ MetricType.RamUsage, new List<string> { "LibreHardware", "MsiAfterburner", "WMI", "PerformanceCounter" } },
				{ MetricType.Framerate1PercentLow, new List<string> { "MsiAfterburner" } },
				{ MetricType.Framerate0Point1PercentLow, new List<string> { "MsiAfterburner" } }
			};
		}
	}
}