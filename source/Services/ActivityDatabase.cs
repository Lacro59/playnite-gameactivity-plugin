using GameActivity.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPluginsPlaynite.Common;
using CommonPluginsPlaynite.Converters;
using System.Globalization;
using CommonPluginsShared;
using System.IO;

namespace GameActivity.Services
{
    public class ActivityDatabase : PluginDatabaseObject<GameActivitySettingsViewModel, GameActivitiesCollection, GameActivities>
    {
        public LocalSystem LocalSystem;


        public ActivityDatabase(IPlayniteAPI PlayniteApi, GameActivitySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "GameActivity", PluginUserDataPath)
        {

        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            Database = new GameActivitiesCollection(Paths.PluginDatabasePath);
            Database.SetGameInfoDetails<Activity, ActivityDetails>(PlayniteApi);
            GetPluginTags();

            LocalSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"), false);

            IsLoaded = true;
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
            if (gameActivities.ItemsDetails.MustBeSaved)
            {
                AddOrUpdate(gameActivities);
            }
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfoDetails<Activity, ActivityDetails>(PlayniteApi, GameUpdated.NewData.Id);
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
