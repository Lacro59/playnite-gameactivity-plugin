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
        private static ILogger Logger => LogManager.GetLogger();
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        private readonly Game _gameContext;

        private ObservableCollection<Activity> _rootActivities;
        public ObservableCollection<Activity> RootActivities
        {
            get => _rootActivities;
            private set => SetValue(ref _rootActivities, value);
        }

        private ObservableCollection<Activity> _mergeActivities;
        public ObservableCollection<Activity> MergeActivities
        {
            get => _mergeActivities;
            private set => SetValue(ref _mergeActivities, value);
        }

        private Activity _selectedRootActivity;
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
        public Activity SelectedMergeActivity
        {
            get => _selectedMergeActivity;
            set
            {
                SetValue(ref _selectedMergeActivity, value);
                OnPropertyChanged(nameof(CanMerge));
            }
        }

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

        public RelayCommand MergeCommand { get; }
        public RelayCommand CancelCommand { get; }

        public event EventHandler CloseRequested;

        public GameActivityMergeTimeViewModel(Game game)
        {
            _gameContext = game;

            RootActivities = new ObservableCollection<Activity>(
                PluginDatabase.Get(game, true).Items.OrderBy(x => x.DateSession));
            MergeActivities = new ObservableCollection<Activity>();

            MergeCommand = new RelayCommand(ExecuteMerge, () => CanMerge);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private void RefreshMergeActivities()
        {
            MergeActivities.Clear();
            SelectedMergeActivity = null;

            if (SelectedRootActivity == null)
            {
                return;
            }

            List<Activity> items = PluginDatabase.Get(_gameContext, true).Items
                .Where(x => x.DateSession > SelectedRootActivity.DateSession)
                .OrderBy(x => x.DateSession)
                .ToList();

            foreach (Activity activity in items)
            {
                MergeActivities.Add(activity);
            }
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

                _gameContext.LastActivity = pluginDataRoot.Items.Max(x => x.DateSession);

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
