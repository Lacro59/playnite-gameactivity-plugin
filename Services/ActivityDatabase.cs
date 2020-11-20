using GameActivity.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Services
{
    public class ActivityDatabase : PluginDatabaseObject
    {
        private GameActivitiesCollection db;

        private GameActivities _GameSelectedData = new GameActivities();
        public GameActivities GameSelectedData
        {
            get
            {
                return _GameSelectedData;
            }

            set
            {
                _GameSelectedData = value;
                OnPropertyChanged();
            }
        }


        public ActivityDatabase(IPlayniteAPI PlayniteApi, GameActivitySettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            PluginName = "GameActivity";

            ControlAndCreateDirectory(PluginUserDataPath, "Activity");
        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            db = new GameActivitiesCollection(PluginDatabaseDirectory);
            db.SetGameInfoDetails<Activity, ActivityDetails>(_PlayniteApi);
#if DEBUG
            logger.Debug($"{PluginName} - db: {JsonConvert.SerializeObject(db)}");
#endif

            IsLoaded = true;
            return true;
        }


        public GameActivities Get(Guid Id)
        {
            GameIsLoaded = false;
            GameActivities gameActivities = db.Get(Id);
#if DEBUG
            logger.Debug($"{PluginName} - GetFromDb({Id.ToString()}) - GameActivities: {JsonConvert.SerializeObject(gameActivities)}");
#endif

            if (gameActivities == null)
            {
                Game game = _PlayniteApi.Database.Games.Get(Id);

                gameActivities = new GameActivities
                {
                    Id = game.Id,
                    Name = game.Name,
                    Hidden = game.Hidden,
                    Icon = game.Icon,
                    CoverImage = game.CoverImage,
                    GenreIds = game.GenreIds,
                    Genres = game.Genres
                };
                Add(gameActivities);
            }

            GameIsLoaded = true;
            return gameActivities;
        }

        public GameActivities Get(Game game)
        {
            return Get(game.Id);
        }

        public void Add(GameActivities itemToAdd)
        {
            db.Add(itemToAdd);
        }

        public void Update(GameActivities itemToUpdate)
        {
            db.Update(itemToUpdate);
        }


        public void SetCurrent(Guid Id)
        {
            GameSelectedData = Get(Id);
        }

        public void SetCurrent(Game game)
        {
            GameSelectedData = Get(game.Id);
        }

        public void SetCurrent(GameActivities gameActivities)
        {
            GameSelectedData = gameActivities;
        }


        /// <summary>
        /// get list GameActivity in ActivityDatabase.
        /// </summary>
        /// <returns></returns>
        public List<GameActivities> GetListGameActivity()
        {
            return db.ToList();
        }
    }
}
