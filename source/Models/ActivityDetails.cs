using Playnite.SDK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameActivity.Models
{
	/// <summary>
	/// Represents a collection of activity details grouped by session date.
	/// </summary>
	public class ActivityDetails : ObservableObject
	{
		/// <summary>
		/// Gets or sets the dictionary of activity details indexed by session date.
		/// </summary>
		public ConcurrentDictionary<DateTime, List<ActivityDetailsData>> Items { get; set; } = new ConcurrentDictionary<DateTime, List<ActivityDetailsData>>();

		/// <summary>
		/// Gets the number of session dates in the collection.
		/// </summary>
		[DontSerialize]
		public int Count => Items.Count;

		/// <summary>
		/// Gets the activity details for a specific session date.
		/// </summary>
		/// <param name="dateSession">The session date to retrieve.</param>
		/// <returns>List of activity details for the specified date, or an empty list if not found.</returns>
		[DontSerialize]
		public List<ActivityDetailsData> this[DateTime dateSession] => Get(dateSession);

		/// <summary>
		/// Gets the average FPS across all sessions.
		/// Returns 0 if no FPS data is available.
		/// </summary>
		[DontSerialize]
		public int AvgFpsAllSession => GetAvgFpsAllSession();

		/// <summary>
		/// Retrieves the activity details for a specific session date.
		/// </summary>
		/// <param name="dateSession">The session date to retrieve.</param>
		/// <returns>List of activity details for the specified date, or an empty list if not found.</returns>
		public List<ActivityDetailsData> Get(DateTime dateSession)
		{
			return Items.TryGetValue(dateSession, out List<ActivityDetailsData> item)
				? item
				: new List<ActivityDetailsData>();
		}

		/// <summary>
		/// Calculates the average FPS across all sessions.
		/// </summary>
		/// <returns>The average FPS value, or 0 if no valid FPS data exists.</returns>
		private int GetAvgFpsAllSession()
		{
			int totalFps = 0;
			int count = 0;

			foreach (KeyValuePair<DateTime, List<ActivityDetailsData>> item in Items)
			{
				foreach (ActivityDetailsData data in item.Value)
				{
					if (data.FPS > 0)
					{
						totalFps += data.FPS;
						count++;
					}
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