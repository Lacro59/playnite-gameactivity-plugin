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

                if (timeRoot.DateSession.HasValue && time.DateSession.HasValue)
                {
                    DateTime rootKey = timeRoot.DateSession.Value;
                    DateTime mergeKey = time.DateSession.Value;
                    var detailsMap = pluginDataRoot.ItemsDetails.Items;

                    DateTime? resolvedRootKey = ResolveDetailsKey(detailsMap, rootKey);
                    DateTime? resolvedMergeKey = ResolveDetailsKey(detailsMap, mergeKey);
                    bool hasRootKey = resolvedRootKey.HasValue;
                    bool hasMergeKey = resolvedMergeKey.HasValue;
                    if (!hasRootKey || !hasMergeKey)
                    {
                        string availableKeys = string.Join(
                            ", ",
                            detailsMap.Keys
                                .OrderBy(x => x)
                                .Select(x => string.Format(
                                    "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] Ticks={2}",
                                    x, x.Kind, x.Ticks)));

                        Common.LogDebug(true, string.Format(
                            "Merge details keys mismatch for {0}. RootKey={1:yyyy-MM-dd HH:mm:ss.fff} [{2}] Ticks={3} Found={4}; MergeKey={5:yyyy-MM-dd HH:mm:ss.fff} [{6}] Ticks={7} Found={8}; AvailableKeys=({9})",
                            _gameContext.Name,
                            rootKey, rootKey.Kind, rootKey.Ticks, hasRootKey,
                            mergeKey, mergeKey.Kind, mergeKey.Ticks, hasMergeKey,
                            availableKeys));
                    }

                    if (hasRootKey && hasMergeKey)
                    {
                        detailsMap[resolvedRootKey.Value].AddRange(detailsMap[resolvedMergeKey.Value]);
                    }
                }

                pluginDataRoot.Items.Remove(time);
                if (time.DateSession.HasValue)
                {
                    DateTime keyToRemove = time.DateSession.Value;
                    DateTime? resolvedRemoveKey = ResolveDetailsKey(pluginDataRoot.ItemsDetails.Items, keyToRemove);
                    List<ActivityDetailsData> deleted;
                    pluginDataRoot.ItemsDetails.Items.TryRemove(resolvedRemoveKey ?? keyToRemove, out deleted);
                }

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

        /// <summary>
        /// Resolves a key from ItemsDetails by exact key first, then by the same
        /// second-level date string used in ActivityDetails.Get().
        /// </summary>
        private static DateTime? ResolveDetailsKey(
            System.Collections.Concurrent.ConcurrentDictionary<DateTime, List<ActivityDetailsData>> detailsMap,
            DateTime lookupKey)
        {
            if (detailsMap.ContainsKey(lookupKey))
            {
                return lookupKey;
            }

            const string dateFormat = "yyyy-MM-dd HH:mm:ss";
            string lookup = lookupKey.ToUniversalTime().ToString(dateFormat);

            KeyValuePair<DateTime, List<ActivityDetailsData>> matched =
                detailsMap.FirstOrDefault(x => x.Key.ToString(dateFormat) == lookup);

            if (!matched.Equals(default(KeyValuePair<DateTime, List<ActivityDetailsData>>)))
            {
                return matched.Key;
            }

            return null;
        }
    }
}
