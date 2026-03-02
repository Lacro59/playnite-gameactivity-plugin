using CommonPluginsShared;
using GameActivity.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Services
{
    public class GameActivityStats
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;


        public static ulong GetPlayTimeYear(uint year, bool withHidden)
        {
            try
            {
                return (ulong)PluginDatabase.GetListGameActivity()
						.Where(x => x.HasData && !x.IsDeleted && (withHidden || x.Hidden == false))
                        .SelectMany(x => x.Items)
                        .Where(x => x.DateSession != null && ((DateTime)x.DateSession).ToLocalTime().Year == year)
                        .Sum(x => (long)x.ElapsedSeconds);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return 0;
        }

        public static ulong GetPlayTimeYearMonth(uint year, uint month, bool withHidden)
        {
            try
            {
                return (ulong)PluginDatabase.GetListGameActivity()
						.Where(x => x.HasData && !x.IsDeleted && (withHidden || x.Hidden == false))
                        .SelectMany(x => x.Items)
                        .Where(x => x.DateSession != null && ((DateTime)x.DateSession).ToLocalTime().Year == year && ((DateTime)x.DateSession).ToLocalTime().Month == month)
                        .Sum(x => (long)x.ElapsedSeconds);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return 0;
        }




    }
}
