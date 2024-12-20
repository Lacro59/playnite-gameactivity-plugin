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
        private LocalSystem _localSystem { get; set; }
        public LocalSystem LocalSystem
        {
            get
            {
                if (_localSystem == null)
                {
                    _localSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"), false);
                }
                return _localSystem;
            }
        }


        public ActivityDatabase(GameActivitySettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "GameActivity", pluginUserDataPath)
        {
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new GameActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfoDetails<Activity, ActivityDetails>();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

            return true;
        }


        public override GameActivities Get(Guid id, bool onlyCache = false, bool force = false)
        {
            GameActivities gameActivities = GetOnlyCache(id);
            if (gameActivities == null)
            {
                Game game = API.Instance.Database.Games.Get(id);
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

            PluginSettings.Settings.RecentActivity = gameActivities.GetRecentActivity();
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e?.UpdatedItems != null)
            {
                foreach (ItemUpdateEvent<Game> GameUpdated in e.UpdatedItems)
                {
                    Database.SetGameInfoDetails<Activity, ActivityDetails>(GameUpdated.NewData.Id);
                    _ = Get(GameUpdated.NewData.Id);
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


        public override PluginDataBaseGameBase MergeData(Guid fromId, Guid toId)
        {
            try
            {
                GameActivities fromData = Get(fromId, true);
                GameActivities toData = Get(toId, true);

                toData.Items.AddRange(fromData.Items);
                fromData.ItemsDetails.Items.ForEach(x =>
                {
                    toData.ItemsDetails.Items.TryAdd(x.Key, x.Value);
                });

                return toData;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }
    }
}
