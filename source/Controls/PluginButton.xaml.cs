using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
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
using System.Windows;

namespace GameActivity.Controls
{
    public partial class PluginButton : PluginUserControlExtend
    {
        private GameActivity _plugin;

        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)value; // Bug fix: was 'controlDataContext'
        }


        public PluginButton(GameActivity plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
        }

        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.Database.ItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.Database.ItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();
                API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;
            ControlDataContext.Text = "\ue97f";
            ControlDataContext.LastActivity = string.Empty;
            ControlDataContext.LastPlaytime = 0;
            ControlDataContext.LastPlaytimeString = string.Empty;
        }


        /// <summary>
        /// Populates last-session data. DisplayDetails is reset to false when no data exists
        /// so the detail panel collapses without modifying the user's setting.
        /// </summary>
        public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
        {
            GameActivities gameActivities = (GameActivities)pluginGameData;

            if (!gameActivities.HasData)
            {
                ControlDataContext.DisplayDetails = false;
                return;
            }

            Activity lastSessionActivity = gameActivities.GetLastSessionActivity();
            ulong elapsedSeconds = lastSessionActivity.ElapsedSeconds;

            ControlDataContext.LastActivity = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
            ControlDataContext.LastPlaytime = elapsedSeconds;
            ControlDataContext.LastPlaytimeString = new PlayTimeToStringConverter()
                .Convert(elapsedSeconds, null, null, null)
                .ToString();
        }


        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginWindows.ShowPluginGameDataWindow(_plugin, CurrentGame);
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private bool _displayDetails;
        public bool DisplayDetails { get => _displayDetails; set => SetValue(ref _displayDetails, value); }

        private string _text;
        public string Text { get => _text; set => SetValue(ref _text, value); }

        private string _lastActivity;
        public string LastActivity { get => _lastActivity; set => SetValue(ref _lastActivity, value); }

        private ulong _lastPlaytime;
        public ulong LastPlaytime { get => _lastPlaytime; set => SetValue(ref _lastPlaytime, value); }

        private string _lastPlaytimeString;
        public string LastPlaytimeString { get => _lastPlaytimeString; set => SetValue(ref _lastPlaytimeString, value); }
    }
}