using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace GameActivity.Services.HardwareMonitoring.Providers
{
	/// <summary>
	/// Reads hardware metrics from MSI Afterburner via its MAHM shared memory segment.
	/// MSI Afterburner must be running and "Hardware Monitoring" must be enabled.
	/// </summary>
	public class MsiAfterburnerProvider : BaseHardwareProvider
	{
		// ── Shared memory constants ──────────────────────────────────────────
		private const string SharedMemoryName = "MAHMSharedMemory";
		private const uint ExpectedSignature = 0x4D41484D; // 'MAHM'
		private const int MaxPath = 260;

		// ── Sensor source names (invariant, English locale) ──────────────────
		private const string SensorFramerate = "Framerate";
		private const string SensorGpuUsage = "GPU usage";
		private const string SensorGpuTemperature = "GPU temperature";
		private const string SensorGpuPower = "GPU power";
		private const string SensorCpuUsage = "CPU usage";
		private const string SensorCpuTemperature = "CPU temperature";
		private const string SensorCpuPower = "CPU power";
		private const string SensorRamUsage = "RAM usage";

		// ── Shared memory layout ─────────────────────────────────────────────
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MahmHeader
		{
			public uint Signature;
			public uint Version;
			public uint HeaderSize;
			public uint NumEntries;
			public uint EntrySize;
			public uint NumSrcEntries;
			public uint SrcEntrySize;
		}

		// Each entry is a flat byte block; we read fields manually to avoid padding issues.
		// Layout per entry (official MAHM format):
		//   [0..259]   szSrcName        char[260]
		//   [260..267] szSrcUnits       char[8]
		//   [268..527] szLocSrcName     char[260]
		//   [528..535] szLocSrcUnits    char[8]
		//   [536..543] szRecommendedFmt char[8]
		//   [544]      data             float (current)
		//   [548]      minValue         float
		//   [552]      maxValue         float
		//   [556]      avgValue         float
		//   [560]      dwSrcId          uint
		//   [564]      dwSrcIndex       uint
		//   Total: 568 bytes
		private const int OffsetSrcName = 0;
		private const int OffsetData = 544;

		// ────────────────────────────────────────────────────────────────────

		public override string ProviderName => "MsiAfterburner";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics =
				MetricType.FPS |
				MetricType.CpuUsage | MetricType.CpuTemperature | MetricType.CpuPower |
				MetricType.GpuUsage | MetricType.GpuTemperature | MetricType.GpuPower |
				MetricType.RamUsage,
			Priority = 10,
			RequiresExternalApp = true,
			RequiresAdminRights = false
		};

		// ── Initialization ───────────────────────────────────────────────────

		protected override bool InitializeInternal()
		{
			try
			{
				using (OpenSharedMemory()) { }
				return true;
			}
			catch (FileNotFoundException)
			{
				logger.Warn($"[{ProviderName}] Shared memory '{SharedMemoryName}' not found — MSI Afterburner may not be running.");
				return false;
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] Initialization failed.");
				return false;
			}
		}

		// ── Metrics ──────────────────────────────────────────────────────────

		protected override HardwareMetrics GetMetricsInternal()
		{
			using (var mmf = OpenSharedMemory())
			using (var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
			{
				MahmHeader header;
				accessor.Read(0, out header);

				if (header.Signature != ExpectedSignature)
				{
					logger.Warn($"[{ProviderName}] Unexpected shared memory signature: 0x{header.Signature:X8}");
					return new HardwareMetrics();
				}

				return ReadEntries(accessor, header);
			}
		}

		// ── Private helpers ──────────────────────────────────────────────────

		private static MemoryMappedFile OpenSharedMemory()
		{
			return MemoryMappedFile.OpenExisting(SharedMemoryName, MemoryMappedFileRights.Read);
		}

		private HardwareMetrics ReadEntries(MemoryMappedViewAccessor accessor, MahmHeader header)
		{
			var metrics = new HardwareMetrics();
			long entryOffset = header.HeaderSize;

			// Accumulate multiple CPU-usage sensors and average them (Afterburner may expose
			// one entry per core in addition to the aggregate, so we pick the first match only).
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (uint i = 0; i < header.NumEntries; i++, entryOffset += header.EntrySize)
			{
				string name = ReadAnsiString(accessor, entryOffset + OffsetSrcName, MaxPath);
				float value = accessor.ReadSingle(entryOffset + OffsetData);

				if (string.IsNullOrEmpty(name) || seen.Contains(name))
				{
					continue;
				}

				seen.Add(name);
				ApplyMetric(metrics, name, value);
			}

			return metrics;
		}

		private static void ApplyMetric(HardwareMetrics metrics, string sensorName, float value)
		{
			// Negative or implausible values from Afterburner mean "no data".
			if (value < 0f)
			{
				return;
			}

			int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);

			switch (sensorName)
			{
				case SensorFramerate:
					if (!metrics.FPS.HasValue) metrics.FPS = rounded; break;
				case SensorGpuUsage:
					if (!metrics.GpuUsage.HasValue) metrics.GpuUsage = rounded; break;
				case SensorGpuTemperature:
					if (!metrics.GpuTemperature.HasValue) metrics.GpuTemperature = rounded; break;
				case SensorGpuPower:
					if (!metrics.GpuPower.HasValue) metrics.GpuPower = rounded; break;
				case SensorCpuUsage:
					if (!metrics.CpuUsage.HasValue) metrics.CpuUsage = rounded; break;
				case SensorCpuTemperature:
					if (!metrics.CpuTemperature.HasValue) metrics.CpuTemperature = rounded; break;
				case SensorCpuPower:
					if (!metrics.CpuPower.HasValue) metrics.CpuPower = rounded; break;
				case SensorRamUsage:
					if (!metrics.RamUsage.HasValue) metrics.RamUsage = rounded; break;
			}
		}

		/// <summary>Reads a null-terminated ANSI string from shared memory at the given offset.</summary>
		private static string ReadAnsiString(MemoryMappedViewAccessor accessor, long offset, int maxLength)
		{
			var buffer = new byte[maxLength];
			accessor.ReadArray(offset, buffer, 0, maxLength);

			int length = Array.IndexOf(buffer, (byte)0);
			if (length < 0) length = maxLength;

			return length == 0 ? string.Empty : Encoding.Default.GetString(buffer, 0, length);
		}
	}
}