using Newtonsoft.Json;
using Playnite.SDK;
using System;

namespace GameActivity.Models
{
    /// <summary>
    /// Specifies <see cref="Activity"/> fields.
    /// </summary>
    public enum ActivityField
    {
        sourceID,
        dateSession,
        elapsedSeconds
    }

    class Activity
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
                    return GameActivity.DatabaseReference.Sources.Get(SourceID).Name;
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
}
