// ============================================================================
// IHardwareDataProvider.cs - Common interface for all providers
// ============================================================================
using GameActivity.Services.HardwareMonitoring.Models;
using System;

namespace GameActivity.Services.HardwareMonitoring.Core
{
	public interface IHardwareDataProvider : IDisposable
	{
		string ProviderName { get; }
		ProviderCapabilities Capabilities { get; }
		bool IsAvailable { get; }
		int FailureCount { get; }

		bool Initialize();
		HardwareMetrics GetMetrics();
		void Reset();
	}
}