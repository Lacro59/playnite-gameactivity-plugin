using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// WMIProvider.cs - Provider Windows Management Instrumentation
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Providers
{
	public class WMIProvider : BaseHardwareProvider
	{
		public override string ProviderName => "WMI";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.GpuUsage | MetricType.CpuUsage | MetricType.RamUsage,
			Priority = 2,
			RequiresExternalApp = false,
			RequiresAdminRights = false
		};

		protected override bool InitializeInternal()
		{
			try
			{
				// WMI connection test
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor"))
				{
					var results = searcher.Get();
					return results.Count > 0;
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] WMI connection test failed");
				return false;
			}
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			var metrics = new HardwareMetrics();

			// GPU Usage
			try
			{
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
					"SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
				{
					float totalUsage = 0;
					foreach (ManagementObject obj in searcher.Get())
					{
						if (obj["UtilizationPercentage"] != null)
						{
							totalUsage += Convert.ToSingle(obj["UtilizationPercentage"]);
						}
					}

					if (totalUsage > 0)
					{
						metrics.GpuUsage = (int)totalUsage;
					}
				}
			}
			catch (Exception ex)
			{
				logger.Warn($"[{ProviderName}] GPU usage not available via WMI: {ex.Message}");
			}

			// CPU Usage
			try
			{
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
					"SELECT LoadPercentage FROM Win32_Processor"))
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						if (obj["LoadPercentage"] != null)
						{
							metrics.CpuUsage = Convert.ToInt32(obj["LoadPercentage"]);
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Warn($"[{ProviderName}] CPU usage not available via WMI: {ex.Message}");
			}

			// RAM Usage
			try
			{
				long totalMemory = 0;
				long freeMemory = 0;

				using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
					"SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						if (obj["TotalVisibleMemorySize"] != null && obj["FreePhysicalMemory"] != null)
						{
							totalMemory = Convert.ToInt64(obj["TotalVisibleMemorySize"]);
							freeMemory = Convert.ToInt64(obj["FreePhysicalMemory"]);
							break;
						}
					}
				}

				if (totalMemory > 0)
				{
					long usedMemory = totalMemory - freeMemory;
					metrics.RamUsage = (int)((usedMemory * 100) / totalMemory);
				}
			}
			catch (Exception ex)
			{
				logger.Warn($"[{ProviderName}] RAM usage not available via WMI: {ex.Message}");
			}

			return metrics;
		}
	}
}