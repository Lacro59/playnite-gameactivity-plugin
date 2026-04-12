using GameActivity;
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
	/// MSI Afterburner must be running and hardware monitoring must be enabled.
	/// Each metric is resolved by matching <see cref="GameActivitySettings"/> sensor names
	/// to MAHM <c>szSrcName</c> (case-insensitive); empty settings use English defaults.
	/// </summary>
	public class MsiAfterburnerProvider : BaseHardwareProvider
	{
		private const string SharedMemoryName = "MAHMSharedMemory";
		private const uint ExpectedSignature = 0x4D41484D; // 'MAHM'
		private const int MaxPath = 260;

		private const string DefaultSensorFramerate = "Framerate";
		private const string DefaultSensorFramerate1PercentLow = "Framerate 1% Low";
		private const string DefaultSensorFramerate0Point1PercentLow = "Framerate 0.1% Low";
		private const string DefaultSensorGpuUsage = "GPU usage";
		private const string DefaultSensorGpuTemperature = "GPU temperature";
		private const string DefaultSensorGpuPower = "GPU power";
		private const string DefaultSensorCpuUsage = "CPU usage";
		private const string DefaultSensorCpuTemperature = "CPU temperature";
		private const string DefaultSensorCpuPower = "CPU power";
		private const string DefaultSensorRamUsage = "RAM usage";

		private const int OffsetSrcName = 0;
		private const int LegacyDataOffset = 544;
		private const int ModernStringFieldSize = MaxPath;
		private const int ModernDataOffset = ModernStringFieldSize * 5;
		private const uint LegacyEntrySizeThreshold = 640;
		private const int MahmHeaderOffsetNumGpuEntries = 24;
		private const int MahmHeaderOffsetGpuEntrySize = 28;
		private const int ModernDwGpuOffset = 1316;
		private const int ModernSrcIdOffset = 1320;
		private const int LegacyLocSrcNameOffset = 268;
		private const int ModernLocSrcNameOffset = 520;

		public MsiAfterburnerProvider()
		{
		}

		/// <summary>
		/// Enumerates distinct MAHM monitoring rows with <see cref="MahmSensorListEntry.SourceName"/>,
		/// optional adapter label from the MAHM GPU table (<c>dwGpu</c>), localized name fallback, and units.
		/// </summary>
		public static List<MahmSensorListEntry> GetAvailableMahmSensorInfos()
		{
			var result = new List<MahmSensorListEntry>();
			try
			{
				using (MemoryMappedFile mmf = OpenSharedMemory())
				using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
				{
					MahmHeader header;
					accessor.Read(0, out header);
					if (header.Signature != ExpectedSignature)
					{
						return result;
					}
					int dataValueOffset = GetDataValueOffset(header.EntrySize);
					if (dataValueOffset < 0)
					{
						return result;
					}
					bool useModernTail = dataValueOffset == ModernDataOffset;
					List<string> gpuAdapters = TryReadMahmGpuAdapterDisplayNames(accessor, header);
					var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					long entryOffset = header.HeaderSize;
					for (uint i = 0; i < header.NumEntries; i++, entryOffset += header.EntrySize)
					{
						string raw = ReadAnsiString(accessor, entryOffset + OffsetSrcName, MaxPath);
						if (string.IsNullOrWhiteSpace(raw))
						{
							continue;
						}
						string trimmed = raw.Trim();
						if (!seen.Add(trimmed))
						{
							continue;
						}
						string units = ReadMahmEntryUnits(accessor, entryOffset, useModernTail);
						string hardware = BuildMahmEntryHardwareContext(
							accessor, entryOffset, trimmed, header.EntrySize, useModernTail, gpuAdapters);
						result.Add(new MahmSensorListEntry(trimmed, hardware, units));
					}
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (Exception ex)
			{
				LogManager.GetLogger().Error(ex, "[MsiAfterburner] Failed to read MAHM sensor name list.");
			}
			result.Sort((a, b) => string.Compare(a.SourceName, b.SourceName, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		/// <summary>
		/// Distinct MAHM <c>szSrcName</c> values only (same order as <see cref="GetAvailableMahmSensorInfos"/>).
		/// </summary>
		public static List<string> GetAvailableMahmSensorNames()
		{
			List<MahmSensorListEntry> infos = GetAvailableMahmSensorInfos();
			var names = new List<string>(infos.Count);
			for (int i = 0; i < infos.Count; i++)
			{
				names.Add(infos[i].SourceName);
			}
			return names;
		}

		private static List<string> TryReadMahmGpuAdapterDisplayNames(MemoryMappedViewAccessor accessor, MahmHeader header)
		{
			var list = new List<string>();
			try
			{
				if (header.HeaderSize < MahmHeaderOffsetGpuEntrySize + sizeof(uint))
				{
					return list;
				}
				uint numGpu = 0;
				uint gpuEntrySize = 0;
				accessor.Read(MahmHeaderOffsetNumGpuEntries, out numGpu);
				accessor.Read(MahmHeaderOffsetGpuEntrySize, out gpuEntrySize);
				if (numGpu == 0 || numGpu > 16 || gpuEntrySize < 780)
				{
					return list;
				}
				long gpuTableBase = header.HeaderSize + (long)header.NumEntries * header.EntrySize;
				for (uint g = 0; g < numGpu; g++)
				{
					long baseOff = gpuTableBase + (long)g * gpuEntrySize;
					string device = (ReadAnsiString(accessor, baseOff + 520, MaxPath) ?? string.Empty).Trim();
					string family = (ReadAnsiString(accessor, baseOff + 260, MaxPath) ?? string.Empty).Trim();
					string gpuId = (ReadAnsiString(accessor, baseOff, MaxPath) ?? string.Empty).Trim();
					string label = !string.IsNullOrEmpty(device)
						? device
						: (!string.IsNullOrEmpty(family) ? family : ShortenGpuIdString(gpuId));
					list.Add(label ?? string.Empty);
				}
			}
			catch
			{
				list.Clear();
			}
			return list;
		}

		private static string ShortenGpuIdString(string gpuId)
		{
			if (string.IsNullOrEmpty(gpuId))
			{
				return string.Empty;
			}
			if (gpuId.Length > 96)
			{
				return gpuId.Substring(0, 96) + "...";
			}
			return gpuId;
		}

		private static string BuildMahmEntryHardwareContext(
			MemoryMappedViewAccessor accessor,
			long entryOffset,
			string trimmedSourceName,
			uint entrySize,
			bool useModernTail,
			List<string> gpuAdapters)
		{
			if (useModernTail && entrySize >= ModernSrcIdOffset + sizeof(uint))
			{
				int dwGpu = accessor.ReadInt32(entryOffset + ModernDwGpuOffset);
				if (dwGpu >= 0 && gpuAdapters != null && dwGpu < gpuAdapters.Count)
				{
					string adapter = gpuAdapters[dwGpu];
					if (!string.IsNullOrWhiteSpace(adapter))
					{
						return string.Format("GPU {0}: {1}", dwGpu, adapter.Trim());
					}
				}
			}

			int locLen = useModernTail ? MaxPath : MaxPath;
			int locOffset = useModernTail ? ModernLocSrcNameOffset : LegacyLocSrcNameOffset;
			if (entrySize < locOffset + locLen)
			{
				return string.Empty;
			}
			string loc = ReadAnsiString(accessor, entryOffset + locOffset, locLen);
			if (string.IsNullOrWhiteSpace(loc))
			{
				return string.Empty;
			}
			string t = loc.Trim();
			if (t.Length > 0 && !t.Equals(trimmedSourceName, StringComparison.OrdinalIgnoreCase))
			{
				return t;
			}
			return string.Empty;
		}

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

		protected override bool InitializeInternal()
		{
			try
			{
				using (MemoryMappedFile mmf = OpenSharedMemory())
				using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
				{
					MahmHeader probe;
					accessor.Read(0, out probe);
					if (probe.Signature == ExpectedSignature)
					{
						logger.Info(string.Format("[{0}] MAHM header: version=0x{1:X8}, headerSize={2}, numEntries={3}, entrySize={4}",
							ProviderName, probe.Version, probe.HeaderSize, probe.NumEntries, probe.EntrySize));
					}
				}
				return true;
			}
			catch (FileNotFoundException)
			{
				logger.Warn(string.Format("[{0}] Shared memory '{1}' not found — MSI Afterburner may not be running.", ProviderName, SharedMemoryName));
				return false;
			}
			catch (Exception ex)
			{
				logger.Error(ex, string.Format("[{0}] Initialization failed.", ProviderName));
				return false;
			}
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			using (MemoryMappedFile mmf = OpenSharedMemory())
			using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
			{
				MahmHeader header;
				accessor.Read(0, out header);

				if (header.Signature != ExpectedSignature)
				{
					logger.Warn(string.Format("[{0}] Unexpected shared memory signature: 0x{1:X8}", ProviderName, header.Signature));
					return new HardwareMetrics();
				}

				return ReadEntries(accessor, header);
			}
		}

		#region MAHM layout

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

		private struct MahmEntryRow
		{
			public string Name;
			public string Units;
			public float Value;
		}

		#endregion

		private HardwareMetrics ReadEntries(MemoryMappedViewAccessor accessor, MahmHeader header)
		{
			var metrics = new HardwareMetrics();
			long entryOffset = header.HeaderSize;

			int dataValueOffset = GetDataValueOffset(header.EntrySize);
			if (dataValueOffset < 0)
			{
				logger.Warn(string.Format("[{0}] Unsupported MAHM entry size: {1}", ProviderName, header.EntrySize));
				return metrics;
			}

			bool useModernTail = dataValueOffset == ModernDataOffset;
			var rows = new List<MahmEntryRow>();

			for (uint i = 0; i < header.NumEntries; i++, entryOffset += header.EntrySize)
			{
				string name = ReadAnsiString(accessor, entryOffset + OffsetSrcName, MaxPath);
				float value = accessor.ReadSingle(entryOffset + dataValueOffset);

				if (string.IsNullOrEmpty(name) || IsUnavailableMahmValue(value))
				{
					continue;
				}

				string units = ReadMahmEntryUnits(accessor, entryOffset, useModernTail);

				rows.Add(new MahmEntryRow { Name = name, Units = units, Value = value });
			}

			ApplyConfiguredMahmSensors(metrics, rows);
			return metrics;
		}

		#region Configured sensor resolution

		private void ApplyConfiguredMahmSensors(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			TryApplyFps(metrics, rows);
			TryApplyFps1PercentLow(metrics, rows);
			TryApplyFps0Point1PercentLow(metrics, rows);
			TryApplyCpuUsage(metrics, rows);
			TryApplyCpuTemperature(metrics, rows);
			TryApplyCpuPower(metrics, rows);
			TryApplyGpuUsage(metrics, rows);
			TryApplyGpuTemperature(metrics, rows);
			TryApplyGpuPower(metrics, rows);
			TryApplyRamUsage(metrics, rows);
		}

		private string EffectiveSensorName(string configured, string defaultEnglish)
		{
			if (configured == null)
			{
				return defaultEnglish;
			}
			string t = configured.Trim();
			if (t.Length == 0)
			{
				return defaultEnglish;
			}
			return t;
		}

		private string SettingOrDefault(Func<GameActivitySettings, string> pick, string defaultEnglish)
		{
			GameActivitySettings live = LivePluginSettings;
			if (live == null)
			{
				return defaultEnglish;
			}
			return EffectiveSensorName(pick(live), defaultEnglish);
		}

		private static bool TryFindMahmRow(List<MahmEntryRow> rows, string sensorName, out MahmEntryRow row)
		{
			row = default(MahmEntryRow);
			if (string.IsNullOrEmpty(sensorName))
			{
				return false;
			}
			string want = sensorName.Trim();
			for (int i = 0; i < rows.Count; i++)
			{
				string n = rows[i].Name;
				if (string.IsNullOrEmpty(n))
				{
					continue;
				}
				if (string.Equals(n.Trim(), want, StringComparison.OrdinalIgnoreCase))
				{
					row = rows[i];
					return true;
				}
			}
			return false;
		}

		private void TryApplyFps(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorFramerate, DefaultSensorFramerate);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value))
			{
				return;
			}
			if (row.Value >= 0f && row.Value <= 2000f)
			{
				metrics.FPS = RoundToInt(row.Value);
			}
		}

		private void TryApplyFps1PercentLow(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorFramerate1PercentLow, DefaultSensorFramerate1PercentLow);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value))
			{
				return;
			}
			if (row.Value >= 0f && row.Value <= 2000f)
			{
				metrics.FPS1PercentLow = RoundToInt(row.Value);
			}
		}

		private void TryApplyFps0Point1PercentLow(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorFramerate0Point1PercentLow, DefaultSensorFramerate0Point1PercentLow);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value))
			{
				return;
			}
			if (row.Value >= 0f && row.Value <= 2000f)
			{
				metrics.FPS0Point1PercentLow = RoundToInt(row.Value);
			}
		}

		private void TryApplyCpuUsage(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorCpuUsage, DefaultSensorCpuUsage);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value) || row.Value < 0f || row.Value > 100f)
			{
				return;
			}
			metrics.CpuUsage = RoundToInt(row.Value);
		}

		private void TryApplyCpuTemperature(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorCpuTemperature, DefaultSensorCpuTemperature);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value) || row.Value < 0f || row.Value > 150f)
			{
				return;
			}
			metrics.CpuTemperature = RoundToInt(row.Value);
		}

		private void TryApplyCpuPower(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorCpuPower, DefaultSensorCpuPower);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value) || row.Value < 0f || row.Value > 2000f)
			{
				return;
			}
			metrics.CpuPower = RoundToInt(row.Value);
		}

		private void TryApplyGpuUsage(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorGpuUsage, DefaultSensorGpuUsage);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value))
			{
				return;
			}
			metrics.GpuUsage = NormalizeMahmGpuUsagePercent(row.Value, row.Units);
		}

		private void TryApplyGpuTemperature(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorGpuTemperature, DefaultSensorGpuTemperature);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value) || row.Value < 0f || row.Value > 125f)
			{
				return;
			}
			metrics.GpuTemperature = RoundToInt(row.Value);
		}

		private void TryApplyGpuPower(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorGpuPower, DefaultSensorGpuPower);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			if (IsUnavailableMahmValue(row.Value) || row.Value < 0f || row.Value > 2000f)
			{
				return;
			}
			metrics.GpuPower = RoundToInt(row.Value);
		}

		private void TryApplyRamUsage(HardwareMetrics metrics, List<MahmEntryRow> rows)
		{
			string want = SettingOrDefault(s => s.MsiAfterburnerSensorRamUsage, DefaultSensorRamUsage);
			MahmEntryRow row;
			if (!TryFindMahmRow(rows, want, out row))
			{
				return;
			}
			int? ramPct = TryMahmRamUsagePercent(row.Value, row.Name, row.Units);
			if (ramPct.HasValue)
			{
				metrics.RamUsage = ramPct.Value;
			}
		}

		#endregion

		private static MemoryMappedFile OpenSharedMemory()
		{
			return MemoryMappedFile.OpenExisting(SharedMemoryName, MemoryMappedFileRights.Read);
		}

		private static bool IsUnavailableMahmValue(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				return true;
			}
			if (value < 0f)
			{
				return true;
			}
			if (value > 1e30f)
			{
				return true;
			}
			return false;
		}

		private static int GetDataValueOffset(uint entrySize)
		{
			if (entrySize == 0)
			{
				return -1;
			}
			if (entrySize < LegacyEntrySizeThreshold)
			{
				if (entrySize < LegacyDataOffset + sizeof(float))
				{
					return -1;
				}
				return LegacyDataOffset;
			}
			if (entrySize < ModernDataOffset + sizeof(float))
			{
				return -1;
			}
			return ModernDataOffset;
		}

		private static string ReadAnsiString(MemoryMappedViewAccessor accessor, long offset, int maxLength)
		{
			var buffer = new byte[maxLength];
			accessor.ReadArray(offset, buffer, 0, maxLength);

			int length = Array.IndexOf(buffer, (byte)0);
			if (length < 0)
			{
				length = maxLength;
			}

			if (length == 0)
			{
				return string.Empty;
			}
			return Encoding.Default.GetString(buffer, 0, length);
		}

		private static string ReadMahmEntryUnits(MemoryMappedViewAccessor accessor, long entryBase, bool useModernTail)
		{
			if (useModernTail)
			{
				return (ReadAnsiString(accessor, entryBase + 260, MaxPath) ?? string.Empty).Trim();
			}
			return (ReadAnsiString(accessor, entryBase + 260, 8) ?? string.Empty).Trim();
		}

		private static int RoundToInt(float value)
		{
			return (int)Math.Round(value, MidpointRounding.AwayFromZero);
		}

		private static float NormalizeMahmGpuUsageToFloat(float value, string units)
		{
			if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
			{
				return 0f;
			}
			float v = value;
			string u = units ?? string.Empty;
			if (u.IndexOf("‰", StringComparison.Ordinal) >= 0
				|| u.IndexOf("PER MIL", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				v = v / 10f;
			}
			if (v > 100f && v <= 1000f)
			{
				v = v / 10f;
			}
			if (v < 0f)
			{
				v = 0f;
			}
			if (v > 100f)
			{
				v = 100f;
			}
			return v;
		}

		private static int NormalizeMahmGpuUsagePercent(float value, string units)
		{
			return RoundToInt(NormalizeMahmGpuUsageToFloat(value, units));
		}

		private static int? TryMahmRamUsagePercent(float value, string name, string units)
		{
			if (IsUnavailableMahmValue(value))
			{
				return null;
			}
			if (value >= 0f && value <= 100f)
			{
				return RoundToInt(value);
			}

			string nm = name ?? string.Empty;
			if (nm.IndexOf("page", StringComparison.OrdinalIgnoreCase) >= 0
				|| nm.IndexOf("commit", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return null;
			}

			double totalMb;
			if (!TryGetPhysicalRamTotalMegabytes(out totalMb) || totalMb < 512d)
			{
				return null;
			}

			double usedMb = value;
			string u = (units ?? string.Empty).ToUpperInvariant();
			if (u.IndexOf("KB", StringComparison.Ordinal) >= 0)
			{
				usedMb = value / 1024d;
			}
			else if (u.IndexOf("GB", StringComparison.Ordinal) >= 0)
			{
				usedMb = value * 1024d;
			}

			if (usedMb > totalMb * 1.08d)
			{
				return null;
			}

			double pct = usedMb * 100d / totalMb;
			if (double.IsNaN(pct) || pct < 0d)
			{
				return null;
			}
			if (pct > 100d)
			{
				pct = 100d;
			}
			return (int)Math.Round(pct, MidpointRounding.AwayFromZero);
		}

		private static bool TryGetPhysicalRamTotalMegabytes(out double totalMegabytes)
		{
			totalMegabytes = 0d;
			var stat = new MEMORYSTATUSEX();
			stat.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
			if (!GlobalMemoryStatusEx(ref stat))
			{
				return false;
			}
			if (stat.ullTotalPhys == 0UL)
			{
				return false;
			}
			totalMegabytes = stat.ullTotalPhys / 1024d / 1024d;
			return true;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORYSTATUSEX
		{
			internal uint dwLength;
			internal uint dwMemoryLoad;
			internal ulong ullTotalPhys;
			internal ulong ullAvailPhys;
			internal ulong ullTotalPageFile;
			internal ulong ullAvailPageFile;
			internal ulong ullTotalVirtual;
			internal ulong ullAvailVirtual;
			internal ulong ullAvailExtendedVirtual;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
	}
}
