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
		All = FPS | CpuUsage | CpuTemperature | CpuPower | GpuUsage | GpuTemperature | GpuPower | RamUsage
	}
}