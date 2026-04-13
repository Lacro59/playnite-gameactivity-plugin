using CommonPluginsShared;
using CommonPluginsShared.Converters;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// View model for <see cref="Views.GameActivityGanttView"/>:
    /// loads Gantt rows, period label, and play time within the visible day range.
    /// </summary>
    public class GameActivityGanttViewModel : ObservableObject
    {
        private static readonly LocalDateConverter LocalDateConverter = new LocalDateConverter();
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private List<GanttData> _allGanttDatas = new List<GanttData>();

        private List<GanttData> _filteredGanttDatas;
        public List<GanttData> FilteredGanttDatas
        {
            get => _filteredGanttDatas;
            set => SetValue(ref _filteredGanttDatas, value);
        }

        private int _columnCount = 30;
        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                if (_columnCount == value)
                {
                    return;
                }
                SetValue(ref _columnCount, value);
                UpdatePeriod();
            }
        }

        private double _headerWidth = 690;
        public double HeaderWidth
        {
            get => _headerWidth;
            set => SetValue(ref _headerWidth, value);
        }

        private DateTime _lastDate = DateTime.Now;
        public DateTime LastDate
        {
            get => _lastDate;
            set
            {
                if (_lastDate == value)
                {
                    return;
                }
                SetValue(ref _lastDate, value);
                UpdatePeriod();
            }
        }

        private string _periodDisplayText = string.Empty;
        public string PeriodDisplayText
        {
            get => _periodDisplayText;
            set => SetValue(ref _periodDisplayText, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                SetValue(ref _searchText, value);
                ApplyFilters();
            }
        }

        private bool _showOnlyActiveGames;
        public bool ShowOnlyActiveGames
        {
            get => _showOnlyActiveGames;
            set
            {
                if (_showOnlyActiveGames == value)
                {
                    return;
                }

                SetValue(ref _showOnlyActiveGames, value);
                ApplyFilters();
            }
        }

        private bool _hasFilteredData;
        public bool HasFilteredData
        {
            get => _hasFilteredData;
            set => SetValue(ref _hasFilteredData, value);
        }

        public RelayCommand<object> Set7DaysCommand { get; private set; }
        public RelayCommand<object> Set30DaysCommand { get; private set; }
        public RelayCommand<object> Set90DaysCommand { get; private set; }

        public string Set7DaysLabel => FormatDaysLabel(7);
        public string Set30DaysLabel => FormatDaysLabel(30);
        public string Set90DaysLabel => FormatDaysLabel(90);

        private string _columnCountLabel = string.Empty;
        public string ColumnCountLabel
        {
            get => _columnCountLabel;
            set => SetValue(ref _columnCountLabel, value);
        }

        public GameActivityGanttViewModel()
        {
            Set7DaysCommand = new RelayCommand<object>((o) => ColumnCount = 7);
            Set30DaysCommand = new RelayCommand<object>((o) => ColumnCount = 30);
            Set90DaysCommand = new RelayCommand<object>((o) => ColumnCount = 90);
            ColumnCountLabel = FormatDaysLabel(ColumnCount);
            LoadGanttData();
            UpdatePeriod();
        }

        /// <summary>
        /// Reloads rows from the plugin database (e.g. after external data changes).
        /// </summary>
        public void LoadGanttData()
        {
            if (PluginDatabase == null)
            {
                _allGanttDatas = new List<GanttData>();
                FilteredGanttDatas = new List<GanttData>();
                UpdatePeriod();
                return;
            }

            var ganttDatas = new List<GanttData>();
            foreach (GameActivities gameActivities in PluginDatabase.GetListGameActivity().Where(x => x.LastActivity != null))
            {
                try
                {
                    var ganttData = new GanttData
                    {
                        Id = gameActivities.Id,
                        Name = gameActivities.Name,
                        Icon = gameActivities.Icon.IsNullOrEmpty() ? gameActivities.Icon : API.Instance.Database.GetFullFilePath(gameActivities.Icon),
                        LastActivity = (DateTime)gameActivities.LastActivity,
                        Playtime = gameActivities.Game.Playtime
                    };

                    List<GanttValue> data = gameActivities.Items.Select(x => new GanttValue { PlayDate = x.DateSession.ToLocalTime(), PlayTime = x.ElapsedSeconds }).ToList();
                    var dataFinal = new List<GanttValue>();

                    foreach (GanttValue x in data)
                    {
                        string dayKey = x.PlayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        GanttValue existing = dataFinal.Find(y => y.PlayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == dayKey);
                        if (existing != null)
                        {
                            existing.PlayTime += x.PlayTime;
                        }
                        else
                        {
                            dataFinal.Add(new GanttValue { PlayDate = x.PlayDate, PlayTime = x.PlayTime });
                        }
                    }

                    ganttData.DateTimes = dataFinal;
                    ganttDatas.Add(ganttData);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            _allGanttDatas = ganttDatas;
            UpdatePeriod();
        }

        /// <summary>
        /// Refreshes the period label and per-row play time for the current <see cref="LastDate"/> and <see cref="ColumnCount"/>.
        /// </summary>
        public void UpdatePeriod()
        {
            ColumnCountLabel = FormatDaysLabel(ColumnCount);
            DateTime dtStart = LastDate.AddDays(ColumnCount * -1);
            PeriodDisplayText =
                (string)LocalDateConverter.Convert(dtStart, null, null, CultureInfo.CurrentCulture)
                + " - "
                + (string)LocalDateConverter.Convert(LastDate, null, null, CultureInfo.CurrentCulture);

            if (_allGanttDatas == null)
            {
                FilteredGanttDatas = new List<GanttData>();
                HasFilteredData = false;
                return;
            }

            DateTime rangeStart = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, 0, 0, 0, DateTimeKind.Local);
            DateTime rangeEnd = new DateTime(LastDate.Year, LastDate.Month, LastDate.Day, 23, 59, 59, DateTimeKind.Local);

            foreach (GanttData ganttData in _allGanttDatas)
            {
                ganttData.PlaytimeInPerdiod = 0;
                if (ganttData.DateTimes == null)
                {
                    continue;
                }

                foreach (GanttValue x in ganttData.DateTimes)
                {
                    if (x.PlayDate >= rangeStart && x.PlayDate <= rangeEnd)
                    {
                        ganttData.PlaytimeInPerdiod += x.PlayTime;
                    }
                }
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<GanttData> query = _allGanttDatas ?? new List<GanttData>();

            if (ShowOnlyActiveGames)
            {
                query = query.Where(x => x.PlaytimeInPerdiod > 0);
            }

            string search = SearchText;
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    !string.IsNullOrEmpty(x.Name)
                    && x.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            FilteredGanttDatas = query.ToList();
            HasFilteredData = FilteredGanttDatas.Count > 0;
        }

        private string FormatDaysLabel(int value)
        {
            string format = ResourceProvider.GetString("LOCCommonDays");
            if (string.IsNullOrEmpty(format))
            {
                format = "{0} days";
            }

            return string.Format(CultureInfo.CurrentCulture, format, value);
        }
    }
}
