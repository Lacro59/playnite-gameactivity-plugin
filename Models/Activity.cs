using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Collections.Generic;

namespace GameActivity.Models
{
    public class Activity : ObservableObject
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
                        var Source = GameActivity.DatabaseReference.Sources.Get(SourceID);

                        if (Source == null)
                        {
                            logger.Warn($"GameActivity - SourceName not find for {SourceID}");
                            return "Playnite";
                        }

                        return Source.Name;
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Common.LogError(ex, "GameActivity", $"SourceId : {SourceID}");
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
