using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// ProviderCapabilities.cs - Capabilities of each provider
// ============================================================================
namespace GameActivity.Services.HardwareMonitoring.Models
{
	public class ProviderCapabilities
	{
		public MetricType SupportedMetrics { get; set; }
		public int Priority { get; set; }
		public bool RequiresExternalApp { get; set; }
		public bool RequiresAdminRights { get; set; }

		public bool Supports(MetricType metric)
		{
			return (SupportedMetrics & metric) == metric;
		}
	}
}