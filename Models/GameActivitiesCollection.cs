using Playnite.SDK;
using PluginCommon.Collections;
using PluginCommon.PlayniteResources.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    public class GameActivitiesCollection : PluginItemCollection<GameActivities>
    {
        public GameActivitiesCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}
