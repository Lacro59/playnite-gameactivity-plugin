using CommonPlayniteShared.Common;
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GameActivity.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
    {
        private GameActivity plugin { get; set; }

        private ActivityDatabase PluginDatabase { get; set; } = GameActivity.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get => PluginDatabase;
            set => PluginDatabase = (ActivityDatabase)_PluginDatabase;
        }

        private PluginButtonDataContext ControlDataContext { get; set; } = new PluginButtonDataContext();
        internal override IDataContext _ControlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
        }


        public PluginButton(GameActivity plugin)
        {
            this.plugin = plugin;

            InitializeComponent();
            this.DataContext = ControlDataContext;

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
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;

            ControlDataContext.Text = "\ue97f";
            ControlDataContext.LastActivity = string.Empty;
            ControlDataContext.LastPlaytime = 0;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
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
        }


        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = true,
                ShowCloseButton = true,
                CanBeResizable = true,
                Height = 740,
                Width = 1280
            };

            GameActivityViewSingle ViewExtension = new GameActivityViewSingle(plugin, PluginDatabase.GameContext);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension, windowOptions);
            windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        public bool _DisplayDetails;
        public bool DisplayDetails { get => _DisplayDetails; set => SetValue(ref _DisplayDetails, value); }

        public string _Text;
        public string Text { get => _Text; set => SetValue(ref _Text, value); }

        public string _LastActivity;
        public string LastActivity { get => _LastActivity; set => SetValue(ref _LastActivity, value); }

        public ulong _LastPlaytime;
        public ulong LastPlaytime { get => _LastPlaytime; set => SetValue(ref _LastPlaytime, value); }

    }
}
