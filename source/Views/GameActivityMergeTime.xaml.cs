using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityMergeTime.xaml
    /// </summary>
    public partial class GameActivityMergeTime : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private Game GameContext { get; set; }


        public GameActivityMergeTime(Game game)
        {
            this.GameContext = game;
            InitializeComponent();

            PART_CbTimeRoot.ItemsSource = PluginDatabase.Get(game, true).Items;
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtMerge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GameActivities pluginDataRoot = PluginDatabase.Get(GameContext, true);
                Activity TimeRoot = (Activity)PART_CbTimeRoot.SelectedItem;
                Activity Time = (Activity)PART_CbTime.SelectedItem;


                pluginDataRoot.Items.Find(x => x.DateSession == TimeRoot.DateSession).ElapsedSeconds += Time.ElapsedSeconds;
                pluginDataRoot.ItemsDetails.Items[(DateTime)TimeRoot.DateSession].AddRange(pluginDataRoot.ItemsDetails.Items[(DateTime)Time.DateSession]);

                pluginDataRoot.Items.Remove(Time);
                pluginDataRoot.ItemsDetails.Items.TryRemove((DateTime)Time.DateSession, out List<ActivityDetailsData> deleted);


                // Set last played date
                GameContext.LastActivity = (DateTime)pluginDataRoot.Items.Max(x => x.DateSession);

                if (GameContext.PlayCount != 0)
                {
                    GameContext.PlayCount--;
                }
                else
                {
                    Logger.Warn($"Play count is already at 0 for {GameContext.Name}");
                }

                PluginDatabase.Update(pluginDataRoot);
                PluginDatabase.PlayniteApi.Database.Games.Update(GameContext);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)this.Parent).Close();
        }


        private void PART_Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PART_CbTimeRoot.SelectedIndex != -1 && ((FrameworkElement)sender).Name == "PART_CbTimeRoot")
            {
                PART_CbTime.ItemsSource = PluginDatabase.Get(GameContext, true).Items.Where(x => x.DateSession > ((Activity)PART_CbTimeRoot.SelectedItem).DateSession).ToList();
            }

            PART_BtMerge.IsEnabled = PART_CbTimeRoot.SelectedIndex == -1 || PART_CbTime.SelectedIndex == -1
                ? false
                : ((Activity)PART_CbTimeRoot.SelectedItem).DateSession != ((Activity)PART_CbTime.SelectedItem).DateSession;
        }
    }
}
