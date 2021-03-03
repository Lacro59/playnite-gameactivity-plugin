using CommonPluginsPlaynite.Common;
using CommonPluginsShared;
using CommonPluginsShared.Controls;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameActivity.Controls
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButton.xaml
    /// </summary>
    public partial class GameActivityButton : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        public GameActivityButton()
        {
            InitializeComponent();

            PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
            PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
            PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

            // Apply settings
            PluginSettings_PropertyChanged(null, null);
        }


        #region OnPropertyChange
        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Apply settings
            this.DataContext = new
            {

            };

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;

            // When control is not used
            if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationButton)
            {
                return;
            }

            string LastActivity = string.Empty;
            long LastPlaytime = 0;
            bool DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;

            if (newContext != null)
            {
                GameActivities gameActivities = PluginDatabase.Get(newContext);

                if (gameActivities.HasData)
                {
                    LastActivity = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
                    LastPlaytime = gameActivities.GetLastSessionActivity().ElapsedSeconds;
                }
                else
                {
                    DisplayDetails = false;
                }
            }

            this.DataContext = new
            {
                DisplayDetails,
                LastActivity,
                LastPlaytime
            };
        }
        #endregion


        private void PART_GameActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var ViewExtension = new GameActivityViewSingle(PluginDatabase.GameContext);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }
    }
}
