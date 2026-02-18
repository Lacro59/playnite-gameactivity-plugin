using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// MetricsValidator.cs - Validate metric values for sanity
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Utilities
{
	/// <summary>
	/// Validates metrics to detect potentially incorrect readings
	/// </summary>
	public static class MetricsValidator
	{
		/// <summary>
		/// Validate metrics and return any warnings
		/// </summary>
		public static List<string> Validate(HardwareMetrics metrics)
		{
			var warnings = new List<string>();

			// FPS validation
			if (metrics.FPS.HasValue)
			{
				if (metrics.FPS < 0)
					warnings.Add($"Invalid FPS value: {metrics.FPS} (negative)");
				else if (metrics.FPS > 1000)
					warnings.Add($"Suspicious FPS value: {metrics.FPS} (unusually high)");
			}

			// CPU usage validation
			if (metrics.CpuUsage.HasValue)
			{
				if (metrics.CpuUsage < 0 || metrics.CpuUsage > 100)
					warnings.Add($"Invalid CPU usage: {metrics.CpuUsage}% (out of range)");
			}

			// GPU usage validation
			if (metrics.GpuUsage.HasValue)
			{
				if (metrics.GpuUsage < 0 || metrics.GpuUsage > 100)
					warnings.Add($"Invalid GPU usage: {metrics.GpuUsage}% (out of range)");
			}

			// RAM usage validation
			if (metrics.RamUsage.HasValue)
			{
				if (metrics.RamUsage < 0 || metrics.RamUsage > 100)
					warnings.Add($"Invalid RAM usage: {metrics.RamUsage}% (out of range)");
			}

			// Temperature validation
			if (metrics.CpuTemperature.HasValue)
			{
				if (metrics.CpuTemperature < -50 || metrics.CpuTemperature > 150)
					warnings.Add($"Suspicious CPU temperature: {metrics.CpuTemperature}°C");
			}

			if (metrics.GpuTemperature.HasValue)
			{
				if (metrics.GpuTemperature < -50 || metrics.GpuTemperature > 150)
					warnings.Add($"Suspicious GPU temperature: {metrics.GpuTemperature}°C");
			}

			// Power validation
			if (metrics.CpuPower.HasValue)
			{
				if (metrics.CpuPower < 0 || metrics.CpuPower > 500)
					warnings.Add($"Suspicious CPU power: {metrics.CpuPower}W");
			}

			if (metrics.GpuPower.HasValue)
			{
				if (metrics.GpuPower < 0 || metrics.GpuPower > 1000)
					warnings.Add($"Suspicious GPU power: {metrics.GpuPower}W");
			}

			return warnings;
		}

		/// <summary>
		/// Sanitize metrics by removing clearly invalid values
		/// </summary>
		public static HardwareMetrics Sanitize(HardwareMetrics metrics)
		{
			var sanitized = new HardwareMetrics
			{
				Timestamp = metrics.Timestamp,
				Source = metrics.Source
			};

			// Only keep valid values
			if (metrics.FPS >= 0 && metrics.FPS <= 1000)
				sanitized.FPS = metrics.FPS;

			if (metrics.CpuUsage >= 0 && metrics.CpuUsage <= 100)
				sanitized.CpuUsage = metrics.CpuUsage;

			if (metrics.GpuUsage >= 0 && metrics.GpuUsage <= 100)
				sanitized.GpuUsage = metrics.GpuUsage;

			if (metrics.RamUsage >= 0 && metrics.RamUsage <= 100)
				sanitized.RamUsage = metrics.RamUsage;

			if (metrics.CpuTemperature >= -50 && metrics.CpuTemperature <= 150)
				sanitized.CpuTemperature = metrics.CpuTemperature;

			if (metrics.GpuTemperature >= -50 && metrics.GpuTemperature <= 150)
				sanitized.GpuTemperature = metrics.GpuTemperature;

			if (metrics.CpuPower >= 0 && metrics.CpuPower <= 500)
				sanitized.CpuPower = metrics.CpuPower;

			if (metrics.GpuPower >= 0 && metrics.GpuPower <= 1000)
				sanitized.GpuPower = metrics.GpuPower;

			return sanitized;
		}
	}
}