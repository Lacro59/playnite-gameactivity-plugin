using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// ViewModel for merging two sessions of the same game.
    /// </summary>
    public class GameActivityMergeTimeViewModel : ObservableObject
    {
        private const string GlyphSortAscending = "\uea6a";
        private const string GlyphSortDescending = "\uea67";

        private static ILogger Logger => LogManager.GetLogger();
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        private readonly Game _gameContext;

        private ObservableCollection<Activity> _rootActivities;
        /// <summary>Candidate sessions for the merge root (kept session).</summary>
        public ObservableCollection<Activity> RootActivities
        {
            get => _rootActivities;
            private set => SetValue(ref _rootActivities, value);
        }

        private ObservableCollection<Activity> _mergeActivities;
        /// <summary>Sessions that can be absorbed into the selected root (later than root).</summary>
        public ObservableCollection<Activity> MergeActivities
        {
            get => _mergeActivities;
            private set => SetValue(ref _mergeActivities, value);
        }

        private Activity _selectedRootActivity;
        /// <summary>Session that remains after the merge.</summary>
        public Activity SelectedRootActivity
        {
            get => _selectedRootActivity;
            set
            {
                SetValue(ref _selectedRootActivity, value);
                RefreshMergeActivities();
                OnPropertyChanged(nameof(CanMerge));
            }
        }

        private Activity _selectedMergeActivity;
        /// <summary>Session absorbed into <see cref="SelectedRootActivity"/>.</summary>
        public Activity SelectedMergeActivity
        {
            get => _selectedMergeActivity;
            set
            {
                SetValue(ref _selectedMergeActivity, value);
                OnPropertyChanged(nameof(CanMerge));
            }
        }

        private bool _sortDescending;
        /// <summary>When true, combo boxes list newest sessions first.</summary>
        public bool SortDescending
        {
            get => _sortDescending;
            private set
            {
                if (_sortDescending == value)
                {
                    return;
                }

                SetValue(ref _sortDescending, value);
                OnPropertyChanged(nameof(SortGlyph));
                OnPropertyChanged(nameof(SortToolTip));
            }
        }

        /// <summary>IcoFont caret reflecting the current sort direction.</summary>
        public string SortGlyph => SortDescending ? GlyphSortDescending : GlyphSortAscending;

        /// <summary>Localized tooltip for the current sort direction.</summary>
        public string SortToolTip => SortDescending
            ? ResourceProvider.GetString("LOCMenuSortDescending")
            : ResourceProvider.GetString("LOCMenuSortAscending");

        /// <summary>True when root and merge selections are distinct and ready to merge.</summary>
        public bool CanMerge
        {
            get
            {
                if (SelectedRootActivity == null || SelectedMergeActivity == null)
                {
                    return false;
                }

                return SelectedRootActivity.DateSession != SelectedMergeActivity.DateSession;
            }
        }

        /// <summary>Merges the selected sessions.</summary>
        public RelayCommand MergeCommand { get; }

        /// <summary>Closes the dialog without merging.</summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>Toggles ascending / descending session order and persists the preference.</summary>
        public RelayCommand ToggleSortCommand { get; }

        /// <summary>Raised when the host window should close.</summary>
        public event EventHandler CloseRequested;

        /// <summary>
        /// Creates the ViewModel for the given game and loads sessions with the persisted sort order.
        /// </summary>
        /// <param name="game">Game whose sessions can be merged.</param>
        public GameActivityMergeTimeViewModel(Game game)
        {
            _gameContext = game;

            _sortDescending = PluginDatabase.PluginSettings == null
                || PluginDatabase.PluginSettings.MergeSessionsSortDescending;

            RootActivities = new ObservableCollection<Activity>(
                OrderActivities(PluginDatabase.Get(game, true).Items));
            MergeActivities = new ObservableCollection<Activity>();

            Common.LogDebug(string.Format(
                "Merge sessions dialog opened — descending={0}, game={1}, sessions={2}",
                SortDescending,
                game?.Name,
                RootActivities.Count));

            MergeCommand = new RelayCommand(ExecuteMerge, () => CanMerge);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ToggleSortCommand = new RelayCommand(ExecuteToggleSort);
        }

        private IOrderedEnumerable<Activity> OrderActivities(IEnumerable<Activity> items)
        {
            if (SortDescending)
            {
                return items.OrderByDescending(x => x.DateSession);
            }

            return items.OrderBy(x => x.DateSession);
        }

        private void RefreshRootActivities()
        {
            DateTime? selectedRoot = SelectedRootActivity?.DateSession;
            DateTime? selectedMerge = SelectedMergeActivity?.DateSession;

            RootActivities = new ObservableCollection<Activity>(
                OrderActivities(PluginDatabase.Get(_gameContext, true).Items));

            if (selectedRoot.HasValue)
            {
                SelectedRootActivity = RootActivities.FirstOrDefault(x => x.DateSession == selectedRoot.Value);
            }
            else
            {
                SelectedRootActivity = null;
            }

            if (selectedMerge.HasValue)
            {
                SelectedMergeActivity = MergeActivities.FirstOrDefault(x => x.DateSession == selectedMerge.Value);
            }
        }

        private void RefreshMergeActivities()
        {
            MergeActivities.Clear();
            SelectedMergeActivity = null;

            if (SelectedRootActivity == null)
            {
                return;
            }

            List<Activity> items = OrderActivities(
                    PluginDatabase.Get(_gameContext, true).Items
                        .Where(x => x.DateSession > SelectedRootActivity.DateSession))
                .ToList();

            foreach (Activity activity in items)
            {
                MergeActivities.Add(activity);
            }
        }

        private void ExecuteToggleSort()
        {
            SortDescending = !SortDescending;

            if (PluginDatabase.PluginSettings == null)
            {
                Logger.Warn("Merge sessions sort changed but PluginSettings is null — preference not saved");
            }
            else
            {
                PluginDatabase.PluginSettings.MergeSessionsSortDescending = SortDescending;
                if (PluginDatabase.PersistSettingsAction == null)
                {
                    Logger.Warn("Merge sessions sort changed but PersistSettingsAction is null — preference not saved");
                }
                else
                {
                    PluginDatabase.PersistSettingsAction.Invoke();
                }
            }

            Common.LogDebug(string.Format(
                "Merge sessions sort toggled — descending={0}, game={1}",
                SortDescending,
                _gameContext?.Name));

            RefreshRootActivities();
        }

        private void ExecuteCancel()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteMerge()
        {
            if (!CanMerge)
            {
                return;
            }

            try
            {
                GameActivities pluginDataRoot = PluginDatabase.Get(_gameContext, true);
                Activity timeRoot = SelectedRootActivity;
                Activity time = SelectedMergeActivity;

                Activity rootActivity = pluginDataRoot.Items.Find(x => x.DateSession == timeRoot.DateSession);
                if (rootActivity == null || time == null)
                {
                    Logger.Warn(string.Format(
                        "Merge aborted — root or merge session not found (game={0})",
                        _gameContext?.Name));
                    return;
                }

                rootActivity.ElapsedSeconds += time.ElapsedSeconds;
                if (rootActivity.Details == null)
                {
                    rootActivity.Details = new List<ActivityDetailsData>();
                }

                if (time.Details != null && time.Details.Count > 0)
                {
                    rootActivity.Details.AddRange(time.Details);
                }

                pluginDataRoot.Items.Remove(time);

                _gameContext.LastActivity = pluginDataRoot.Items.Max(x => x.DateSession).ToLocalTime();

                if (_gameContext.PlayCount != 0)
                {
                    _gameContext.PlayCount--;
                }
                else
                {
                    Logger.Warn(string.Format("Play count is already at 0 for {0}", _gameContext.Name));
                }

                PluginDatabase.Update(pluginDataRoot);
                API.Instance.Database.Games.Update(_gameContext);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
