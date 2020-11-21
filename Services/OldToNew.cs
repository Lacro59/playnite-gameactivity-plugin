using GameActivity.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameActivity.Services
{
    public class OldToNew
    {
        private ILogger logger = LogManager.GetLogger();

        public bool IsOld = false;

        private string PathActivityDB = "activity";
        private string PathActivityDetailsDB = "activityDetails";

        public ConcurrentDictionary<Guid, GameActivityClassOld> Items { get; set; } = new ConcurrentDictionary<Guid, GameActivityClassOld>();


        public OldToNew(string PluginUserDataPath)
        {
            PathActivityDB = Path.Combine(PluginUserDataPath, PathActivityDB);
            PathActivityDetailsDB = Path.Combine(PluginUserDataPath, PathActivityDetailsDB);

            if (Directory.Exists(PathActivityDB) && Directory.Exists(PathActivityDetailsDB))
            {
                Directory.Move(PathActivityDB, PathActivityDB + "_old");
                Directory.Move(PathActivityDetailsDB, PathActivityDetailsDB + "_old");

                PathActivityDB += "_old";
                PathActivityDetailsDB += "_old";

                LoadOldDB();
                IsOld = true;
            }
        }

        public void LoadOldDB()
        {
            logger.Info($"GameActivity - LoadOldDB()");

            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileDetails = string.Empty;

                try
                {
                    var JsonStringData = File.ReadAllText(objectFile);
                    if (JsonStringData.IsNullOrEmpty() || JsonStringData == "{}" || JsonStringData == "[]")
                    {
                        File.Delete(objectFile);
                        logger.Info($"GameActivity - Delete empty file {objectFile}");
                    }
                    else
                    {
                        // Get game activities.
#if DEBUG
                        logger.Debug(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));
#endif
                        Guid gameId = Guid.Parse(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));
                        List<ActivityOld> obj = JsonConvert.DeserializeObject<List<ActivityOld>>(JsonStringData);

                        // Initialize GameActivity
                        GameActivityClassOld objGameActivity = new GameActivityClassOld(gameId);
                        objGameActivity.Activities = obj;

                        // Set data games activities details.
                        if (Directory.Exists(PathActivityDetailsDB))
                        {
                            objectFileDetails = PathActivityDetailsDB + "\\" + objectFile.Replace(PathActivityDB, "");
                            if (File.Exists(objectFileDetails))
                            {
                                var JsonStringDataDetails = File.ReadAllText(objectFileDetails);
                                if (JsonStringDataDetails.IsNullOrEmpty() || JsonStringDataDetails == "{}" || JsonStringDataDetails == "[]")
                                {
                                    File.Delete(objectFile);
                                    logger.Info($"GameActivity - Delete empty file {objectFileDetails}");
                                }
                                else
                                {
                                    ActivityDetailsOld objDetails = new ActivityDetailsOld(JsonStringDataDetails);
                                    objGameActivity.ActivitiesDetails = objDetails;
                                }
                            };
                        }

                        // Set GameActivity in collection
                        Items.TryAdd(gameId, objGameActivity);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", $"Failed to load item from {objectFile} or {objectFileDetails}");
                }
            });

            logger.Info($"GameActivity - Find {Items.Count} items");
        }

        public void ConvertDB(IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "GameActivity - Database migration",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                logger.Info($"GameActivity - ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            GameActivities GameActivitiesLog = GameActivity.PluginDatabase.Get(item.Key);

                            foreach (var Activity in item.Value.Activities)
                            {
                                DateTime DateSession = (DateTime)Activity.DateSession;

                                GameActivitiesLog.Items.Add(new Activity
                                {
                                    DateSession = Activity.DateSession,
                                    SourceID = Activity.SourceID,
                                    ElapsedSeconds = Activity.ElapsedSeconds
                                });


                                var ActivitiesDetails = item.Value.GetSessionActivityDetails(DateSession.ToString("o"));

                                List<ActivityDetailsData> ListActivityDetails = new List<ActivityDetailsData>();
                                foreach (var ActivityDetails in ActivitiesDetails)
                                {
                                    ListActivityDetails.Add(new ActivityDetailsData
                                    {
                                        Datelog = ActivityDetails.Datelog,
                                        FPS = ActivityDetails.FPS,
                                        CPU = ActivityDetails.CPU,
                                        CPUT = ActivityDetails.CPUT,
                                        GPU = ActivityDetails.GPU,
                                        GPUT = ActivityDetails.GPUT,
                                        RAM = ActivityDetails.RAM
                                    });
                                }

                                GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, ListActivityDetails);
                            }

                            Thread.Sleep(10);
                            GameActivity.PluginDatabase.Update(GameActivitiesLog);
                            Converted++;
                        }
                        else
                        {
                            logger.Warn($"GameActivity - Game is deleted - {item.Key.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "GameActivity", $"Failed to load ConvertDB from {item.Key} - {item.Value.GameName}");
                    }
                }

                logger.Info($"GameActivity - Converted {Converted} / {Items.Count}");

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"GameActivity - Migration - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);

            IsOld = false;
        }
    }


    public class ActivityOld
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Gets or sets source of the game.
        /// </summary>
        public Guid SourceID { get; set; }

        /// <summary>
        /// Get source name.
        /// </summary>
        [JsonIgnore]
        public string SourceName
        {
            get
            {
                if (SourceID != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    try
                    {
                        return GameActivity.DatabaseReference.Sources.Get(SourceID).Name;
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Common.LogError(ex, "GameActivity", $"Error in ActivitySourceName");
#endif
                        return "Playnite";
                    }
                }

                return "Playnite";
            }
        }

        /// <summary>
        /// Gets or sets date game session.
        /// </summary>
        public DateTime? DateSession { get; set; }

        /// <summary>
        /// Gets or sets played time in seconds.
        /// </summary>
        public long ElapsedSeconds { get; set; } = 0;
    }

    public class ActivityDetailsOld
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public ConcurrentDictionary<string, List<ActivityDetailsDataOld>> Items { get; set; } = new ConcurrentDictionary<string, List<ActivityDetailsDataOld>>();

        public int Count => Items.Count;

        public List<ActivityDetailsDataOld> this[string dateSession]
        {
            get => Get(dateSession);
            set
            {
                new NotImplementedException();
            }
        }

        public ActivityDetailsOld(string readDataJSON)
        {
            if (readDataJSON != "")
            {
                JObject obj = JObject.Parse(readDataJSON);
                foreach (var objItem in obj)
                {
                    JArray DetailsData = (JArray)objItem.Value;
                    List<ActivityDetailsDataOld> objActivityDetails = new List<ActivityDetailsDataOld>();
                    for (int iDetails = 0; iDetails < DetailsData.Count; iDetails++)
                    {
                        ActivityDetailsDataOld data = new ActivityDetailsDataOld();
                        JsonConvert.PopulateObject(JsonConvert.SerializeObject(DetailsData[iDetails]), data);
                        objActivityDetails.Add(data);
                    }

                    Items.TryAdd(objItem.Key, objActivityDetails);
                }
            }
            else
            {

            }
        }

        /// <summary>
        /// Get GameActivityDetails for a date session.
        /// </summary>
        /// <param name="dateSession"></param>
        /// <returns></returns>
        public List<ActivityDetailsDataOld> Get(string dateSession)
        {
            if (Items.TryGetValue(dateSession, out var item))
            {
                return item;
            }
            else
            {
                return new List<ActivityDetailsDataOld>();
            }
        }
    }

    public class ActivityDetailsDataOld
    {
        /// <summary>
        /// Gets or sets date log.
        /// </summary>
        [JsonProperty("datelog")]
        public DateTime? Datelog { get; set; }

        /// <summary>
        /// Gets or sets fps log.
        /// </summary>
        [JsonProperty("fps")]
        public int FPS { get; set; }

        /// <summary>
        /// Gets or sets cpu log.
        /// </summary>
        [JsonProperty("cpu")]
        public int CPU { get; set; }

        /// <summary>
        /// Gets or sets gpu log.
        /// </summary>
        [JsonProperty("gpu")]
        public int GPU { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [JsonProperty("ram")]
        public int RAM { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [JsonProperty("cpuT")]
        public int CPUT { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [JsonProperty("gpuT")]
        public int GPUT { get; set; }
    }

    public class GameActivityClassOld
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public int CountActivities => Activities.Count;
        public int CountActivitiesDetails => ActivitiesDetails.Count;


        public GameActivityClassOld(Guid gameId)
        {
            GameID = gameId;
        }

        /// <summary>
        /// Gets or sets list activities.
        /// </summary>
        public List<ActivityOld> Activities { get; set; } = new List<ActivityOld>();

        /// <summary>
        /// Gets or sets list activities details.
        /// </summary>
        public ActivityDetailsOld ActivitiesDetails { get; set; } = new ActivityDetailsOld("");


        /// <summary>
        /// Get game id.
        /// </summary>
        public Guid GameID { get; }

        /// <summary>
        /// Get game name.
        /// </summary>
        public string GameName
        {
            get
            {
                if (GameActivity.DatabaseReference.Games.Get(GameID) != null)
                {
                    return GameActivity.DatabaseReference.Games.Get(GameID).Name;
                }
                return null;
            }

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
                try
                {
                    if (genreIds?.Any() == true && GameActivity.DatabaseReference != null)
                    {
                        return new List<Genre>(GameActivity.DatabaseReference?.Genres.Where(a => genreIds.Contains(a.Id)).OrderBy(a => a.Name));
                    }
                }
                catch
                {
                    return null;
                }

                return null;
            }
        }


        public int avgCPU(string dateSession)
        {
            decimal avg = 0;

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

            List<ActivityDetailsDataOld> acDetailsData = ActivitiesDetails.Get(dateSession);
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

        public string GetDateSelectedSession(string dateSelected, string title)
        {
            if (!dateSelected.IsNullOrEmpty())
            {
                dateSelected = Convert.ToDateTime(dateSelected).ToString("yyyy-MM-dd");
            }

            int indicator = 1;
            for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
            {
                DateTime dateTemp = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                if (dateSelected == dateTemp.ToString("yyyy-MM-dd"))
                {
                    int titleValue = 0;
                    int.TryParse(title, out titleValue);
                    if (indicator == titleValue)
                    {
                        return dateTemp.ToUniversalTime().ToString("o");
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
        public ActivityOld GetLastSessionActivity()
        {
            // Easter eggs :)
            DateTime datePrev = new DateTime(1982, 12, 15, 00, 15, 23);
            DateTime dateLastSession = DateTime.Now;
            ActivityOld lastActivity = new ActivityOld();
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
        public List<ActivityDetailsDataOld> GetSessionActivityDetails(string dateSelected = "", string title = "")
        {
            string dateLastSession = GetDateSelectedSession(dateSelected, title);
            return ActivitiesDetails.Get(dateLastSession);
        }
    }
}
