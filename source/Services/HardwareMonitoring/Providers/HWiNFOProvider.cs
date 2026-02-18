using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// HWiNFOProvider.cs - Provider HWiNFO (Memory-mapped file)
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Providers
{
	public class HWiNFOProvider : BaseHardwareProvider
	{
		private HWiNFODumper _hwinfo;
		private readonly HWiNFOConfiguration _config;

		public override string ProviderName => "HWiNFO";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.FPS | MetricType.CpuUsage | MetricType.CpuTemperature |
							 MetricType.CpuPower | MetricType.GpuUsage | MetricType.GpuTemperature |
							 MetricType.GpuPower,
			Priority = 4,
			RequiresExternalApp = true,
			RequiresAdminRights = false
		};

		public HWiNFOProvider(HWiNFOConfiguration config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
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
				if (_config.FPS_SensorsID != null &&
					sensorsID.Equals(_config.FPS_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.FPS = GetSensorValue(sensorObj, _config.FPS_ElementID);
				}

				// GPU Usage
				if (_config.GPU_SensorsID != null &&
					sensorsID.Equals(_config.GPU_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.GpuUsage = GetSensorValue(sensorObj, _config.GPU_ElementID);
				}

				// GPU Temperature
				if (_config.GPUT_SensorsID != null &&
					sensorsID.Equals(_config.GPUT_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.GpuTemperature = GetSensorValue(sensorObj, _config.GPUT_ElementID);
				}

				// CPU Temperature
				if (_config.CPUT_SensorsID != null &&
					sensorsID.Equals(_config.CPUT_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.CpuTemperature = GetSensorValue(sensorObj, _config.CPUT_ElementID);
				}

				// GPU Power
				if (_config.GPUP_SensorsID != null &&
					sensorsID.Equals(_config.GPUP_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.GpuPower = GetSensorValue(sensorObj, _config.GPUP_ElementID);
				}

				// CPU Power
				if (_config.CPUP_SensorsID != null &&
					sensorsID.Equals(_config.CPUP_SensorsID, StringComparison.OrdinalIgnoreCase))
				{
					metrics.CpuPower = GetSensorValue(sensorObj, _config.CPUP_ElementID);
				}
			}

			return metrics;
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
		public string GPU_SensorsID { get; set; }
		public string GPU_ElementID { get; set; }
		public string GPUT_SensorsID { get; set; }
		public string GPUT_ElementID { get; set; }
		public string CPUT_SensorsID { get; set; }
		public string CPUT_ElementID { get; set; }
		public string GPUP_SensorsID { get; set; }
		public string GPUP_ElementID { get; set; }
		public string CPUP_SensorsID { get; set; }
		public string CPUP_ElementID { get; set; }
	}
}