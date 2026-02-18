using GameActivity.Services.HardwareMonitoring.Core;
using GameActivity.Services.HardwareMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// ProviderHealthMonitor.cs - Monitor provider health over time
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Utilities
{
	/// <summary>
	/// Monitors provider health and provides recommendations
	/// </summary>
	public class ProviderHealthMonitor
	{
		private readonly Dictionary<string, ProviderHealthStats> _healthStats;
		private readonly HardwareDataAggregator _aggregator;

		public ProviderHealthMonitor(HardwareDataAggregator aggregator)
		{
			_aggregator = aggregator;
			_healthStats = new Dictionary<string, ProviderHealthStats>();
		}

		/// <summary>
		/// Record a monitoring cycle
		/// </summary>
		public void RecordCycle(HardwareMetrics metrics)
		{
			var status = _aggregator.GetProviderStatus();

			foreach (var kvp in status)
			{
				if (!_healthStats.ContainsKey(kvp.Key))
				{
					_healthStats[kvp.Key] = new ProviderHealthStats { ProviderName = kvp.Key };
				}

				var stats = _healthStats[kvp.Key];
				stats.TotalCycles++;

				if (kvp.Value.IsAvailable && !kvp.Value.IsInFallback)
				{
					stats.SuccessfulCycles++;
				}

				stats.CurrentFailureCount = kvp.Value.FailureCount;
				stats.IsInFallback = kvp.Value.IsInFallback;
			}
		}

		/// <summary>
		/// Get health statistics for all providers
		/// </summary>
		public Dictionary<string, ProviderHealthStats> GetHealthStats()
		{
			return new Dictionary<string, ProviderHealthStats>(_healthStats);
		}

		/// <summary>
		/// Get recommendations based on health statistics
		/// </summary>
		public List<string> GetRecommendations()
		{
			var recommendations = new List<string>();

			foreach (var stats in _healthStats.Values)
			{
				double successRate = stats.TotalCycles > 0
					? (double)stats.SuccessfulCycles / stats.TotalCycles
					: 0;

				if (stats.IsInFallback)
				{
					recommendations.Add($"{stats.ProviderName} is in fallback mode. " +
									  $"Check if the application is running and configured correctly.");
				}
				else if (successRate < 0.8 && stats.TotalCycles > 10)
				{
					recommendations.Add($"{stats.ProviderName} has low reliability ({successRate:P0}). " +
									  $"Consider checking its configuration or using an alternative provider.");
				}
				else if (stats.CurrentFailureCount > 0 && successRate > 0.95)
				{
					recommendations.Add($"{stats.ProviderName} has recovered from recent failures. " +
									  $"Monitoring for stability.");
				}
			}

			// Overall recommendations
			int activeProviders = _healthStats.Values.Count(s => !s.IsInFallback && s.SuccessfulCycles > 0);

			if (activeProviders == 0)
			{
				recommendations.Add("WARNING: No providers are currently active. " +
								  "Hardware monitoring may not be functioning correctly.");
			}
			else if (activeProviders == 1)
			{
				recommendations.Add("INFO: Only one provider is active. " +
								  "Consider enabling additional providers for redundancy.");
			}

			return recommendations;
		}

		/// <summary>
		/// Reset health statistics
		/// </summary>
		public void Reset()
		{
			_healthStats.Clear();
		}
	}

	public class ProviderHealthStats
	{
		public string ProviderName { get; set; }
		public int TotalCycles { get; set; }
		public int SuccessfulCycles { get; set; }
		public int CurrentFailureCount { get; set; }
		public bool IsInFallback { get; set; }

		public double SuccessRate => TotalCycles > 0
			? (double)SuccessfulCycles / TotalCycles
			: 0;
	}
}