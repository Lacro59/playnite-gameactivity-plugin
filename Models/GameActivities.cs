using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    public class GameActivities : PluginDataBaseGameDetails<Activity, ActivityDetails>
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<Activity> _Items = new List<Activity>();
        public override List<Activity> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                _Items = value;
                OnPropertyChanged();
            }
        }

        private ActivityDetails _ItemsDetails = new ActivityDetails();
        public override ActivityDetails ItemsDetails
        {
            get
            {
                return _ItemsDetails;
            }

            set
            {
                _ItemsDetails = value;
                OnPropertyChanged();
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
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
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }




        #region Activities
        /// <summary>
        /// Get the date last session.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastSession()
        {
            // Easter eggs :)
            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                if (datePrev < dateTemp)
                {
                    dateLastSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    datePrev = dateLastSession;
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

            int indicator = 1;
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                if (((DateTime)dateSelected).ToString("yyyy-MM-dd") == dateTemp.ToString("yyyy-MM-dd"))
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

            return GetLastSession();
        }


        /// <summary>
        /// Get the last session activity.
        /// </summary>
        /// <returns></returns>
        public Activity GetLastSessionActivity()
        {
            // Easter eggs :)
            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            Activity lastActivity = new Activity();
            for (int iActivity = 0; iActivity < Items.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                if (datePrev < dateTemp)
                {
                    lastActivity = Items[iActivity];
                    dateLastSession = Convert.ToDateTime(Items[iActivity].DateSession).ToLocalTime();
                    datePrev = dateLastSession;
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
        #endregion



        public bool HasDataDetails(DateTime? dateSelected = null, string title = "")
        {
            return GetSessionActivityDetails(dateSelected, title).Count > 0;
        }
    }
}
