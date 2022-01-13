using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services;
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
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;
        private Game game;


        public GameActivityMergeTime(Game game)
        {
            this.game = game;
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
                var pluginDataRoot = PluginDatabase.Get(game, true);
                var TimeRoot = ((Activity)PART_CbTimeRoot.SelectedItem);                
                var Time = ((Activity)PART_CbTime.SelectedItem);


                pluginDataRoot.Items.Find(x => x.DateSession == TimeRoot.DateSession).ElapsedSeconds += Time.ElapsedSeconds;
                pluginDataRoot.ItemsDetails.Items[(DateTime)TimeRoot.DateSession].AddRange(pluginDataRoot.ItemsDetails.Items[(DateTime)Time.DateSession]);

                pluginDataRoot.Items.Remove(Time);
                pluginDataRoot.ItemsDetails.Items.TryRemove((DateTime)Time.DateSession, out List<ActivityDetailsData> deleted);

                game.PlayCount--;


                PluginDatabase.Update(pluginDataRoot);
                PluginDatabase.PlayniteApi.Database.Games.Update(game);
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
                PART_CbTime.ItemsSource = PluginDatabase.Get(game, true).Items.Where(x => x.DateSession > ((Activity)PART_CbTimeRoot.SelectedItem).DateSession).ToList();
            }

            if (PART_CbTimeRoot.SelectedIndex == -1 || PART_CbTime.SelectedIndex == -1)
            {
                PART_BtMerge.IsEnabled = false;
            }
            else
            {
                if (((Activity)PART_CbTimeRoot.SelectedItem).DateSession == ((Activity)PART_CbTime.SelectedItem).DateSession)
                {
                    PART_BtMerge.IsEnabled = false;
                }
                else
                {
                    PART_BtMerge.IsEnabled = true;
                }
            }
        }
    }
}
