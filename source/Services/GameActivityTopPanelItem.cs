using CommonPluginsShared;
using GameActivity.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivity.Services
{
    public class GameActivityTopPanelItem : TopPanelItem
    {
        public GameActivityTopPanelItem(GameActivity plugin)
        {
            Icon = new TextBlock
            {
                Text = "\ue97f",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Title = ResourceProvider.GetString("LOCGameActivityViewGamesActivities");
            Activated = () =>
            {
                GameActivity.PluginDatabase.PluginWindows.ShowPluginGameDataWindow(plugin);
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonHeader;
        }
    }
}
