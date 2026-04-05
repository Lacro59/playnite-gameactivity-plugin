using System;
using System.Collections.Generic;
using System.Windows;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Controls.Primitives;
using GameActivity.Models;
using LiveCharts;
using CommonPluginsShared;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Globalization;
using System.Threading.Tasks;
using CommonPlayniteShared.Converters;
using CommonPluginsControls.LiveChartsCommon;
using CommonPlayniteShared.Common;
using GameActivity.Services;
using GameActivity.Controls;
using CommonPluginsControls.Controls;
using System.Windows.Media;
using CommonPluginsShared.Extensions;
using Playnite.SDK.Data;
using CommonPluginsShared.SystemInfo;
using CommonPluginsShared.Utilities;
using GameActivity.ViewModels;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Input;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityView.xaml.
    /// </summary>
    public partial class GameActivityView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private GameActivity Plugin { get; set; }
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private GameActivityViewModel ViewModel { get; }


        private List<string> ListSources
        {
            get => ViewModel.ListSources;
            set => ViewModel.ListSources = value;
        }
        private DateTime LabelDataSelected
        {
            get => ViewModel.LabelDataSelected;
            set => ViewModel.LabelDataSelected = value;
        }

        private PluginChartTime PART_GameActivityChartTime { get; set; }
        private PluginChartLog PART_GameActivityChartLog { get; set; }
        private readonly DispatcherTimer _searchDebounceTimer;

        private Game _pendingInitialGameContext;

#if DEBUG
        private DebugTimer _ctorDebugTimer;
#endif

        private PlayTimeToStringConverter Converter { get; set; } = new PlayTimeToStringConverter();

        private bool _customerTimeMapperInitialized;
        private int _monthChartReloadVersion;
        private int _weekChartReloadVersion;
        private int _dayChartReloadVersion;
        private CancellationTokenSource _monthChartReloadCts;
        private CancellationTokenSource _weekChartReloadCts;
        private CancellationTokenSource _dayChartReloadCts;

        private int _monthGameActivitiesCacheYear = -1;
        private int _monthGameActivitiesCacheMonth = -1;
        private List<GameActivities> _monthGameActivitiesCache;
        private readonly object _monthGameActivitiesCacheLock = new object();

        private List<GameActivities> GetMonthFilteredGameActivities(int year, int month)
        {
            lock (_monthGameActivitiesCacheLock)
            {
                if (_monthGameActivitiesCache != null
                    && _monthGameActivitiesCacheYear == year
                    && _monthGameActivitiesCacheMonth == month)
                {
                    return _monthGameActivitiesCache;
                }

                DateTime startOfMonth = new DateTime(year, month, 1, 0, 0, 0);
                DateTime endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                listGameActivities = listGameActivities
                    .Where(x => x.GetListDateTimeActivity().Any(y => y >= startOfMonth && y <= endOfMonth))
                    .ToList();

                _monthGameActivitiesCache = listGameActivities;
                _monthGameActivitiesCacheYear = year;
                _monthGameActivitiesCacheMonth = month;

                return listGameActivities;
            }
        }

        private void EnsureCustomerTimeMapper()
        {
            if (_customerTimeMapperInitialized)
            {
                return;
            }

            CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            Charting.For<CustomerForTime>(customerVmMapper);
            _customerTimeMapperInitialized = true;
        }

        public int YearCurrent { get => ViewModel.YearCurrent; set => ViewModel.YearCurrent = value; }
        public int MonthCurrent { get => ViewModel.MonthCurrent; set => ViewModel.MonthCurrent = value; }
        public string GameIDCurrent { get => ViewModel.GameIDCurrent; set => ViewModel.GameIDCurrent = value; }
        public int VariateurTime { get => ViewModel.VariateurTime; set => ViewModel.VariateurTime = value; }
        public int VariateurLog { get => ViewModel.VariateurLog; set => ViewModel.VariateurLog = value; }
        public int VariateurLogTemp { get => ViewModel.VariateurLogTemp; set => ViewModel.VariateurLogTemp = value; }
        public string TitleChart { get => ViewModel.TitleChart; set => ViewModel.TitleChart = value; }

        private List<ListSource> FilterSourceItems => ViewModel.FilterSourceItems;
        private List<string> SearchSources => ViewModel.SearchSources;
        public List<ListActivities> ActivityListByGame { get => ViewModel.ActivityListByGame; set => ViewModel.ActivityListByGame = value; }

        public bool IsMonthSources { get => ViewModel.IsMonthSources; set => ViewModel.IsMonthSources = value; }
        public bool IsGenresSources { get => ViewModel.IsGenresSources; set => ViewModel.IsGenresSources = value; }
        public bool IsGameTime { get => ViewModel.IsGameTime; set => ViewModel.IsGameTime = value; }

        public bool ShowIcon { get => ViewModel.ShowIcon; set => ViewModel.ShowIcon = value; }
        public TextBlockWithIconMode ModeComplet { get => ViewModel.ModeComplet; set => ViewModel.ModeComplet = value; }
        public TextBlockWithIconMode ModeSimple { get => ViewModel.ModeSimple; set => ViewModel.ModeSimple = value; }


        public GameActivityView(GameActivity plugin, Game gameContext = null)
        {
#if DEBUG
            _ctorDebugTimer = new DebugTimer("GameActivityView.ctor");
#endif
            Plugin = plugin;
            ViewModel = new GameActivityViewModel();
            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(180)
            };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            ViewModel.InitializeCurrentMonth();

            InitializeComponent();
#if DEBUG
            _ctorDebugTimer.Step("InitializeComponent done");
#endif

            PART_DataLoad.Visibility = Visibility.Visible;
            PART_DataTop.Visibility = Visibility.Hidden;
            PART_DataBottom.Visibility = Visibility.Hidden;

            _pendingInitialGameContext = gameContext;
            if (IsLoaded)
            {
                Dispatcher.BeginInvoke((Action)BeginDeferredHeavyInitialization, DispatcherPriority.ApplicationIdle);
            }
            else
            {
                Loaded += GameActivityView_OnFirstLoaded;
            }
        }

        private void GameActivityView_OnFirstLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GameActivityView_OnFirstLoaded;
            Dispatcher.BeginInvoke((Action)BeginDeferredHeavyInitialization, DispatcherPriority.ApplicationIdle);
        }

        private void BeginDeferredHeavyInitialization()
        {
            Game gameContext = _pendingInitialGameContext;
            _pendingInitialGameContext = null;
            ContinueGameActivityViewInitialization(gameContext);
        }

        private void ContinueGameActivityViewInitialization(Game gameContext)
        {
            if (!PluginDatabase.PluginSettings.EnableLogging)
            {
                ToggleButtonTime.Visibility = Visibility.Hidden;
                ToggleButtonLog.Visibility = Visibility.Hidden;
            }

            PART_GameActivityChartTime = new PluginChartTime
            {
                Truncate = PluginDatabase.PluginSettings.ChartTimeTruncate,
                IgnoreSettings = true,
                LabelsRotation = true
            };
            PART_GameActivityChartTime.GameSeriesDataClick += GameSeries_DataClick;
            _ = PART_GameActivityChartTime_Contener.Children.Add(PART_GameActivityChartTime);

            PART_GameActivityChartLog = new PluginChartLog
            {
                IgnoreSettings = true,
                AxisLimit = 10
            };
            _ = PART_GameActivityChartLog_Contener.Children.Add(PART_GameActivityChartLog);

            lvGames.EnableColumnPersistence = PluginDatabase.PluginSettings.SaveColumnOrder;
            lvGames.ColumnConfigurationFilePath = System.IO.Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "ListViewColumns.json");
            lvGames.ColumnConfigurationScope = CommonPluginsShared.Controls.ColumnConfigurationScope.Custom;
            lvGames.ColumnConfigurationKey = "GameActivityView.lvGames";

            GridView lvView = (GridView)lvGames.View;

            if (!PluginDatabase.PluginSettings.EnableLogging)
            {
                HideColumn(lvAvgGpuP, lvAvgGpuPHeader, true);
                HideColumn(lvAvgCpuP, lvAvgCpuPHeader, true);
                HideColumn(lvAvgGpuT, lvAvgGpuTHeader, true);
                HideColumn(lvAvgCpuT, lvAvgCpuTHeader, true);
                HideColumn(lvAvgFps, lvAvgFpsHeader, true);
                HideColumn(lvAvgRam, lvAvgRamHeader, true);
                HideColumn(lvAvgGpu, lvAvgGpuHeader, true);
                HideColumn(lvAvgCpu, lvAvgCpuHeader, true);
            }

            activityForGamesGraphics.Visibility = Visibility.Hidden;

            #region Get & set datas
            ListSources = GetListSourcesName();
#if DEBUG
            DebugTimer monthWeekTimer = new DebugTimer("GameActivityView.GetActivityByMonthWeek deferred");
            monthWeekTimer.Step("queue");
#endif
            this.Dispatcher.BeginInvoke((Action)delegate
            {
#if DEBUG
                monthWeekTimer.Step("start");
#endif
                GetActivityByMonth(YearCurrent, MonthCurrent);
                GetActivityByWeek(YearCurrent, MonthCurrent);
#if DEBUG
                monthWeekTimer.Stop("done");
#endif
            });

            _ = Task.Run(() =>
            {
#if DEBUG
                var backgroundTimer = new DebugTimer("GameActivityView.ctor.background");
#endif
                GetActivityByDay(YearCurrent, MonthCurrent);
#if DEBUG
                backgroundTimer.Step("GetActivityByDay done");
#endif
                GetActivityByListGame();
#if DEBUG
                backgroundTimer.Step("GetActivityByListGame done");
#endif
                SetSourceFilter();
#if DEBUG
                backgroundTimer.Step("SetSourceFilter done");
#endif

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (gameContext != null)
                    {
                        for (int i = 0; i < lvGames.Items.Count; i++)
                        {
                            if (((ListActivities)lvGames.Items[i]).GameTitle == gameContext.Name)
                            {
                                lvGames.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    lvGames.ScrollIntoView(lvGames.SelectedItem);

                    if (PluginDatabase.PluginSettings.CumulPlaytimeStore)
                    {
                        PART_ChartTotalHoursSource.Visibility = Visibility.Hidden;
                        PART_ChartTotalHoursSource_Label.Visibility = Visibility.Hidden;

                        Grid.SetColumn(GridDay, 0);
                        Grid.SetColumnSpan(GridDay, 3);
                    }
                });
#if DEBUG
                backgroundTimer.Step("UI finalize done");
#endif

            }).ContinueWith(antecedent =>
            {
#if DEBUG
                DebugTimer endTimer = new DebugTimer("GameActivityView.ctor.hideLoad+showBottom");
                endTimer.Step("start");
#endif
                this.Dispatcher.BeginInvoke((Action)delegate
                {
#if DEBUG
                    DebugTimer sortTimer = new DebugTimer("GameActivityView.ctor.lvGames.Sorting");
                    sortTimer.Step("start");
#endif
                    PART_DataLoad.Visibility = Visibility.Collapsed;
                    PART_DataTop.Visibility = Visibility.Visible;
                    lvGames.Sorting();
                    if (lvGames.SelectedItem != null)
                    {
                        lvGames.ScrollIntoView(lvGames.SelectedItem);
                    }
                    PART_DataBottom.Visibility = Visibility.Visible;
#if DEBUG
                    sortTimer.Stop();
#endif
                });

#if DEBUG
                endTimer.Stop();
                _ctorDebugTimer.Stop();
#endif
            });
            #endregion

            ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons;
            ModeComplet = (PluginDatabase.PluginSettings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstWithText : TextBlockWithIconMode.IconFirstWithText;
            ModeSimple = (PluginDatabase.PluginSettings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstOnly : TextBlockWithIconMode.IconFirstOnly;

            PART_ChartTotalHoursSource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartTotalHoursSource_ToolTip.Mode = ModeComplet;

            PART_ChartHoursByDaySource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartHoursByDaySource_ToolTip.Mode = ModeComplet;

            PART_ChartHoursByWeekSource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartHoursByWeekSource_ToolTip.Mode = ModeComplet;
            PART_ChartHoursByWeekSource_ToolTip.ShowWeekPeriode = true;

            DataContext = ViewModel;
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            Filter();
        }

        /// <summary>
        /// Hides a GridView column and optionally forces it to remain hidden in ListViewExtend management.
        /// </summary>
        private static void HideColumn(GridViewColumn column, GridViewColumnHeader header, bool forceHidden = false)
        {
            if (column == null || header == null)
            {
                return;
            }

            column.Width = 0;
            header.IsHitTestVisible = false;
            CommonPluginsShared.Controls.ListViewColumnOptions.SetForceHidden(column, forceHidden);
        }


        private void SetSourceFilter()
        {
            FilterSourceItems.Clear();
            IEnumerable<string> ListSourceName = ActivityListByGame.Select(x => x.GameSourceName).Distinct();
            foreach (string sourcename in ListSourceName)
            {
                string Icon = PlayniteTools.GetPlatformIcon(sourcename);
                string IconText = TransformIcon.Get(sourcename);

                FilterSourceItems.Add(new ListSource
                {
                    TypeStoreIcon = ModeSimple,
                    SourceIcon = Icon,
                    SourceIconText = IconText,
                    SourceName = sourcename,
                    SourceNameShort = sourcename,
                    IsCheck = false
                });
            }

            FilterSourceItems.Sort((x, y) => x.SourceNameShort.CompareTo(y.SourceNameShort));
            ViewModel.FilterSourceText = string.Empty;
        }


        #region Generate graphics and list
        private void StartReloadMonthChart(int year, int month)
        {
            int myVersion = Interlocked.Increment(ref _monthChartReloadVersion);
            CancellationTokenSource previousCts = Interlocked.Exchange(ref _monthChartReloadCts, new CancellationTokenSource());
            if (previousCts != null)
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }
            CancellationToken token = _monthChartReloadCts.Token;

            bool isMonthSourcesSnapshot = IsMonthSources;
            bool isGenresSourcesSnapshot = IsGenresSources;
            bool cumulSnapshot = PluginDatabase.PluginSettings.CumulPlaytimeStore;

            bool showLauncherIcons = PluginDatabase.PluginSettings.ShowLauncherIcons;
            double labelsRotation = showLauncherIcons ? 0 : 160;
            double fontSize = showLauncherIcons ? 30 : (double)ResourceProvider.GetResource("FontSize");

            // Tooltip mode only depends on the current aggregation mode.
            TextBlockWithIconMode tooltipMode = isMonthSourcesSnapshot
                ? ModeComplet
                : TextBlockWithIconMode.TextOnly;

            bool showAxisLabels = isMonthSourcesSnapshot || isGenresSourcesSnapshot;
            bool isTagsMode = !isMonthSourcesSnapshot && !isGenresSourcesSnapshot;

            _ = Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
#if DEBUG
                DebugTimer computeTimer = new DebugTimer(string.Format("GameActivityView.MonthChart compute async ({0},{1})", year, month));
                computeTimer.Step("start");
#endif
                PlayTimeToStringConverter localConverter = new PlayTimeToStringConverter();

                DateTime startOfMonth = new DateTime(year, month, 1, 0, 0, 0);
                DateTime endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

                Dictionary<string, ulong> activityByMonth = new Dictionary<string, ulong>();
                List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);

                // Total hours by source/genre/tag (same rules as previous implementation).
                if (isMonthSourcesSnapshot)
                {
                    for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }
                        try
                        {
                            List<Activity> activities = listGameActivities[iGame].FilterItems;
                            for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return null;
                                }
                                ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                                DateTime dateSession = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime();
                                if (dateSession < startOfMonth || dateSession > endOfMonth)
                                {
                                    continue;
                                }

                                string sourceName = activities[iActivity].SourceName;
                                if (activityByMonth.ContainsKey(sourceName))
                                {
                                    activityByMonth[sourceName] = activityByMonth[sourceName] + elapsedSeconds;
                                }
                                else
                                {
                                    activityByMonth.Add(sourceName, elapsedSeconds);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error in month chart compute ({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                        }
                    }
                }
                else if (isGenresSourcesSnapshot)
                {
                    for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }
                        try
                        {
                            List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                            List<Activity> activities = listGameActivities[iGame].FilterItems;
                            for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return null;
                                }
                                ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                                DateTime dateSession = Convert.ToDateTime(activities[iActivity].DateSession).AddSeconds(-(double)elapsedSeconds).ToLocalTime();
                                if (dateSession < startOfMonth || dateSession > endOfMonth)
                                {
                                    continue;
                                }

                                for (int iGenre = 0; iGenre < listGameListGenres?.Count; iGenre++)
                                {
                                    string genreName = listGameListGenres[iGenre].Name;
                                    if (activityByMonth.ContainsKey(genreName))
                                    {
                                        activityByMonth[genreName] = activityByMonth[genreName] + elapsedSeconds;
                                    }
                                    else
                                    {
                                        activityByMonth.Add(genreName, elapsedSeconds);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error in month chart genres compute ({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                        }
                    }
                }
                else
                {
                    // Tags mode.
                    for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }
                        try
                        {
                            List<Tag> listGameListTags = listGameActivities[iGame].Tags;
                            List<Activity> activities = listGameActivities[iGame].FilterItems;
                            for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return null;
                                }
                                ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                                DateTime dateSession = Convert.ToDateTime(activities[iActivity].DateSession).AddSeconds(-(double)elapsedSeconds).ToLocalTime();
                                if (dateSession < startOfMonth || dateSession > endOfMonth)
                                {
                                    continue;
                                }

                                for (int iTag = 0; iTag < listGameListTags?.Count; iTag++)
                                {
                                    string tagName = listGameListTags[iTag].Name;
                                    if (activityByMonth.ContainsKey(tagName))
                                    {
                                        activityByMonth[tagName] = activityByMonth[tagName] + elapsedSeconds;
                                    }
                                    else
                                    {
                                        activityByMonth.Add(tagName, elapsedSeconds);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error in month chart tags compute ({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                        }
                    }

                    // Join same time (keep legacy behavior).
                    Dictionary<string, ulong> activityTEMP = new Dictionary<string, ulong>();
                    activityByMonth.ForEach(x =>
                    {
                        string val = (string)localConverter.Convert(x.Value, null, null, CultureInfo.CurrentCulture);
                        IEnumerable<KeyValuePair<string, ulong>> d = activityTEMP.Where(y => ((string)localConverter.Convert(y.Value, null, null, CultureInfo.CurrentCulture)).IsEqual(val));
                        if (d.Count() != 0)
                        {
                            string k = d.First().Key;
                            ulong v = d.First().Value;

                            _ = activityTEMP.Remove(k);
                            activityTEMP.Add(k + "\r\n" + x.Key, v);
                        }
                        else
                        {
                            activityTEMP.Add(x.Key, x.Value);
                        }
                    });
                    activityByMonth = activityTEMP;
                }

                // Build plain data only (no LiveCharts/WPF types here).
                List<CustomerForTime> items = new List<CustomerForTime>(activityByMonth.Count);
                string[] labels = new string[activityByMonth.Count];
                int compteur = 0;
                foreach (KeyValuePair<string, ulong> item in activityByMonth)
                {
                    items.Add(new CustomerForTime
                    {
                        Icon = PlayniteTools.GetPlatformIcon(item.Key),
                        IconText = TransformIcon.Get(item.Key),
                        Name = item.Key,
                        Values = (long)item.Value,
                    });

                    labels[compteur] = item.Key;
                    if (showLauncherIcons)
                    {
                        labels[compteur] = TransformIcon.Get(labels[compteur]);
                    }

                    compteur++;
                }

                bool hideTotalHoursChart = isMonthSourcesSnapshot && cumulSnapshot;
                bool hideTotalHoursLabel = isMonthSourcesSnapshot && cumulSnapshot;
                int gridMonthColumnSpan = isMonthSourcesSnapshot ? 1 : 5;

                bool showDayChart = isMonthSourcesSnapshot;
                bool showWeekChart = isMonthSourcesSnapshot;

                bool showTotalHoursChart = !hideTotalHoursChart;
                bool showTotalHoursLabel = !hideTotalHoursLabel;

                // In legacy logic, when cumulative is enabled in month sources mode, we also adjust GridDay column.
                bool adjustGridDay = isMonthSourcesSnapshot && cumulSnapshot;

#if DEBUG
                computeTimer.Stop(string.Format("labels={0}, items={1}", labels.Length, activityByMonth.Count));
#endif
                return new
                {
                    myVersion,
                    items,
                    labels,
                    labelsRotation,
                    fontSize,
                    showAxisLabels,
                    isTagsMode,
                    tooltipMode,
                    showTotalHoursChart,
                    showTotalHoursLabel,
                    showDayChart,
                    showWeekChart,
                    gridMonthColumnSpan,
                    adjustGridDay
                };
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    return;
                }

                var result = t.Result;
                if (result == null)
                {
                    return;
                }
                if ((int)result.myVersion != _monthChartReloadVersion)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
#if DEBUG
                    DebugTimer uiTimer = new DebugTimer(string.Format("GameActivityView.MonthChart UI ({0},{1})", year, month));
                    uiTimer.Step("start");
#endif
                    EnsureCustomerTimeMapper();

                    PART_ChartTotalHoursSource_X.LabelsRotation = result.labelsRotation;
                    PART_ChartTotalHoursSource_X.FontSize = result.fontSize;

                    if (result.adjustGridDay)
                    {
                        PART_ChartTotalHoursSource.Visibility = Visibility.Hidden;
                        PART_ChartTotalHoursSource_Label.Visibility = Visibility.Hidden;
                        Grid.SetColumn(GridDay, 0);
                        Grid.SetColumnSpan(GridDay, 3);
                    }
                    else
                    {
                        PART_ChartTotalHoursSource.Visibility = result.showTotalHoursChart ? Visibility.Visible : Visibility.Hidden;
                        PART_ChartTotalHoursSource_Label.Visibility = result.showTotalHoursLabel ? Visibility.Visible : Visibility.Hidden;
                    }

                    Grid.SetColumnSpan(gridMonth, result.gridMonthColumnSpan);

                    PART_ChartHoursByDaySource.Visibility = result.showDayChart ? Visibility.Visible : Visibility.Hidden;
                    actLabel.Visibility = result.showDayChart ? Visibility.Visible : Visibility.Hidden;
                    PART_ChartHoursByWeekSource.Visibility = result.showWeekChart ? Visibility.Visible : Visibility.Hidden;
                    acwLabel.Visibility = result.showWeekChart ? Visibility.Visible : Visibility.Hidden;

                    PART_ChartTotalHoursSource_Y.LabelFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);
                    PART_ChartTotalHoursSource_Y.MinValue = 0;

                    ChartValues<CustomerForTime> values = new ChartValues<CustomerForTime>();
                    for (int i = 0; i < result.items.Count; i++)
                    {
                        values.Add(result.items[i]);
                    }

                    SeriesCollection chartSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = string.Empty,
                            Values = values,
                            Fill = PluginDatabase.PluginSettings.ChartColors
                        }
                    };

                    PART_ChartTotalHoursSource.Series = chartSeries;

                    PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime
                    {
                        ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons,
                        Mode = result.tooltipMode
                    };

                    PART_ChartTotalHoursSource_X.Labels = result.labels;
                    PART_ChartTotalHoursSource_X.ShowLabels = result.showAxisLabels;
                    if (!showAxisLabels)
                    {
                        PART_ChartTotalHoursSource_X.ShowLabels = false;
                    }
                    PART_ChartTotalHoursSource_X.Separator = result.isTagsMode ? new LiveCharts.Wpf.Separator { IsEnabled = false } : new LiveCharts.Wpf.Separator { IsEnabled = true };

#if DEBUG
                    uiTimer.Stop();
#endif
                });
            });
        }

        /// <summary>
        /// Get data graphic activity by month with time by source or by genre.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        public void GetActivityByMonth(int year, int month)
        {
            StartReloadMonthChart(year, month);
#if false
#if DEBUG
            var timer = new DebugTimer(string.Format("GameActivityView.GetActivityByMonth({0},{1})", year, month));
#endif
            DateTime startOfMonth = new DateTime(year, month, 1, 0, 0, 0);
            DateTime endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

            Dictionary<string, ulong> activityByMonth = new Dictionary<string, ulong>();

            // Cache DB fetch for month range (used by month/week/day charts).
            List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);

            // Total hours by source.
            if (IsMonthSources)
            {
                if (PluginDatabase.PluginSettings.ShowLauncherIcons)
                {
                    PART_ChartTotalHoursSource_X.LabelsRotation = 0;
                    PART_ChartTotalHoursSource_X.FontSize = 30;
                }
                else
                {
                    PART_ChartTotalHoursSource_X.LabelsRotation = 160;
                    PART_ChartTotalHoursSource_X.FontSize = (double)ResourceProvider.GetResource("FontSize");
                }

                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        //This filters the items but only of the session is valid, so longer than ignored seconds or has any duration at all
                        //This does not return a filtered session data for dates
                        List<Activity> Activities = listGameActivities[iGame].FilterItems;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                            string sourceName = Activities[iActivity].SourceName;
                            if (dateSession >= startOfMonth && dateSession <= endOfMonth)
                            {
                                // Cumul data
                                if (activityByMonth.ContainsKey(sourceName))
                                {
                                    activityByMonth[sourceName] = (ulong)activityByMonth[sourceName] + elapsedSeconds;
                                }
                                else
                                {
                                    activityByMonth.Add(sourceName, elapsedSeconds);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                    }
                }

                PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime { ShowIcon = ShowIcon, Mode = ModeComplet };
            }
            // Total hours by genres.
            else if (IsGenresSources)
            {
                PART_ChartTotalHoursSource_X.LabelsRotation = 160;
                PART_ChartTotalHoursSource_X.FontSize = (double)ResourceProvider.GetResource("FontSize");

                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                        List<Activity> Activities = listGameActivities[iGame].FilterItems;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).AddSeconds(-(double)elapsedSeconds).ToLocalTime();

                            for (int iGenre = 0; iGenre < listGameListGenres?.Count; iGenre++)
                            {
                                if (dateSession >= startOfMonth && dateSession <= endOfMonth)
                                {
                                    // Cumul data
                                    if (activityByMonth.ContainsKey(listGameListGenres[iGenre].Name))
                                    {
                                        activityByMonth[listGameListGenres[iGenre].Name] = activityByMonth[listGameListGenres[iGenre].Name] + elapsedSeconds;
                                    }
                                    else
                                    {
                                        activityByMonth.Add(listGameListGenres[iGenre].Name, elapsedSeconds);
                                    }
                                }
                            }
                        }

                        // Tooltip set once after the loop: same mode for all genres.
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                    }
                }

                PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime { ShowIcon = false, Mode = TextBlockWithIconMode.TextOnly };
            }
            else
            {
                PART_ChartTotalHoursSource_X.LabelsRotation = 160;
                PART_ChartTotalHoursSource_X.FontSize = (double)ResourceProvider.GetResource("FontSize");

                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Tag> listGameListTags = listGameActivities[iGame].Tags;
                        List<Activity> Activities = listGameActivities[iGame].FilterItems;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).AddSeconds(-(double)elapsedSeconds).ToLocalTime();

                            for (int iTag = 0; iTag < listGameListTags?.Count; iTag++)
                            {
                                if (dateSession >= startOfMonth && dateSession <= endOfMonth)
                                {
                                    // Cumul data
                                    if (activityByMonth.ContainsKey(listGameListTags[iTag].Name))
                                    {
                                        activityByMonth[listGameListTags[iTag].Name] = activityByMonth[listGameListTags[iTag].Name] + elapsedSeconds;
                                    }
                                    else
                                    {
                                        activityByMonth.Add(listGameListTags[iTag].Name, elapsedSeconds);
                                    }
                                }
                            }
                        }

                        // Tooltip set once after the loop: same mode for all tags.
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                    }
                }

                PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime { ShowIcon = false, Mode = TextBlockWithIconMode.TextOnly };

                // Join same time
                Dictionary<string, ulong> activityTEMP = new Dictionary<string, ulong>();
                activityByMonth.ForEach(x =>
                {
                    string val = (string)Converter.Convert(x.Value, null, null, CultureInfo.CurrentCulture);
                    IEnumerable<KeyValuePair<string, ulong>> d = activityTEMP.Where(y => ((string)Converter.Convert(y.Value, null, null, CultureInfo.CurrentCulture)).IsEqual(val));
                    if (d.Count() != 0)
                    {
                        string k = d.First().Key;
                        ulong v = d.First().Value;

                        _ = activityTEMP.Remove(k);
                        activityTEMP.Add(k + "\r\n" + x.Key, v);
                    }
                    else
                    {
                        activityTEMP.Add(x.Key, x.Value);
                    }
                });
                activityByMonth = activityTEMP;
            }


            // Set data in graphic.
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
            string[] labels = new string[activityByMonth.Count];
            int compteur = 0;
            foreach (KeyValuePair<string, ulong> item in activityByMonth)
            {
                series.Add(new CustomerForTime
                {
                    Icon = PlayniteTools.GetPlatformIcon(item.Key),
                    IconText = TransformIcon.Get(item.Key),

                    Name = item.Key,
                    Values = (long)item.Value,
                });
                labels[compteur] = item.Key;
                if (PluginDatabase.PluginSettings.ShowLauncherIcons)
                {
                    labels[compteur] = TransformIcon.Get(labels[compteur]);
                }
                compteur++;
            }

            SeriesCollection ActivityByMonthSeries;
            ActivityByMonthSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = string.Empty,
                    Values = series,
                    Fill = PluginDatabase.PluginSettings.ChartColors
                }
            };
            string[] ActivityByMonthLabels = labels;

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally (avoid re-registering on each reload)
            if (!_customerTimeMapperInitialized)
            {
                Charting.For<CustomerForTime>(customerVmMapper);
                _customerTimeMapperInitialized = true;
            }

            Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

            if (IsMonthSources)
            {
                if (PluginDatabase.PluginSettings.CumulPlaytimeStore)
                {
                    PART_ChartTotalHoursSource.Visibility = Visibility.Hidden;
                    PART_ChartTotalHoursSource_Label.Visibility = Visibility.Hidden;

                    Grid.SetColumn(GridDay, 0);
                    Grid.SetColumnSpan(GridDay, 3);
                }

                Grid.SetColumnSpan(gridMonth, 1);
                PART_ChartHoursByDaySource.Visibility = Visibility.Visible;
                actLabel.Visibility = Visibility.Visible;
                PART_ChartHoursByWeekSource.Visibility = Visibility.Visible;
                acwLabel.Visibility = Visibility.Visible;
            }
            else
            {
                if (PluginDatabase.PluginSettings.CumulPlaytimeStore)
                {
                    PART_ChartTotalHoursSource.Visibility = Visibility.Visible;
                    PART_ChartTotalHoursSource_Label.Visibility = Visibility.Visible;
                }

                Grid.SetColumnSpan(gridMonth, 5);
                PART_ChartHoursByDaySource.Visibility = Visibility.Hidden;
                actLabel.Visibility = Visibility.Hidden;
                PART_ChartHoursByWeekSource.Visibility = Visibility.Hidden;
                acwLabel.Visibility = Visibility.Hidden;
            }


            PART_ChartTotalHoursSource_Y.LabelFormatter = activityForGameLogFormatter;
            PART_ChartTotalHoursSource.Series = ActivityByMonthSeries;
            PART_ChartTotalHoursSource_Y.MinValue = 0;
            ((CustomerToolTipForTime)PART_ChartTotalHoursSource.DataTooltip).ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons;
            PART_ChartTotalHoursSource_X.Labels = ActivityByMonthLabels;

            PART_ChartTotalHoursSource_X.ShowLabels = true;
            if (!IsMonthSources && !IsGenresSources)
            {
                PART_ChartTotalHoursSource_X.ShowLabels = false;
            }
#if DEBUG
            timer.Stop(string.Format("series={0}, labels={1}", PART_ChartTotalHoursSource.Series?.Count ?? 0, PART_ChartTotalHoursSource_X.Labels?.Count ?? 0));
#endif
#endif
        }

        private void StartReloadDayChart(int year, int month)
        {
            int myVersion = Interlocked.Increment(ref _dayChartReloadVersion);
            CancellationTokenSource previousCts = Interlocked.Exchange(ref _dayChartReloadCts, new CancellationTokenSource());
            if (previousCts != null)
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }
            CancellationToken token = _dayChartReloadCts.Token;

#if DEBUG
            DebugTimer timer = new DebugTimer(string.Format("GameActivityView.GetActivityByDay({0},{1})", year, month));
#endif
            _ = Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
#if DEBUG
                timer.Step("start");
#endif
                DateTime startDate = new DateTime(year, month, 1, 0, 0, 0);
                int numberDayInMonth = DateTime.DaysInMonth(year, month);
                DateTime endDate = new DateTime(year, month, numberDayInMonth, 23, 59, 59);

                string[] activityByDateLabels = new string[numberDayInMonth];
                long[] dayValues = new long[numberDayInMonth];

                for (int iDay = 0; iDay < numberDayInMonth; iDay++)
                {
                    activityByDateLabels[iDay] = startDate.AddDays(iDay).ToString(Constants.DateUiFormat);
                }

                List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }
                    List<Activity> activities = listGameActivities[iGame].FilterItems;
                    for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }
                        ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                        DateTime sessionDate = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime();
                        if (sessionDate < startDate || sessionDate > endDate)
                        {
                            continue;
                        }

                        int dayIndex = sessionDate.Day - 1;
                        if (dayIndex >= 0 && dayIndex < dayValues.Length)
                        {
                            dayValues[dayIndex] = dayValues[dayIndex] + (long)elapsedSeconds;
                        }
                    }
                }

                return new DayChartData
                {
                    Labels = activityByDateLabels,
                    Values = dayValues
                };
            }, token).ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (myVersion != _dayChartReloadVersion)
                {
                    return;
                }

                DayChartData data = t.Result;
                if (data == null)
                {
                    return;
                }
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (myVersion != _dayChartReloadVersion)
                    {
                        return;
                    }

                    EnsureCustomerTimeMapper();

                    ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
                    for (int i = 0; i < data.Values.Length; i++)
                    {
                        series.Add(new CustomerForTime
                        {
                            Name = data.Labels[i],
                            Values = data.Values[i]
                        });
                    }

                    SeriesCollection activityByDaySeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = string.Empty,
                            Values = series,
                            Fill = PluginDatabase.PluginSettings.ChartColors
                        }
                    };

                    Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                    PART_ChartHoursByDaySource_Y.LabelFormatter = activityForGameLogFormatter;
                    PART_ChartHoursByDaySource.DataTooltip = new CustomerToolTipForTime { ShowIcon = ShowIcon, Mode = ModeComplet };
                    PART_ChartHoursByDaySource.Series = activityByDaySeries;
                    PART_ChartHoursByDaySource_Y.MinValue = 0;
                    PART_ChartHoursByDaySource_X.Labels = data.Labels;
                });

#if DEBUG
                timer.Stop(string.Format("days={0}, seriesPoints={1}", data.Labels?.Length ?? 0, data.Values?.Length ?? 0));
#endif
            });
        }

        public void GetActivityByDay(int year, int month)
        {
            StartReloadDayChart(year, month);
#if false
#if DEBUG
            var timer = new DebugTimer(string.Format("GameActivityView.GetActivityByDay({0},{1})", year, month));
#endif
            DateTime startDate = new DateTime(year, month, 1, 0, 0, 0);
            int numberDayInMonth = DateTime.DaysInMonth(year, month);
            DateTime endDate = new DateTime(year, month, numberDayInMonth, 23, 59, 59);

            string[] activityByDateLabels = new string[numberDayInMonth];
            SeriesCollection activityByDaySeries = new SeriesCollection();
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();

            for (int iDay = 0; iDay < numberDayInMonth; iDay++)
            {
                activityByDateLabels[iDay] = startDate.AddDays(iDay).ToString(Constants.DateUiFormat);
                series.Add(new CustomerForTime
                {
                    Name = activityByDateLabels[iDay],
                    Values = 0
                });
            }

            List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                List<Activity> activities = listGameActivities[iGame].FilterItems;
                for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                {
                    ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                    DateTime sessionDate = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime();
                    if (sessionDate < startDate || sessionDate > endDate)
                    {
                        continue;
                    }

                    int dayIndex = sessionDate.Day - 1;
                    if (dayIndex >= 0 && dayIndex < series.Count)
                    {
                        series[dayIndex].Values += (long)elapsedSeconds;
                    }
                }
            }

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                activityByDaySeries.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = series,
                    Fill = PluginDatabase.PluginSettings.ChartColors
                });

                //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                var customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);

                //lets save the mapper globally (avoid re-registering)
                if (!_customerTimeMapperInitialized)
                {
                    Charting.For<CustomerForTime>(customerVmMapper);
                    _customerTimeMapperInitialized = true;
                }

                Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartHoursByDaySource_Y.LabelFormatter = activityForGameLogFormatter;
                PART_ChartHoursByDaySource.DataTooltip = new CustomerToolTipForTime { ShowIcon = ShowIcon, Mode = ModeComplet };
                PART_ChartHoursByDaySource.Series = activityByDaySeries;
                PART_ChartHoursByDaySource_Y.MinValue = 0;
                PART_ChartHoursByDaySource_X.Labels = activityByDateLabels;
            });
#if DEBUG
            timer.Stop(string.Format("days={0}, seriesPoints={1}", numberDayInMonth, series.Count));
#endif
#endif
        }

        private class WeekChartData
        {
            public List<WeekStartEnd> DatesPeriodes { get; set; }
            public string[] WeekLabels { get; set; }
            public bool UseCumulPlaytimeStore { get; set; }
            public List<string> SourceNames { get; set; }
            public List<string> SourceLabels { get; set; }
            public long[][] ValuesBySource { get; set; }
            public long[] WeekTotals { get; set; }
        }

        private class DayChartData
        {
            public string[] Labels { get; set; }
            public long[] Values { get; set; }
        }

        private void StartReloadWeekChart(int year, int month)
        {
            if (!IsMonthSources)
            {
                // Month chart hides the week chart when not in "sources" mode.
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PART_ChartHoursByWeekSource.Visibility = Visibility.Hidden;
                    acwLabel.Visibility = Visibility.Hidden;
                });
                return;
            }

            int myVersion = Interlocked.Increment(ref _weekChartReloadVersion);
            CancellationTokenSource previousCts = Interlocked.Exchange(ref _weekChartReloadCts, new CancellationTokenSource());
            if (previousCts != null)
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }
            CancellationToken token = _weekChartReloadCts.Token;

            bool useCumul = PluginDatabase.PluginSettings.CumulPlaytimeStore;
            bool showLauncherIcons = PluginDatabase.PluginSettings.ShowLauncherIcons;

#if DEBUG
            DebugTimer computeTimer = new DebugTimer(string.Format("GameActivityView.WeekChart compute async ({0},{1})", year, month));
#endif

            _ = Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
#if DEBUG
                computeTimer.Step("start");
#endif

                DateTime StartDate = new DateTime(year, month, 1, 0, 0, 0);
                DateTime SeriesEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

                // find first monday
                DateTime firstMonday = Enumerable.Range(0, 7)
                    .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                    .Select(x => StartDate.AddDays(x))
                    .First();

                if (firstMonday > StartDate)
                {
                    firstMonday = Enumerable.Range(-6, 7)
                        .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                        .Select(x => StartDate.AddDays(x))
                        .First();
                }

                // create week periods
                TimeSpan ts = (TimeSpan)(SeriesEndDate - firstMonday);
                List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();
                int iDays = 0;
                for (iDays = 0; iDays < ts.Days; iDays += 7)
                {
                    datesPeriodes.Add(new WeekStartEnd
                    {
                        Monday = firstMonday.AddDays(iDays),
                        Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59)
                    });
                }

                if (datesPeriodes.Count > 0 && datesPeriodes[datesPeriodes.Count - 1].Sunday < SeriesEndDate)
                {
                    datesPeriodes.Add(new WeekStartEnd
                    {
                        Monday = firstMonday.AddDays(iDays),
                        Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59)
                    });
                }

                int weekCount = datesPeriodes.Count;

                Dictionary<string, long[]> valuesBySource = new Dictionary<string, long[]>();

                List<string> sourcesSnapshot = ListSources;
                if (sourcesSnapshot == null)
                {
                    sourcesSnapshot = new List<string>();
                }

                for (int iSource = 0; iSource < sourcesSnapshot.Count; iSource++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }
                    string src = sourcesSnapshot[iSource];
                    if (!valuesBySource.ContainsKey(src))
                    {
                        valuesBySource.Add(src, new long[weekCount]);
                    }
                }

                List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);

                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }
                    List<Activity> activities = listGameActivities[iGame].FilterItems;
                    for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }
                        ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime();
                        string sourceName = activities[iActivity].SourceName;

                        int matchedWeek = -1;
                        for (int iWeek = 0; iWeek < weekCount; iWeek++)
                        {
                            if (datesPeriodes[iWeek].Monday <= dateSession && dateSession <= datesPeriodes[iWeek].Sunday)
                            {
                                matchedWeek = iWeek;
                                break;
                            }
                        }

                        if (matchedWeek == -1)
                        {
                            continue;
                        }

                        if (!valuesBySource.ContainsKey(sourceName))
                        {
                            valuesBySource.Add(sourceName, new long[weekCount]);
                        }

                        valuesBySource[sourceName][matchedWeek] = valuesBySource[sourceName][matchedWeek] + (long)elapsedSeconds;
                    }
                }

                List<string> sourceNamesWithData = new List<string>();
                foreach (KeyValuePair<string, long[]> kvp in valuesBySource)
                {
                    long sum = 0;
                    for (int w = 0; w < weekCount; w++)
                    {
                        sum += kvp.Value[w];
                    }

                    if (sum != 0)
                    {
                        sourceNamesWithData.Add(kvp.Key);
                    }
                }

                string[] weekLabels = new string[weekCount];
                for (int w = 0; w < weekCount; w++)
                {
                    weekLabels[w] = ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[w].Monday);
                }

                List<string> sourceLabels = new List<string>(sourceNamesWithData.Count);
                long[][] valuesBySourceArr = new long[sourceNamesWithData.Count][];

                for (int i = 0; i < sourceNamesWithData.Count; i++)
                {
                    string src = sourceNamesWithData[i];
                    sourceLabels.Add(showLauncherIcons ? TransformIcon.Get(src) : src);
                    valuesBySourceArr[i] = valuesBySource[src];
                }

                long[] weekTotals = null;
                if (useCumul)
                {
                    weekTotals = new long[weekCount];
                    for (int iSource = 0; iSource < sourceNamesWithData.Count; iSource++)
                    {
                        long[] arr = valuesBySourceArr[iSource];
                        for (int w = 0; w < weekCount; w++)
                        {
                            weekTotals[w] = weekTotals[w] + arr[w];
                        }
                    }
                }

#if DEBUG
                computeTimer.Stop(string.Format("weekCount={0}, sources={1}", weekCount, sourceNamesWithData.Count));
#endif

                return new WeekChartData
                {
                    DatesPeriodes = datesPeriodes,
                    WeekLabels = weekLabels,
                    UseCumulPlaytimeStore = useCumul,
                    SourceNames = sourceNamesWithData,
                    SourceLabels = sourceLabels,
                    ValuesBySource = valuesBySourceArr,
                    WeekTotals = weekTotals
                };
            }, token).ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                WeekChartData data = t.Result;
                if (data == null)
                {
                    return;
                }
                if (myVersion != _weekChartReloadVersion)
                {
                    return;
                }

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (myVersion != _weekChartReloadVersion)
                    {
                        return;
                    }

#if DEBUG
                    DebugTimer uiTimer = new DebugTimer(string.Format("GameActivityView.WeekChart UI ({0},{1})", year, month));
                    uiTimer.Step("start");
#endif
                    EnsureCustomerTimeMapper();

                    Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);
                    PART_ChartHoursByWeekSource_Y.LabelFormatter = activityForGameLogFormatter;
                    PART_ChartHoursByWeekSource_Y.MinValue = 0;
                    PART_ChartHoursByWeekSource_X.Labels = data.WeekLabels;

                    if (data.UseCumulPlaytimeStore)
                    {
                        ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
                        for (int w = 0; w < data.WeekTotals.Length; w++)
                        {
                            series.Add(new CustomerForTime { Name = data.WeekLabels[w], Values = data.WeekTotals[w] });
                        }

                        PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForTime
                        {
                            ShowIcon = ShowIcon,
                            ShowTitle = true,
                            Mode = ModeComplet,
                            ShowWeekPeriode = true,
                            DatesPeriodes = data.DatesPeriodes
                        };

                        SeriesCollection activityByWeekSeries = new SeriesCollection();
                        activityByWeekSeries.Add(new ColumnSeries
                        {
                            Title = string.Empty,
                            Values = series,
                            Fill = PluginDatabase.PluginSettings.ChartColors
                        });

                        PART_ChartHoursByWeekSource.Series = activityByWeekSeries;
                    }
                    else
                    {
                        if (PluginDatabase.PluginSettings.StoreColors.Count == 0)
                        {
                            PluginDatabase.PluginSettings.StoreColors = GameActivitySettingsViewModel.GetDefaultStoreColors();
                        }

                        PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForMultipleTime
                        {
                            ShowIcon = ShowIcon,
                            ShowTitle = true,
                            Mode = ModeComplet,
                            ShowWeekPeriode = true,
                            DatesPeriodes = data.DatesPeriodes
                        };

                        SeriesCollection activityByWeekSeries = new SeriesCollection();
                        for (int iSource = 0; iSource < data.SourceNames.Count; iSource++)
                        {
                            string sourceName = data.SourceNames[iSource];
                            string sourceLabel = data.SourceLabels[iSource];
                            long[] valuesArr = data.ValuesBySource[iSource];

                            ChartValues<CustomerForTime> values = new ChartValues<CustomerForTime>();
                            for (int w = 0; w < data.DatesPeriodes.Count; w++)
                            {
                                values.Add(new CustomerForTime { Name = sourceName, Values = (int)valuesArr[w] });
                            }

                            Brush fill = PluginDatabase.PluginSettings.StoreColors
                                .Where(x => x.Name.Contains(sourceName, StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault()?.Fill;

                            activityByWeekSeries.Add(new StackedColumnSeries
                            {
                                Title = sourceLabel,
                                Values = values,
                                StackMode = StackMode.Values,
                                DataLabels = false,
                                Fill = fill
                            });
                        }

                        PART_ChartHoursByWeekSource.Series = activityByWeekSeries;
                    }

#if DEBUG
                    uiTimer.Stop();
#endif
                });
            });
        }


        /// <summary>
        /// Get data graphic activity by week.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>        
        public void GetActivityByWeek(int year, int month)
        {
            StartReloadWeekChart(year, month);
#if false
#if DEBUG
            var timer = new DebugTimer(string.Format("GameActivityView.GetActivityByWeek({0},{1})", year, month));
#endif
            // legacy implementation disabled (async reload below)
            //https://www.codeproject.com/Questions/1276907/Get-every-weeks-start-and-end-date-from-series-end
            //usage:
            DateTime StartDate = new DateTime(year, month, 1, 0, 0, 0);
            DateTime SeriesEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
            //find first monday
            DateTime firstMonday = Enumerable.Range(0, 7)
                .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                .Select(x => StartDate.AddDays(x))
                .First();

            if (firstMonday > StartDate)
            {
                firstMonday = Enumerable.Range(-6, 7)
                    .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                    .Select(x => StartDate.AddDays(x))
                    .First();
            }

            //get count of days
            TimeSpan ts = (TimeSpan)(SeriesEndDate - firstMonday);
            //create new list of WeekStartEnd class
            List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();
            //add dates to list
            int iDays = 0;
            for (iDays = 0; iDays < ts.Days; iDays += 7)
            {
                datesPeriodes.Add(new WeekStartEnd() { Monday = firstMonday.AddDays(iDays), Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59) });
            }

            if (datesPeriodes.Last().Sunday < SeriesEndDate)
            {
                datesPeriodes.Add(new WeekStartEnd() { Monday = firstMonday.AddDays(iDays), Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59) });
            }

            // Source activty by month
            Dictionary<string, long> activityByWeek1 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek2 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek3 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek4 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek5 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek6 = new Dictionary<string, long>();

            List<Dictionary<string, long>> activityByWeek = new List<Dictionary<string, long>>();
            SeriesCollection activityByWeekSeries = new SeriesCollection();
            IChartValues Values = new ChartValues<CustomerForTime>();

            if (IsMonthSources)
            {
                // Insert sources
                for (int iSource = 0; iSource < ListSources.Count; iSource++)
                {
                    activityByWeek1.Add(ListSources[iSource], 0);
                    activityByWeek2.Add(ListSources[iSource], 0);
                    activityByWeek3.Add(ListSources[iSource], 0);
                    activityByWeek4.Add(ListSources[iSource], 0);
                    activityByWeek5.Add(ListSources[iSource], 0);
                    activityByWeek6.Add(ListSources[iSource], 0);
                }

                activityByWeek.Add(activityByWeek1);
                activityByWeek.Add(activityByWeek2);
                activityByWeek.Add(activityByWeek3);
                activityByWeek.Add(activityByWeek4);
                activityByWeek.Add(activityByWeek5);
                activityByWeek.Add(activityByWeek6);


                // Cache DB fetch for month range (used by month/week/day charts).
                List<GameActivities> listGameActivities = GetMonthFilteredGameActivities(year, month);

                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> Activities = listGameActivities[iGame].FilterItems;
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                        string sourceName = Activities[iActivity].SourceName;

                        // Cumul data
                        for (int iWeek = 0; iWeek < datesPeriodes.Count; iWeek++)
                        {
                            if (datesPeriodes[iWeek].Monday <= dateSession && dateSession <= datesPeriodes[iWeek].Sunday)
                            {
                                // Add source by platform
                                if (!activityByWeek[iWeek].ContainsKey(sourceName))
                                {
                                    activityByWeek1.Add(sourceName, 0);
                                    activityByWeek2.Add(sourceName, 0);
                                    activityByWeek3.Add(sourceName, 0);
                                    activityByWeek4.Add(sourceName, 0);
                                    activityByWeek5.Add(sourceName, 0);
                                    activityByWeek6.Add(sourceName, 0);
                                }

                                activityByWeek[iWeek][sourceName] = activityByWeek[iWeek][sourceName] + (long)elapsedSeconds;
                            }
                        }
                    }
                }


                // Check source with data (only view this)
                List<string> listNoDelete = new List<string>();
                for (int i = 0; i < activityByWeek.Count; i++)
                {
                    foreach (KeyValuePair<string, long> item in activityByWeek[i])
                    {
                        if (item.Value != 0 && listNoDelete.TakeWhile(x => x.ToString() == item.Key).Count() != 1)
                        {
                            listNoDelete.Add(item.Key);
                        }
                    }
                }
                listNoDelete = listNoDelete.Select(x => x).Distinct().ToList();


                // Prepare data.
                string[] labels = new string[listNoDelete.Count];
                if (PluginDatabase.PluginSettings.StoreColors.Count == 0)
                {
                    PluginDatabase.PluginSettings.StoreColors = GameActivitySettingsViewModel.GetDefaultStoreColors();
                }
                for (int iSource = 0; iSource < listNoDelete.Count; iSource++)
                {
                    labels[iSource] = listNoDelete[iSource];
                    if (PluginDatabase.PluginSettings.ShowLauncherIcons)
                    {
                        labels[iSource] = TransformIcon.Get(listNoDelete[iSource]);
                    }

                    Brush Fill = null;
                    Fill = PluginDatabase.PluginSettings.StoreColors
                                .Where(x => x.Name.Contains(listNoDelete[iSource], StringComparison.InvariantCultureIgnoreCase))?.FirstOrDefault()?.Fill;


                    Values = new ChartValues<CustomerForTime>();
                    for (int i = 0; i < datesPeriodes.Count; i++)
                    {
                        Values.Add(new CustomerForTime { Name = listNoDelete[iSource], Values = (int)activityByWeek[i][listNoDelete[iSource]] });
                    }

                    activityByWeekSeries.Add(new StackedColumnSeries
                    {
                        Title = labels[iSource],
                        Values = Values,
                        StackMode = StackMode.Values,
                        DataLabels = false,
                        Fill = Fill
                    });
                }
            }


            // Set data in graphics.
            string[] activityByWeekLabels = new[]
            {
                ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[3].Monday)
            };
            if (datesPeriodes.Count == 5)
            {
                activityByWeekLabels = new[]
                {
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[3].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[4].Monday)
                };
            }
            if (datesPeriodes.Count == 6)
            {
                activityByWeekLabels = new[]
                {
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[3].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[4].Monday),
                    ResourceProvider.GetString("LOCGameActivityWeekLabel") + " " + UtilityTools.WeekOfYearISO8601(datesPeriodes[5].Monday)
                };
            }


            if (PluginDatabase.PluginSettings.CumulPlaytimeStore)
            {
                ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
                for (int i = 0; i < activityByWeekSeries.Count; i++)
                {
                    for (int j = 0; j < activityByWeekSeries[i].Values.Count; j++)
                    {
                        if (series.Count == j)
                        {
                            series.Add(new CustomerForTime
                            {
                                Name = activityByWeekLabels[j],
                                Values = ((CustomerForTime)activityByWeekSeries[i].Values[j]).Values
                            });
                        }
                        else
                        {
                            series[j].Values += ((CustomerForTime)activityByWeekSeries[i].Values[j]).Values;
                        }
                    }
                }

                activityByWeekSeries = new SeriesCollection();
                activityByWeekSeries.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = series,
                    Fill = PluginDatabase.PluginSettings.ChartColors
                });

                PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForTime
                {
                    ShowIcon = ShowIcon,
                    ShowTitle = true,
                    Mode = ModeComplet,
                    ShowWeekPeriode = true,
                    DatesPeriodes = datesPeriodes
                };
            }
            else
            {
                PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForMultipleTime
                {
                    ShowIcon = ShowIcon,
                    ShowTitle = true,
                    Mode = ModeComplet,
                    ShowWeekPeriode = true,
                    DatesPeriodes = datesPeriodes
                };
            }

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally (avoid re-registering)
            if (!_customerTimeMapperInitialized)
            {
                Charting.For<CustomerForTime>(customerVmMapper);
                _customerTimeMapperInitialized = true;
            }

            Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

            PART_ChartHoursByWeekSource_Y.LabelFormatter = activityForGameLogFormatter;
            PART_ChartHoursByWeekSource.Series = activityByWeekSeries;
            PART_ChartHoursByWeekSource_Y.MinValue = 0;
            PART_ChartHoursByWeekSource_X.Labels = activityByWeekLabels;
#if DEBUG
            timer.Stop(string.Format("weeks={0}, stackedSeries={1}", datesPeriodes.Count, activityByWeekSeries.Count));
#endif
#endif
        }


        /// <summary>
        /// Get list games with an activities.
        /// </summary>
        public void GetActivityByListGame()
        {
#if DEBUG
            var timer = new DebugTimer("GameActivityView.GetActivityByListGame");
#endif
            ActivityListByGame = new List<ListActivities>();

            List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
            listGameActivities = listGameActivities.Where(x => x.FilterItems.Count > 0 && !x.IsDeleted).ToList();

            string gameID = string.Empty;
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                try
                {
                    gameID = listGameActivities[iGame].Id.ToString();
                    if (!listGameActivities[iGame].Name.IsNullOrEmpty())
                    {
                        string gameTitle = listGameActivities[iGame].Name;
                        Activity lastSessionActivity = listGameActivities[iGame].GetLastSessionActivity();
                        string sourceName = string.Empty;
                        try
                        {
                            sourceName = lastSessionActivity.SourceName;
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, "Error to get SourceName", true, PluginDatabase.PluginName);
                        }
                        ulong elapsedSeconds = lastSessionActivity.ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(lastSessionActivity.DateSession).ToLocalTime();
                        ulong timePlayedInMonth = 0;

                        // Compute all average metrics from the same details list in one pass,
                        // instead of calling AvgCPU/AvgGPU/... methods multiple times.
                        List<ActivityDetailsData> details = lastSessionActivity.Details;
                        int detailsCount = details?.Count ?? 0;
                        long sumFPS = 0;
                        long sumCPU = 0;
                        long sumGPU = 0;
                        long sumRAM = 0;
                        long sumCPUT = 0;
                        long sumGPUT = 0;
                        long sumCPUP = 0;
                        long sumGPUP = 0;

                        if (detailsCount > 0)
                        {
                            for (int i = 0; i < detailsCount; i++)
                            {
                                ActivityDetailsData d = details[i];
                                sumFPS += d.FPS;
                                sumCPU += d.CPU;
                                sumGPU += d.GPU;
                                sumRAM += d.RAM;
                                sumCPUT += d.CPUT;
                                sumGPUT += d.GPUT;
                                sumCPUP += d.CPUP;
                                sumGPUP += d.GPUP;
                            }
                        }

                        int avgFPS = detailsCount > 0 ? (int)Math.Round(sumFPS / (double)detailsCount) : 0;
                        int avgCPU = detailsCount > 0 ? (int)Math.Round(sumCPU / (double)detailsCount) : 0;
                        int avgGPU = detailsCount > 0 ? (int)Math.Round(sumGPU / (double)detailsCount) : 0;
                        int avgRAM = detailsCount > 0 ? (int)Math.Round(sumRAM / (double)detailsCount) : 0;
                        int avgCPUT = detailsCount > 0 ? (int)Math.Round(sumCPUT / (double)detailsCount) : 0;
                        int avgGPUT = detailsCount > 0 ? (int)Math.Round(sumGPUT / (double)detailsCount) : 0;
                        int avgCPUP = detailsCount > 0 ? (int)Math.Round(sumCPUP / (double)detailsCount) : 0;
                        int avgGPUP = detailsCount > 0 ? (int)Math.Round(sumGPUP / (double)detailsCount) : 0;

                        SystemConfiguration config = lastSessionActivity.Configuration;

                        string GameIcon = listGameActivities[iGame].Icon;
                        if (!GameIcon.IsNullOrEmpty())
                        {
                            GameIcon = API.Instance.Database.GetFullFilePath(GameIcon);
                        }

                        ActivityListByGame.Add(new ListActivities()
                        {
                            Id = listGameActivities[iGame].Id,
                            GameId = gameID,
                            GameTitle = gameTitle,
                            GameIcon = GameIcon,
                            GameLastActivity = dateSession,
                            GameElapsedSeconds = elapsedSeconds,
                            TimePlayedInMonth = timePlayedInMonth,
                            GameSourceName = sourceName,
                            GameSourceIcon = TransformIcon.Get(sourceName),
                            DateActivity = listGameActivities[iGame].GetListDateActivity(),
                            AvgCPU = avgCPU + "%",
                            AvgGPU = avgGPU + "%",
                            AvgRAM = avgRAM + "%",
                            AvgFPS = avgFPS + "",
                            AvgCPUT = avgCPUT + "°",
                            AvgGPUT = avgGPUT + "°",
                            AvgCPUP = avgCPUP + "W",
                            AvgGPUP = avgGPUP + "W",

                            EnableWarm = PluginDatabase.PluginSettings.EnableWarning,
                            MaxCPUT = PluginDatabase.PluginSettings.MaxCpuTemp.ToString(),
                            MaxGPUT = PluginDatabase.PluginSettings.MaxGpuTemp.ToString(),
                            MinFPS = PluginDatabase.PluginSettings.MinFps.ToString(),
                            MaxCPU = PluginDatabase.PluginSettings.MaxCpuUsage.ToString(),
                            MaxGPU = PluginDatabase.PluginSettings.MaxGpuUsage.ToString(),
                            MaxRAM = PluginDatabase.PluginSettings.MaxRamUsage.ToString(),

                            PCConfigurationId = lastSessionActivity.IdConfiguration,
                            PCName = config.Name,

                            TypeStoreIcon = ModeSimple,
                            SourceIcon = PlayniteTools.GetPlatformIcon(sourceName),
                            SourceIconText = TransformIcon.Get(sourceName),

                            GameActionName = lastSessionActivity.GameActionName
                        });
                    }
                    // Game is deleted
                    else
                    {
                        Logger.Warn($"Failed to load GameActivities from {gameID} because the game is deleted");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load GameActivities from {gameID}", true, PluginDatabase.PluginName);
                }
            }

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                ViewModel.ActivityListByGame = ActivityListByGame;
                Filter(false);
            });
#if DEBUG
            timer.Stop(string.Format("rows={0}", ActivityListByGame?.Count ?? 0));
#endif
        }


        /// <summary>
        /// Get data for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="variateur"></param>
        public void GetActivityForGamesTimeGraphics(string gameID, bool isNavigation = false)
        {
            PART_GameActivityChartTime.GameContext = API.Instance.Database.Games.Get(Guid.Parse(gameID));
            PART_GameActivityChartTime.DisableAnimations = true;
            PART_GameActivityChartTime.AxisVariator = VariateurTime;

            if (!isNavigation)
            {
                gameLabel.Content = ResourceProvider.GetString("LOCGameActivityTimeTitle");
            }
        }

        /// <summary>
        /// Get data detail for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        public void GetActivityForGamesLogGraphics(string gameID, DateTime? dateSelected = null, string title = "", bool isNavigation = false)
        {
            GameActivities gameActivities = GameActivity.PluginDatabase.Get(Guid.Parse(gameID));

            PART_GameActivityChartLog.GameContext = API.Instance.Database.Games.Get(Guid.Parse(gameID));
            PART_GameActivityChartLog.DisableAnimations = true;
            PART_GameActivityChartLog.DateSelected = dateSelected;
            PART_GameActivityChartLog.TitleChart = title;
            PART_GameActivityChartLog.AxisVariator = VariateurLog;

            if (!isNavigation)
            {
                gameLabel.Content = dateSelected == null || dateSelected == default(DateTime)
                    ? ResourceProvider.GetString("LOCGameActivityLogTitleDate") + " ("
                        + Convert.ToDateTime(gameActivities.GetLastSession()).ToString(Constants.DateUiFormat) + ")"
                    : ResourceProvider.GetString("LOCGameActivityLogTitleDate") + " "
                        + Convert.ToDateTime(dateSelected).ToString(Constants.DateUiFormat);
            }
        }
        #endregion


        /// <summary>
        /// Get list sources name in database.
        /// </summary>
        /// <returns></returns>
        public List<string> GetListSourcesName()
        {
            List<string> arrayReturn = new List<string>();
            foreach (GameSource source in API.Instance.Database.Sources)
            {
                if (arrayReturn.Find(x => x.IsEqual(source.Name)) == null)
                {
                    _ = arrayReturn.AddMissing(source.Name);
                }
            }

            // Source for game add manually.
            _ = arrayReturn.AddMissing("Playnite");

            Common.LogDebug(true, Serialization.ToJson(arrayReturn));
            return arrayReturn;
        }


        /// <summary>
        /// Get details game activity on selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            ViewModel.ResetGameVariators();

            if (sender != null)
            {
                ListBox item = (ListBox)sender;
                if (((List<ListActivities>)item.ItemsSource)?.Count > 0)
                {
                    ListActivities gameItem = (ListActivities)item.SelectedItem;
                    if (gameItem?.GameId == null) { return; }

                    GameIDCurrent = gameItem.GameId;

                    if (IsGameTime)
                    {
                        GetActivityForGamesTimeGraphics(GameIDCurrent);
                    }
                    else
                    {
                        GetActivityForGamesLogGraphics(GameIDCurrent);
                    }

                    activityForGamesGraphics.Visibility = Visibility.Visible;
                }


                int index = -1;
                if (lvGames.SelectedItem != null)
                {
                    index = ((ListActivities)lvGames.SelectedItem).PCConfigurationId;
                }

                if (index != -1 && index < PluginDatabase.SystemConfigurationManager.GetConfigurations().Count)
                {
                    SystemConfiguration Configuration = PluginDatabase.SystemConfigurationManager.GetConfigurations()[index];

                    PART_PcName.Content = Configuration.Name;
                    PART_Os.Content = Configuration.Os;
                    PART_CpuName.Content = Configuration.Cpu;
                    PART_GpuName.Content = Configuration.GpuName;
                    PART_Ram.Content = Configuration.RamUsage;
                    PART_PcConfigExpander.Tag = string.Format("{0} · {1} · {2}", Configuration.Name, Configuration.Cpu, Configuration.GpuName);
                }
                else
                {
                    PART_PcName.Content = string.Empty;
                    PART_Os.Content = string.Empty;
                    PART_CpuName.Content = string.Empty;
                    PART_GpuName.Content = string.Empty;
                    PART_Ram.Content = string.Empty;
                    PART_PcConfigExpander.Tag = string.Empty;
                }
            }
        }


        #region Butons click event
        private void Button_Click_PrevMonth(object sender, RoutedEventArgs e)
        {
            ViewModel.ChangeMonth(-1);

            // get data
            GetActivityByMonth(YearCurrent, MonthCurrent);
            GetActivityByWeek(YearCurrent, MonthCurrent);
            GetActivityByDay(YearCurrent, MonthCurrent);

            Filter();
        }

        private void Button_Click_NextMonth(object sender, RoutedEventArgs e)
        {
            ViewModel.ChangeMonth(1);

            // get data
            GetActivityByMonth(YearCurrent, MonthCurrent);
            GetActivityByWeek(YearCurrent, MonthCurrent);
            GetActivityByDay(YearCurrent, MonthCurrent);

            Filter();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;

            DateTime dateNew = (DateTime)control.SelectedDate;
            ViewModel.SetMonth(dateNew);

            // get data
            GetActivityByMonth(YearCurrent, MonthCurrent);
            GetActivityByWeek(YearCurrent, MonthCurrent);
            GetActivityByDay(YearCurrent, MonthCurrent);

            Filter();
        }


        private void ToggleButtonTime_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    ViewModel.SetGameChartMode(true);
                    ToggleButtonLog.IsChecked = false;
                    GetActivityForGamesTimeGraphics(GameIDCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (ToggleButtonLog.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }

        private void ToggleButtonLog_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    ViewModel.SetGameChartMode(false);
                    ToggleButtonTime.IsChecked = false;
                    GetActivityForGamesLogGraphics(GameIDCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (ToggleButtonTime.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }


        private void ToggleButtonSources_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    ViewModel.SetMonthSourceMode(true, false);
                    tbMonthGenres.IsChecked = false;
                    tbMonthTags.IsChecked = false;
                    GetActivityByMonth(YearCurrent, MonthCurrent);
                    GetActivityByWeek(YearCurrent, MonthCurrent);
                    GetActivityByDay(YearCurrent, MonthCurrent);

                    Col1.Width = new GridLength(1, GridUnitType.Star);
                    Col2.Width = new GridLength(1, GridUnitType.Star);
                    Col3.Width = new GridLength(1, GridUnitType.Star);
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthGenres.IsChecked == false && tbMonthTags.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }

        private void ToggleButtonGenres_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    ViewModel.SetMonthSourceMode(false, true);
                    tbMonthSources.IsChecked = false;
                    tbMonthTags.IsChecked = false;
                    GetActivityByMonth(YearCurrent, MonthCurrent);

                    Col1.Width = new GridLength(1, GridUnitType.Star);
                    Col2.Width = new GridLength(0);
                    Col3.Width = new GridLength(0);
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthSources.IsChecked == false && tbMonthSources.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }

        private void ToggleButtonTags_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    ViewModel.SetMonthSourceMode(false, false);
                    tbMonthSources.IsChecked = false;
                    tbMonthGenres.IsChecked = false;
                    GetActivityByMonth(YearCurrent, MonthCurrent);

                    Col1.Width = new GridLength(1, GridUnitType.Star);
                    Col2.Width = new GridLength(0);
                    Col3.Width = new GridLength(0);
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthSources.IsChecked == false && tbMonthSources.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }


        private void Bt_Truncate(object sender, RoutedEventArgs e)
        {
            if (IsGameTime)
            {
                PART_GameActivityChartTime.Truncate = (bool)((ToggleButton)sender).IsChecked;
                PART_GameActivityChartTime.AxisVariator = 0;
            }
        }

        private void Button_Click_prevGame(object sender, RoutedEventArgs e)
        {
            if (IsGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Prev();
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = TitleChart;
                PART_GameActivityChartLog.Prev();
            }
        }

        private void Button_Click_nextGame(object sender, RoutedEventArgs e)
        {
            if (IsGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Next();
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = TitleChart;
                PART_GameActivityChartLog.Next();
            }
        }

        private void Button_Click_prevGamePlus(object sender, RoutedEventArgs e)
        {
            if (IsGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Prev(PluginDatabase.PluginSettings.VariatorTime);
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = TitleChart;
                PART_GameActivityChartLog.Prev(PluginDatabase.PluginSettings.VariatorLog);
            }
        }

        private void Button_Click_nextGamePlus(object sender, RoutedEventArgs e)
        {
            if (IsGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Next(PluginDatabase.PluginSettings.VariatorTime);
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = TitleChart;
                PART_GameActivityChartLog.Next(PluginDatabase.PluginSettings.VariatorLog);
            }
        }
        #endregion


        // TODO Show stack time for can select details data
        // TODO Select details data
        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {
            if (PluginDatabase.PluginSettings.EnableLogging)
            {
                int index = (int)chartPoint.X;
                TitleChart = chartPoint.SeriesView.Title;
                IChartValues data = chartPoint.SeriesView.Values;

                LabelDataSelected = Convert.ToDateTime(((CustomerForTime)data[index]).Name);

                ViewModel.SetGameChartMode(false);
                ToggleButtonTime.IsChecked = false;
                ToggleButtonLog.IsChecked = true;

                GetActivityForGamesLogGraphics(GameIDCurrent, LabelDataSelected, TitleChart);
            }
        }


        #region Filter
        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = TextboxSearch.Text;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void Filter()
        {
            Filter(true);
        }

        private void Filter(bool shouldSort)
        {
#if DEBUG
            var timer = new DebugTimer("GameActivityView.Filter");
#endif
            ViewModel.ApplyFilter();
            if (shouldSort)
            {
                lvGames.Sorting();
            }
#if DEBUG
            timer.Stop(string.Format("filteredRows={0}", ViewModel.FilteredActivityList?.Count ?? 0));
#endif
        }


        private void PART_CbSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterSourceItemSelectionChanged(sender);
        }
        private void PART_CbSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterSourceItemSelectionChanged(sender);
        }
        private void FilterSourceItemSelectionChanged(object sender)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox == null || checkBox.Tag == null)
            {
                return;
            }

            ListSource listSource = checkBox.Tag as ListSource;
            if (listSource == null)
            {
                return;
            }

            bool isChecked = checkBox.IsChecked == true;
            ViewModel.ToggleSourceFilter(listSource.SourceNameShort, isChecked);

            Filter();
        }

        private void FilterSourceReset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SearchSources.Clear();
            ViewModel.FilterSourceText = string.Empty;

            for (int i = 0; i < FilterSourceItems.Count; i++)
            {
                FilterSourceItems[i].IsCheck = false;
            }

            Filter();
        }

        private void FilterSourceButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.ContextMenu == null)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }

        private void FilterSourceMenuItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1)
            {
                return;
            }

            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            ListSource listSource = menuItem.Tag as ListSource;
            if (listSource == null)
            {
                return;
            }

            bool newChecked = !listSource.IsCheck;
            listSource.IsCheck = newChecked;
            ViewModel.ToggleSourceFilter(listSource.SourceNameShort, newChecked);
            Filter();
            e.Handled = true;
        }
        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = true,
                ShowCloseButton = true,
                CanBeResizable = true,
                Height = 740,
                Width = 1280
            };

            Button bt = sender as Button;
            Game game = API.Instance.Database.Games.Get((Guid)bt.Tag);
            PluginDatabase.PluginWindows.ShowPluginGameDataWindow(Plugin, game);
        }
    }

    public class ListSource : ObservableObject
    {
        public TextBlockWithIconMode TypeStoreIcon { get; set; }

        public string SourceIcon { get; set; }
        public string SourceIconText { get; set; }
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }

        private bool _isCheck;
        public bool IsCheck
        {
            get => _isCheck;
            set => SetValue(ref _isCheck, value);
        }
    }
}
