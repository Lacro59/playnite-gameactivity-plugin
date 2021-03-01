using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using System;
using System.Collections.Generic;
using GameActivity.Services;

namespace GameActivity.Models
{
    public class Activity : ObservableObject
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public Guid SourceID { get; set; }
        public Guid PlatformID { get; set; }


        [JsonIgnore]
        public string SourceName
        {
            get
            {
                if (SourceID != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    try
                    {
                        var Source = PluginDatabase.PlayniteApi.Database.Sources.Get(SourceID);

                        if (Source == null)
                        {
                            logger.Warn($"SourceName not find for {SourceID.ToString()} && {PlatformID.ToString()}");
                            return "Playnite";
                        }

                        return Source.Name;
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Common.LogError(ex, true, $"SourceId: {SourceID.ToString()} && {PlatformID.ToString()}");
#endif
                        return "Playnite";
                    }
                }

                if (PlatformID != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    var platform = PluginDatabase.PlayniteApi.Database.Platforms.Get(PlatformID);

                    if (platform != null)
                    {
                        switch (platform.Name.ToLower())
                        {
                            case "pc":
                                return "Playnite";
                            default:
                                return platform.Name;
                        }
                    }
                }

                return "Playnite";
            }
        }

        public DateTime? DateSession { get; set; }

        public long ElapsedSeconds { get; set; } = 0;
    }
}
