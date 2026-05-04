using GameActivity;
using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;

// ============================================================================
// HWiNFOProvider.cs - Provider HWiNFO (Memory-mapped file)
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Providers
{
	public class HWiNFOProvider : BaseHardwareProvider
	{
		private HWiNFODumper _hwinfo;

		public override string ProviderName => "HWiNFO";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.FPS |
							 MetricType.Framerate1PercentLow | MetricType.Framerate0Point1PercentLow |
							 MetricType.CpuUsage | MetricType.CpuTemperature | MetricType.CpuPower |
							 MetricType.GpuUsage | MetricType.GpuTemperature | MetricType.GpuPower |
							 MetricType.RamUsage,
			Priority = 4,
			RequiresExternalApp = true,
			RequiresAdminRights = false
		};

		public HWiNFOProvider()
		{
		}

		private HWiNFOConfiguration BuildHwInfoConfigurationFromLiveSettings()
		{
			GameActivitySettings s = LivePluginSettings;
			if (s == null)
			{
				return new HWiNFOConfiguration();
			}
			return new HWiNFOConfiguration
			{
				FPS_SensorsID = s.HWiNFO_fps_sensorsID,
				FPS_ElementID = s.HWiNFO_fps_elementID,
				FPS_InstanceID = s.HWiNFO_fps_instanceID,
				FPS1PercentLow_SensorsID = s.HWiNFO_fps1PercentLow_sensorsID,
				FPS1PercentLow_ElementID = s.HWiNFO_fps1PercentLow_elementID,
				FPS1PercentLow_InstanceID = s.HWiNFO_fps1PercentLow_instanceID,
				FPS0Point1PercentLow_SensorsID = s.HWiNFO_fps0Point1PercentLow_sensorsID,
				FPS0Point1PercentLow_ElementID = s.HWiNFO_fps0Point1PercentLow_elementID,
				FPS0Point1PercentLow_InstanceID = s.HWiNFO_fps0Point1PercentLow_instanceID,
				GPU_SensorsID = s.HWiNFO_gpu_sensorsID,
				GPU_ElementID = s.HWiNFO_gpu_elementID,
				GPU_InstanceID = s.HWiNFO_gpu_instanceID,
				GPUT_SensorsID = s.HWiNFO_gpuT_sensorsID,
				GPUT_ElementID = s.HWiNFO_gpuT_elementID,
				GPUT_InstanceID = s.HWiNFO_gpuT_instanceID,
				CPU_SensorsID = s.HWiNFO_cpu_sensorsID,
				CPU_ElementID = s.HWiNFO_cpu_elementID,
				CPU_InstanceID = s.HWiNFO_cpu_instanceID,
				CPUT_SensorsID = s.HWiNFO_cpuT_sensorsID,
				CPUT_ElementID = s.HWiNFO_cpuT_elementID,
				CPUT_InstanceID = s.HWiNFO_cpuT_instanceID,
				GPUP_SensorsID = s.HWiNFO_gpuP_sensorsID,
				GPUP_ElementID = s.HWiNFO_gpuP_elementID,
				GPUP_InstanceID = s.HWiNFO_gpuP_instanceID,
				CPUP_SensorsID = s.HWiNFO_cpuP_sensorsID,
				CPUP_ElementID = s.HWiNFO_cpuP_elementID,
				CPUP_InstanceID = s.HWiNFO_cpuP_instanceID,
				RAM_SensorsID = s.HWiNFO_ram_sensorsID,
				RAM_ElementID = s.HWiNFO_ram_elementID,
				RAM_InstanceID = s.HWiNFO_ram_instanceID
			};
		}

		protected override bool InitializeInternal()
		{
			try
			{
				_hwinfo = new HWiNFODumper();
				var testData = _hwinfo.ReadMem();
				return testData != null && testData.Count > 0;
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"[{ProviderName}] HWiNFO shared memory not accessible");
				return false;
			}
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			var metrics = new HardwareMetrics();
			HWiNFOConfiguration config = BuildHwInfoConfigurationFromLiveSettings();
			var data = _hwinfo.ReadMem();

			if (data == null || data.Count == 0)
			{
				return metrics;
			}

			foreach (var sensorItems in data)
			{
				dynamic sensorObj = Serialization.FromJson<dynamic>(Serialization.ToJson(sensorItems));
				string sensorsID = "0x" + ((uint)sensorObj["szSensorSensorID"]).ToString("X");

				// FPS
				if (config.FPS_SensorsID != null &&
					sensorsID.Equals(config.FPS_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.FPS_InstanceID))
				{
					metrics.FPS = GetSensorValue(sensorObj, config.FPS_ElementID);
				}
				if (config.FPS1PercentLow_SensorsID != null &&
					sensorsID.Equals(config.FPS1PercentLow_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.FPS1PercentLow_InstanceID))
				{
					metrics.FPS1PercentLow = GetSensorValue(sensorObj, config.FPS1PercentLow_ElementID);
				}
				if (config.FPS0Point1PercentLow_SensorsID != null &&
					sensorsID.Equals(config.FPS0Point1PercentLow_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.FPS0Point1PercentLow_InstanceID))
				{
					metrics.FPS0Point1PercentLow = GetSensorValue(sensorObj, config.FPS0Point1PercentLow_ElementID);
				}

				// GPU Usage
				if (config.GPU_SensorsID != null &&
					sensorsID.Equals(config.GPU_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.GPU_InstanceID))
				{
					metrics.GpuUsage = GetSensorValue(sensorObj, config.GPU_ElementID);
				}

				// GPU Temperature
				if (config.GPUT_SensorsID != null &&
					sensorsID.Equals(config.GPUT_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.GPUT_InstanceID))
				{
					metrics.GpuTemperature = GetSensorValue(sensorObj, config.GPUT_ElementID);
				}

				// CPU Temperature
				if (config.CPUT_SensorsID != null &&
					sensorsID.Equals(config.CPUT_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.CPUT_InstanceID))
				{
					metrics.CpuTemperature = GetSensorValue(sensorObj, config.CPUT_ElementID);
				}

				// GPU Power
				if (config.GPUP_SensorsID != null &&
					sensorsID.Equals(config.GPUP_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.GPUP_InstanceID))
				{
					metrics.GpuPower = GetSensorValue(sensorObj, config.GPUP_ElementID);
				}
				if (config.CPU_SensorsID != null &&
					sensorsID.Equals(config.CPU_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.CPU_InstanceID))
				{
					metrics.CpuUsage = GetSensorValue(sensorObj, config.CPU_ElementID);
				}

				// CPU Power
				if (config.CPUP_SensorsID != null &&
					sensorsID.Equals(config.CPUP_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.CPUP_InstanceID))
				{
					metrics.CpuPower = GetSensorValue(sensorObj, config.CPUP_ElementID);
				}
				if (config.RAM_SensorsID != null &&
					sensorsID.Equals(config.RAM_SensorsID, StringComparison.OrdinalIgnoreCase) &&
					MatchesConfiguredSensorInstance(sensorObj, config.RAM_InstanceID))
				{
					metrics.RamUsage = GetSensorValue(sensorObj, config.RAM_ElementID);
				}
			}

			return metrics;
		}

		/// <summary>
		/// When <paramref name="configuredInstanceId"/> is empty, any sensor header matches (legacy behavior).
		/// Otherwise the header's <c>szSensorInst</c> must equal the configured instance (decimal or 0x hex).
		/// </summary>
		private static bool MatchesConfiguredSensorInstance(dynamic sensorObj, string configuredInstanceId)
		{
			if (string.IsNullOrWhiteSpace(configuredInstanceId))
			{
				return true;
			}

			string actual = GetSensorInstanceRawString(sensorObj);
			return SensorInstanceIdsEqual(configuredInstanceId.Trim(), actual);
		}

		private static string GetSensorInstanceRawString(dynamic sensorObj)
		{
			if (sensorObj == null)
			{
				return string.Empty;
			}

			try
			{
				object inst = sensorObj["szSensorInst"];
				return inst == null ? string.Empty : inst.ToString();
			}
			catch
			{
				return string.Empty;
			}
		}

		private static bool SensorInstanceIdsEqual(string configured, string actual)
		{
			uint? c = TryParseHwInfoUInt(configured);
			uint? a = TryParseHwInfoUInt(actual);
			if (c.HasValue && a.HasValue)
			{
				return c.Value == a.Value;
			}

			return string.Equals(configured, actual, StringComparison.OrdinalIgnoreCase);
		}

		private static uint? TryParseHwInfoUInt(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			string s = value.Trim();
			if (s.Length >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
			{
				string hex = s.Substring(2);
				uint v;
				if (uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out v))
				{
					return v;
				}

				return null;
			}

			uint dec;
			if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out dec))
			{
				return dec;
			}

			return null;
		}

		private int? GetSensorValue(dynamic sensorObj, string elementID)
		{
			if (string.IsNullOrEmpty(elementID))
				return null;

			foreach (dynamic item in sensorObj["sensors"])
			{
				dynamic itemObj = Serialization.FromJson<dynamic>(Serialization.ToJson(item));
				string dataID = "0x" + ((uint)itemObj["dwSensorID"]).ToString("X");

				if (dataID.Equals(elementID, StringComparison.OrdinalIgnoreCase))
				{
					return (int)Math.Round((double)itemObj["Value"]);
				}
			}

			return null;
		}
	}

	public class HWiNFOConfiguration
	{
		public string FPS_SensorsID { get; set; }
		public string FPS_ElementID { get; set; }
		public string FPS_InstanceID { get; set; }
		public string FPS1PercentLow_SensorsID { get; set; }
		public string FPS1PercentLow_ElementID { get; set; }
		public string FPS1PercentLow_InstanceID { get; set; }
		public string FPS0Point1PercentLow_SensorsID { get; set; }
		public string FPS0Point1PercentLow_ElementID { get; set; }
		public string FPS0Point1PercentLow_InstanceID { get; set; }
		public string GPU_SensorsID { get; set; }
		public string GPU_ElementID { get; set; }
		public string GPU_InstanceID { get; set; }
		public string CPU_SensorsID { get; set; }
		public string CPU_ElementID { get; set; }
		public string CPU_InstanceID { get; set; }
		public string GPUT_SensorsID { get; set; }
		public string GPUT_ElementID { get; set; }
		public string GPUT_InstanceID { get; set; }
		public string CPUT_SensorsID { get; set; }
		public string CPUT_ElementID { get; set; }
		public string CPUT_InstanceID { get; set; }
		public string GPUP_SensorsID { get; set; }
		public string GPUP_ElementID { get; set; }
		public string GPUP_InstanceID { get; set; }
		public string CPUP_SensorsID { get; set; }
		public string CPUP_ElementID { get; set; }
		public string CPUP_InstanceID { get; set; }
		public string RAM_SensorsID { get; set; }
		public string RAM_ElementID { get; set; }
		public string RAM_InstanceID { get; set; }
	}
}
