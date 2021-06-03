using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;

namespace GameActivity.Models
{
    public class ActivityDetails : ObservableObject
    {
        public ConcurrentDictionary<DateTime, List<ActivityDetailsData>> Items { get; set; } = new ConcurrentDictionary<DateTime, List<ActivityDetailsData>>();

        [JsonIgnore]
        public int Count => Items.Count;

        public List<ActivityDetailsData> this[DateTime dateSession]
        {
            get => Get(dateSession);
        }
        
        /// <summary>
        /// Get GameActivityDetails for a date session.
        /// </summary>
        /// <param name="dateSession"></param>
        /// <returns></returns>
        public List<ActivityDetailsData> Get(DateTime dateSession)
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
