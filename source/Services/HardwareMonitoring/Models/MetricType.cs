using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Services.HardwareMonitoring.Models
{
	[Flags]
	public enum MetricType
	{
		None = 0,
		FPS = 1,
		CpuUsage = 2,
		CpuTemperature = 4,
		CpuPower = 8,
		GpuUsage = 16,
		GpuTemperature = 32,
		GpuPower = 64,
		RamUsage = 128,
		/// <summary>Framerate 1% Low (FPS), typically from MSI Afterburner MAHM.</summary>
		Framerate1PercentLow = 256,
		/// <summary>Framerate 0.1% Low (FPS), typically from MSI Afterburner MAHM.</summary>
		Framerate0Point1PercentLow = 512,
		All = FPS | CpuUsage | CpuTemperature | CpuPower | GpuUsage | GpuTemperature | GpuPower | RamUsage
			| Framerate1PercentLow | Framerate0Point1PercentLow
	}
}