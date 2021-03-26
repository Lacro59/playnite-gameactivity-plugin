using CommonPluginsPlaynite.Common;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace GameActivity.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (ActivityDatabase)_PluginDatabase;
            }
        }

        private PluginButtonDataContext ControlDataContext;
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
            }
        }


        public PluginButton()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext = new PluginButtonDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton,
                DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails,

                Text = "\ue97f",
                LastActivity = string.Empty,
                LastPlaytime = 0
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameActivities gameActivities = (GameActivities)PluginGameData;

                if (gameActivities.HasData)
                {
                    ControlDataContext.LastActivity = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
                    ControlDataContext.LastPlaytime = gameActivities.GetLastSessionActivity().ElapsedSeconds;
                }
                else
                {
                    ControlDataContext.DisplayDetails = false;
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }


        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            var ViewExtension = new GameActivityViewSingle(PluginDatabase.GameContext);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool DisplayDetails { get; set; }

        public string Text { get; set; }
        public string LastActivity { get; set; }
        public long LastPlaytime { get; set; }
    }
}
