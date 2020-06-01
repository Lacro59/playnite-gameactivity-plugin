using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Playnite.SDK;
using GameActivity.Models;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace GameActivity.Database.Collections
{
    class GameActivityCollection : List<GameActivityClass>
    {
        private ILogger logger = LogManager.GetLogger();

        private string pathActivityDB = "\\activity\\";
        private string pathActivityDetailsDB = "\\activityDetails\\";

        public ConcurrentDictionary<Guid, GameActivityClass> Items { get; set; }

        public new int Count => Items.Count;

        public GameActivityClass this[Guid id]
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

            Items = new ConcurrentDictionary<Guid, GameActivityClass>();

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
                        GameActivityClass objGameActivity = new GameActivityClass(gameId);
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
        public GameActivityClass Get(Guid id)
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
        public List<GameActivityClass> GetListGameActivity()
        {
            List<GameActivityClass> list = new List<GameActivityClass>();
            foreach (var Item in Items)
            {
                list.Add(Item.Value);
            }
            return list;
        }
    }
}
