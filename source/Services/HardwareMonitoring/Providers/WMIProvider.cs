using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Management;

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

			// GPU Usage — Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine exposes one row per
			// process / physical adapter / engine. Summing every row mixes parallel engines and multiple GPUs,
			// which inflates the percentage. We take 3D engine rows only, aggregate per physical adapter (phys_n),
			// then use the highest adapter load (similar to "dominant" GPU, capped at 100%).
			try
			{
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
					"SELECT Name, UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
				{
					int? usage = TryGetGpuUsageFromGpuEngineCounters(searcher);
					if (usage.HasValue)
					{
						metrics.GpuUsage = usage.Value;
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

		/// <summary>
		/// Derives one GPU usage value from GPUEngine rows: max utilization per physical adapter, then the
		/// largest across adapters (avoids summing hybrid GPUs and unrelated engine types when 3D data exists).
		/// </summary>
		private int? TryGetGpuUsageFromGpuEngineCounters(ManagementObjectSearcher searcher)
		{
			if (searcher == null)
			{
				return null;
			}

			var rows = new List<Tuple<string, float>>();
			foreach (ManagementObject obj in searcher.Get())
			{
				string name = null;
				if (obj["Name"] != null)
				{
					name = obj["Name"].ToString();
				}

				if (obj["UtilizationPercentage"] == null)
				{
					continue;
				}

				float u = Convert.ToSingle(obj["UtilizationPercentage"]);
				if (u < 0f || float.IsNaN(u) || float.IsInfinity(u))
				{
					continue;
				}

				rows.Add(Tuple.Create(name, u));
			}

			if (rows.Count == 0)
			{
				return null;
			}

			float value = AggregateGpuLoadByAdapter(rows, threeDEnginesOnly: true);
			if (value <= 0f)
			{
				value = AggregateGpuLoadByAdapter(rows, threeDEnginesOnly: false);
			}

			if (value <= 0f)
			{
				return null;
			}

			int rounded = (int)Math.Min(100, Math.Round(value, MidpointRounding.AwayFromZero));
			if (rounded <= 0)
			{
				return null;
			}

			return rounded;
		}

		private static float AggregateGpuLoadByAdapter(IList<Tuple<string, float>> rows, bool threeDEnginesOnly)
		{
			var perPhys = new Dictionary<int, float>();
			for (int i = 0; i < rows.Count; i++)
			{
				string name = rows[i].Item1;
				if (threeDEnginesOnly && !IsThreeDEngineInstanceName(name))
				{
					continue;
				}

				float u = rows[i].Item2;
				int phys = ParsePhysIndexFromGpuEngineName(name);
				if (!perPhys.ContainsKey(phys) || u > perPhys[phys])
				{
					perPhys[phys] = u;
				}
			}

			if (perPhys.Count == 0)
			{
				return 0f;
			}

			float max = 0f;
			foreach (float v in perPhys.Values)
			{
				if (v > max)
				{
					max = v;
				}
			}

			return max;
		}

		private static bool IsThreeDEngineInstanceName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			return name.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static int ParsePhysIndexFromGpuEngineName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return 0;
			}

			int start = name.IndexOf("phys_", StringComparison.OrdinalIgnoreCase);
			if (start < 0)
			{
				return 0;
			}

			start += 5;
			int end = start;
			while (end < name.Length && name[end] >= '0' && name[end] <= '9')
			{
				end++;
			}

			if (end == start)
			{
				return 0;
			}

			if (int.TryParse(name.Substring(start, end - start), out int value))
			{
				return value;
			}

			return 0;
		}
	}
}