using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
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

    public class Activity
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
}
