using CommonPluginsControls.Controls;
using CommonPluginsShared;
using CommonPlayniteShared.Converters;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
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

        private int _yearCurrent;
        public int YearCurrent
        {
            get => _yearCurrent;
            set => SetValue(ref _yearCurrent, value);
        }

        private int _monthCurrent;
        public int MonthCurrent
        {
            get => _monthCurrent;
            set => SetValue(ref _monthCurrent, value);
        }

        private string _gameIdCurrent;
        public string GameIDCurrent
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

        private bool _isMonthSources = true;
        public bool IsMonthSources
        {
            get => _isMonthSources;
            set => SetValue(ref _isMonthSources, value);
        }

        private bool _isGenresSources;
        public bool IsGenresSources
        {
            get => _isGenresSources;
            set => SetValue(ref _isGenresSources, value);
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
            set => SetValue(ref _activityListByGame, value);
        }

        private List<ListActivities> _filteredActivityList = new List<ListActivities>();
        public List<ListActivities> FilteredActivityList
        {
            get => _filteredActivityList;
            set => SetValue(ref _filteredActivityList, value);
        }

        public List<ListSource> FilterSourceItems { get; } = new List<ListSource>();
        public List<string> SearchSources { get; } = new List<string>();
        public List<string> ListSources { get; set; } = new List<string>();
        public DateTime LabelDataSelected { get; set; }

        public void InitializeCurrentMonth()
        {
            YearCurrent = DateTime.Now.Year;
            MonthCurrent = DateTime.Now.Month;
            UpdateActivityLabel();
        }

        public void ChangeMonth(int monthOffset)
        {
            DateTime dateNew = new DateTime(YearCurrent, MonthCurrent, 1).AddMonths(monthOffset);
            YearCurrent = dateNew.Year;
            MonthCurrent = dateNew.Month;
            UpdateActivityLabel();
        }

        public void SetMonth(DateTime date)
        {
            YearCurrent = date.Year;
            MonthCurrent = date.Month;
            UpdateActivityLabel();
        }

        public void SetMonthSourceMode(bool useSources, bool useGenres)
        {
            IsMonthSources = useSources;
            IsGenresSources = useGenres;
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

        public void ApplyFilter()
        {
            string monthKey = string.Format("{0}-{1:D2}", YearCurrent, MonthCurrent);
            IEnumerable<ListActivities> query = ActivityListByGame.Where(x => x.DateActivity.Contains(monthKey));

            if (!string.IsNullOrEmpty(SearchText))
            {
                string search = SearchText.ToLowerInvariant();
                query = query.Where(x => x.GameTitle.ToLowerInvariant().IndexOf(search) > -1);
            }

            if (SearchSources.Count > 0)
            {
                query = query.Where(x => SearchSources.Contains(x.GameSourceName));
            }

            List<ListActivities> filteredData = query.ToList();
            for (int i = 0; i < filteredData.Count; i++)
            {
                filteredData[i].TimePlayedInMonth = 0;
                List<Activity> activities = PluginDatabase.Get(filteredData[i].Id)?.GetActivities(YearCurrent, MonthCurrent);
                if (activities != null)
                {
                    for (int j = 0; j < activities.Count; j++)
                    {
                        filteredData[i].TimePlayedInMonth += activities[j].ElapsedSeconds;
                    }
                }
            }

            FilteredActivityList = filteredData;
        }

        public void UpdateActivityLabel()
        {
            DateTime monthStart = new DateTime(YearCurrent, MonthCurrent, 1);
            ulong monthPlaytime = GameActivityStats.GetPlayTimeYearMonth((uint)YearCurrent, (uint)MonthCurrent, false);
            ActivityLabelText = monthStart.ToString("MMMM yyyy") + " (" +
                _converter.Convert(monthPlaytime, null, null, CultureInfo.CurrentCulture) + ")";
        }
    }
}
