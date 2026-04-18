using CommonPlayniteShared.Common;
using CommonPluginsShared;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Code-behind for the GameActivityBackup view.
    /// UI initialisation and chart wiring only — all business logic lives in
    /// <see cref="GameActivityBackupViewModel"/>.
    /// </summary>
    public partial class GameActivityBackup : UserControl
    {
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        public GameActivityBackup(ActivityBackup activityBackup)
        {
            InitializeComponent();

            Game game = API.Instance.Database.Games.Get(activityBackup.Id);

            // Build the ViewModel and wire close/window access via a factory callback
            // so the ViewModel does not hold a direct reference to the View (MVVM).
            var viewModel = new GameActivityBackupViewModel(
                activityBackup,
                game,
                PluginDatabase,
                closeWindow: () => Window.GetWindow(this)?.Close());

            DataContext = viewModel;

            // Wire chart control (cannot be done in ViewModel — control-specific API)
            InitializeChart(activityBackup, viewModel);
        }

        /// <summary>
        /// Configures the <see cref="PluginChartLog"/> child control.
        /// Kept in code-behind because the chart uses a control-specific API
        /// that cannot be bound through standard MVVM bindings.
        /// </summary>
        private void InitializeChart(ActivityBackup activityBackup, GameActivityBackupViewModel viewModel)
        {
            if (activityBackup.ItemsDetailsDatas?.Count == 0)
            {
                PART_ChartLogContener.Visibility = Visibility.Collapsed;
                return;
            }

            PluginChartLog chartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
            chartLog.SetDefaultDataContext();

            GameActivities pluginData = PluginDatabase.GetDefault(activityBackup.Id);
            pluginData.Items.Add(new Activity
            {
                IdConfiguration = activityBackup.IdConfiguration,
                GameActionName = activityBackup.GameActionName,
                DateSession = activityBackup.DateSession,
                SourceID = activityBackup.SourceID,
                PlatformIDs = activityBackup.PlatformIDs,
                ElapsedSeconds = activityBackup.ElapsedSeconds,
                Details = activityBackup.ItemsDetailsDatas ?? new List<ActivityDetailsData>()
            });

            chartLog.GetActivityForGamesLogGraphics(pluginData, 0, 10, activityBackup.DateSession, "1");
        }

        /// <summary>
        /// Fires after the layout has rendered so the chart can be sized to its container.
        /// The Loaded event must stay in code-behind because ActualWidth/Height are only
        /// available after the visual tree is fully measured.
        /// </summary>
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (PART_ChartLogContener.Visibility == Visibility.Collapsed)
            {
                return;
            }

            PluginChartLog chartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
            chartLog.Width = PART_ChartLogContener.ActualWidth;
            chartLog.Height = PART_ChartLogContener.ActualHeight;
            ((PluginChartLogDataContext)chartLog.DataContext).UseControls = false;
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  VIEW MODEL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for <see cref="GameActivityBackup"/>.
    /// Exposes display data and the three user actions (Add / Close / Remove)
    /// as <see cref="RelayCommand"/> instances so the view is fully declarative.
    /// </summary>
    public class GameActivityBackupViewModel : ObservableObject
    {
        // ── Dependencies ─────────────────────────────────────────────────────
        private readonly ActivityBackup _activityBackup;
        private readonly Game _game;
        private readonly GameActivityDatabase _pluginDatabase;
        private readonly Action _closeWindow;

        // ── Bindable properties ──────────────────────────────────────────────

        private string _name;
        /// <summary>Game name shown in the title TextBlock.</summary>
        public string Name
        {
            get => _name;
            private set => SetValue(ref _name, value);
        }

        private string _cover;
        /// <summary>Full file path of the game cover image.</summary>
        public string Cover
        {
            get => _cover;
            private set => SetValue(ref _cover, value);
        }

        private DateTime? _dateSession;
        /// <summary>Date and time when the backup session started.</summary>
        public DateTime? DateSession
        {
            get => _dateSession;
            private set => SetValue(ref _dateSession, value);
        }

        private ulong _elapsedSeconds;
        /// <summary>Duration of the backup session in seconds.</summary>
        public ulong ElapsedSeconds
        {
            get => _elapsedSeconds;
            private set => SetValue(ref _elapsedSeconds, value);
        }

        private DateTime? _dateLastPlayed;
        /// <summary>Date the game was last played according to Playnite.</summary>
        public DateTime? DateLastPlayed
        {
            get => _dateLastPlayed;
            private set => SetValue(ref _dateLastPlayed, value);
        }

        private ulong _playtime;
        /// <summary>Total recorded playtime in seconds from Playnite.</summary>
        public ulong Playtime
        {
            get => _playtime;
            private set => SetValue(ref _playtime, value);
        }

        // ── Commands ─────────────────────────────────────────────────────────

        /// <summary>
        /// Merges the backup session into Playnite and the plugin database,
        /// then deletes the backup file.
        /// </summary>
        public RelayCommand CmdAdd { get; }

        /// <summary>Closes the host window without making any changes.</summary>
        public RelayCommand CmdClose { get; }

        /// <summary>Deletes the backup file without importing the session.</summary>
        public RelayCommand CmdRemove { get; }

        // ── Constructor ──────────────────────────────────────────────────────

        public GameActivityBackupViewModel(
            ActivityBackup activityBackup,
            Game game,
            GameActivityDatabase pluginDatabase,
            Action closeWindow)
        {
            _activityBackup = activityBackup;
            _game = game;
            _pluginDatabase = pluginDatabase;
            _closeWindow = closeWindow;

            // Populate display data
            Name = activityBackup.Name;
            DateSession = activityBackup.DateSession;
            ElapsedSeconds = activityBackup.ElapsedSeconds;
            DateLastPlayed = game?.LastActivity;
            Playtime = game?.Playtime ?? 0;

            if (!game?.CoverImage.IsNullOrEmpty() == true)
            {
                Cover = API.Instance.Database.GetFullFilePath(game.CoverImage);
            }

            // Wire commands
            CmdClose = new RelayCommand(ExecuteClose);
            CmdRemove = new RelayCommand(ExecuteRemove);
            CmdAdd = new RelayCommand(ExecuteAdd);
        }

        // ── Command implementations ──────────────────────────────────────────

        /// <summary>Closes the dialog with no side-effects.</summary>
        private void ExecuteClose()
        {
            _closeWindow?.Invoke();
        }

        /// <summary>
        /// Persists the backup session into both Playnite and the plugin database,
        /// then removes the backup file from disk.
        /// </summary>
        private void ExecuteAdd()
        {
            try
            {
                // Update playtime in Playnite
                _game.Playtime += _activityBackup.ElapsedSeconds;

                // Retrieve existing plugin data and append the recovered session
                GameActivities pluginData = _pluginDatabase.Get(_activityBackup.Id);
                pluginData.Items.Add(new Activity
                {
                    IdConfiguration = _activityBackup.IdConfiguration,
                    GameActionName = _activityBackup.GameActionName,
                    DateSession = _activityBackup.DateSession,
                    SourceID = _activityBackup.SourceID,
                    PlatformIDs = _activityBackup.PlatformIDs,
                    ElapsedSeconds = _activityBackup.ElapsedSeconds,
                    Details = _activityBackup.ItemsDetailsDatas ?? new List<ActivityDetailsData>()
                });

                // Persist both changes
                API.Instance.Database.Games.Update(_game);
                _pluginDatabase.Update(pluginData);

                // Clean up the backup file
                DeleteBackupFile();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, _pluginDatabase.PluginName);
            }

            _closeWindow?.Invoke();
        }

        /// <summary>
        /// Discards the backup by deleting its file, without touching Playnite data.
        /// </summary>
        private void ExecuteRemove()
        {
            try
            {
                DeleteBackupFile();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, _pluginDatabase.PluginName);
            }

            _closeWindow?.Invoke();
        }

        /// <summary>
        /// Deletes the JSON backup file for the current game session from disk.
        /// </summary>
        private void DeleteBackupFile()
        {
            string path = Path.Combine(
                _pluginDatabase.Paths.PluginUserDataPath,
                $"SaveSession_{_game.Id}.json");

            FileSystem.DeleteFile(path);
        }
    }
}