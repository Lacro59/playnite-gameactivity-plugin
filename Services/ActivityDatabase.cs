using GameActivity.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Services
{
    public class ActivityDatabase : PluginDatabaseObject<GameActivitySettings, GameActivitiesCollection, GameActivities>
    {
        public ActivityDatabase(IPlayniteAPI PlayniteApi, GameActivitySettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            PluginName = "GameActivity";

            ControlAndCreateDirectory(PluginUserDataPath, "Activity");
        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            Database = new GameActivitiesCollection(PluginDatabaseDirectory);
            Database.SetGameInfoDetails<Activity, ActivityDetails>(_PlayniteApi);

            GameSelectedData = new GameActivities();
            GetPluginTags();

            IsLoaded = true;
            return true;
        }


        public override GameActivities Get(Guid Id, bool OnlyCache = false)
        {
            GameIsLoaded = false;
            GameActivities gameActivities = GetOnlyCache(Id);

            if (gameActivities == null)
            {
                Game game = _PlayniteApi.Database.Games.Get(Id);
                gameActivities = GetDefault(game);
                Add(gameActivities);
            }

            GameIsLoaded = true;
            return gameActivities;
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
