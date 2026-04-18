using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// MonitoringDiagnostics.cs - Diagnostic utilities for hardware monitoring
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Utilities
{
	/// <summary>
	/// Provides diagnostic and debugging tools for the monitoring system
	/// </summary>
	public class MonitoringDiagnostics
	{
		private static readonly ILogger logger = LogManager.GetLogger();
		private readonly HardwareDataAggregator _aggregator;

		public MonitoringDiagnostics(HardwareDataAggregator aggregator)
		{
			_aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
		}

		/// <summary>
		/// Run a comprehensive diagnostic of all providers
		/// </summary>
		public DiagnosticReport RunDiagnostics()
		{
			var report = new DiagnosticReport
			{
				Timestamp = DateTime.Now.ToLocalTime(),
				ProviderReports = new List<ProviderDiagnostic>()
			};

			var providerStatus = _aggregator.GetProviderStatus();

			foreach (var kvp in providerStatus)
			{
				var diagnostic = new ProviderDiagnostic
				{
					ProviderName = kvp.Key,
					IsAvailable = kvp.Value.IsAvailable,
					FailureCount = kvp.Value.FailureCount,
					IsInFallback = kvp.Value.IsInFallback,
					SupportedMetrics = kvp.Value.Capabilities.SupportedMetrics,
					Priority = kvp.Value.Capabilities.Priority,
					RequiresExternalApp = kvp.Value.Capabilities.RequiresExternalApp,
					RequiresAdminRights = kvp.Value.Capabilities.RequiresAdminRights
				};

				// Test the provider
				if (kvp.Value.IsAvailable && !kvp.Value.IsInFallback)
				{
					try
					{
						var metrics = _aggregator.GetMetrics(true);
						diagnostic.TestSuccessful = true;
						diagnostic.CollectedMetrics = CountProviderMetrics(metrics, kvp.Key);
					}
					catch (Exception ex)
					{
						diagnostic.TestSuccessful = false;
						diagnostic.ErrorMessage = ex.Message;
					}
				}

				report.ProviderReports.Add(diagnostic);
			}

			return report;
		}

		/// <summary>
		/// Generate a detailed report as formatted text
		/// </summary>
		public string GenerateTextReport()
		{
			var report = RunDiagnostics();
			var sb = new StringBuilder();

			sb.AppendLine("╔════════════════════════════════════════════════════════════╗");
			sb.AppendLine("║     HARDWARE MONITORING DIAGNOSTIC REPORT                  ║");
			sb.AppendLine("╚════════════════════════════════════════════════════════════╝");
			sb.AppendLine();
			sb.AppendLine($"Timestamp: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine();

			foreach (var provider in report.ProviderReports.OrderByDescending(p => p.Priority))
			{
				sb.AppendLine($"┌─ {provider.ProviderName} (Priority: {provider.Priority})");
				sb.AppendLine($"│  Status: {(provider.IsAvailable ? "✓ Available" : "✗ Not Available")}");

				if (provider.IsInFallback)
				{
					sb.AppendLine($"│  ⚠ IN FALLBACK MODE (Failures: {provider.FailureCount})");
				}
				else if (provider.FailureCount > 0)
				{
					sb.AppendLine($"│  ⚠ Failures: {provider.FailureCount}");
				}

				sb.AppendLine($"│  Requires External App: {provider.RequiresExternalApp}");
				sb.AppendLine($"│  Requires Admin: {provider.RequiresAdminRights}");
				sb.AppendLine($"│  Supported Metrics: {GetMetricsList(provider.SupportedMetrics)}");

				if (provider.IsAvailable)
				{
					if (provider.TestSuccessful)
					{
						sb.AppendLine($"│  Test Result: ✓ Success ({provider.CollectedMetrics} metrics collected)");
					}
					else
					{
						sb.AppendLine($"│  Test Result: ✗ Failed - {provider.ErrorMessage}");
					}
				}

				sb.AppendLine("└─");
				sb.AppendLine();
			}

			// Summary
			int availableProviders = report.ProviderReports.Count(p => p.IsAvailable);
			int fallbackProviders = report.ProviderReports.Count(p => p.IsInFallback);

			sb.AppendLine("═══════════════════════════════════════════════════════════");
			sb.AppendLine($"SUMMARY: {availableProviders} available, {fallbackProviders} in fallback");
			sb.AppendLine("═══════════════════════════════════════════════════════════");

			return sb.ToString();
		}

		/// <summary>
		/// Count metrics collected by a specific provider
		/// </summary>
		private int CountProviderMetrics(HardwareMetrics metrics, string providerName)
		{
			int count = 0;
			if (metrics.FPS.HasValue && metrics.Source.FPS == providerName) count++;
			if (metrics.FPS1PercentLow.HasValue && metrics.Source.FPS1PercentLow == providerName) count++;
			if (metrics.FPS0Point1PercentLow.HasValue && metrics.Source.FPS0Point1PercentLow == providerName) count++;
			if (metrics.CpuUsage.HasValue && metrics.Source.CpuUsage == providerName) count++;
			if (metrics.CpuTemperature.HasValue && metrics.Source.CpuTemperature == providerName) count++;
			if (metrics.CpuPower.HasValue && metrics.Source.CpuPower == providerName) count++;
			if (metrics.GpuUsage.HasValue && metrics.Source.GpuUsage == providerName) count++;
			if (metrics.GpuTemperature.HasValue && metrics.Source.GpuTemperature == providerName) count++;
			if (metrics.GpuPower.HasValue && metrics.Source.GpuPower == providerName) count++;
			if (metrics.RamUsage.HasValue && metrics.Source.RamUsage == providerName) count++;
			return count;
		}

		private string GetMetricsList(MetricType metrics)
		{
			var list = new List<string>();

			if ((metrics & MetricType.FPS) == MetricType.FPS) list.Add("FPS");
			if ((metrics & MetricType.Framerate1PercentLow) == MetricType.Framerate1PercentLow) list.Add("FPS 1% Low");
			if ((metrics & MetricType.Framerate0Point1PercentLow) == MetricType.Framerate0Point1PercentLow) list.Add("FPS 0.1% Low");
			if ((metrics & MetricType.CpuUsage) == MetricType.CpuUsage) list.Add("CPU");
			if ((metrics & MetricType.CpuTemperature) == MetricType.CpuTemperature) list.Add("CPU Temp");
			if ((metrics & MetricType.CpuPower) == MetricType.CpuPower) list.Add("CPU Power");
			if ((metrics & MetricType.GpuUsage) == MetricType.GpuUsage) list.Add("GPU");
			if ((metrics & MetricType.GpuTemperature) == MetricType.GpuTemperature) list.Add("GPU Temp");
			if ((metrics & MetricType.GpuPower) == MetricType.GpuPower) list.Add("GPU Power");
			if ((metrics & MetricType.RamUsage) == MetricType.RamUsage) list.Add("RAM");

			return list.Count > 0 ? string.Join(", ", list) : "None";
		}

		/// <summary>
		/// Log diagnostic report to Playnite logger
		/// </summary>
		public void LogDiagnostics()
		{
			var report = GenerateTextReport();
			logger.Info("Diagnostics:\n" + report);
		}
	}

	public class DiagnosticReport
	{
		public DateTime Timestamp { get; set; }
		public List<ProviderDiagnostic> ProviderReports { get; set; }
	}

	public class ProviderDiagnostic
	{
		public string ProviderName { get; set; }
		public bool IsAvailable { get; set; }
		public int FailureCount { get; set; }
		public bool IsInFallback { get; set; }
		public MetricType SupportedMetrics { get; set; }
		public int Priority { get; set; }
		public bool RequiresExternalApp { get; set; }
		public bool RequiresAdminRights { get; set; }
		public bool TestSuccessful { get; set; }
		public int CollectedMetrics { get; set; }
		public string ErrorMessage { get; set; }
	}
}