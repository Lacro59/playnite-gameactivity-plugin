using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// HardwareMetrics.cs - Data model for metrics
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Models
{
	public class HardwareMetrics
	{
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		public int? FPS { get; set; }

		/// <summary>Optional Framerate 1% Low (FPS), typically from MSI Afterburner.</summary>
		public int? FPS1PercentLow { get; set; }

		/// <summary>Optional Framerate 0.1% Low (FPS), typically from MSI Afterburner.</summary>
		public int? FPS0Point1PercentLow { get; set; }

		public int? CpuUsage { get; set; }
		public int? CpuTemperature { get; set; }
		public int? CpuPower { get; set; }
		public int? GpuUsage { get; set; }
		public int? GpuTemperature { get; set; }
		public int? GpuPower { get; set; }
		public int? RamUsage { get; set; }

		public MetricSource Source { get; set; } = new MetricSource();

		public void Merge(HardwareMetrics other)
		{
			if (other == null) return;

			FPS = FPS ?? other.FPS;
			FPS1PercentLow = FPS1PercentLow ?? other.FPS1PercentLow;
			FPS0Point1PercentLow = FPS0Point1PercentLow ?? other.FPS0Point1PercentLow;
			CpuUsage = CpuUsage ?? other.CpuUsage;
			CpuTemperature = CpuTemperature ?? other.CpuTemperature;
			CpuPower = CpuPower ?? other.CpuPower;
			GpuUsage = GpuUsage ?? other.GpuUsage;
			GpuTemperature = GpuTemperature ?? other.GpuTemperature;
			GpuPower = GpuPower ?? other.GpuPower;
			RamUsage = RamUsage ?? other.RamUsage;

			Source.Merge(other.Source);
		}

		public bool IsComplete()
		{
			return FPS.HasValue && CpuUsage.HasValue && GpuUsage.HasValue && RamUsage.HasValue;
		}
	}

	public class MetricSource
	{
		public string FPS { get; set; }
		public string CpuUsage { get; set; }
		public string CpuTemperature { get; set; }
		public string CpuPower { get; set; }
		public string GpuUsage { get; set; }
		public string GpuTemperature { get; set; }
		public string GpuPower { get; set; }
		public string RamUsage { get; set; }

		public void Merge(MetricSource other)
		{
			FPS = FPS ?? other.FPS;
			CpuUsage = CpuUsage ?? other.CpuUsage;
			CpuTemperature = CpuTemperature ?? other.CpuTemperature;
			CpuPower = CpuPower ?? other.CpuPower;
			GpuUsage = GpuUsage ?? other.GpuUsage;
			GpuTemperature = GpuTemperature ?? other.GpuTemperature;
			GpuPower = GpuPower ?? other.GpuPower;
			RamUsage = RamUsage ?? other.RamUsage;
		}
	}
}