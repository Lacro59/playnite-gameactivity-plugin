using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;

namespace GameActivity.Models
{
    public class ActivityDetails
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public ConcurrentDictionary<string, List<ActivityDetailsData>> Items { get; set; } = new ConcurrentDictionary<string, List<ActivityDetailsData>>();

        public int Count => Items.Count;

        public List<ActivityDetailsData> this[string dateSession]
        {
            get => Get(dateSession);
            set
            {
                new NotImplementedException();
            }
        }

        public ActivityDetails(string readDataJSON)
        {
            if (readDataJSON != "")
            {
                JObject obj = JObject.Parse(readDataJSON);
                foreach (var objItem in obj)
                {
                    JArray DetailsData = (JArray)objItem.Value;
                    List<ActivityDetailsData> objActivityDetails = new List<ActivityDetailsData>();
                    for (int iDetails = 0; iDetails < DetailsData.Count; iDetails++)
                    {
                        ActivityDetailsData data = new ActivityDetailsData();
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
        public List<ActivityDetailsData> Get(string dateSession)
        {
            if (Items.TryGetValue(dateSession, out var item))
            {
                return item;
            }
            else
            {
                return new List<ActivityDetailsData>();
            }
        }
    }

    public class ActivityDetailsData
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
}
