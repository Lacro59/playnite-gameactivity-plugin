using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Globalization;

namespace GameActivity.Services.HardwareMonitoring.Providers
{
	/// <summary>
	/// Reads HWiNFO Gadget values from the registry (ValueRaw{index} under HKCU\SOFTWARE\HWiNFO64\VSB).
	/// </summary>
	public class HWiNFOGadgetProvider : BaseHardwareProvider
	{
		public override string ProviderName => "HWiNFOGadget";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.FPS | MetricType.GpuUsage | MetricType.GpuTemperature |
							 MetricType.CpuTemperature | MetricType.GpuPower | MetricType.CpuPower,
			Priority = 3,
			RequiresExternalApp = true,
			RequiresAdminRights = false
		};

		protected override bool InitializeInternal()
		{
			try
			{
				GameActivitySettings settings = LivePluginSettings;
				if (settings == null)
				{
					return false;
				}

				// A configured index plus readable registry value is enough to consider the provider available.
				return TryReadInt(settings.HWiNFO_fps_index).HasValue ||
					   TryReadInt(settings.HWiNFO_gpu_index).HasValue ||
					   TryReadInt(settings.HWiNFO_gpuT_index).HasValue ||
					   TryReadInt(settings.HWiNFO_cpuT_index).HasValue ||
					   TryReadInt(settings.HWiNFO_gpuP_index).HasValue ||
					   TryReadInt(settings.HWiNFO_cpuP_index).HasValue;
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] HWiNFO Gadget registry access failed");
				return false;
			}
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			var metrics = new HardwareMetrics();
			GameActivitySettings settings = LivePluginSettings;
			if (settings == null)
			{
				return metrics;
			}

			metrics.FPS = TryReadInt(settings.HWiNFO_fps_index);
			metrics.GpuUsage = TryReadInt(settings.HWiNFO_gpu_index);
			metrics.GpuTemperature = TryReadInt(settings.HWiNFO_gpuT_index);
			metrics.CpuTemperature = TryReadInt(settings.HWiNFO_cpuT_index);
			metrics.GpuPower = TryReadInt(settings.HWiNFO_gpuP_index);
			metrics.CpuPower = TryReadInt(settings.HWiNFO_cpuP_index);

			return metrics;
		}

		private static int? TryReadInt(long index)
		{
			if (index <= 0)
			{
				return null;
			}

			string rawValue = HWiNFOGadget.GetData(index);
			if (string.IsNullOrWhiteSpace(rawValue))
			{
				return null;
			}

			double parsed;
			if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed) ||
				double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsed))
			{
				return (int)Math.Round(parsed);
			}

			return null;
		}
	}
}
