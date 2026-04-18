using CommonPluginsShared;
using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// RivaTunerSDKProvider.cs - Using official RTSS SDK (if available)
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Providers
{
	public class RivaTunerProvider : BaseHardwareProvider
	{
		private const string RTSS_SHARED_MEMORY_NAME = "RTSSSharedMemoryV2";
		private const uint RTSS_SIGNATURE = 0x52545353; // 'RTSS'

		// Constants for RTSS offsets
		private const int HEADER_APP_ENTRY_SIZE_OFFSET = 8;
		private const int HEADER_APP_ENTRY_OFFSET_OFFSET = 12;
		private const int HEADER_APP_ENUM_OFFSET_OFFSET = 16;

		private const int APP_ENTRY_NAME_OFFSET = 4;
		private const int APP_ENTRY_FRAMES_OFFSET = 276;
		private const int APP_ENTRY_FRAME_TIME_OFFSET = 280;

		private MemoryMappedFile _memoryMappedFile;
		private MemoryMappedViewAccessor _accessor;

		public override string ProviderName => "RivaTuner";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.FPS,
			Priority = 5,
			RequiresExternalApp = true
		};

		protected override bool InitializeInternal()
		{
            try
            {
				_memoryMappedFile = MemoryMappedFile.OpenExisting(RTSS_SHARED_MEMORY_NAME, MemoryMappedFileRights.Read);
				_accessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

				if (_accessor.ReadUInt32(0) != RTSS_SIGNATURE) return false;

				logger.Info($"[{ProviderName}] Successfully connected to RTSS Shared Memory.");
				return true;
			}
			catch (Exception)
            {
				logger.Warn($"[{ProviderName}] RTSS not found or not running.");
				return false;
			}
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			var metrics = new HardwareMetrics();
			try
			{
				if (_accessor == null) return metrics;

				uint entrySize = _accessor.ReadUInt32(HEADER_APP_ENTRY_SIZE_OFFSET);
				uint entryOffset = _accessor.ReadUInt32(HEADER_APP_ENTRY_OFFSET_OFFSET);
				uint appCount = _accessor.ReadUInt32(HEADER_APP_ENUM_OFFSET_OFFSET);

				uint maxFrames = 0;
				float detectedFps = 0;
				string activeProcess = "Unknown";

				for (uint i = 0; i < appCount; i++)
				{
					long baseOffset = entryOffset + (i * entrySize);

					// Total frames rendered by this entry
					uint dwFrames = _accessor.ReadUInt32(baseOffset + APP_ENTRY_FRAMES_OFFSET);

					// We look for the entry with the highest frame count (the active game)
					if (dwFrames > maxFrames)
					{
						uint dwFrameTime = _accessor.ReadUInt32(baseOffset + APP_ENTRY_FRAME_TIME_OFFSET);

						if (dwFrameTime > 0)
						{
							maxFrames = dwFrames;
							detectedFps = 1000000f / dwFrameTime;

							// Debug: Get process name
							byte[] nameBuffer = new byte[260];
							_accessor.ReadArray(baseOffset + APP_ENTRY_NAME_OFFSET, nameBuffer, 0, 260);
							activeProcess = Encoding.ASCII.GetString(nameBuffer).Split('\0')[0];
						}
					}
				}

				if (detectedFps > 0)
				{
					metrics.FPS = (int)Math.Round(detectedFps);
					// Log only if you need to debug which app is being tracked
					// logger.Debug($"[{ProviderName}] Tracking {activeProcess} at {metrics.FPS} FPS");
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] Error polling RTSS metrics");
			}

			return metrics;
		}

		protected override void DisposeInternal()
		{
			_accessor?.Dispose();
			_memoryMappedFile?.Dispose();
		}
	}
}