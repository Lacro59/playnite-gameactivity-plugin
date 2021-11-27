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

        public string GameActionName { get; set; }

        [DontSerialize]
        public string SourceName
        {
            get
            {
                return PlayniteTools.GetSourceBySourceIdOrPlatformId(SourceID, PlatformIDs);
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
