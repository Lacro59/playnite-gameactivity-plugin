using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameActivity.Models
{
    class GameActivityClass
    {
        public int CountActivities => Activities.Count;
        public int CountActivitiesDetails => ActivitiesDetails.Count;


        public GameActivityClass(Guid gameId)
        {
            GameID = gameId;
        }

        /// <summary>
        /// Gets or sets list activities.
        /// </summary>
        public List<Activity> Activities { get; set; } = new List<Activity>();

        /// <summary>
        /// Gets or sets list activities details.
        /// </summary>
        public ActivityDetails ActivitiesDetails { get; set; } = new ActivityDetails("");


        /// <summary>
        /// Get game id.
        /// </summary>
        public Guid GameID { get; }

        /// <summary>
        /// Get game name.
        /// </summary>
        public string GameName
        {
            get => GameActivity.DatabaseReference.Games.Get(GameID).Name;
        }

        /// <summary>
        /// Get game icon.
        /// </summary>
        public string GameIcon
        {
            get => GameActivity.DatabaseReference.Games.Get(GameID).Icon;
        }

        /// <summary>
        /// Get game's genres id.
        /// </summary>
        public List<Guid> genreIds
        {
            get => GameActivity.DatabaseReference.Games.Get(GameID).GenreIds;
        }

        /// <summary>
        /// Gets game's genres.
        /// </summary>
        public List<Genre> Genres
        {
            get
            {
                if (genreIds?.Any() == true && GameActivity.DatabaseReference != null)
                {
                    return new List<Genre>(GameActivity.DatabaseReference?.Genres.Where(a => genreIds.Contains(a.Id)).OrderBy(a => a.Name));
                }

                return null;
            }
        }


        public int avgCPU(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPU;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }

        public int avgGPU(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPU;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }

        public int avgRAM(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].RAM;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }


        public int avgFPS(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].FPS;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }

        public int avgCPUT(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].CPUT;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }

        public int avgGPUT(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsData> acDetailsData = ActivitiesDetails.Get(dateSession);
            for (int iData = 0; iData < acDetailsData.Count; iData++)
            {
                avg += acDetailsData[iData].GPUT;
            }

            if (acDetailsData.Count != 0)
                return (int)Math.Round(avg / acDetailsData.Count);
            else
                return 0;
        }



        /// <summary>
        /// Get the date last session.
        /// </summary>
        /// <returns></returns>
        public string GetLastSession()
        {
            // Easter eggs :)
            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                if (datePrev < dateTemp)
                {
                    dateLastSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                    datePrev = dateLastSession;
                }
            }

            return dateLastSession.ToUniversalTime().ToString("o"); ;
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
            for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                if (datePrev < dateTemp)
                {
                    lastActivity = Activities[iActivity];
                    dateLastSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                    datePrev = dateLastSession;
                }
            }

            return lastActivity;
        }

        /// <summary>
        /// Get the last session activity details.
        /// </summary>
        /// <returns></returns>
        public List<ActivityDetailsData> GetLastSessionActivityDetails()
        {
            string dateLastSession = GetLastSession();
            return ActivitiesDetails.Get(dateLastSession);
        }
    }
}
