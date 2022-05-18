using Playnite.SDK;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using GameActivity.Services;
using Playnite.SDK.Data;
using MoreLinq.Extensions;

namespace GameActivity.Models
{
    public class GameActivities : PluginDataBaseGameDetails<Activity, ActivityDetails>
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        private List<Activity> _Items = new List<Activity>();
        public override List<Activity> Items { get => _Items; set => SetValue(ref _Items, value); }

        private ActivityDetails _ItemsDetails = new ActivityDetails();
        public override ActivityDetails ItemsDetails { get => _ItemsDetails; set => SetValue(ref _ItemsDetails, value); }


        [DontSerialize]
        public List<Activity> FilterItems
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
                {
                    return Items.Where(x => (int)x.ElapsedSeconds > PluginDatabase.PluginSettings.Settings.IgnoreSessionTime).Distinct().ToList();
                }
                else
                {
                    return Items.Where(x => (int)x.ElapsedSeconds > 0).Distinct().ToList();
                }
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

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgGPU(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPU;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgRAM(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].RAM;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }


        public int avgFPS(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].FPS;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgCPUT(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPUT;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgGPUT(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPUT;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgCPUP(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPUP;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }

        public int avgGPUP(DateTime dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ItemsDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPUP;
            }

            if (acDetailsData.Count != 0)
            {
                return (int)Math.Round(avg / acDetailsData.Count);
            }
            else
            {
                return 0;
            }
        }


        public ulong avgPlayTime()
        {
            ulong avgPlayTime = 0;
            int CountWithTime = 0;

            foreach (Activity Item in Items)
            {
                avgPlayTime += Item.ElapsedSeconds;
                CountWithTime++;
            }

            if (avgPlayTime != 0 && CountWithTime != 0)
            {
                avgPlayTime = avgPlayTime / (ulong)CountWithTime;
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

            DateTime datePrev = new DateTime(2050, 12, 15, 00, 15, 23);
            DateTime dateFirstSession = DateTime.Now;
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                if ((int)Items[iActivity].ElapsedSeconds > TimeIgnore)
                {
                    DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    if (datePrev > dateTemp)
                    {
                        dateFirstSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                        datePrev = dateFirstSession;
                    }
                }
            }

            return dateFirstSession.ToUniversalTime();
        }

        /// <summary>
        /// Get the date last session.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastSession()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                if ((int)Items[iActivity].ElapsedSeconds > TimeIgnore)
                {
                    DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    if (datePrev < dateTemp)
                    {
                        dateLastSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                        datePrev = dateLastSession;
                    }
                }
            }

            return dateLastSession.ToUniversalTime();
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
                        int titleValue = 0;
                        int.TryParse(title, out titleValue);
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

            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            Activity lastActivity = new Activity();
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                if ((int)Items[iActivity].ElapsedSeconds > TimeIgnore)
                {
                    DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    if (datePrev < dateTemp)
                    {
                        lastActivity = Items[iActivity];
                        dateLastSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                        datePrev = dateLastSession;
                    }
                }
            }

            return lastActivity;
        }

        public Activity GetFirstSessionactivity()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            DateTime datePrev = new DateTime(2050, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            Activity lastActivity = new Activity();
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                if ((int)Items[iActivity].ElapsedSeconds > TimeIgnore)
                {
                    DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    if (datePrev > dateTemp)
                    {
                        lastActivity = Items[iActivity];
                        dateLastSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                        datePrev = dateLastSession;
                    }
                }
            }

            return lastActivity;
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
            try
            {
                var els = Items.FindAll(x => x.DateSession <= new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month))
                    && x.DateSession >= new DateTime(Year, Month, 1));
                return els.Count > 0;
            }
            catch
            {

            }

            return false;
        }

        public List<string> GetListDateActivity()
        {
            int TimeIgnore = -1;
            if (PluginDatabase.PluginSettings.Settings.IgnoreSession)
            {
                TimeIgnore = PluginDatabase.PluginSettings.Settings.IgnoreSessionTime;
            }

            List<string> Result = new List<string>();

            foreach(Activity el in Items)
            {
                if ((int)el.ElapsedSeconds > TimeIgnore)
                {
                    string DateString = ((DateTime)el.DateSession).ToString("yyyy-MM");

                    if (!Result.Contains(DateString))
                    {
                        Result.Add(DateString);
                    }
                }
            }            

            return Result;
        }


        public void DeleteActivity(DateTime dateSelected)
        {
            var activity = Items.Where(x => x.DateSession == dateSelected.ToUniversalTime()).FirstOrDefault();
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
