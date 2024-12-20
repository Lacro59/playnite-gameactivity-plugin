using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using Playnite.SDK;
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
        private GameActivity Plugin { get; set; }

        private ActivityDatabase PluginDatabase { get; set; } = GameActivity.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext { get; set; } = new PluginButtonDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)controlDataContext;
        }


        public PluginButton(GameActivity plugin)
        {
            Plugin = plugin;

            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

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

            GameActivityViewSingle ViewExtension = new GameActivityViewSingle(Plugin, PluginDatabase.GameContext);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivity"), ViewExtension, windowOptions);
            _ = windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        public bool _displayDetails;
        public bool DisplayDetails { get => _displayDetails; set => SetValue(ref _displayDetails, value); }

        public string _text;
        public string Text { get => _text; set => SetValue(ref _text, value); }

        public string _lastActivity;
        public string LastActivity { get => _lastActivity; set => SetValue(ref _lastActivity, value); }

        public ulong _lastPlaytime;
        public ulong LastPlaytime { get => _lastPlaytime; set => SetValue(ref _lastPlaytime, value); }

    }
}
