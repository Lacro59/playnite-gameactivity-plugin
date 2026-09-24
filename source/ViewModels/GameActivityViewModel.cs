using CommonPluginsControls.Controls;
using CommonPluginsShared;
using CommonPlayniteShared.Converters;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// State container for GameActivityView.
    /// The view keeps UI-only responsibilities (controls/events),
    /// while this ViewModel centralizes bindable state.
    /// </summary>
    public class GameActivityViewModel : ObservableObject
    {
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private readonly PlayTimeToStringConverter _converter = new PlayTimeToStringConverter();
        private readonly Dictionary<Guid, ulong> _periodPlaytimeByGame = new Dictionary<Guid, ulong>();
        private DateTime _cachePeriodStart = DateTime.MinValue;
        private DateTime _cachePeriodEnd = DateTime.MinValue;

        private int _yearCurrent;
        /// <summary>Anchor year (end of the selected period) for week/day charts and DatePicker.</summary>
        public int YearCurrent
        {
            get => _yearCurrent;
            set => SetValue(ref _yearCurrent, value);
        }

        private int _monthCurrent;
        /// <summary>Anchor month (end of the selected period) for week/day charts and DatePicker.</summary>
        public int MonthCurrent
        {
            get => _monthCurrent;
            set => SetValue(ref _monthCurrent, value);
        }

        private DateTime _periodStart;
        /// <summary>Inclusive local start of the active period filter.</summary>
        public DateTime PeriodStart
        {
            get => _periodStart;
            private set => SetValue(ref _periodStart, value);
        }

        private DateTime _periodEnd;
        /// <summary>Inclusive local end of the active period filter.</summary>
        public DateTime PeriodEnd
        {
            get => _periodEnd;
            private set => SetValue(ref _periodEnd, value);
        }

        private ActivityPeriodKind _periodKind = ActivityPeriodKind.Month;
        /// <summary>Selected period preset / granularity.</summary>
        public ActivityPeriodKind PeriodKind
        {
            get => _periodKind;
            private set => SetValue(ref _periodKind, value);
        }

        private Guid? _gameIdCurrent;
        public Guid? GameIDCurrent
        {
            get => _gameIdCurrent;
            set => SetValue(ref _gameIdCurrent, value);
        }

        private int _variateurTime;
        public int VariateurTime
        {
            get => _variateurTime;
            set => SetValue(ref _variateurTime, value);
        }

        private int _variateurLog;
        public int VariateurLog
        {
            get => _variateurLog;
            set => SetValue(ref _variateurLog, value);
        }

        private int _variateurLogTemp;
        public int VariateurLogTemp
        {
            get => _variateurLogTemp;
            set => SetValue(ref _variateurLogTemp, value);
        }

        private string _titleChart;
        public string TitleChart
        {
            get => _titleChart;
            set => SetValue(ref _titleChart, value);
        }

        private AggregateKind _aggregateKind = AggregateKind.Games;
        /// <summary>Period chart aggregation mode (Games / Sources / Genres / Tags).</summary>
        public AggregateKind AggregateKind
        {
            get => _aggregateKind;
            set
            {
                if (_aggregateKind == value)
                {
                    return;
                }

                SetValue(ref _aggregateKind, value);
                Common.LogDebug($"PeriodView: aggregate mode={value}");
            }
        }

        private bool _isGameTime = true;
        public bool IsGameTime
        {
            get => _isGameTime;
            set => SetValue(ref _isGameTime, value);
        }

        private bool _showIcon;
        public bool ShowIcon
        {
            get => _showIcon;
            set => SetValue(ref _showIcon, value);
        }

        private TextBlockWithIconMode _modeComplet;
        public TextBlockWithIconMode ModeComplet
        {
            get => _modeComplet;
            set => SetValue(ref _modeComplet, value);
        }

        private TextBlockWithIconMode _modeSimple;
        public TextBlockWithIconMode ModeSimple
        {
            get => _modeSimple;
            set => SetValue(ref _modeSimple, value);
        }

        private string _activityLabelText = string.Empty;
        public string ActivityLabelText
        {
            get => _activityLabelText;
            set => SetValue(ref _activityLabelText, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetValue(ref _searchText, value);
        }

        private string _filterSourceText = string.Empty;
        public string FilterSourceText
        {
            get => _filterSourceText;
            set => SetValue(ref _filterSourceText, value);
        }

        private List<ListActivities> _activityListByGame = new List<ListActivities>();
        public List<ListActivities> ActivityListByGame
        {
            get => _activityListByGame;
            set
            {
                SetValue(ref _activityListByGame, value);
                InvalidatePeriodCache();
            }
        }

        private List<ListActivities> _filteredActivityList = new List<ListActivities>();
        public List<ListActivities> FilteredActivityList
        {
            get => _filteredActivityList;
            set => SetValue(ref _filteredActivityList, value);
        }

        private List<ListActivities> _periodSessionList = new List<ListActivities>();
        /// <summary>Sessions of the selected game within <see cref="PeriodStart"/>–<see cref="PeriodEnd"/>.</summary>
        public List<ListActivities> PeriodSessionList
        {
            get => _periodSessionList;
            set => SetValue(ref _periodSessionList, value);
        }

        public List<ListSource> FilterSourceItems { get; } = new List<ListSource>();
        public List<string> SearchSources { get; } = new List<string>();
        public List<string> ListSources { get; set; } = new List<string>();
        public DateTime LabelDataSelected { get; set; }

        /// <summary>
        /// Initializes the period to the current calendar month.
        /// </summary>
        public void InitializeCurrentMonth()
        {
            SetPeriod(ActivityPeriodKind.Month, DateTime.Now);
        }

        /// <summary>
        /// Shifts the active period by one step (day window, month, or year depending on <see cref="PeriodKind"/>).
        /// </summary>
        /// <param name="step">Negative for previous, positive for next.</param>
        public void ShiftPeriod(int step)
        {
            if (step == 0)
            {
                return;
            }

            Common.LogDebug($"PeriodView: ShiftPeriod step={step} kind={PeriodKind}");
            DateTime anchor = new DateTime(YearCurrent, MonthCurrent, 1);
            switch (PeriodKind)
            {
                case ActivityPeriodKind.Last7Days:
                    ApplyPeriodBounds(PeriodKind, PeriodEnd.Date.AddDays(7 * step));
                    break;
                case ActivityPeriodKind.Last3Months:
                    ApplyPeriodBounds(PeriodKind, anchor.AddMonths(step));
                    break;
                case ActivityPeriodKind.Year:
                    ApplyPeriodBounds(PeriodKind, anchor.AddYears(step));
                    break;
                case ActivityPeriodKind.Month:
                default:
                    ApplyPeriodBounds(ActivityPeriodKind.Month, anchor.AddMonths(step));
                    break;
            }
        }

        /// <summary>
        /// Sets a single calendar month period from a date (DatePicker).
        /// </summary>
        /// <param name="date">Any day within the target month.</param>
        public void SetMonth(DateTime date)
        {
            ApplyPeriodBounds(ActivityPeriodKind.Month, date);
        }

        /// <summary>
        /// Applies a period preset using the current anchor (or now for relative presets).
        /// </summary>
        /// <param name="kind">Preset to apply.</param>
        public void SetPeriod(ActivityPeriodKind kind)
        {
            SetPeriod(kind, new DateTime(YearCurrent, MonthCurrent, 1));
        }

        /// <summary>
        /// Applies a period preset anchored on the given date.
        /// </summary>
        /// <param name="kind">Preset to apply.</param>
        /// <param name="anchor">Reference date for month/year and end of rolling windows.</param>
        public void SetPeriod(ActivityPeriodKind kind, DateTime anchor)
        {
            ApplyPeriodBounds(kind, anchor);
        }

        /// <summary>
        /// Sets the period chart aggregation mode.
        /// </summary>
        /// <param name="kind">Target aggregate kind.</param>
        public void SetAggregateKind(AggregateKind kind)
        {
            AggregateKind = kind;
        }

        public void SetGameChartMode(bool isGameTime)
        {
            IsGameTime = isGameTime;
        }

        public void ResetGameVariators()
        {
            VariateurTime = 0;
            VariateurLog = 0;
            VariateurLogTemp = 0;
        }

        public void ToggleSourceFilter(string sourceName, bool isChecked)
        {
            if (isChecked)
            {
                if (!SearchSources.Contains(sourceName))
                {
                    SearchSources.Add(sourceName);
                }
            }
            else
            {
                SearchSources.Remove(sourceName);
            }

            FilterSourceText = SearchSources.Count == 0 ? string.Empty : string.Join(", ", SearchSources);
        }

        /// <summary>
        /// Filters the game list to titles with playtime in the active period, then fills <see cref="ListActivities.TimePlayedInPeriod"/>.
        /// </summary>
        public void ApplyFilter()
        {
            IEnumerable<ListActivities> query = ActivityListByGame.Where(x => GetPeriodPlaytimeForGame(x.Id) > 0);

            if (!string.IsNullOrEmpty(SearchText))
            {
                string search = SearchText.Trim();
                query = query.Where(x => x.GameTitle.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (SearchSources.Count > 0)
            {
                query = query.Where(x => SearchSources.Contains(x.GameSourceName));
            }

            List<ListActivities> filteredData = query.ToList();
            for (int i = 0; i < filteredData.Count; i++)
            {
                filteredData[i].TimePlayedInPeriod = GetPeriodPlaytimeForGame(filteredData[i].Id);
            }

            FilteredActivityList = filteredData;
            Common.LogDebug($"PeriodView: ApplyFilter games={filteredData.Count} sourceFilters={SearchSources.Count} search={!string.IsNullOrEmpty(SearchText)}");
        }

        /// <summary>
        /// Refreshes the period label (localized range + total playtime).
        /// </summary>
        public void UpdateActivityLabel()
        {
            ulong periodPlaytime = GameActivityStats.GetPlayTimePeriod(PeriodStart, PeriodEnd, false);
            string playtimeText = (string)_converter.Convert(periodPlaytime, null, null, CultureInfo.CurrentCulture);
            ActivityLabelText = FormatPeriodLabel() + " (" + playtimeText + ")";
        }

        /// <summary>
        /// Display name for a period kind (LOC keys).
        /// </summary>
        /// <param name="kind">Period kind.</param>
        /// <returns>Localized label.</returns>
        public static string GetPeriodKindDisplayName(ActivityPeriodKind kind)
        {
            switch (kind)
            {
                case ActivityPeriodKind.Last7Days:
                    return ResourceProvider.GetString("LOCGameActivityPeriodPresetLast7Days");
                case ActivityPeriodKind.Last3Months:
                    return ResourceProvider.GetString("LOCGameActivityPeriodPresetLast3Months");
                case ActivityPeriodKind.Year:
                    return ResourceProvider.GetString("LOCGameActivityPeriodPresetThisYear");
                case ActivityPeriodKind.Month:
                default:
                    return ResourceProvider.GetString("LOCGameActivityPeriodPresetThisMonth");
            }
        }

        private void ApplyPeriodBounds(ActivityPeriodKind kind, DateTime anchor)
        {
            DateTime localAnchor = anchor.Kind == DateTimeKind.Utc ? anchor.ToLocalTime() : anchor;
            DateTime start;
            DateTime end;

            switch (kind)
            {
                case ActivityPeriodKind.Last7Days:
                    end = localAnchor.Date.AddDays(1).AddSeconds(-1);
                    start = end.Date.AddDays(-6);
                    break;
                case ActivityPeriodKind.Last3Months:
                    {
                        DateTime endMonth = new DateTime(localAnchor.Year, localAnchor.Month, 1);
                        DateTime startMonth = endMonth.AddMonths(-2);
                        start = startMonth;
                        end = new DateTime(endMonth.Year, endMonth.Month, DateTime.DaysInMonth(endMonth.Year, endMonth.Month), 23, 59, 59);
                        break;
                    }
                case ActivityPeriodKind.Year:
                    start = new DateTime(localAnchor.Year, 1, 1);
                    end = new DateTime(localAnchor.Year, 12, 31, 23, 59, 59);
                    break;
                case ActivityPeriodKind.Month:
                default:
                    start = new DateTime(localAnchor.Year, localAnchor.Month, 1);
                    end = new DateTime(localAnchor.Year, localAnchor.Month, DateTime.DaysInMonth(localAnchor.Year, localAnchor.Month), 23, 59, 59);
                    break;
            }

            PeriodKind = kind;
            PeriodStart = start;
            PeriodEnd = end;
            YearCurrent = end.Year;
            MonthCurrent = end.Month;
            InvalidatePeriodCache();
            UpdateActivityLabel();
            Common.LogDebug($"PeriodView: bounds kind={kind} start={start:yyyy-MM-dd HH:mm:ss} end={end:yyyy-MM-dd HH:mm:ss} anchor={localAnchor:yyyy-MM-dd}");
        }

        private string FormatPeriodLabel()
        {
            switch (PeriodKind)
            {
                case ActivityPeriodKind.Last7Days:
                    return PeriodStart.ToString("d") + " – " + PeriodEnd.ToString("d");
                case ActivityPeriodKind.Last3Months:
                    return PeriodStart.ToString("MMM yyyy") + " – " + PeriodEnd.ToString("MMM yyyy");
                case ActivityPeriodKind.Year:
                    return PeriodStart.ToString("yyyy");
                case ActivityPeriodKind.Month:
                default:
                    return PeriodStart.ToString("MMMM yyyy");
            }
        }

        private void EnsurePeriodPlaytimeCache()
        {
            if (_cachePeriodStart == PeriodStart && _cachePeriodEnd == PeriodEnd)
            {
                return;
            }

            _periodPlaytimeByGame.Clear();
            _cachePeriodStart = PeriodStart;
            _cachePeriodEnd = PeriodEnd;
        }

        private ulong GetPeriodPlaytimeForGame(Guid gameId)
        {
            EnsurePeriodPlaytimeCache();

            if (_periodPlaytimeByGame.ContainsKey(gameId))
            {
                return _periodPlaytimeByGame[gameId];
            }

            ulong total = 0;
            List<Activity> activities = PluginDatabase.Get(gameId)?.GetActivities(PeriodStart, PeriodEnd);
            if (activities != null)
            {
                for (int j = 0; j < activities.Count; j++)
                {
                    total += activities[j].ElapsedSeconds;
                }
            }

            _periodPlaytimeByGame[gameId] = total;
            return total;
        }

        private void InvalidatePeriodCache()
        {
            _cachePeriodStart = DateTime.MinValue;
            _cachePeriodEnd = DateTime.MinValue;
            _periodPlaytimeByGame.Clear();
        }
    }
}
