using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GameActivity.Services.HardwareMonitoring.Providers
{
	public class LibreHardwareProvider : BaseHardwareProvider
	{
		private readonly string _remoteIp;
		private readonly bool _useRemote;
		private static readonly Dictionary<string, SensorPath[]> _sensorPaths = InitializeSensorPaths();

		public override string ProviderName => "LibreHardware";

		public override ProviderCapabilities Capabilities => new ProviderCapabilities
		{
			SupportedMetrics = MetricType.CpuUsage | MetricType.CpuTemperature | MetricType.CpuPower |
							 MetricType.GpuUsage | MetricType.GpuTemperature | MetricType.GpuPower |
							 MetricType.RamUsage,
			Priority = 5,
			RequiresExternalApp = true,
			RequiresAdminRights = false
		};

		public LibreHardwareProvider(string remoteIp = null)
		{
			_remoteIp = remoteIp;
			_useRemote = !string.IsNullOrEmpty(remoteIp);
		}

		protected override bool InitializeInternal()
		{
			if (_useRemote)
			{
				try
				{
					var testData = GetRemoteData();
					return testData != null;
				}
				catch
				{
					logger.Warn($"[{ProviderName}] Remote server at {_remoteIp} is not accessible");
					return false;
				}
			}

			return true;
		}

		protected override HardwareMetrics GetMetricsInternal()
		{
			if (_useRemote)
			{
				return GetRemoteMetrics();
			}

			return new HardwareMetrics();
		}

		private LibreHardwareData GetRemoteData()
		{
			string url = $"http://{_remoteIp}/data.json";
			string webData = Web.DownloadStringData(url).GetAwaiter().GetResult();
			Serialization.TryFromJson(webData, out LibreHardwareData data);
			return data;
		}

		private static Dictionary<string, SensorPath[]> InitializeSensorPaths()
		{
			return new Dictionary<string, SensorPath[]>
			{
				{
					"CpuPower", new[]
					{
						new SensorPath { CategoryPath = new[] { "Powers" }, SensorName = "CPU Package", Unit = "W" },
						new SensorPath { CategoryPath = new[] { "Powers" }, SensorName = "Package", Unit = "W" }
					}
				},
				{
					"CpuUsage", new[]
					{
						new SensorPath { CategoryPath = new[] { "Load" }, SensorName = "CPU Total", Unit = "%" }
					}
				},
				{
					"CpuTemperature", new[]
					{
						new SensorPath { CategoryPath = new[] { "Temperatures" }, SensorName = "CPU Package", Unit = "°C", AlternateUnit = "°F" },
						new SensorPath { CategoryPath = new[] { "Temperatures" }, SensorName = "Core (Tctl/Tdie)", Unit = "°C", AlternateUnit = "°F" }
					}
				},
				{
					"RamUsage", new[]
					{
						new SensorPath { CategoryPath = new[] { "Load" }, SensorName = "Memory", Unit = "%" }
					}
				},
				{
					"GpuPower", new[]
					{
						// Dedicated GPUs first (priority)
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", ExcludeTextPatterns = new[] { "Intel", "UHD", "Graphics" }, CategoryPath = new[] { "Powers" }, SensorName = "GPU Package", Unit = "W" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", ExcludeTextPatterns = new[] { "Radeon(TM)", "Graphics", "Vega" }, CategoryPath = new[] { "Powers" }, SensorName = "GPU Package", Unit = "W" },
						// Integrated GPUs fallback
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", CategoryPath = new[] { "Powers" }, SensorName = "GPU Package", Unit = "W" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", CategoryPath = new[] { "Powers" }, SensorName = "GPU Package", Unit = "W" }
					}
				},
				{
					"GpuUsage", new[]
					{
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", ExcludeTextPatterns = new[] { "Intel", "UHD", "Graphics" }, CategoryPath = new[] { "Load" }, SensorName = "D3D 3D", Unit = "%" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", ExcludeTextPatterns = new[] { "Radeon(TM)", "Graphics", "Vega" }, CategoryPath = new[] { "Load" }, SensorName = "D3D 3D", Unit = "%" },
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", CategoryPath = new[] { "Load" }, SensorName = "D3D 3D", Unit = "%" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", CategoryPath = new[] { "Load" }, SensorName = "D3D 3D", Unit = "%" }
					}
				},
				{
					"GpuTemperature", new[]
					{
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", ExcludeTextPatterns = new[] { "Intel", "UHD", "Graphics" }, CategoryPath = new[] { "Temperatures" }, SensorName = "GPU Core", Unit = "°C", AlternateUnit = "°F" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", ExcludeTextPatterns = new[] { "Radeon(TM)", "Graphics", "Vega" }, CategoryPath = new[] { "Temperatures" }, SensorName = "GPU Core", Unit = "°C", AlternateUnit = "°F" },
						new SensorPath { HardwareIdContains = "/gpu-nvidia/", CategoryPath = new[] { "Temperatures" }, SensorName = "GPU Core", Unit = "°C", AlternateUnit = "°F" },
						new SensorPath { HardwareIdContains = "/gpu-amd/", CategoryPath = new[] { "Temperatures" }, SensorName = "GPU Core", Unit = "°C", AlternateUnit = "°F" }
					}
				}
			};
		}

		private HardwareMetrics GetRemoteMetrics()
		{
			var metrics = new HardwareMetrics();
			var data = GetRemoteData();

			if (data?.Children == null || data.Children.Count == 0)
			{
				return metrics;
			}

			var rootNode = data.Children[0];
			if (rootNode?.Children == null)
			{
				return metrics;
			}

			metrics.CpuPower = FindMetricValue(rootNode, "CpuPower");
			metrics.CpuUsage = FindMetricValue(rootNode, "CpuUsage");
			metrics.CpuTemperature = FindMetricValue(rootNode, "CpuTemperature");
			metrics.RamUsage = FindMetricValue(rootNode, "RamUsage");
			metrics.GpuPower = FindMetricValue(rootNode, "GpuPower");
			metrics.GpuUsage = FindMetricValue(rootNode, "GpuUsage");
			metrics.GpuTemperature = FindMetricValue(rootNode, "GpuTemperature");

			return metrics;
		}

		private int? FindMetricValue(Child rootNode, string metricKey)
		{
			if (!_sensorPaths.ContainsKey(metricKey))
			{
				return null;
			}

			foreach (var sensorPath in _sensorPaths[metricKey])
			{
				var value = FindSensorValue(rootNode, sensorPath);
				if (value.HasValue)
				{
					return value;
				}
			}

			return null;
		}

		private int? FindSensorValue(Child rootNode, SensorPath path)
		{
			var hardwareNodes = rootNode.Children;

			if (!string.IsNullOrEmpty(path.HardwareIdContains))
			{
				hardwareNodes = hardwareNodes.Where(n => n.HardwareId != null && n.HardwareId.Contains(path.HardwareIdContains)).ToList();
			}

			// Exclude integrated GPUs if pattern specified
			if (path.ExcludeTextPatterns != null && path.ExcludeTextPatterns.Length > 0)
			{
				hardwareNodes = hardwareNodes.Where(n =>
				{
					if (string.IsNullOrEmpty(n.Text))
					{
						return true;
					}

					foreach (var pattern in path.ExcludeTextPatterns)
					{
						if (n.Text.Contains(pattern))
						{
							return false;
						}
					}
					return true;
				}).ToList();
			}

			foreach (var hardwareNode in hardwareNodes)
			{
				if (hardwareNode.Children == null)
				{
					continue;
				}

				var currentNode = hardwareNode.Children.AsEnumerable();

				foreach (var category in path.CategoryPath)
				{
					var categoryNode = currentNode.FirstOrDefault(n => n.Text == category);
					if (categoryNode?.Children == null)
					{
						break;
					}
					currentNode = categoryNode.Children;
				}

				var sensorNode = currentNode.FirstOrDefault(n => n.Text == path.SensorName);
				if (sensorNode != null && !string.IsNullOrEmpty(sensorNode.Value))
				{
					var units = string.IsNullOrEmpty(path.AlternateUnit)
						? new[] { path.Unit }
						: new[] { path.Unit, path.AlternateUnit };

					return ParseValue(sensorNode.Value, units);
				}
			}

			return null;
		}

		private int? ParseValue(string value, params string[] suffixes)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}

			string cleaned = value
				.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
				.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
				.Trim();

			foreach (var suffix in suffixes)
			{
				cleaned = cleaned.Replace(suffix, string.Empty).Trim();
			}

			if (double.TryParse(cleaned, out double result))
			{
				return (int)Math.Round(result, 0);
			}

			return null;
		}

		private class SensorPath
		{
			public string HardwareIdContains { get; set; }
			public string[] ExcludeTextPatterns { get; set; }
			public string[] CategoryPath { get; set; }
			public string SensorName { get; set; }
			public string Unit { get; set; }
			public string AlternateUnit { get; set; }
		}
	}
}