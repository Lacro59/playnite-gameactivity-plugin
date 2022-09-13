using GameActivity.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
using System.Globalization;
using CommonPluginsShared;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace GameActivity.Services
{
    public class ActivityDatabase : PluginDatabaseObject<GameActivitySettingsViewModel, GameActivitiesCollection, GameActivities, Activity>
    {
        private LocalSystem _LocalSystem { get; set; }
        public LocalSystem LocalSystem
        {
            get
            {
                if (_LocalSystem == null)
                {
                    _LocalSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"), false);
                }
                return _LocalSystem;
            }
        }


        public ActivityDatabase(IPlayniteAPI PlayniteApi, GameActivitySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "GameActivity", PluginUserDataPath)
        {

        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new GameActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfoDetails<Activity, ActivityDetails>(PlayniteApi);

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "GameActivity");
                return false;
            }

            return true;
        }


        public override GameActivities Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameActivities gameActivities = GetOnlyCache(Id);

            if (gameActivities == null)
            {
                Game game = PlayniteApi.Database.Games.Get(Id);
                if (game != null)
                {
                    gameActivities = GetDefault(game);
                    Add(gameActivities);
                }
            }

            return gameActivities;
        }


        public override void SetThemesResources(Game game)
        {
            GameActivities gameActivities = Get(game, true);

            if (gameActivities == null)
            {
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.HasDataLog = false;

                PluginSettings.Settings.LastDateSession = string.Empty;
                PluginSettings.Settings.LastDateTimeSession = string.Empty;

                PluginSettings.Settings.LastPlaytimeSession = string.Empty;
                PluginSettings.Settings.AvgFpsAllSession = 0;

                return;
            }

            PluginSettings.Settings.HasData = gameActivities.HasData;
            PluginSettings.Settings.HasDataLog = gameActivities.HasDataDetails();

            PluginSettings.Settings.LastDateSession = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
            PluginSettings.Settings.LastDateTimeSession = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat)
                + " " + gameActivities.GetLastSession().ToLocalTime().ToString(Constants.TimeUiFormat);

            PlayTimeToStringConverter converter = new PlayTimeToStringConverter();
            string playtime = (string)converter.Convert((long)gameActivities.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
            PluginSettings.Settings.LastPlaytimeSession = playtime;

            PluginSettings.Settings.AvgFpsAllSession = gameActivities.ItemsDetails.AvgFpsAllSession;
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e?.UpdatedItems != null)
            {
                foreach (ItemUpdateEvent<Game> GameUpdated in e.UpdatedItems)
                {
                    Database.SetGameInfoDetails<Activity, ActivityDetails>(PlayniteApi, GameUpdated.NewData.Id);
                    GameActivities data = Get(GameUpdated.NewData.Id);
                }
            }
        }


        /// <summary>
        /// get list GameActivity in ActivityDatabase.
        /// </summary>
        /// <returns></returns>
        public List<GameActivities> GetListGameActivity()
        {
            return Database.ToList();
        }
    }
}
