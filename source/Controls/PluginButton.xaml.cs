using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Windows;

namespace GameActivity.Controls
{
    /// <summary>
    /// Interaction logic for PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
    {
        private GameActivity _plugin;

        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)value;
        }

        public PluginButton(GameActivity plugin)
        {
#if DEBUG
            var timer = new DebugTimer("PluginButton.ctor");
#endif

            _plugin = plugin;
            InitializeComponent();

#if DEBUG
            timer.Step("InitializeComponent done");
#endif

            DataContext = ControlDataContext;
            Loaded += OnLoaded;

#if DEBUG
            timer.Stop();
#endif
        }

        /// <summary>
        /// Attaches static event handlers for the GameActivity plugin.
        /// Plugin-specific handlers are guarded by <see cref="PluginUserControlExtendBase.AttachPluginEvents"/> to prevent
        /// double-subscription when multiple instances of this control exist simultaneously.
        /// </summary>
        protected override void AttachStaticEvents()
        {
#if DEBUG
            var timer = new DebugTimer("PluginButton.AttachStaticEvents");
#endif

            base.AttachStaticEvents();

#if DEBUG
            timer.Step("base done");
#endif

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
#if DEBUG
                timer.Step("registering plugin-specific handlers");
#endif

                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();
            });

#if DEBUG
            timer.Stop();
#endif
        }

        public override void SetDefaultDataContext()
        {
#if DEBUG
            var timer = new DebugTimer("PluginButton.SetDefaultDataContext");
#endif

            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationButton;
            ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.EnableIntegrationButtonDetails;
            ControlDataContext.Text = "\ue97f";
            ControlDataContext.LastActivity = string.Empty;
            ControlDataContext.LastPlaytime = 0;
            ControlDataContext.LastPlaytimeString = string.Empty;

#if DEBUG
            timer.Stop(string.Format("IsActivated={0}, DisplayDetails={1}", ControlDataContext.IsActivated, ControlDataContext.DisplayDetails));
#endif
        }

        /// <summary>
        /// Populates last-session data. DisplayDetails is reset to false when no data exists
        /// so the detail panel collapses without modifying the user's setting.
        /// </summary>
        public override void SetData(Game newContext, PluginGameEntry pluginGameData)
        {
#if DEBUG
            var timer = new DebugTimer(string.Format("PluginButton.SetData(game='{0}')", newContext?.Name ?? "null"));
#endif

            GameActivities gameActivities = (GameActivities)pluginGameData;

            if (!gameActivities.HasData)
            {
                ControlDataContext.DisplayDetails = false;

#if DEBUG
                timer.Stop("HasData=false, DisplayDetails collapsed");
#endif
                return;
            }

            Activity lastSessionActivity = gameActivities.GetLastSessionActivity();
            ulong elapsedSeconds = lastSessionActivity.ElapsedSeconds;

            ControlDataContext.LastActivity = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
            ControlDataContext.LastPlaytime = elapsedSeconds;
            ControlDataContext.LastPlaytimeString = new PlayTimeToStringConverter()
                .Convert(elapsedSeconds, null, null, null)
                .ToString();

#if DEBUG
            timer.Stop(string.Format("LastActivity={0}, ElapsedSeconds={1}", ControlDataContext.LastActivity, elapsedSeconds));
#endif
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