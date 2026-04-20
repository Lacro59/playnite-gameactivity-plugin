using CommonPluginsShared.Controls;
using GameActivity.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivity.Services
{
    public class GameActivityViewSidebar : SidebarItem
    {
        public GameActivityViewSidebar(GameActivity plugin)
        {
            Type = SiderbarItemType.View;
            Title = ResourceProvider.GetString("LOCGameActivityViewGamesActivities");
            Icon = new TextBlock
            {
                Text = "\ue97f",
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Opened = () =>
            {
                if (plugin.SidebarItemControl == null)
                {
                    plugin.SidebarItemControl = new SidebarItemControl();
                    plugin.SidebarItemControl.SetTitle(ResourceProvider.GetString("LOCGamesActivitiesTitle"));
                    plugin.SidebarView = new GameActivityView(plugin);
                    plugin.SidebarItemControl.AddContent(plugin.SidebarView);
                }
                else
                {
                    plugin.SidebarView?.RefreshOnReopen();
                }
                return plugin.SidebarItemControl;
            };
            Visible = plugin.PluginSettingsViewModel.Settings.EnableIntegrationButtonSide;
        }
    }
}