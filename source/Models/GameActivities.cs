using CommonPluginsShared.Collections;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameActivity.Models
{
	/// <summary>
	/// Represents a collection of game activities with performance details and session statistics.
	/// </summary>
	public class GameActivities : PluginGameCollection<Activity>
	{
		/// <summary>
		/// Transitional payload used only for one-shot migration from legacy schema.
		/// </summary>
		[SerializationPropertyName("ItemsDetails")]
		public LegacyActivityDetailsContainer LegacyItemsDetails { get; set; }

		private static ILogger Logger => LogManager.GetLogger();
		private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		/// <summary>
		/// Gets the list of activities filtered based on session time settings.
		/// Excludes sessions below the configured ignore threshold if enabled.
		/// </summary>
		[DontSerialize]
		public List<Activity> FilterItems
		{
			get
			{
				int timeThreshold = PluginDatabase.PluginSettings.IgnoreSession
					? PluginDatabase.PluginSettings.IgnoreSessionTime
					: 0;

				return Items.Where(x => (int)x.ElapsedSeconds > timeThreshold)
					.Distinct()
					.ToList();
			}
		}

		/// <summary>
		/// Gets the total playtime across all sessions in seconds.
		/// </summary>
		public ulong SessionPlaytime => (ulong)(Items?.Sum(x => (long)x.ElapsedSeconds) ?? 0);

		/// <summary>
		/// Gets the average FPS across all sessions.
		/// </summary>
		[DontSerialize]
		public int AvgFpsAllSession
		{
			get
			{
				List<int> fpsValues = GetAllSessionFpsValues();
				if (fpsValues.Count == 0)
				{
					return 0;
				}

				return (int)Math.Round(fpsValues.Average());
			}
		}

		/// <summary>
		/// Gets the minimum FPS observed across all sessions (only samples with FPS &gt; 0).
		/// </summary>
		[DontSerialize]
		public int MinFpsAllSession
		{
			get
			{
				List<int> fpsValues = GetAllSessionFpsValues();
				if (fpsValues.Count == 0)
				{
					return 0;
				}

				return fpsValues.Min();
			}
		}

		/// <summary>
		/// Gets the maximum FPS observed across all sessions (only samples with FPS &gt; 0).
		/// </summary>
		[DontSerialize]
		public int MaxFpsAllSession
		{
			get
			{
				List<int> fpsValues = GetAllSessionFpsValues();
				if (fpsValues.Count == 0)
				{
					return 0;
				}

				return fpsValues.Max();
			}
		}

		/// <summary>
		/// Gets the median FPS across all sessions (only samples with FPS &gt; 0).
		/// </summary>
		[DontSerialize]
		public int MedianFpsAllSession
		{
			get
			{
				return CalculateMedianFps(GetAllSessionFpsValues());
			}
		}

		/// <summary>
		/// Gets the sample standard deviation of FPS across all sessions (only samples with FPS &gt; 0; divisor <c>n - 1</c> when <c>n ≥ 2</c>).
		/// </summary>
		[DontSerialize]
		public int StdDevFpsAllSession
		{
			get
			{
				return CalculateSampleStdDevFps(GetAllSessionFpsValues());
			}
		}

		#region Performance Metrics Averages

		/// <summary>
		/// Calculates the average CPU usage for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average CPU usage percentage, or 0 if no data is available.</returns>
		public int AvgCPU(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.CPU);
		}

		/// <summary>
		/// Calculates the average GPU usage for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average GPU usage percentage, or 0 if no data is available.</returns>
		public int AvgGPU(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.GPU);
		}

		/// <summary>
		/// Calculates the average RAM usage for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average RAM usage in megabytes, or 0 if no data is available.</returns>
		public int AvgRAM(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.RAM);
		}

		/// <summary>
		/// Calculates the average FPS for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average frames per second, or 0 if no data is available.</returns>
		public int AvgFPS(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.FPS);
		}

		/// <summary>
		/// Gets the minimum FPS for a specific session (only samples with FPS &gt; 0).
		/// </summary>
		/// <param name="dateSession">The session date.</param>
		/// <returns>The minimum FPS, or 0 if no data is available.</returns>
		public int MinFPS(DateTime dateSession)
		{
			List<int> fpsValues = GetSessionFpsValues(dateSession);
			if (fpsValues.Count == 0)
			{
				return 0;
			}

			return fpsValues.Min();
		}

		/// <summary>
		/// Gets the maximum FPS for a specific session (only samples with FPS &gt; 0).
		/// </summary>
		/// <param name="dateSession">The session date.</param>
		/// <returns>The maximum FPS, or 0 if no data is available.</returns>
		public int MaxFPS(DateTime dateSession)
		{
			List<int> fpsValues = GetSessionFpsValues(dateSession);
			if (fpsValues.Count == 0)
			{
				return 0;
			}

			return fpsValues.Max();
		}

		/// <summary>
		/// Gets the median FPS for a specific session (only samples with FPS &gt; 0).
		/// </summary>
		/// <param name="dateSession">The session date.</param>
		/// <returns>The median FPS, or 0 if no data is available.</returns>
		public int MedianFPS(DateTime dateSession)
		{
			return CalculateMedianFps(GetSessionFpsValues(dateSession));
		}

		/// <summary>
		/// Gets the sample standard deviation of FPS for a specific session (only samples with FPS &gt; 0; divisor <c>n - 1</c> when <c>n ≥ 2</c>).
		/// </summary>
		/// <param name="dateSession">The session date.</param>
		/// <returns>The standard deviation in FPS (rounded), or 0 if fewer than two samples.</returns>
		public int StdDevFPS(DateTime dateSession)
		{
			return CalculateSampleStdDevFps(GetSessionFpsValues(dateSession));
		}

		/// <summary>
		/// Average Framerate 1% Low (FPS) for the session, using only samples with a value &gt; 0.
		/// </summary>
		public int AvgFPS1PercentLow(DateTime dateSession)
		{
			List<int> values = GetSessionPositiveIntValues(dateSession, d => d.FPS1PercentLow);
			if (values.Count == 0)
			{
				return 0;
			}

			return (int)Math.Round(values.Average());
		}

		/// <summary>
		/// Minimum Framerate 1% Low (FPS) for the session (worst among samples &gt; 0).
		/// </summary>
		public int MinFPS1PercentLow(DateTime dateSession)
		{
			List<int> values = GetSessionPositiveIntValues(dateSession, d => d.FPS1PercentLow);
			if (values.Count == 0)
			{
				return 0;
			}

			return values.Min();
		}

		/// <summary>
		/// Average Framerate 0.1% Low (FPS) for the session, using only samples with a value &gt; 0.
		/// </summary>
		public int AvgFPS0Point1PercentLow(DateTime dateSession)
		{
			List<int> values = GetSessionPositiveIntValues(dateSession, d => d.FPS0Point1PercentLow);
			if (values.Count == 0)
			{
				return 0;
			}

			return (int)Math.Round(values.Average());
		}

		/// <summary>
		/// Minimum Framerate 0.1% Low (FPS) for the session (worst among samples &gt; 0).
		/// </summary>
		public int MinFPS0Point1PercentLow(DateTime dateSession)
		{
			List<int> values = GetSessionPositiveIntValues(dateSession, d => d.FPS0Point1PercentLow);
			if (values.Count == 0)
			{
				return 0;
			}

			return values.Min();
		}

		/// <summary>
		/// Calculates the average CPU temperature for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average CPU temperature in degrees Celsius, or 0 if no data is available.</returns>
		public int AvgCPUT(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.CPUT);
		}

		/// <summary>
		/// Calculates the average GPU temperature for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average GPU temperature in degrees Celsius, or 0 if no data is available.</returns>
		public int AvgGPUT(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.GPUT);
		}

		/// <summary>
		/// Calculates the average CPU power consumption for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average CPU power consumption in watts, or 0 if no data is available.</returns>
		public int AvgCPUP(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.CPUP);
		}

		/// <summary>
		/// Calculates the average GPU power consumption for a specific session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <returns>The average GPU power consumption in watts, or 0 if no data is available.</returns>
		public int AvgGPUP(DateTime dateSession)
		{
			return CalculateAverage(dateSession, data => data.GPUP);
		}

		/// <summary>
		/// Helper method to calculate the average of a specific metric for a session.
		/// </summary>
		/// <param name="dateSession">The session date to calculate the average for.</param>
		/// <param name="selector">Function to select the metric value from activity details data.</param>
		/// <returns>The average value rounded to the nearest integer, or 0 if no data is available.</returns>
		private int CalculateAverage(DateTime dateSession, Func<ActivityDetailsData, int> selector)
		{
			List<ActivityDetailsData> data = GetActivityDetails(dateSession);
			if (data.Count == 0)
			{
				return 0;
			}

			decimal sum = data.Sum(selector);
			return (int)Math.Round(sum / data.Count);
		}

		#endregion

		/// <summary>
		/// Calculates the average playtime per session.
		/// </summary>
		/// <returns>The average playtime in seconds.</returns>
		public ulong AvgPlayTime()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			List<Activity> validActivities = Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.ToList();

			if (validActivities.Count == 0)
			{
				return 0;
			}

			ulong totalPlayTime = (ulong)validActivities.Sum(x => (long)x.ElapsedSeconds);
			return totalPlayTime / (ulong)validActivities.Count;
		}

		#region Session Retrieval Methods

		/// <summary>
		/// Gets the date of the first recorded session.
		/// </summary>
		/// <returns>The date of the first session, or current date if no sessions exist.</returns>
		public DateTime GetFirstSession()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.OrderBy(x => x.DateSession)
				.FirstOrDefault()?.DateSession ?? DateTime.Now;
		}

		/// <summary>
		/// Gets the date of the last recorded session.
		/// Note: Should not be used to get currently playing session.
		/// </summary>
		/// <returns>The date of the last session, or current date if no sessions exist.</returns>
		public DateTime GetLastSession()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.OrderByDescending(x => x.DateSession)
				.FirstOrDefault()?.DateSession ?? DateTime.Now;
		}

		/// <summary>
		/// Gets the date of a specific session based on selection criteria.
		/// </summary>
		/// <param name="dateSelected">The selected date to match.</param>
		/// <param name="title">The title indicator for duplicate session dates.</param>
		/// <returns>The matching session date, or the last session date if no match is found.</returns>
		public DateTime GetDateSelectedSession(DateTime? dateSelected, string title)
		{
			if (dateSelected == null || dateSelected == default(DateTime))
			{
				return GetLastSession();
			}

			int timeIgnore = GetTimeIgnoreThreshold();
			int indicator = 1;

			foreach (Activity activity in Items.Where(x => (int)x.ElapsedSeconds > timeIgnore))
			{
				DateTime dateTemp = Convert.ToDateTime(activity.DateSession).ToLocalTime();
				if (dateSelected.Value.ToString("yyyy-MM-dd HH:mm:ss") == dateTemp.ToString("yyyy-MM-dd HH:mm:ss"))
				{
					if (int.TryParse(title, out int titleValue) && indicator == titleValue)
					{
						return dateTemp.ToUniversalTime();
					}
					indicator++;
				}
			}

			return GetLastSession();
		}

		/// <summary>
		/// Gets the last session activity object.
		/// </summary>
		/// <param name="usedTimeIgnore">Whether to apply the time ignore threshold.</param>
		/// <returns>The last activity, or a new empty activity if none exists.</returns>
		public Activity GetLastSessionActivity(bool usedTimeIgnore = true)
		{
			int timeIgnore = usedTimeIgnore ? GetTimeIgnoreThreshold() : -1;

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.OrderByDescending(x => x.DateSession)
				.FirstOrDefault() ?? new Activity();
		}

		/// <summary>
		/// Gets the first session activity object.
		/// </summary>
		/// <returns>The first activity, or a new empty activity if none exists.</returns>
		public Activity GetFirstSessionActivity()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.OrderBy(x => x.DateSession)
				.FirstOrDefault() ?? new Activity();
		}

		/// <summary>
		/// Gets all activities for a specific month and year.
		/// </summary>
		/// <param name="year">The year to filter by.</param>
		/// <param name="month">The month to filter by.</param>
		/// <returns>List of activities for the specified month and year.</returns>
		public List<Activity> GetActivities(int year, int month)
		{
			return Items
				.Where(x => x.DateSession.ToLocalTime().Year == year && x.DateSession.ToLocalTime().Month == month)
				.OrderBy(x => x.DateSession)
				.ToList();
		}

		/// <summary>
		/// Gets all activities within a specified number of weeks.
		/// </summary>
		/// <param name="week">Number of weeks to look back (currently unused, uses settings).</param>
		/// <returns>List of activities within the time range.</returns>
		public List<Activity> GetListActivitiesWeek(int week)
		{
			int countDay = PluginDatabase.PluginSettings.RecentActivityWeek * 7;
			DateTime dtEnd = DateTime.Now.Date.AddDays(1).AddSeconds(-1); // End of today
			DateTime dtStart = DateTime.Now.AddDays(-countDay).Date; // Start of day N days ago

			return Items
				.Where(x => x.DateSession.ToLocalTime() >= dtStart && x.DateSession.ToLocalTime() <= dtEnd)
				.ToList();
		}

		/// <summary>
		/// Gets a formatted string describing recent activity.
		/// </summary>
		/// <returns>A localized string describing recent playtime or no activity message.</returns>
		public string GetRecentActivity()
		{
			List<Activity> recentActivities = GetListActivitiesWeek(PluginDatabase.PluginSettings.RecentActivityWeek);

			if (recentActivities == null || recentActivities.Count == 0)
			{
				return ResourceProvider.GetString("LOCGameActivityNoRecentActivity");
			}

			ulong totalSeconds = (ulong)recentActivities.Sum(x => (long)x.ElapsedSeconds);
			double totalHours = totalSeconds / 3600.0;

			if (totalHours == 0)
			{
				return ResourceProvider.GetString("LOCGameActivityNoRecentActivity");
			}

			int weeks = PluginDatabase.PluginSettings.RecentActivityWeek;
			string resourceKey = weeks == 1
				? "LOCGameActivityRecentActivitySingular"
				: "LOCGameActivityRecentActivityPlurial";

			return string.Format(ResourceProvider.GetString(resourceKey), totalHours.ToString("F1"), weeks);
		}

		/// <summary>
		/// Gets the performance details for a specific session.
		/// </summary>
		/// <param name="dateSelected">The selected session date (null for last session).</param>
		/// <param name="title">The title indicator for duplicate session dates.</param>
		/// <returns>List of activity details data for the session.</returns>
		public List<ActivityDetailsData> GetSessionActivityDetails(DateTime? dateSelected = null, string title = "")
		{
			DateTime sessionDate = GetDateSelectedSession(dateSelected, title);
			return GetActivityDetails(sessionDate);
		}

		/// <summary>
		/// Checks if there are any activities in a specific month and year.
		/// </summary>
		/// <param name="year">The year to check.</param>
		/// <param name="month">The month to check.</param>
		/// <returns>True if activities exist for the specified month and year.</returns>
		public bool HasActivity(int year, int month)
		{
			DateTime startDate = new DateTime(year, month, 1);
			DateTime endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

			return Items.Any(x => x.DateSession.ToLocalTime() >= startDate && x.DateSession.ToLocalTime() <= endDate);
		}

		/// <summary>
		/// Gets a list of unique year-month strings for all activities.
		/// </summary>
		/// <returns>List of date strings in "yyyy-MM" format.</returns>
		public List<string> GetListDateActivity()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.Select(x => x.DateSession.ToLocalTime().ToString("yyyy-MM"))
				.ToList();
		}

		/// <summary>
		/// Gets a list of all activity session dates.
		/// </summary>
		/// <returns>List of DateTime objects representing session dates.</returns>
		public List<DateTime> GetListDateTimeActivity()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => (int)x.ElapsedSeconds > timeIgnore)
				.Select(x => x.DateSession.ToLocalTime())
				.ToList();
		}

		/// <summary>
		/// Deletes an activity with the specified session date.
		/// </summary>
		/// <param name="dateSelected">The session date to delete.</param>
		public void DeleteActivity(DateTime dateSelected)
		{
			Activity activity = Items.FirstOrDefault(x => x.DateSession.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") == dateSelected.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));

			if (activity != null)
			{
				Items.Remove(activity);
			}
			else
			{
				Logger.Warn($"No activity for {Name} with date {dateSelected:yyyy-MM-dd HH:mm:ss}");
			}
		}

		#endregion

		/// <summary>
		/// Checks if performance details exist for a specific session.
		/// </summary>
		/// <param name="dateSelected">The selected session date (null for last session).</param>
		/// <param name="title">The title indicator for duplicate session dates.</param>
		/// <returns>True if performance details exist for the session.</returns>
		public bool HasDataDetails(DateTime? dateSelected = null, string title = "")
		{
			return GetSessionActivityDetails(dateSelected, title).Count > 0;
		}

		#region Helper Methods

		/// <summary>
		/// Gets the time ignore threshold based on current settings.
		/// </summary>
		/// <returns>The threshold in seconds, or -1 if ignore is disabled.</returns>
		private int GetTimeIgnoreThreshold()
		{
			return PluginDatabase.PluginSettings.IgnoreSession
				? PluginDatabase.PluginSettings.IgnoreSessionTime
				: -1;
		}

		private List<ActivityDetailsData> GetActivityDetails(DateTime dateSession)
		{
			Activity activity = Items.FirstOrDefault(x => x.DateSession.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") == dateSession.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));

			if (activity?.Details == null)
			{
				return new List<ActivityDetailsData>();
			}

			return activity.Details;
		}

		/// <summary>
		/// Collects all FPS samples &gt; 0 across every activity session.
		/// </summary>
		private List<int> GetAllSessionFpsValues()
		{
			if (Items == null)
			{
				return new List<int>();
			}

			return Items
				.Where(x => x?.Details != null)
				.SelectMany(x => x.Details)
				.Where(x => x != null && x.FPS > 0)
				.Select(x => x.FPS)
				.ToList();
		}

		/// <summary>
		/// Collects FPS samples &gt; 0 for the session matching <paramref name="dateSession"/>.
		/// </summary>
		private List<int> GetSessionFpsValues(DateTime dateSession)
		{
			return GetActivityDetails(dateSession)
				.Where(x => x != null && x.FPS > 0)
				.Select(x => x.FPS)
				.ToList();
		}

		/// <summary>
		/// Collects positive metric samples for a session (values &gt; 0 only).
		/// </summary>
		private List<int> GetSessionPositiveIntValues(DateTime dateSession, Func<ActivityDetailsData, int> selector)
		{
			return GetActivityDetails(dateSession)
				.Where(x => x != null && selector(x) > 0)
				.Select(selector)
				.ToList();
		}

		/// <summary>
		/// Computes the median of FPS samples (does not mutate the input list).
		/// </summary>
		private static int CalculateMedianFps(List<int> fpsValues)
		{
			if (fpsValues == null || fpsValues.Count == 0)
			{
				return 0;
			}

			List<int> sorted = new List<int>(fpsValues);
			sorted.Sort();
			int count = sorted.Count;
			if ((count % 2) == 1)
			{
				return sorted[count / 2];
			}

			int mid = count / 2;
			return (int)Math.Round((sorted[mid - 1] + (double)sorted[mid]) / 2.0);
		}

		/// <summary>
		/// Sample standard deviation (square root of unbiased sample variance, divisor <c>n - 1</c>).
		/// </summary>
		private static int CalculateSampleStdDevFps(List<int> fpsValues)
		{
			if (fpsValues == null || fpsValues.Count < 2)
			{
				return 0;
			}

			double mean = fpsValues.Average();
			double sumSquaredDeviations = 0;
			for (int i = 0; i < fpsValues.Count; i++)
			{
				double d = fpsValues[i] - mean;
				sumSquaredDeviations += d * d;
			}

			double variance = sumSquaredDeviations / (fpsValues.Count - 1);
			return (int)Math.Round(Math.Sqrt(variance));
		}

		#endregion
	}

	/// <summary>
	/// Transitional representation of the previous details schema.
	/// </summary>
	public class LegacyActivityDetailsContainer
	{
		public Dictionary<DateTime, List<ActivityDetailsData>> Items { get; set; } = new Dictionary<DateTime, List<ActivityDetailsData>>();
	}
}