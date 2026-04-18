using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ============================================================================
// PerformanceCounterProvider.cs - Provider Windows Performance Counters
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Providers
{
	/// <summary>
	/// Provides hardware monitoring metrics using Windows Performance Counters API.
	/// </summary>
	/// <remarks>
	/// This provider utilizes the Windows Performance Counter infrastructure to gather CPU and RAM usage statistics.
	/// It samples metrics synchronously to provide accurate measurements. Does not require administrator 
	/// rights or external applications.
	/// </remarks>
	public class PerformanceCounterProvider : BaseHardwareProvider
	{
		/// <summary>
		/// Number of samples collected for each metric calculation.
		/// </summary>
		private const int SampleCount = 3;

		/// <summary>
		/// Interval in milliseconds between consecutive samples.
		/// </summary>
		private const int SampleIntervalMs = 100;

		/// <summary>
		/// Performance counter for measuring CPU usage percentage.
		/// </summary>
		private PerformanceCounter _cpuCounter;

		/// <summary>
		/// Performance counter for measuring available RAM in megabytes.
		/// </summary>
		private PerformanceCounter _ramCounter;

		/// <summary>
		/// Gets the friendly name of this hardware provider.
		/// </summary>
		public override string ProviderName => "PerformanceCounter";

		/// <summary>
		/// Gets the capabilities and configuration of this provider.
		/// </summary>
		/// <remarks>
		/// This provider supports CPU and RAM usage metrics with priority level 1.
		/// It does not require external applications or administrator rights.
		/// </remarks>
		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.CpuUsage | MetricType.RamUsage,
			Priority = 1,
			RequiresExternalApp = false,
			RequiresAdminRights = false
		};

		/// <summary>
		/// Represents the memory status information structure used by GlobalMemoryStatusEx API.
		/// </summary>
		/// <remarks>
		/// This structure contains memory information including total physical memory, available memory,
		/// and virtual memory statistics. Used for accurate RAM usage calculation.
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORYSTATUSEX
		{
			/// <summary>
			/// Size of the structure in bytes.
			/// </summary>
			internal uint dwLength;

			/// <summary>
			/// Percentage of memory in use (0-100).
			/// </summary>
			internal uint dwMemoryLoad;

			/// <summary>
			/// Total physical RAM in bytes.
			/// </summary>
			internal ulong ullTotalPhys;

			/// <summary>
			/// Available physical RAM in bytes.
			/// </summary>
			internal ulong ullAvailPhys;

			/// <summary>
			/// Total page file size in bytes.
			/// </summary>
			internal ulong ullTotalPageFile;

			/// <summary>
			/// Available page file size in bytes.
			/// </summary>
			internal ulong ullAvailPageFile;

			/// <summary>
			/// Total virtual memory in bytes.
			/// </summary>
			internal ulong ullTotalVirtual;

			/// <summary>
			/// Available virtual memory in bytes.
			/// </summary>
			internal ulong ullAvailVirtual;

			/// <summary>
			/// Available extended virtual memory in bytes.
			/// </summary>
			internal ulong ullAvailExtendedVirtual;
		}

		/// <summary>
		/// Retrieves the current state of the system's memory.
		/// </summary>
		/// <param name="lpBuffer">Reference to MEMORYSTATUSEX structure to receive memory information.</param>
		/// <returns>True if the operation succeeds; otherwise false.</returns>
		/// <remarks>
		/// This is a P/Invoke wrapper for the Windows API GlobalMemoryStatusEx function.
		/// </remarks>
		[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

		/// <summary>
		/// Initializes the performance counters and prepares the provider for operation.
		/// </summary>
		/// <returns>True if initialization succeeds; false otherwise.</returns>
		/// <remarks>
		/// Creates PerformanceCounter instances for CPU and RAM metrics and performs an initial read
		/// to warm up the counters.
		/// </remarks>
		protected override bool InitializeInternal()
		{
			try
			{
				_cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
				_ramCounter = new PerformanceCounter("Memory", "Available MBytes");

				_cpuCounter.NextValue();
				_ramCounter.NextValue();

				return true;
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] Failed to initialize counters");
				return false;
			}
		}

		/// <summary>
		/// Retrieves the current hardware metrics from performance counters.
		/// </summary>
		/// <returns>A HardwareMetrics object containing current CPU and RAM usage percentages.</returns>
		/// <remarks>
		/// This method performs synchronous sampling for both CPU and RAM metrics.
		/// Samples are collected over a brief period to provide accurate measurements.
		/// </remarks>
		protected override HardwareMetrics GetMetricsInternal()
		{
			var metrics = new HardwareMetrics();

			try
			{
				metrics.CpuUsage = SampleCpu();
			}
			catch (Exception ex)
			{
				logger.Warn($"[{ProviderName}] Failed to get CPU usage: {ex.Message}");
			}

			try
			{
				metrics.RamUsage = SampleRam();
			}
			catch (Exception ex)
			{
				logger.Warn($"[{ProviderName}] Failed to get RAM usage: {ex.Message}");
			}

			return metrics;
		}

		/// <summary>
		/// Samples CPU usage over multiple intervals and returns the average.
		/// </summary>
		/// <returns>The average CPU usage percentage over the sample period.</returns>
		/// <remarks>
		/// Takes SampleCount readings at SampleIntervalMs intervals and calculates the average.
		/// </remarks>
		private int SampleCpu()
		{
			double sum = 0;
			for (int i = 0; i < SampleCount; i++)
			{
				sum += _cpuCounter.NextValue();
				if (i < SampleCount - 1)
				{
					Thread.Sleep(SampleIntervalMs);
				}
			}
			return (int)Math.Ceiling(sum / SampleCount);
		}

		/// <summary>
		/// Samples RAM usage over multiple intervals and returns the average percentage.
		/// </summary>
		/// <returns>The average RAM usage percentage (0-100) based on used vs total RAM.</returns>
		/// <remarks>
		/// Retrieves total physical RAM using GlobalMemoryStatusEx API, samples available RAM
		/// multiple times, calculates the average, and computes the usage percentage.
		/// </remarks>
		private int SampleRam()
		{
			MEMORYSTATUSEX statEX = new MEMORYSTATUSEX();
			statEX.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

			if (!GlobalMemoryStatusEx(ref statEX))
			{
				return 0;
			}

			double totalRam = statEX.ullTotalPhys / 1024.0 / 1024.0;
			double availableSum = 0;

			for (int i = 0; i < SampleCount; i++)
			{
				availableSum += _ramCounter.NextValue();
				if (i < SampleCount - 1)
				{
					Thread.Sleep(SampleIntervalMs);
				}
			}

			int availableRam = (int)Math.Round(availableSum / SampleCount);
			int usedRam = (int)totalRam - availableRam;
			return (int)(usedRam * 100 / totalRam);
		}

		/// <summary>
		/// Releases resources used by the provider.
		/// </summary>
		/// <remarks>
		/// Disposes the performance counter instances.
		/// </remarks>
		protected override void DisposeInternal()
		{
			_cpuCounter?.Dispose();
			_ramCounter?.Dispose();
		}
	}
}