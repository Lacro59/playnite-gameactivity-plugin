using Playnite.SDK;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using GameActivity.Services;
using Playnite.SDK.Data;

namespace GameActivity.Models
{
	/// <summary>
	/// Represents a collection of game activities with performance details and session statistics.
	/// </summary>
	public class GameActivities : PluginDataBaseGameDetails<Activity, ActivityDetails>
	{
		private static ILogger Logger => LogManager.GetLogger();
		private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		/// <summary>
		/// Gets the list of activities filtered based on session time settings.
		/// Excludes sessions below the configured ignore threshold if enabled.
		/// </summary>
		[DontSerialize]
		public List<Activity> FilterItems
		{
			get
			{
				int timeThreshold = PluginDatabase.PluginSettings.Settings.IgnoreSession
					? PluginDatabase.PluginSettings.Settings.IgnoreSessionTime
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
			List<ActivityDetailsData> data = ItemsDetails.Get(dateSession);
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
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
				.Where(x => x.DateSession != null)
				.Where(x => x.DateSession.Value.ToLocalTime().Year == year &&
						   x.DateSession.Value.ToLocalTime().Month == month)
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
			int countDay = PluginDatabase.PluginSettings.Settings.RecentActivityWeek * 7;
			DateTime dtEnd = DateTime.Now.Date.AddDays(1).AddSeconds(-1); // End of today
			DateTime dtStart = DateTime.Now.AddDays(-countDay).Date; // Start of day N days ago

			return Items
				.Where(x => x.DateSession != null)
				.Where(x => x.DateSession.Value.ToLocalTime() >= dtStart &&
						   x.DateSession.Value.ToLocalTime() <= dtEnd)
				.ToList();
		}

		/// <summary>
		/// Gets a formatted string describing recent activity.
		/// </summary>
		/// <returns>A localized string describing recent playtime or no activity message.</returns>
		public string GetRecentActivity()
		{
			List<Activity> recentActivities = GetListActivitiesWeek(PluginDatabase.PluginSettings.Settings.RecentActivityWeek);

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

			int weeks = PluginDatabase.PluginSettings.Settings.RecentActivityWeek;
			string resourceKey = weeks == 1
				? "LOCGameActivityRecentActivitySingular"
				: "LOCGameActivityRecentActivityPlurial";

			return string.Format(ResourceProvider.GetString(resourceKey), totalHours, weeks);
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
			return ItemsDetails.Get(sessionDate);
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

			return Items.Any(x => x.DateSession != null &&
								 x.DateSession.Value.ToLocalTime() >= startDate &&
								 x.DateSession.Value.ToLocalTime() <= endDate);
		}

		/// <summary>
		/// Gets a list of unique year-month strings for all activities.
		/// </summary>
		/// <returns>List of date strings in "yyyy-MM" format.</returns>
		public List<string> GetListDateActivity()
		{
			int timeIgnore = GetTimeIgnoreThreshold();

			return Items
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
				.Select(x => x.DateSession.Value.ToLocalTime().ToString("yyyy-MM"))
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
				.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > timeIgnore)
				.Select(x => x.DateSession.Value.ToLocalTime())
				.ToList();
		}

		/// <summary>
		/// Deletes an activity with the specified session date.
		/// </summary>
		/// <param name="dateSelected">The session date to delete.</param>
		public void DeleteActivity(DateTime dateSelected)
		{
			Activity activity = Items.FirstOrDefault(x => x.DateSession == dateSelected.ToUniversalTime());

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
			return PluginDatabase.PluginSettings.Settings.IgnoreSession
				? PluginDatabase.PluginSettings.Settings.IgnoreSessionTime
				: -1;
		}

		#endregion
	}
}