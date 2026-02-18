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

		// Priorities by metric (order of attempts)
		public Dictionary<MetricType, List<string>> MetricPriorities { get; set; }

		// Enable/disable automatic fallback
		public bool EnableAutoFallback { get; set; } = true;

		public MonitoringConfiguration()
		{
			MetricPriorities = new Dictionary<MetricType, List<string>>
			{
				{ MetricType.FPS, new List<string> { "RivaTuner", "HWiNFO", "MSIAfterburner" } },
				{ MetricType.CpuUsage, new List<string> { "LibreHardware", "HWiNFO", "WMI", "PerformanceCounter" } },
				{ MetricType.CpuTemperature, new List<string> { "LibreHardware", "HWiNFO", "MSIAfterburner" } },
				{ MetricType.CpuPower, new List<string> { "LibreHardware", "HWiNFO", "MSIAfterburner" } },
				{ MetricType.GpuUsage, new List<string> { "LibreHardware", "HWiNFO", "MSIAfterburner", "WMI", "PerformanceCounter" } },
				{ MetricType.GpuTemperature, new List<string> { "LibreHardware", "HWiNFO", "MSIAfterburner" } },
				{ MetricType.GpuPower, new List<string> { "LibreHardware", "HWiNFO", "MSIAfterburner" } },
				{ MetricType.RamUsage, new List<string> { "LibreHardware", "WMI", "PerformanceCounter" } }
			};
		}
	}
}