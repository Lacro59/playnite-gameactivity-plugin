using Playnite.SDK;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using GameActivity.Services;
using Playnite.SDK.Data;
//using MoreLinq.Extensions;

namespace GameActivity.Models
{
    public class GameActivities : PluginDataBaseGameDetails<Activity, ActivityDetails>
    {
        private static ILogger logger => LogManager.GetLogger();
        private static IResourceProvider resources => new ResourceProvider();
        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;


        private List<Activity> _Items = new List<Activity>();
        public override List<Activity> Items { get => _Items; set => SetValue(ref _Items, value); }

        private ActivityDetails _ItemsDetails = new ActivityDetails();
        public override ActivityDetails ItemsDetails { get => _ItemsDetails; set => SetValue(ref _ItemsDetails, value); }


        [DontSerialize]
        public List<Activity> FilterItems
        {
            get
            {
                return PluginDatabase.PluginSettings.Settings.IgnoreSession
                    ? Items.Where(x => (int)x.ElapsedSeconds > PluginDatabase.PluginSettings.Settings.IgnoreSessionTime).Distinct().ToList()
                    : Items.Where(x => (int)x.ElapsedSeconds > 0).Distinct().ToList();
            }
        }


        public int avgCPU(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPU;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgGPU(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPU;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgRAM(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].RAM;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgFPS(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].FPS;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgCPUT(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPUT;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgGPUT(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPUT;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgCPUP(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPUP;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }

        public int avgGPUP(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPUP;
            }

            return acDetailsData.Count != 0 ? (int)Math.Round(avg / acDetailsData.Count) : 0;
        }


        public ulong avgPlayTime()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            ulong avgPlayTime = 0;
            int CountWithTime = 0;

            Items.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore).ForEach(x => 
            {
                avgPlayTime += x.ElapsedSeconds;
                CountWithTime++;
            });

            if (avgPlayTime != 0 && CountWithTime != 0)
            {
                avgPlayTime /= (ulong)CountWithTime;
            }

            return avgPlayTime;
        }


        #region Activities
        public DateTime GetFirstSession()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.OrderBy(x => x.DateSession)
                .Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?.FirstOrDefault()?.DateSession ?? DateTime.Now;
        }

        /// <summary>
        /// Get the date last session.
        /// </summary>
        /// <returns></returns>
        // TODO Don't use to get on playing session
        public DateTime GetLastSession()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.OrderByDescending(x => x.DateSession)
                .Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?.FirstOrDefault()?.DateSession ?? DateTime.Now;
        }

        public DateTime GetDateSelectedSession(DateTime? dateSelected, string title)
        {
            if (dateSelected == null || dateSelected == default(DateTime))
            {
                return GetLastSession();
            }

            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            int indicator = 1;
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                if ((int)Items[iActivity].ElapsedSeconds > TimeIgnore)
                {
                    DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    if (((DateTime)dateSelected).ToString("yyyy-MM-dd HH:mm:ss") == dateTemp.ToString("yyyy-MM-dd HH:mm:ss"))
                    {
                        int.TryParse(title, out int titleValue);
                        if (indicator == titleValue)
                        {
                            return dateTemp.ToUniversalTime();
                        }
                        else
                        {
                            indicator += 1;
                        }
                    }
                }
            }

            return GetLastSession();
        }


        /// <summary>
        /// Get the last session activity.
        /// </summary>
        /// <returns></returns>
        public Activity GetLastSessionActivity(bool UsedTimeIgnore = true)
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession && UsedTimeIgnore)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.OrderByDescending(x => x.DateSession)
                .Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?.FirstOrDefault() ?? new Activity();
        }

        public Activity GetFirstSessionactivity()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.OrderBy(x => x.DateSession)
                .Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?.FirstOrDefault() ?? new Activity();
        }


        public List<Activity> GetListActivitiesWeek(int week)
        {
            int CountDay = PluginDatabase.PluginSettings.Settings.RecentActivityWeek * 7;
            DateTime dtEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59, 59);
            DateTime dtStart = new DateTime(DateTime.Now.AddDays(-CountDay).Year, DateTime.Now.AddDays(-CountDay).Month, DateTime.Now.AddDays(-CountDay).Day, 0, 0, 0, 0);

            return Items.Where(x => x.DateSession != null && x.DateSession >= dtStart && x.DateSession <= dtEnd)?.ToList() ?? new List<Activity>();
        }

        public string GetRecentActivity()
        {
            List<Activity> RecentActivities = GetListActivitiesWeek(PluginDatabase.PluginSettings.Settings.RecentActivityWeek);
            ulong CountElapsedSeconds = RecentActivities?.Count == 0 ? 0 : RecentActivities?.Select(x => x.ElapsedSeconds)?.Aggregate((a, c) => a + c) ?? 0;
            double CountElapsedHours = CountElapsedSeconds / 3600;

            return CountElapsedHours == 0
                ? resources.GetString("LOCGameActivityNoRecentActivity")
                : PluginDatabase.PluginSettings.Settings.RecentActivityWeek == 1
                    ? string.Format(resources.GetString("LOCGameActivityRecentActivitySingular"), CountElapsedHours, PluginDatabase.PluginSettings.Settings.RecentActivityWeek)
                    : string.Format(resources.GetString("LOCGameActivityRecentActivityPlurial"), CountElapsedHours, PluginDatabase.PluginSettings.Settings.RecentActivityWeek);
        }


        /// <summary>
        /// Get the last session activity details.
        /// </summary>
        /// <returns></returns>
        public List<ActivityDetailsData> GetSessionActivityDetails(DateTime? dateSelected = null, string title = "")
        {
            DateTime dateLastSession = GetDateSelectedSession(dateSelected, title);
            return ItemsDetails.Get(dateLastSession);
        }

        public bool HasActivity(int Year, int Month)
        {
            List<Activity> els = Items.FindAll(x => x.DateSession != null && x.DateSession <= new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month)) && x.DateSession >= new DateTime(Year, Month, 1));
            return els?.Count > 0;
        }

        public List<string> GetListDateActivity()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?
                .Select(x => ((DateTime)x.DateSession).ToString("yyyy-MM"))?
                .ToList() ?? new List<string>();
        }

        public List<DateTime> GetListDateTimeActivity()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            return Items.Where(x => x.DateSession != null && (int)x.ElapsedSeconds > TimeIgnore)?
                .Select(x => (DateTime)x.DateSession)?
                .ToList() ?? new List<DateTime>();
        }

        public void DeleteActivity(DateTime dateSelected)
        {
            Activity activity = Items.Where(x => x.DateSession == dateSelected.ToUniversalTime()).FirstOrDefault();
            if (activity != null)
            {
                Items.Remove(activity);
            }
            else
            {
                logger.Warn($"No activity for {Name} with date {dateSelected.ToString("yyyy-MM-dd HH:mm:ss")}");
            }
        }
        #endregion


        public bool HasDataDetails(DateTime? dateSelected = null, string title = "")
        {
            return GetSessionActivityDetails(dateSelected, title).Count > 0;
        }
    }
}
