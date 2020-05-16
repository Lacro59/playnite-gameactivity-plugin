using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Playnite.SDK;
using Dashboard.Models;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Dashboard.Database.Collections
{
    class GameActivityCollection : List<GameActivity>
    {
        private ILogger logger = LogManager.GetLogger();

        private string pathActivityDB = "\\activity\\";
        private string pathActivityDetailsDB = "\\activityDetails\\";

        public ConcurrentDictionary<Guid, GameActivity> Items { get; set; }

        public int Count => Items.Count;

        public GameActivity this[Guid id]
        {
            get => Get(id);
            set
            {
                new NotImplementedException();
            }
        }

        /// <summary>
        /// Initialize ActivityDatabase.
        /// </summary>
        /// <param name="pathExtData"></param>
        public void InitializeCollection(string pathExtData)
        {
            pathActivityDB = pathExtData + pathActivityDB;
            pathActivityDetailsDB = pathExtData + pathActivityDetailsDB;

            Items = new ConcurrentDictionary<Guid, GameActivity>();

            // Set data games activities.
            if (Directory.Exists(pathActivityDB))
            {
                Parallel.ForEach(Directory.EnumerateFiles(pathActivityDB, "*.json"), (objectFile) =>
                {
                    try
                    {
                        // Get game activities.
                        Guid gameId = Guid.Parse(objectFile.Replace(pathActivityDB, "").Replace(".json", ""));
                        List<Activity> obj = JsonConvert.DeserializeObject<List<Activity>>(File.ReadAllText(objectFile));

                        // Initialize GameActivity
                        GameActivity objGameActivity = new GameActivity(gameId);
                        objGameActivity.Activities = obj;

                        // Set data games activities details.
                        if (Directory.Exists(pathActivityDetailsDB))
                        {
                            string objectFileDetails = pathActivityDetailsDB + "\\" + objectFile.Replace(pathActivityDB, "");
                            if(File.Exists(objectFileDetails))
                            {
                                ActivityDetails objDetails = new ActivityDetails(File.ReadAllText(objectFileDetails));
                                objGameActivity.ActivitiesDetails = objDetails;
                            };
                        }

                        // Set GameActivity in collection
                        Items.TryAdd(gameId, objGameActivity);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to load item from {objectFile}");
                    }
                });
            }
        }

        /// <summary>
        /// Get GameActivity for a game by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GameActivity Get(Guid id)
        {
            if (Items.TryGetValue(id, out var item))
            {
                return item;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// get list GameActivity in ActivityDatabase.
        /// </summary>
        /// <returns></returns>
        public List<GameActivity> GetListGameActivity()
        {
            List<GameActivity> list = new List<GameActivity>();
            foreach (var Item in Items)
            {
                list.Add(Item.Value);
            }
            return list;
        }
    }
}
