using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input; // Required for CommandManager

namespace GameActivity.ViewModels
{
    /// <summary>
    /// ViewModel for managing the addition or edition of game sessions.
    /// Handles time logic, grouped play actions, and custom action persistence.
    /// </summary>
    public class GameActivityAddTimeViewModel : ObservableObject
    {
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private readonly PlayTimeToStringConverter _playTimeConverter = new PlayTimeToStringConverter();

        /// <summary> Resulting activity to be read by the view after dialog closes. </summary>
        public Activity ResultActivity { get; private set; }

        private DateTime _selectedDateStart;
        private string _selectedTimeStart;
        private DateTime _selectedDateEnd;
        private string _selectedTimeEnd;
        private string _elapsedTimeDisplay = "--";
        private bool _isAddEnabled;
        private bool _isStartLocked;
        private string _confirmLabel;
        private CbListHeader _selectedPlayAction;
        private ListCollectionView _playActionsView;
        private string _customActionNameInput = string.Empty;

        #region Properties

        public DateTime SelectedDateStart { get => _selectedDateStart; set { SetValue(ref _selectedDateStart, value); RefreshElapsed(); } }
        public string SelectedTimeStart { get => _selectedTimeStart; set { SetValue(ref _selectedTimeStart, value); RefreshElapsed(); } }
        public DateTime SelectedDateEnd { get => _selectedDateEnd; set { SetValue(ref _selectedDateEnd, value); RefreshElapsed(); } }
        public string SelectedTimeEnd { get => _selectedTimeEnd; set { SetValue(ref _selectedTimeEnd, value); RefreshElapsed(); } }
        public string ElapsedTimeDisplay { get => _elapsedTimeDisplay; private set => SetValue(ref _elapsedTimeDisplay, value); }
        public bool IsAddEnabled { get => _isAddEnabled; private set => SetValue(ref _isAddEnabled, value); }
        public bool IsStartLocked { get => _isStartLocked; private set => SetValue(ref _isStartLocked, value); }
        public string ConfirmLabel { get => _confirmLabel; private set => SetValue(ref _confirmLabel, value); }
        public CbListHeader SelectedPlayAction { get => _selectedPlayAction; set => SetValue(ref _selectedPlayAction, value); }
        public ListCollectionView PlayActionsView { get => _playActionsView; private set => SetValue(ref _playActionsView, value); }

        /// <summary>
        /// Input for the new custom action name.
        /// Triggers a UI command re-evaluation on every keystroke.
        /// </summary>
        public string CustomActionNameInput
        {
            get => _customActionNameInput;
            set
            {
                SetValue(ref _customActionNameInput, value);
                // Force WPF to re-evaluate CanExecute for all commands
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        private readonly Game _game;
        private readonly Activity _activityEdit;
        private readonly List<CbListHeader> _cbListHeaders = new List<CbListHeader>();

        public RelayCommand<object> ConfirmCommand { get; }
        public RelayCommand<object> CancelCommand { get; }
        public RelayCommand<object> AddCustomActionCommand { get; }
        public RelayCommand<string> RemoveCustomActionCommand { get; }

        public event EventHandler CloseRequested;

        public GameActivityAddTimeViewModel(GameActivity plugin, Game game, Activity activityEdit)
        {
            _game = game;
            _activityEdit = activityEdit;

            // Initialization
            DateTime now = DateTime.Now;
            _selectedDateStart = now;
            _selectedTimeStart = now.ToString("HH:mm:ss");
            _selectedDateEnd = now;
            _selectedTimeEnd = now.ToString("HH:mm:ss");

            InitializeActionList(game);

            // Commands
            ConfirmCommand = new RelayCommand<object>(_ => ExecuteConfirm(), _ => IsAddEnabled);
            CancelCommand = new RelayCommand<object>(_ => ExecuteCancel());
            AddCustomActionCommand = new RelayCommand<object>(
                _ => ExecuteAddCustomAction(CustomActionNameInput),
                _ => !string.IsNullOrWhiteSpace(CustomActionNameInput)
            );
            RemoveCustomActionCommand = new RelayCommand<string>(
                name => ExecuteRemoveCustomAction(name),
                name => !string.IsNullOrEmpty(name)
            );
        }

        private void InitializeActionList(Game game)
        {
            // Build the list of available actions
            var actions = game.GameActions
                ?.Select(x => x.Name.IsNullOrEmpty() ? ResourceProvider.GetString("LOCGameActivityDefaultAction") : x.Name)
                ?.ToList() ?? new List<string> { ResourceProvider.GetString("LOCGameActivityDefaultAction") };

            PluginDatabase.PluginSettings.CustomGameActions.TryGetValue(game.Id, out List<string> customActions);

            _cbListHeaders.AddRange(actions.Select(x => new CbListHeader { Name = x, Category = ResourceProvider.GetString("LOCGaGameActions") }));

            if (customActions != null)
            {
                _cbListHeaders.AddRange(customActions.Select(x => new CbListHeader { Name = x, Category = ResourceProvider.GetString("LOCGaCustomGameActions"), IsCustom = true }));
            }

            ConfirmLabel = ResourceProvider.GetString("LOCAddTitle");
            if (_activityEdit != null) ApplyEditData();

            RebuildPlayActionsView();
            RefreshElapsed();
        }

        private void ApplyEditData()
        {
            DateTime sessionStart = ((DateTime)_activityEdit.DateSession).ToLocalTime();
            _selectedDateStart = sessionStart;
            _selectedTimeStart = sessionStart.ToString("HH:mm:ss");
            _selectedDateEnd = sessionStart.AddSeconds(_activityEdit.ElapsedSeconds);
            _selectedTimeEnd = _selectedDateEnd.ToString("HH:mm:ss");

            IsStartLocked = true;
            ConfirmLabel = ResourceProvider.GetString("LOCSaveLabel");

            var actionName = _activityEdit.GameActionName;
            SelectedPlayAction = _cbListHeaders.Find(x => x.Name.IsEqual(actionName));

            if (SelectedPlayAction == null)
            {
                SelectedPlayAction = new CbListHeader { Name = actionName, Category = ResourceProvider.GetString("LOCOther") };
                _cbListHeaders.Add(SelectedPlayAction);
            }
        }

        private void ExecuteConfirm()
        {
            try
            {
                DateTime start = ParseDateTime(_selectedDateStart, _selectedTimeStart);
                DateTime end = ParseDateTime(_selectedDateEnd, _selectedTimeEnd);

                Activity activity = IsStartLocked ? _activityEdit : new Activity { DateSession = start.ToUniversalTime() };
                activity.GameActionName = SelectedPlayAction?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction");
                activity.ElapsedSeconds = (ulong)(end - start).TotalSeconds;
                activity.IdConfiguration = PluginDatabase.SystemConfigurationManager.GetConfigurationIndex();
                activity.PlatformIDs = _game.PlatformIds;
                activity.SourceID = _game.SourceId;

                ResultActivity = activity;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex) { Common.LogError(ex, false, true, PluginDatabase.PluginName); }
        }

        private void ExecuteCancel()
        {
            ResultActivity = null;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteAddCustomAction(string name)
        {
            if (_cbListHeaders.Any(x => x.Name.IsEqual(name))) return;

            var newItem = new CbListHeader { Name = name, Category = ResourceProvider.GetString("LOCGaCustomGameActions"), IsCustom = true };
            _cbListHeaders.Add(newItem);
            CustomActionNameInput = string.Empty; // Reset field
            RebuildPlayActionsView();
            SelectedPlayAction = newItem;
            Persist();
        }

        private void ExecuteRemoveCustomAction(string name)
        {
            var item = _cbListHeaders.Find(x => x.Name.IsEqual(name) && x.IsCustom);
            if (item == null) return;

            _cbListHeaders.Remove(item);
            if (SelectedPlayAction?.Name == name) SelectedPlayAction = null;
            RebuildPlayActionsView();
            Persist();
        }

        private void RefreshElapsed()
        {
            try
            {
                DateTime s = ParseDateTime(_selectedDateStart, _selectedTimeStart);
                DateTime e = ParseDateTime(_selectedDateEnd, _selectedTimeEnd);
                ulong diff = e <= s ? 0 : (ulong)(e - s).TotalSeconds;
                ElapsedTimeDisplay = (string)_playTimeConverter.Convert(diff, null, null, CultureInfo.CurrentCulture);
                IsAddEnabled = diff > 0;
            }
            catch { ElapsedTimeDisplay = "--"; IsAddEnabled = false; }
        }

        private void RebuildPlayActionsView()
        {
            var lcv = new ListCollectionView(_cbListHeaders);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            PlayActionsView = lcv;
        }

        private void Persist()
        {
            PluginDatabase.PluginSettings.CustomGameActions[_game.Id] = _cbListHeaders.Where(x => x.IsCustom).Select(x => x.Name).ToList();
            PluginDatabase.PersistSettingsAction?.Invoke();
        }

        private static DateTime ParseDateTime(DateTime d, string t) => DateTime.Parse(d.ToString("yyyy-MM-dd") + " " + t);
    }

    public class CbListHeader
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public bool IsCustom { get; set; }
    }
}