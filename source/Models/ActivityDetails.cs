using Playnite.SDK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GameActivity.Models
{
    public class ActivityDetails : ObservableObject
    {
        public ConcurrentDictionary<DateTime, List<ActivityDetailsData>> Items { get; set; } = new ConcurrentDictionary<DateTime, List<ActivityDetailsData>>();

        [DontSerialize]
        public int Count => Items.Count;

        [DontSerialize]
        public List<ActivityDetailsData> this[DateTime dateSession] => Get(dateSession);

        /// <summary>
        /// Get GameActivityDetails for a date session.
        /// </summary>
        /// <param name="dateSession"></param>
        /// <returns></returns>
        public List<ActivityDetailsData> Get(DateTime dateSession)
        {
            return Items.TryGetValue(dateSession, out List<ActivityDetailsData> item) ? item : new List<ActivityDetailsData>();
        }

        [DontSerialize]
        public int AvgFpsAllSession => GetAvgFpsAllSession();

        private int GetAvgFpsAllSession()
        {
            int AvgFps = 0;
            int div = 1;

            foreach (KeyValuePair<DateTime, List<ActivityDetailsData>> Item in Items)
            {
                foreach (ActivityDetailsData el in Item.Value)
                {
                    if (el.FPS != 0)
                    {
                        AvgFps += el.FPS;
                        div++;
                    }
                }
            }

            return AvgFps / div;
        }
    }

    public class ActivityDetailsData
    {
        /// <summary>
        /// Gets or sets date log.
        /// </summary>
        [SerializationPropertyName("datelog")]
        public DateTime? Datelog { get; set; }

        /// <summary>
        /// Gets or sets fps log.
        /// </summary>
        [SerializationPropertyName("fps")]
        public int FPS { get; set; }

        /// <summary>
        /// Gets or sets cpu log.
        /// </summary>
        [SerializationPropertyName("cpu")]
        public int CPU { get; set; }

        /// <summary>
        /// Gets or sets gpu log.
        /// </summary>
        [SerializationPropertyName("gpu")]
        public int GPU { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [SerializationPropertyName("ram")]
        public int RAM { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [SerializationPropertyName("cpuT")]
        public int CPUT { get; set; }

        /// <summary>
        /// Gets or sets ram log.
        /// </summary>
        [SerializationPropertyName("gpuT")]
        public int GPUT { get; set; }


        [SerializationPropertyName("cpuP")]
        public int CPUP { get; set; }

        [SerializationPropertyName("gpuP")]
        public int GPUP { get; set; }
    }
}
