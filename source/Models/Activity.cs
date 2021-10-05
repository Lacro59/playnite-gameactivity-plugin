using Playnite.SDK;
using Playnite.SDK.Data;
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
        // TODO Must deleted
        public Guid PlatformID { get; set; }
        public List<Guid> PlatformIDs { get; set; }


        [DontSerialize]
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
                        Common.LogError(ex, true, $"SourceId: {SourceID.ToString()} && {PlatformID.ToString()}");
                        return "Playnite";
                    }
                }

                foreach (Guid PlatformID in PlatformIDs)
                {
                    if (PlatformID != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                    {
                        var platform = PluginDatabase.PlayniteApi.Database.Platforms.Get(PlatformID);

                        if (platform != null)
                        {
                            switch (platform.Name.ToLower())
                            {
                                case "pc":
                                case "pc (windows)":
                                case "pc (mac)":
                                case "pc (linux)":
                                    return "Playnite";
                                default:
                                    return platform.Name;
                            }
                        }
                    }
                }

                return "Playnite";
            }
        }

        public int IdConfiguration { get; set; } = -1;

        [DontSerialize]
        public SystemConfiguration Configuration
        {
            get
            {
                if (IdConfiguration == -1)
                {
                    return new SystemConfiguration();
                }

                if (IdConfiguration >= PluginDatabase.LocalSystem.GetConfigurations().Count)
                {
                    return new SystemConfiguration();
                }

                return PluginDatabase.LocalSystem.GetConfigurations()[IdConfiguration];
            }
        }

        public DateTime? DateSession { get; set; }

        public ulong ElapsedSeconds { get; set; } = 0;
    }
}
