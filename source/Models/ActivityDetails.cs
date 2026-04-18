using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameActivity.Models
{
	/// <summary>
	/// Represents details values for a single activity session.
	/// </summary>
	public class ActivityDetails : ObservableObject
	{
		/// <summary>
		/// Gets or sets the details entries of one session.
		/// </summary>
		public List<ActivityDetailsData> Items { get; set; } = new List<ActivityDetailsData>();

		/// <summary>
		/// Gets the number of detail entries in the session.
		/// </summary>
		[DontSerialize]
		public int Count => Items?.Count ?? 0;

		/// <summary>
		/// Gets the average FPS across all sessions.
		/// Returns 0 if no FPS data is available.
		/// </summary>
		[DontSerialize]
		public int AvgFpsAllSession => GetAvgFpsAllSession();

		/// <summary>
		/// Calculates the average FPS across all sessions.
		/// </summary>
		/// <returns>The average FPS value, or 0 if no valid FPS data exists.</returns>
		private int GetAvgFpsAllSession()
		{
			int totalFps = 0;
			int count = 0;

			foreach (ActivityDetailsData data in Items ?? Enumerable.Empty<ActivityDetailsData>())
			{
				if (data.FPS > 0)
				{
					totalFps += data.FPS;
					count++;
				}
			}

			// Avoid division by zero
			return count > 0 ? totalFps / count : 0;
		}
	}

	/// <summary>
	/// Represents performance metrics data captured at a specific point in time.
	/// </summary>
	public class ActivityDetailsData
	{
		/// <summary>
		/// Gets or sets the timestamp when this data was logged.
		/// </summary>
		[SerializationPropertyName("datelog")]
		public DateTime? Datelog { get; set; }

		/// <summary>
		/// Gets or sets the frames per second (FPS) value.
		/// </summary>
		[SerializationPropertyName("fps")]
		public int FPS { get; set; }

		/// <summary>
		/// Gets or sets the Framerate 1% Low value (FPS) when provided by a source such as MSI Afterburner MAHM.
		/// </summary>
		[SerializationPropertyName("fps1l")]
		public int FPS1PercentLow { get; set; }

		/// <summary>
		/// Gets or sets the Framerate 0.1% Low value (FPS) when provided by a source such as MSI Afterburner MAHM.
		/// </summary>
		[SerializationPropertyName("fps01l")]
		public int FPS0Point1PercentLow { get; set; }

		/// <summary>
		/// Gets or sets the CPU usage percentage.
		/// </summary>
		[SerializationPropertyName("cpu")]
		public int CPU { get; set; }

		/// <summary>
		/// Gets or sets the GPU usage percentage.
		/// </summary>
		[SerializationPropertyName("gpu")]
		public int GPU { get; set; }

		/// <summary>
		/// Gets or sets the RAM usage in megabytes.
		/// </summary>
		[SerializationPropertyName("ram")]
		public int RAM { get; set; }

		/// <summary>
		/// Gets or sets the CPU temperature in degrees Celsius.
		/// </summary>
		[SerializationPropertyName("cpuT")]
		public int CPUT { get; set; }

		/// <summary>
		/// Gets or sets the GPU temperature in degrees Celsius.
		/// </summary>
		[SerializationPropertyName("gpuT")]
		public int GPUT { get; set; }

		/// <summary>
		/// Gets or sets the CPU power consumption in watts.
		/// </summary>
		[SerializationPropertyName("cpuP")]
		public int CPUP { get; set; }

		/// <summary>
		/// Gets or sets the GPU power consumption in watts.
		/// </summary>
		[SerializationPropertyName("gpuP")]
		public int GPUP { get; set; }
	}
}