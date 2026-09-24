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
using System.Windows.Data;
using System.IO;
using CommonPluginsShared.Converters;

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
        private bool _aggregateTabsSyncing;
        private AggregateSourcesChartView PART_AggregateSourcesCharts;
        private AggregateGamesChartView _gamesCharts;
        private AggregateGenresChartView _genresCharts;
        private AggregateTagsChartView _tagsCharts;

        private List<GameActivities> _periodGameActivitiesCache;
        private readonly object _periodGameActivitiesCacheLock = new object();
        private DateTime _cachedPeriodStart = DateTime.MinValue;
        private DateTime _cachedPeriodEnd = DateTime.MinValue;

        /// <summary>
        /// Returns games that have at least one session in the inclusive local period (cached).
        /// </summary>
        private List<GameActivities> GetPeriodFilteredGameActivities(DateTime periodStart, DateTime periodEnd)
        {
            lock (_periodGameActivitiesCacheLock)
            {
                if (_periodGameActivitiesCache != null
                    && _cachedPeriodStart == periodStart
                    && _cachedPeriodEnd == periodEnd)
                {
                    Common.LogDebug($"PeriodView: period cache HIT games={_periodGameActivitiesCache.Count} {periodStart:yyyy-MM-dd}..{periodEnd:yyyy-MM-dd}");
                    return _periodGameActivitiesCache;
                }

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                listGameActivities = listGameActivities
                    .Where(x => x.GetListDateTimeActivity().Any(y => y >= periodStart && y <= periodEnd))
                    .ToList();

                _periodGameActivitiesCache = listGameActivities;
                _cachedPeriodStart = periodStart;
                _cachedPeriodEnd = periodEnd;

                Common.LogDebug($"PeriodView: period cache MISS games={listGameActivities.Count} {periodStart:yyyy-MM-dd}..{periodEnd:yyyy-MM-dd}");
                return listGameActivities;
            }
        }

        /// <summary>
        /// Keeps the top <paramref name="topN"/> entries by playtime and folds the rest into an "Others" bucket.
        /// </summary>
        private static List<KeyValuePair<string, ulong>> ReduceToTopNWithOthers(
            Dictionary<string, ulong> source,
            int topN,
            string othersLabel)
        {
            List<KeyValuePair<string, ulong>> ordered = source
                .OrderByDescending(x => x.Value)
                .ToList();

            if (ordered.Count <= topN)
            {
                return ordered;
            }

            List<KeyValuePair<string, ulong>> result = new List<KeyValuePair<string, ulong>>(topN + 1);
            for (int i = 0; i < topN; i++)
            {
                result.Add(ordered[i]);
            }

            ulong othersTotal = 0;
            for (int i = topN; i < ordered.Count; i++)
            {
                othersTotal += ordered[i].Value;
            }

            if (othersTotal > 0)
            {
                result.Add(new KeyValuePair<string, ulong>(othersLabel, othersTotal));
            }

            return result;
        }

        /// <summary>
        /// Shows only the ContentControl host for the active <see cref="AggregateKind"/>.
        /// </summary>
        private void SetAggregateHostVisibility()
        {
            AggregateKind kind = ViewModel.AggregateKind;
            SetHostVisibility(PART_AggregateGamesHost, kind == AggregateKind.Games);
            SetHostVisibility(PART_AggregateGenresHost, kind == AggregateKind.Genres);
            SetHostVisibility(PART_AggregateTagsHost, kind == AggregateKind.Tags);
            SetHostVisibility(PART_AggregateSourcesHost, kind == AggregateKind.Sources);
            Common.LogDebug($"PeriodView: AggregateHostVisibility kind={kind}");
        }

        private static void SetHostVisibility(ContentControl host, bool visible)
        {
            if (host != null)
            {
                host.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Ensures the chart host for <paramref name="kind"/> is created and reloaded when stale.
        /// </summary>
        private void EnsureAggregateChartsLoaded(AggregateKind kind)
        {
            SetAggregateHostVisibility();
            if (kind == AggregateKind.Sources)
            {
                EnsureSourcesChartsLoaded();
                return;
            }

            AggregateMonoChartView mono = EnsureMonoChartInstance(kind);
            if (mono == null)
            {
                Common.LogDebug($"PeriodView: EnsureAggregateChartsLoaded kind={kind} mono=null");
                return;
            }

            Common.LogDebug($"PeriodView: EnsureAggregateChartsLoaded kind={kind} needsReload={mono.NeedsReload} loaded={mono.HasBeenLoaded} stale={mono.IsStale}");
            if (mono.NeedsReload)
            {
                GetActivityByMonth(YearCurrent, MonthCurrent);
                mono.MarkLoaded();
            }
        }

        private AggregateMonoChartView EnsureMonoChartInstance(AggregateKind kind)
        {
            switch (kind)
            {
                case AggregateKind.Games:
                    if (_gamesCharts == null && PART_AggregateGamesHost != null)
                    {
                        _gamesCharts = new AggregateGamesChartView();
                        PART_AggregateGamesHost.Content = _gamesCharts;
                        Common.LogDebug("PeriodView: create AggregateGamesChartView");
                    }

                    return _gamesCharts;
                case AggregateKind.Genres:
                    if (_genresCharts == null && PART_AggregateGenresHost != null)
                    {
                        _genresCharts = new AggregateGenresChartView();
                        PART_AggregateGenresHost.Content = _genresCharts;
                        Common.LogDebug("PeriodView: create AggregateGenresChartView");
                    }

                    return _genresCharts;
                case AggregateKind.Tags:
                    if (_tagsCharts == null && PART_AggregateTagsHost != null)
                    {
                        _tagsCharts = new AggregateTagsChartView();
                        PART_AggregateTagsHost.Content = _tagsCharts;
                        Common.LogDebug("PeriodView: create AggregateTagsChartView");
                    }

                    return _tagsCharts;
                default:
                    return null;
            }
        }

        private AggregateMonoChartView GetMonoChart(AggregateKind kind)
        {
            switch (kind)
            {
                case AggregateKind.Games:
                    return _gamesCharts;
                case AggregateKind.Genres:
                    return _genresCharts;
                case AggregateKind.Tags:
                    return _tagsCharts;
                default:
                    return null;
            }
        }

        private void InvalidateInactiveAggregateCharts(AggregateKind activeKind)
        {
            bool invGames = false;
            bool invGenres = false;
            bool invTags = false;
            bool invSources = false;

            if (activeKind != AggregateKind.Games && _gamesCharts != null)
            {
                _gamesCharts.Invalidate();
                invGames = _gamesCharts.IsStale;
            }

            if (activeKind != AggregateKind.Genres && _genresCharts != null)
            {
                _genresCharts.Invalidate();
                invGenres = _genresCharts.IsStale;
            }

            if (activeKind != AggregateKind.Tags && _tagsCharts != null)
            {
                _tagsCharts.Invalidate();
                invTags = _tagsCharts.IsStale;
            }

            if (activeKind != AggregateKind.Sources && PART_AggregateSourcesCharts != null)
            {
                PART_AggregateSourcesCharts.Invalidate();
                invSources = PART_AggregateSourcesCharts.IsStale;
            }

            Common.LogDebug($"PeriodView: InvalidateInactive active={activeKind} games={invGames} genres={invGenres} tags={invTags} sources={invSources}");
        }

        /// <summary>
        /// Marks the active aggregate chart stale (required on period change — inactive-only invalidate would skip reload).
        /// </summary>
        /// <param name="activeKind">Currently selected aggregate mode.</param>
        private void InvalidateActiveAggregateChart(AggregateKind activeKind)
        {
            switch (activeKind)
            {
                case AggregateKind.Games:
                    if (_gamesCharts != null)
                    {
                        _gamesCharts.Invalidate();
                        Common.LogDebug($"PeriodView: InvalidateActive kind=Games stale={_gamesCharts.IsStale}");
                    }
                    break;
                case AggregateKind.Genres:
                    if (_genresCharts != null)
                    {
                        _genresCharts.Invalidate();
                        Common.LogDebug($"PeriodView: InvalidateActive kind=Genres stale={_genresCharts.IsStale}");
                    }
                    break;
                case AggregateKind.Tags:
                    if (_tagsCharts != null)
                    {
                        _tagsCharts.Invalidate();
                        Common.LogDebug($"PeriodView: InvalidateActive kind=Tags stale={_tagsCharts.IsStale}");
                    }
                    break;
                case AggregateKind.Sources:
                    if (PART_AggregateSourcesCharts != null)
                    {
                        PART_AggregateSourcesCharts.Invalidate();
                        Common.LogDebug($"PeriodView: InvalidateActive kind=Sources stale={PART_AggregateSourcesCharts.IsStale}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Ensures Sources charts are visible and reloads them when first shown or stale.
        /// </summary>
        private void EnsureSourcesChartsLoaded()
        {
            if (PART_AggregateSourcesHost == null)
            {
                Common.LogDebug("PeriodView: EnsureSourcesChartsLoaded host=null");
                return;
            }

            bool created = false;
            if (PART_AggregateSourcesCharts == null)
            {
                PART_AggregateSourcesCharts = new AggregateSourcesChartView();
                PART_AggregateSourcesHost.Content = PART_AggregateSourcesCharts;
                PART_AggregateSourcesCharts.ConfigureDefaultTooltips(ShowIcon, ModeComplet);
                created = true;
                Common.LogDebug("PeriodView: create AggregateSourcesChartView");
            }

            PART_AggregateSourcesHost.Visibility = Visibility.Visible;
            bool needsReload = PART_AggregateSourcesCharts.NeedsReload;
            Common.LogDebug($"PeriodView: EnsureSourcesChartsLoaded created={created} needsReload={needsReload} loaded={PART_AggregateSourcesCharts.HasBeenLoaded} stale={PART_AggregateSourcesCharts.IsStale}");
            if (needsReload)
            {
                GetActivityByMonth(YearCurrent, MonthCurrent);
                GetActivityByWeek(YearCurrent, MonthCurrent);
                GetActivityByDay(YearCurrent, MonthCurrent);
                PART_AggregateSourcesCharts.MarkLoaded();
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
        public Guid? GameIDCurrent { get => ViewModel.GameIDCurrent; set => ViewModel.GameIDCurrent = value; }
        public int VariateurTime { get => ViewModel.VariateurTime; set => ViewModel.VariateurTime = value; }
        public int VariateurLog { get => ViewModel.VariateurLog; set => ViewModel.VariateurLog = value; }
        public int VariateurLogTemp { get => ViewModel.VariateurLogTemp; set => ViewModel.VariateurLogTemp = value; }
        public string TitleChart { get => ViewModel.TitleChart; set => ViewModel.TitleChart = value; }

        private List<ListSource> FilterSourceItems => ViewModel.FilterSourceItems;
        private List<string> SearchSources => ViewModel.SearchSources;
        public List<ListActivities> ActivityListByGame { get => ViewModel.ActivityListByGame; set => ViewModel.ActivityListByGame = value; }

        public AggregateKind AggregateKind
        {
            get => ViewModel.AggregateKind;
            set => ViewModel.AggregateKind = value;
        }
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
            Loaded += GameActivityView_Loaded;

#if DEBUG
            _ctorDebugTimer.Step("InitializeComponent done");
#endif

            InitializePeriodPresetCombo();
            SyncAggregateModeTabs();
            EnsureAggregateChartsLoaded(ViewModel.AggregateKind);

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

        private void GameActivityView_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = CommonPluginsShared.UI.UIHelper.FindParent<ScrollViewer>(this);
            if (scrollViewer != null)
            {
                var binding = new Binding("ActualHeight")
                {
                    Source = scrollViewer,
                    FallbackValue = 740d
                };
                this.SetBinding(FrameworkElement.HeightProperty, binding);
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

            lvSessions.EnableColumnPersistence = PluginDatabase.PluginSettings.SaveColumnOrder;
            lvSessions.ColumnConfigurationFilePath = System.IO.Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "ListViewColumns.json");
            lvSessions.ColumnConfigurationScope = CommonPluginsShared.Controls.ColumnConfigurationScope.Custom;
            lvSessions.ColumnConfigurationKey = "GameActivityView.lvSessions";

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
                        // Mono/Sources hosts manage their own visibility.

                        Grid.SetColumn(PART_AggregateSourcesCharts.DayGrid, 0);
                        Grid.SetColumnSpan(PART_AggregateSourcesCharts.DayGrid, 3);
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


            DataContext = ViewModel;
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            Filter();
        }

        /// <summary>
        /// Refreshes sidebar data when the existing view instance becomes visible again.
        /// Keeps current month and active filters while reloading charts and game rows.
        /// </summary>
        public void RefreshOnReopen()
        {
            if (!IsLoaded)
            {
                RoutedEventHandler onLoaded = null;
                onLoaded = (s, e) =>
                {
                    Loaded -= onLoaded;
                    RefreshOnReopen();
                };
                Loaded += onLoaded;
                return;
            }

            Guid? selectedGameId = (lvGames.SelectedItem as ListActivities)?.Id;

            GetActivityByMonth(YearCurrent, MonthCurrent);
            GetActivityByWeek(YearCurrent, MonthCurrent);
            GetActivityByDay(YearCurrent, MonthCurrent);
            GetActivityByListGame();

            if (selectedGameId != null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)delegate
                {
                    ListActivities matched = ViewModel.FilteredActivityList?.FirstOrDefault(x => x.Id == selectedGameId.Value);
                    if (matched == null)
                    {
                        return;
                    }

                    lvGames.SelectedItem = matched;
                    lvGames.ScrollIntoView(matched);
                });
            }
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

        /// <summary>Max distinct game bars on the period chart before collapsing into "Others".</summary>
        private const int GamesChartTopCount = 15;

        /// <summary>Max characters for Games / Genres / Tags names on the aggregate chart X axis (full name stays in tooltip).</summary>
        private const int AggregateChartAxisLabelMaxChars = 14;

        /// <summary>Target max visible labels on dense chart X axes (Sources day/week, Tags/Genres mono) before Separator.Step.</summary>
        private const int SourcesTimeAxisMaxLabels = 14;

        /// <summary>Above this day count, Sources day chart uses LineSeries (ColumnSeries bars become invisible).</summary>
        private const int DayChartColumnMaxPoints = 62;

        /// <summary>CommonFont glyph fallback when DefaultGameIcon is unavailable.</summary>
        private const string DefaultGameIconGlyph = "\ue90f";

        private static readonly DefaultIconConverter DefaultGameIconConverterInstance = new DefaultIconConverter();

        /// <summary>
        /// Truncates a chart axis label with an ellipsis when longer than <paramref name="maxChars"/>.
        /// </summary>
        private static string TruncateChartAxisLabel(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars < 2 || text.Length <= maxChars)
            {
                return text;
            }

            return text.Substring(0, maxChars - 1) + "…";
        }

        /// <summary>
        /// Computes LiveCharts X-axis <c>Separator.Step</c> so at most ~<paramref name="maxLabels"/> labels stay readable.
        /// </summary>
        /// <param name="pointCount">Number of axis categories (days or weeks).</param>
        /// <param name="maxLabels">Target maximum visible labels.</param>
        /// <returns>Step &gt;= 1.</returns>
        private static int GetChartAxisLabelStep(int pointCount, int maxLabels)
        {
            if (pointCount <= 0 || maxLabels <= 0 || pointCount <= maxLabels)
            {
                return 1;
            }

            int step = (int)Math.Ceiling(pointCount / (double)maxLabels);
            return step < 1 ? 1 : step;
        }

        /// <summary>
        /// Resolves a game icon for chart tooltips: existing file path, else theme <c>DefaultGameIcon</c>, else glyph.
        /// </summary>
        private static void ApplyGameChartIcon(CustomerForTime point, string iconPathOrRelative)
        {
            string filePath = string.Empty;
            if (!iconPathOrRelative.IsNullOrEmpty())
            {
                if (File.Exists(iconPathOrRelative))
                {
                    filePath = iconPathOrRelative;
                }
                else
                {
                    string fullPath = API.Instance.Database.GetFullFilePath(iconPathOrRelative);
                    if (!fullPath.IsNullOrEmpty() && File.Exists(fullPath))
                    {
                        filePath = fullPath;
                    }
                }
            }

            if (!filePath.IsNullOrEmpty())
            {
                point.Icon = filePath;
                point.IconText = string.Empty;
                return;
            }

            object defaultIcon = DefaultGameIconConverterInstance.Convert(null, typeof(object), null, CultureInfo.CurrentCulture);
            if (defaultIcon != null)
            {
                point.Icon = defaultIcon;
                point.IconText = string.Empty;
                return;
            }

            point.Icon = null;
            point.IconText = DefaultGameIconGlyph;
        }

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

            AggregateKind kindSnapshot = ViewModel.AggregateKind;
            bool cumulSnapshot = PluginDatabase.PluginSettings.CumulPlaytimeStore;
            bool showLauncherIcons = PluginDatabase.PluginSettings.ShowLauncherIcons;

            double labelsRotation;
            double fontSize;
            TextBlockWithIconMode tooltipMode;
            bool showTooltipIcon;
            bool showTooltipLabel;
            switch (kindSnapshot)
            {
                case AggregateKind.Sources:
                    labelsRotation = showLauncherIcons ? 0 : 160;
                    fontSize = showLauncherIcons ? 30 : (double)ResourceProvider.GetResource("FontSize");
                    tooltipMode = TextBlockWithIconMode.TextOnly;
                    showTooltipIcon = false;
                    showTooltipLabel = true;
                    break;
                case AggregateKind.Tags:
                    labelsRotation = 30;
                    fontSize = 11;
                    tooltipMode = TextBlockWithIconMode.TextOnly;
                    showTooltipIcon = false;
                    showTooltipLabel = true;
                    break;
                case AggregateKind.Genres:
                    labelsRotation = 30;
                    fontSize = (double)ResourceProvider.GetResource("FontSize");
                    tooltipMode = TextBlockWithIconMode.TextOnly;
                    showTooltipIcon = false;
                    showTooltipLabel = true;
                    break;
                default:
                    // Games — same slant as Sources day chart LabelsRotation in AggregateSourcesChartView.
                    labelsRotation = 30;
                    fontSize = (double)ResourceProvider.GetResource("FontSize");
                    tooltipMode = TextBlockWithIconMode.IconFirstWithText;
                    showTooltipIcon = true;
                    showTooltipLabel = true;
                    break;
            }

            string othersLabel = ResourceProvider.GetString("LOCGameActivityChartOthers");

            DateTime periodStartSnapshot = ViewModel.PeriodStart;
            DateTime periodEndSnapshot = ViewModel.PeriodEnd;

            Common.LogDebug($"PeriodView: StartReloadMonthChart mode={kindSnapshot} period={periodStartSnapshot:yyyy-MM-dd}..{periodEndSnapshot:yyyy-MM-dd} v={myVersion}");

            if (kindSnapshot == AggregateKind.Sources)
            {
                if (PART_AggregateSourcesCharts == null && PART_AggregateSourcesHost != null)
                {
                    PART_AggregateSourcesCharts = new AggregateSourcesChartView();
                    PART_AggregateSourcesHost.Content = PART_AggregateSourcesCharts;
                    PART_AggregateSourcesCharts.ConfigureDefaultTooltips(ShowIcon, ModeComplet);
                }
            }
            else
            {
                EnsureMonoChartInstance(kindSnapshot);
            }

            _ = Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
#if DEBUG
                DebugTimer computeTimer = new DebugTimer(string.Format("GameActivityView.MonthChart compute async ({0:d}-{1:d})", periodStartSnapshot, periodEndSnapshot));
                computeTimer.Step("start");
#endif
                DateTime startOfPeriod = periodStartSnapshot;
                DateTime endOfPeriod = periodEndSnapshot;

                Dictionary<string, ulong> activityByMonth = new Dictionary<string, ulong>();
                Dictionary<string, string> gameIconByName = new Dictionary<string, string>();
                List<GameActivities> listGameActivities = GetPeriodFilteredGameActivities(startOfPeriod, endOfPeriod);

                // Total hours by game / source / genre / tag.
                if (kindSnapshot == AggregateKind.Games)
                {
                    for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return null;
                        }

                        try
                        {
                            string gameName = listGameActivities[iGame].Name;
                            if (gameName.IsNullOrEmpty())
                            {
                                continue;
                            }

                            ulong gameTotal = 0;
                            List<Activity> activities = listGameActivities[iGame].FilterItems;
                            for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return null;
                                }

                                DateTime dateSession = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime();
                                if (dateSession < startOfPeriod || dateSession > endOfPeriod)
                                {
                                    continue;
                                }

                                gameTotal += activities[iActivity].ElapsedSeconds;
                            }

                            if (gameTotal == 0)
                            {
                                continue;
                            }

                            if (activityByMonth.ContainsKey(gameName))
                            {
                                activityByMonth[gameName] = activityByMonth[gameName] + gameTotal;
                            }
                            else
                            {
                                activityByMonth.Add(gameName, gameTotal);
                            }

                            if (!gameIconByName.ContainsKey(gameName))
                            {
                                gameIconByName[gameName] = listGameActivities[iGame].Icon ?? string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error in month chart games compute with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                        }
                    }
                }
                else if (kindSnapshot == AggregateKind.Sources)
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
                                if (dateSession < startOfPeriod || dateSession > endOfPeriod)
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
                else if (kindSnapshot == AggregateKind.Genres)
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
                                if (dateSession < startOfPeriod || dateSession > endOfPeriod)
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
                                if (dateSession < startOfPeriod || dateSession > endOfPeriod)
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
                }

                // Build plain data only (no LiveCharts/WPF types here).
                List<KeyValuePair<string, ulong>> chartPairs = kindSnapshot == AggregateKind.Games
                    ? ReduceToTopNWithOthers(activityByMonth, GamesChartTopCount, othersLabel)
                    : activityByMonth.ToList();

                Common.LogDebug($"PeriodView: MonthChart series={chartPairs.Count} rawKeys={activityByMonth.Count} mode={kindSnapshot}");

                List<CustomerForTime> items = new List<CustomerForTime>(chartPairs.Count);
                string[] labels = new string[chartPairs.Count];
                int compteur = 0;
                bool truncateAxisLabels = kindSnapshot != AggregateKind.Sources;
                for (int i = 0; i < chartPairs.Count; i++)
                {
                    KeyValuePair<string, ulong> item = chartPairs[i];
                    string fullName = item.Key;
                    CustomerForTime point = new CustomerForTime
                    {
                        Name = fullName,
                        Values = (long)item.Value,
                    };

                    if (kindSnapshot == AggregateKind.Games)
                    {
                        string rawIcon = gameIconByName.ContainsKey(fullName) ? gameIconByName[fullName] : string.Empty;
                        ApplyGameChartIcon(point, rawIcon);
                    }
                    else if (kindSnapshot == AggregateKind.Sources)
                    {
                        point.Icon = PlayniteTools.GetPlatformIcon(fullName);
                        point.IconText = TransformIcon.Get(fullName);
                    }
                    else
                    {
                        point.Icon = null;
                        point.IconText = string.Empty;
                    }

                    items.Add(point);

                    if (truncateAxisLabels)
                    {
                        labels[compteur] = TruncateChartAxisLabel(fullName, AggregateChartAxisLabelMaxChars);
                    }
                    else if (showLauncherIcons && kindSnapshot == AggregateKind.Sources)
                    {
                        labels[compteur] = TransformIcon.Get(fullName);
                    }
                    else
                    {
                        labels[compteur] = fullName;
                    }

                    compteur++;
                }

                bool isSources = kindSnapshot == AggregateKind.Sources;
                bool showTotalHoursChart = !(isSources && cumulSnapshot);
                bool showTotalHoursLabel = showTotalHoursChart;
                // Cumul Sources: hide total hours card and expand day chart columns.
                bool adjustGridDay = isSources && cumulSnapshot;

#if DEBUG
                computeTimer.Stop(string.Format("labels={0}, items={1}", labels.Length, chartPairs.Count));
#endif
                return new
                {
                    myVersion,
                    items,
                    labels,
                    labelsRotation,
                    fontSize,
                    tooltipMode,
                    showTooltipIcon,
                    showTooltipLabel,
                    showTotalHoursChart,
                    showTotalHoursLabel,
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

                    bool useSourcesCharts = kindSnapshot == AggregateKind.Sources && PART_AggregateSourcesCharts != null;
                    AggregateMonoChartView monoCharts = useSourcesCharts
                        ? null
                        : GetMonoChart(kindSnapshot);
                    if (!useSourcesCharts && monoCharts == null)
                    {
                        return;
                    }

                    CartesianChart totalChart = useSourcesCharts ? PART_AggregateSourcesCharts.ChartTotal : monoCharts.Chart;
                    Axis totalAxisX = useSourcesCharts ? PART_AggregateSourcesCharts.ChartTotalX : monoCharts.ChartX;
                    Axis totalAxisY = useSourcesCharts ? PART_AggregateSourcesCharts.ChartTotalY : monoCharts.ChartY;
                    TextBlock totalLabel = useSourcesCharts ? PART_AggregateSourcesCharts.ChartTotalLabel : monoCharts.ChartLabel;
                    Border totalCard = useSourcesCharts ? PART_AggregateSourcesCharts.TotalHoursCard : monoCharts.TotalHoursCard;
                    Grid totalGrid = useSourcesCharts ? PART_AggregateSourcesCharts.TotalHoursGrid : monoCharts.ChartGrid;

                    totalAxisX.LabelsRotation = result.labelsRotation;
                    totalAxisX.FontSize = result.fontSize;

                    if (result.adjustGridDay && useSourcesCharts)
                    {
                        totalChart.Visibility = Visibility.Hidden;
                        totalLabel.Visibility = Visibility.Hidden;
                        totalCard.Visibility = Visibility.Collapsed;
                        Grid.SetColumn(PART_AggregateSourcesCharts.DayGrid, 0);
                        Grid.SetColumnSpan(PART_AggregateSourcesCharts.DayGrid, 3);
                    }
                    else
                    {
                        totalChart.Visibility = result.showTotalHoursChart ? Visibility.Visible : Visibility.Hidden;
                        totalLabel.Visibility = result.showTotalHoursLabel ? Visibility.Visible : Visibility.Hidden;
                        totalCard.Visibility = result.showTotalHoursChart ? Visibility.Visible : Visibility.Collapsed;
                    }

                    Grid.SetColumnSpan(totalGrid, useSourcesCharts ? 1 : 5);

                    if (useSourcesCharts)
                    {
                        PART_AggregateSourcesCharts.ChartByDay.Visibility = Visibility.Visible;
                        PART_AggregateSourcesCharts.DayLabel.Visibility = Visibility.Visible;
                        PART_AggregateSourcesCharts.HoursByDayCard.Visibility = Visibility.Visible;
                        PART_AggregateSourcesCharts.ChartByWeek.Visibility = Visibility.Visible;
                        PART_AggregateSourcesCharts.WeekLabel.Visibility = Visibility.Visible;
                        PART_AggregateSourcesCharts.HoursByWeekCard.Visibility = Visibility.Visible;
                    }

                    totalAxisY.LabelFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);
                    totalAxisY.MinValue = 0;

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

                    totalChart.Series = chartSeries;

                    if (useSourcesCharts)
                    {
                        PART_AggregateSourcesCharts.BindTotalTooltip();
                    }
                    else
                    {
                        monoCharts.BindChartTooltip();
                    }

                    totalAxisX.Labels = result.labels;
                    totalAxisX.ShowLabels = true;
                    int axisStep = GetChartAxisLabelStep(result.labels?.Length ?? 0, SourcesTimeAxisMaxLabels);
                    totalAxisX.Separator = new LiveCharts.Wpf.Separator { Step = axisStep, IsEnabled = true };

                    Common.LogDebug($"PeriodView: MonthChart UI bind mode={kindSnapshot} host={(useSourcesCharts ? "Sources" : "Mono")} tooltip icon={result.showTooltipIcon} label={result.showTooltipLabel} mode={result.tooltipMode} series={result.items.Count} step={axisStep} adjustGridDay={result.adjustGridDay}");

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
        }

        private void StartReloadDayChart()
        {
            if (ViewModel.AggregateKind != AggregateKind.Sources || PART_AggregateSourcesCharts == null)
            {
                Common.LogDebug($"PeriodView: StartReloadDayChart skip kind={ViewModel.AggregateKind} sourcesNull={PART_AggregateSourcesCharts == null}");
                return;
            }

            int myVersion = Interlocked.Increment(ref _dayChartReloadVersion);
            CancellationTokenSource previousCts = Interlocked.Exchange(ref _dayChartReloadCts, new CancellationTokenSource());
            if (previousCts != null)
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }
            CancellationToken token = _dayChartReloadCts.Token;

            DateTime periodStartSnapshot = ViewModel.PeriodStart.Date;
            DateTime periodEndSnapshot = ViewModel.PeriodEnd;
            DateTime periodEndDay = periodEndSnapshot.Date;
            if (periodEndDay < periodStartSnapshot)
            {
                periodEndDay = periodStartSnapshot;
            }

            int dayCount = (periodEndDay - periodStartSnapshot).Days + 1;
            Common.LogDebug($"PeriodView: StartReloadDayChart period={periodStartSnapshot:yyyy-MM-dd}..{periodEndDay:yyyy-MM-dd} days={dayCount} v={myVersion}");

#if DEBUG
            DebugTimer timer = new DebugTimer(string.Format("GameActivityView.GetActivityByDay({0:d}-{1:d})", periodStartSnapshot, periodEndDay));
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
                DateTime startDate = periodStartSnapshot;
                DateTime endDate = new DateTime(periodEndDay.Year, periodEndDay.Month, periodEndDay.Day, 23, 59, 59);

                string[] activityByDateLabels = new string[dayCount];
                long[] dayValues = new long[dayCount];

                for (int iDay = 0; iDay < dayCount; iDay++)
                {
                    activityByDateLabels[iDay] = startDate.AddDays(iDay).ToString(Constants.DateUiFormat);
                }

                List<GameActivities> listGameActivities = GetPeriodFilteredGameActivities(startDate, endDate);
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

                        int dayIndex = (sessionDate.Date - startDate).Days;
                        if (dayIndex >= 0 && dayIndex < dayValues.Length)
                        {
                            dayValues[dayIndex] = dayValues[dayIndex] + (long)elapsedSeconds;
                        }
                    }
                }

                int nonZeroDays = 0;
                long totalSeconds = 0;
                for (int i = 0; i < dayValues.Length; i++)
                {
                    if (dayValues[i] > 0)
                    {
                        nonZeroDays++;
                        totalSeconds += dayValues[i];
                    }
                }

                return new DayChartData
                {
                    Labels = activityByDateLabels,
                    Values = dayValues,
                    NonZeroDays = nonZeroDays,
                    TotalSeconds = totalSeconds
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

                    bool useLineSeries = data.Values.Length > DayChartColumnMaxPoints;
                    SeriesCollection activityByDaySeries;
                    if (useLineSeries)
                    {
                        // Dense periods (year / multi-month): columns become sub-pixel and look empty.
                        activityByDaySeries = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Title = string.Empty,
                                Values = series,
                                Stroke = PluginDatabase.PluginSettings.ChartColors,
                                Fill = Brushes.Transparent,
                                StrokeThickness = 2,
                                PointGeometrySize = 0,
                                LineSmoothness = 0
                            }
                        };
                    }
                    else
                    {
                        activityByDaySeries = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = string.Empty,
                                Values = series,
                                Fill = PluginDatabase.PluginSettings.ChartColors
                            }
                        };
                    }

                    Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                    PART_AggregateSourcesCharts.ChartByDayY.LabelFormatter = activityForGameLogFormatter;
                    PART_AggregateSourcesCharts.BindDayTooltip();
                    PART_AggregateSourcesCharts.ChartByDay.Series = activityByDaySeries;
                    PART_AggregateSourcesCharts.ChartByDayY.MinValue = 0;
                    PART_AggregateSourcesCharts.ChartByDayX.Labels = data.Labels;
                    int dayStep = GetChartAxisLabelStep(data.Labels?.Length ?? 0, SourcesTimeAxisMaxLabels);
                    PART_AggregateSourcesCharts.ChartByDayX.Separator = new LiveCharts.Wpf.Separator { Step = dayStep, IsEnabled = true };
                    Common.LogDebug($"PeriodView: DayChart UI bind days={data.Labels?.Length ?? 0} nonZero={data.NonZeroDays} totalSec={data.TotalSeconds} series={(useLineSeries ? "Line" : "Column")} step={dayStep}");
                });

#if DEBUG
                timer.Stop(string.Format("days={0}, seriesPoints={1}", data.Labels?.Length ?? 0, data.Values?.Length ?? 0));
#endif
            });
        }

        /// <summary>
        /// Reloads the Sources hours-by-day chart for the active period (<see cref="GameActivityViewModel.PeriodStart"/> / <see cref="GameActivityViewModel.PeriodEnd"/>).
        /// </summary>
        /// <param name="year">Unused — kept for call-site compatibility.</param>
        /// <param name="month">Unused — kept for call-site compatibility.</param>
        public void GetActivityByDay(int year, int month)
        {
            StartReloadDayChart();
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
            public int NonZeroDays { get; set; }
            public long TotalSeconds { get; set; }
        }

        private void StartReloadWeekChart()
        {
            if (ViewModel.AggregateKind != AggregateKind.Sources || PART_AggregateSourcesCharts == null)
            {
                Common.LogDebug($"PeriodView: StartReloadWeekChart skip kind={ViewModel.AggregateKind} sourcesNull={PART_AggregateSourcesCharts == null}");
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

            DateTime periodStartSnapshot = ViewModel.PeriodStart.Date;
            DateTime periodEndSnapshot = ViewModel.PeriodEnd;
            bool useCumul = PluginDatabase.PluginSettings.CumulPlaytimeStore;
            bool showLauncherIcons = PluginDatabase.PluginSettings.ShowLauncherIcons;
            Common.LogDebug($"PeriodView: StartReloadWeekChart period={periodStartSnapshot:yyyy-MM-dd}..{periodEndSnapshot:yyyy-MM-dd} v={myVersion} cumul={useCumul}");

#if DEBUG
            DebugTimer computeTimer = new DebugTimer(string.Format("GameActivityView.WeekChart compute async ({0:d}-{1:d})", periodStartSnapshot, periodEndSnapshot));
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

                DateTime StartDate = periodStartSnapshot;
                DateTime SeriesEndDate = periodEndSnapshot;
                if (SeriesEndDate < StartDate)
                {
                    SeriesEndDate = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 23, 59, 59);
                }

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

                // create week periods covering the active period
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

                if (datesPeriodes.Count == 0)
                {
                    datesPeriodes.Add(new WeekStartEnd
                    {
                        Monday = firstMonday,
                        Sunday = firstMonday.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59)
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

                List<GameActivities> listGameActivities = GetPeriodFilteredGameActivities(StartDate, SeriesEndDate);

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
                        if (dateSession < StartDate || dateSession > SeriesEndDate)
                        {
                            continue;
                        }

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
                    DebugTimer uiTimer = new DebugTimer(string.Format("GameActivityView.WeekChart UI ({0:d}-{1:d})", periodStartSnapshot, periodEndSnapshot));
                    uiTimer.Step("start");
#endif
                    EnsureCustomerTimeMapper();

                    Func<double, string> activityForGameLogFormatter = value => (string)Converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);
                    PART_AggregateSourcesCharts.ChartByWeekY.LabelFormatter = activityForGameLogFormatter;
                    PART_AggregateSourcesCharts.ChartByWeekY.MinValue = 0;
                    PART_AggregateSourcesCharts.ChartByWeekX.Labels = data.WeekLabels;
                    int weekStep = GetChartAxisLabelStep(data.WeekLabels?.Length ?? 0, SourcesTimeAxisMaxLabels);
                    PART_AggregateSourcesCharts.ChartByWeekX.Separator = new LiveCharts.Wpf.Separator { Step = weekStep, IsEnabled = true };

                    if (data.UseCumulPlaytimeStore)
                    {
                        ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
                        for (int w = 0; w < data.WeekTotals.Length; w++)
                        {
                            series.Add(new CustomerForTime { Name = data.WeekLabels[w], Values = data.WeekTotals[w] });
                        }

                        // Cumul week series under Sources: playtime only (week range stays in title when enabled).
                        PART_AggregateSourcesCharts.BindWeekCumulTooltip(data.DatesPeriodes);

                        SeriesCollection activityByWeekSeries = new SeriesCollection();
                        activityByWeekSeries.Add(new ColumnSeries
                        {
                            Title = string.Empty,
                            Values = series,
                            Fill = PluginDatabase.PluginSettings.ChartColors
                        });

                        PART_AggregateSourcesCharts.ChartByWeek.Series = activityByWeekSeries;
                    }
                    else
                    {
                        if (PluginDatabase.PluginSettings.StoreColors == null
                            || PluginDatabase.PluginSettings.StoreColors.Count == 0)
                        {
                            PluginDatabase.PluginSettings.StoreColors = GameActivitySettingsViewModel.GetDefaultStoreColors();
                        }

                        PART_AggregateSourcesCharts.BindWeekSourcesTooltip(ShowIcon, ModeComplet, data.DatesPeriodes);

                        SeriesCollection activityByWeekSeries = new SeriesCollection();
                        for (int iSource = 0; iSource < data.SourceNames.Count; iSource++)
                        {
                            string sourceName = data.SourceNames[iSource];
                            string sourceLabel = data.SourceLabels[iSource];
                            long[] valuesArr = data.ValuesBySource[iSource];

                            ChartValues<CustomerForTime> values = new ChartValues<CustomerForTime>();
                            for (int w = 0; w < data.DatesPeriodes.Count; w++)
                            {
                                values.Add(new CustomerForTime
                                {
                                    Name = sourceName,
                                    Values = (int)valuesArr[w],
                                    Icon = PlayniteTools.GetPlatformIcon(sourceName),
                                    IconText = TransformIcon.Get(sourceName)
                                });
                            }

                            Brush fill = PluginDatabase.PluginSettings.StoreColors
                                ?.Where(x => x != null
                                    && !string.IsNullOrEmpty(x.Name)
                                    && x.Name.Contains(sourceName, StringComparison.InvariantCultureIgnoreCase))
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

                        PART_AggregateSourcesCharts.ChartByWeek.Series = activityByWeekSeries;
                    }

                    Common.LogDebug($"PeriodView: WeekChart UI bind weeks={data.WeekLabels?.Length ?? 0} sources={data.SourceNames?.Count ?? 0} step={weekStep}");

#if DEBUG
                    uiTimer.Stop();
#endif
                });
            });
        }


        /// <summary>
        /// Reloads the Sources hours-by-week chart for the active period.
        /// </summary>
        /// <param name="year">Unused — kept for call-site compatibility.</param>
        /// <param name="month">Unused — kept for call-site compatibility.</param>
        public void GetActivityByWeek(int year, int month)
        {
            StartReloadWeekChart();
        }


        /// <summary>
        /// Builds the games list for the active period (one row per game with sessions in range).
        /// </summary>
        public void GetActivityByListGame()
        {
#if DEBUG
            var timer = new DebugTimer("GameActivityView.GetActivityByListGame");
#endif
            ActivityListByGame = new List<ListActivities>();
            DateTime periodStart = ViewModel.PeriodStart;
            DateTime periodEnd = ViewModel.PeriodEnd;
            Common.LogDebug($"PeriodView: GetActivityByListGame start {periodStart:yyyy-MM-dd}..{periodEnd:yyyy-MM-dd}");

            List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
            listGameActivities = listGameActivities.Where(x => x.FilterItems.Count > 0 && !x.IsDeleted).ToList();

            string gameID = string.Empty;
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                try
                {
                    gameID = listGameActivities[iGame].Id.ToString();
                    if (listGameActivities[iGame].Name.IsNullOrEmpty())
                    {
                        Logger.Warn($"Failed to load GameActivities from {gameID} because the game is deleted");
                        continue;
                    }

                    List<Activity> periodActivities = listGameActivities[iGame].GetActivities(periodStart, periodEnd);
                    if (periodActivities == null || periodActivities.Count == 0)
                    {
                        continue;
                    }

                    string gameTitle = listGameActivities[iGame].Name;
                    Activity lastSessionActivity = periodActivities[periodActivities.Count - 1];
                    string sourceName = string.Empty;
                    try
                    {
                        sourceName = lastSessionActivity.SourceName;
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, "Error to get SourceName", true, PluginDatabase.PluginName);
                    }

                    ulong timePlayedInPeriod = 0;
                    for (int iActivity = 0; iActivity < periodActivities.Count; iActivity++)
                    {
                        timePlayedInPeriod += periodActivities[iActivity].ElapsedSeconds;
                    }

                    ulong elapsedSeconds = lastSessionActivity.ElapsedSeconds;
                    DateTime dateSession = Convert.ToDateTime(lastSessionActivity.DateSession).ToLocalTime();

                    ListActivities row = BuildListActivitiesRow(
                        listGameActivities[iGame],
                        lastSessionActivity,
                        gameID,
                        gameTitle,
                        sourceName,
                        dateSession,
                        elapsedSeconds,
                        timePlayedInPeriod);
                    ActivityListByGame.Add(row);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load GameActivities from {gameID}", true, PluginDatabase.PluginName);
                }
            }

            Common.LogDebug($"PeriodView: GetActivityByListGame built={ActivityListByGame.Count}");

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                Guid? keepGameId = GameIDCurrent;
                ViewModel.PeriodSessionList = new List<ListActivities>();
                ViewModel.ActivityListByGame = ActivityListByGame;
                Filter(false);

                if (keepGameId == null || keepGameId == Guid.Empty)
                {
                    return;
                }

                ListActivities matched = null;
                List<ListActivities> filtered = ViewModel.FilteredActivityList;
                if (filtered != null)
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        if (filtered[i].Id == keepGameId.Value)
                        {
                            matched = filtered[i];
                            break;
                        }
                    }
                }

                if (matched != null)
                {
                    lvGames.SelectedItem = matched;
                    lvGames.ScrollIntoView(matched);
                }
                else
                {
                    GameIDCurrent = null;
                    activityForGamesGraphics.Visibility = Visibility.Hidden;
                }
            });
#if DEBUG
            timer.Stop(string.Format("rows={0}", ActivityListByGame?.Count ?? 0));
#endif
        }

        /// <summary>
        /// Loads sessions of <paramref name="gameId"/> that fall inside the active period.
        /// </summary>
        private void LoadPeriodSessionsForGame(Guid gameId)
        {
            List<ListActivities> sessions = new List<ListActivities>();
            GameActivities gameActivities = GameActivity.PluginDatabase.Get(gameId);
            if (gameActivities == null)
            {
                Common.LogDebug($"PeriodView: LoadPeriodSessions game={gameId} missing");
                ViewModel.PeriodSessionList = sessions;
                return;
            }

            List<Activity> periodActivities = gameActivities.GetActivities(ViewModel.PeriodStart, ViewModel.PeriodEnd);
            if (periodActivities == null)
            {
                Common.LogDebug($"PeriodView: LoadPeriodSessions game={gameActivities.Name} sessions=0 (null)");
                ViewModel.PeriodSessionList = sessions;
                return;
            }

            string gameIdString = gameId.ToString();
            for (int i = 0; i < periodActivities.Count; i++)
            {
                try
                {
                    Activity activity = periodActivities[i];
                    DateTime dateSession = Convert.ToDateTime(activity.DateSession).ToLocalTime();
                    string sourceName = activity.SourceName ?? string.Empty;
                    sessions.Add(BuildListActivitiesRow(
                        gameActivities,
                        activity,
                        gameIdString,
                        gameActivities.Name,
                        sourceName,
                        dateSession,
                        activity.ElapsedSeconds,
                        activity.ElapsedSeconds));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to build period session row #{i} for {gameActivities.Name}", true, PluginDatabase.PluginName);
                }
            }

            ViewModel.PeriodSessionList = sessions;
            Common.LogDebug($"PeriodView: LoadPeriodSessions game={gameActivities.Name} sessions={sessions.Count} period={ViewModel.PeriodStart:yyyy-MM-dd}..{ViewModel.PeriodEnd:yyyy-MM-dd}");
        }

        /// <summary>
        /// Builds a <see cref="ListActivities"/> row from a session (and optional period total).
        /// </summary>
        private ListActivities BuildListActivitiesRow(
            GameActivities gameActivities,
            Activity sessionActivity,
            string gameId,
            string gameTitle,
            string sourceName,
            DateTime dateSession,
            ulong elapsedSeconds,
            ulong timePlayedInPeriod)
        {
            List<ActivityDetailsData> details = sessionActivity.Details;
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

            SystemConfiguration config = sessionActivity.Configuration;
            string gameIcon = gameActivities.Icon;
            if (!gameIcon.IsNullOrEmpty())
            {
                gameIcon = API.Instance.Database.GetFullFilePath(gameIcon);
            }

            return new ListActivities
            {
                Id = gameActivities.Id,
                GameId = gameId,
                GameTitle = gameTitle,
                GameIcon = gameIcon,
                GameLastActivity = dateSession,
                GameElapsedSeconds = elapsedSeconds,
                TimePlayedInPeriod = timePlayedInPeriod,
                GameSourceName = sourceName,
                GameSourceIcon = TransformIcon.Get(sourceName),
                DateActivity = gameActivities.GetListDateActivity(),
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
                PCConfigurationId = sessionActivity.IdConfiguration,
                PCName = config != null ? config.Name : string.Empty,
                TypeStoreIcon = ModeSimple,
                SourceIcon = PlayniteTools.GetPlatformIcon(sourceName),
                SourceIconText = TransformIcon.Get(sourceName),
                GameActionName = sessionActivity.GameActionName
            };
        }


        /// <summary>
        /// Get data for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="variateur"></param>
        public void GetActivityForGamesTimeGraphics(Guid? gameId, bool isNavigation = false)
        {
            if (gameId == null || gameId == Guid.Empty)
            {
                PART_GameActivityChartTime.GameContext = null;
                return;
            }

            PART_GameActivityChartTime.PeriodFilterStart = ViewModel.PeriodStart;
            PART_GameActivityChartTime.PeriodFilterEnd = ViewModel.PeriodEnd;
            Common.LogDebug($"PeriodView: ChartTime game={gameId} period={ViewModel.PeriodStart:yyyy-MM-dd}..{ViewModel.PeriodEnd:yyyy-MM-dd} nav={isNavigation}");
            PART_GameActivityChartTime.GameContext = API.Instance.Database.Games.Get(gameId.Value);
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
        public void GetActivityForGamesLogGraphics(Guid? gameId, DateTime? dateSelected = null, string title = "", bool isNavigation = false)
        {
            if (gameId == null || gameId == Guid.Empty)
            {
                PART_GameActivityChartLog.GameContext = null;
                return;
            }

            Guid parsedGameId = gameId.Value;
            GameActivities gameActivities = GameActivity.PluginDatabase.Get(parsedGameId);

            // Prefer an explicit session, else the last session in the active period.
            DateTime? resolvedDate = dateSelected;
            if (resolvedDate == null || resolvedDate == default(DateTime))
            {
                if (ViewModel.PeriodSessionList != null && ViewModel.PeriodSessionList.Count > 0)
                {
                    resolvedDate = ViewModel.PeriodSessionList[ViewModel.PeriodSessionList.Count - 1].GameLastActivity;
                }
            }

            PART_GameActivityChartLog.GameContext = API.Instance.Database.Games.Get(parsedGameId);
            PART_GameActivityChartLog.DisableAnimations = true;
            PART_GameActivityChartLog.DateSelected = resolvedDate;
            PART_GameActivityChartLog.TitleChart = title;
            PART_GameActivityChartLog.AxisVariator = VariateurLog;
            Common.LogDebug($"PeriodView: ChartLog game={parsedGameId} date={(resolvedDate.HasValue ? resolvedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "null")} nav={isNavigation}");

            if (!isNavigation)
            {
                DateTime? lastSession = gameActivities?.GetLastSession();
                bool hasDateSelection = resolvedDate != null && resolvedDate != default(DateTime);

                if (hasDateSelection)
                {
                    gameLabel.Content = ResourceProvider.GetString("LOCGameActivityLogTitleDate") + " "
                        + Convert.ToDateTime(resolvedDate).ToString(Constants.DateUiFormat);
                }
                else if (lastSession != null && lastSession != default(DateTime))
                {
                    gameLabel.Content = ResourceProvider.GetString("LOCGameActivityLogTitleDate") + " ("
                        + Convert.ToDateTime(lastSession).ToString(Constants.DateUiFormat) + ")";
                }
                else
                {
                    gameLabel.Content = ResourceProvider.GetString("LOCGameActivityLogTitleDate");
                }
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

            Common.LogDebug(Serialization.ToJson(arrayReturn));
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
            ViewModel.PeriodSessionList = new List<ListActivities>();

            if (sender != null)
            {
                ListBox item = (ListBox)sender;
                ListActivities gameItem = item.SelectedItem as ListActivities;
                if (gameItem == null || string.IsNullOrEmpty(gameItem.GameId))
                {
                    GameIDCurrent = null;
                    ClearPcConfigurationDetails();
                    return;
                }

                Guid parsedGameId;
                if (!Guid.TryParse(gameItem.GameId, out parsedGameId))
                {
                    GameIDCurrent = null;
                    ClearPcConfigurationDetails();
                    return;
                }

                GameIDCurrent = parsedGameId;
                Common.LogDebug($"PeriodView: select game={gameItem.GameTitle} id={parsedGameId} chartMode={(IsGameTime ? "Time" : "Log")}");
                LoadPeriodSessionsForGame(parsedGameId);

                ListActivities lastPeriodSession = null;
                if (ViewModel.PeriodSessionList != null && ViewModel.PeriodSessionList.Count > 0)
                {
                    lastPeriodSession = ViewModel.PeriodSessionList[ViewModel.PeriodSessionList.Count - 1];
                    lvSessions.SelectedItem = lastPeriodSession;
                }

                if (IsGameTime)
                {
                    GetActivityForGamesTimeGraphics(GameIDCurrent);
                }
                else
                {
                    GetActivityForGamesLogGraphics(
                        GameIDCurrent,
                        lastPeriodSession != null ? (DateTime?)lastPeriodSession.GameLastActivity : null);
                }

                activityForGamesGraphics.Visibility = Visibility.Visible;

                int index = lastPeriodSession != null
                    ? lastPeriodSession.PCConfigurationId
                    : gameItem.PCConfigurationId;
                List<SystemConfiguration> configurations = PluginDatabase.SystemConfigurationManager.GetConfigurations();
                if (index != -1 && index < configurations.Count)
                {
                    ApplyPcConfigurationDetails(configurations[index]);
                }
                else
                {
                    ClearPcConfigurationDetails();
                }
            }
        }

        private void LvSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListActivities sessionItem = lvSessions?.SelectedItem as ListActivities;
            if (sessionItem == null)
            {
                return;
            }

            LabelDataSelected = sessionItem.GameLastActivity;
            ViewModel.LabelDataSelected = sessionItem.GameLastActivity;
            Common.LogDebug($"PeriodView: select session={sessionItem.GameLastActivity:yyyy-MM-dd HH:mm:ss} chartMode={(IsGameTime ? "Time" : "Log")}");

            int index = sessionItem.PCConfigurationId;
            List<SystemConfiguration> configurations = PluginDatabase.SystemConfigurationManager.GetConfigurations();
            if (index != -1 && index < configurations.Count)
            {
                ApplyPcConfigurationDetails(configurations[index]);
            }
            else
            {
                ClearPcConfigurationDetails();
            }

            // Step 5 will fully bind ChartLog to the selected session; prepare DateSelected now.
            if (!IsGameTime && GameIDCurrent != null)
            {
                GetActivityForGamesLogGraphics(GameIDCurrent, sessionItem.GameLastActivity);
            }
        }

        /// <summary>
        /// Applies selected PC configuration details to the UI panel.
        /// </summary>
        /// <param name="configuration">Selected configuration.</param>
        private void ApplyPcConfigurationDetails(SystemConfiguration configuration)
        {
            if (configuration == null)
            {
                ClearPcConfigurationDetails();
                return;
            }

            PART_PcName.Content = configuration.Name;
            PART_Os.Content = configuration.Os;
            PART_CpuName.Content = configuration.Cpu;
            PART_GpuName.Content = configuration.GpuName;
            PART_Ram.Content = configuration.RamUsage;
            PART_PcConfigExpander.Tag = string.Format("{0} · {1} · {2}", configuration.Name, configuration.Cpu, configuration.GpuName);
        }

        /// <summary>
        /// Clears selected PC configuration details from the UI panel.
        /// </summary>
        private void ClearPcConfigurationDetails()
        {
            PART_PcName.Content = string.Empty;
            PART_Os.Content = string.Empty;
            PART_CpuName.Content = string.Empty;
            PART_GpuName.Content = string.Empty;
            PART_Ram.Content = string.Empty;
            PART_PcConfigExpander.Tag = string.Empty;
        }


        #region Butons click event
        private void Button_Click_PrevMonth(object sender, RoutedEventArgs e)
        {
            ViewModel.ShiftPeriod(-1);
            ReloadPeriodChartsAndFilter();
        }

        private void Button_Click_NextMonth(object sender, RoutedEventArgs e)
        {
            ViewModel.ShiftPeriod(1);
            ReloadPeriodChartsAndFilter();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;
            if (control?.SelectedDate == null)
            {
                return;
            }

            ViewModel.SetMonth(control.SelectedDate.Value);
            ReloadPeriodChartsAndFilter();
        }

        private bool _periodPresetReady;

        private void PeriodPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_periodPresetReady)
            {
                return;
            }

            ComboBox combo = sender as ComboBox;
            if (combo == null)
            {
                return;
            }

            PeriodPresetItem item = combo.SelectedItem as PeriodPresetItem;
            if (item == null)
            {
                return;
            }

            if (item.Kind == ViewModel.PeriodKind)
            {
                return;
            }

            ViewModel.SetPeriod(item.Kind);
            ReloadPeriodChartsAndFilter();
        }

        /// <summary>
        /// Reloads aggregate charts for the active period and re-applies the shared list filter.
        /// Day/week Sources charts use the same <see cref="GameActivityViewModel.PeriodStart"/> / <see cref="GameActivityViewModel.PeriodEnd"/> as the total chart.
        /// </summary>
        private void ReloadPeriodChartsAndFilter()
        {
            Common.LogDebug($"PeriodView: ReloadPeriodChartsAndFilter period={ViewModel.PeriodKind} aggregate={ViewModel.AggregateKind} {ViewModel.PeriodStart:yyyy-MM-dd}..{ViewModel.PeriodEnd:yyyy-MM-dd}");
            SyncPeriodPresetComboSelection();
            AggregateKind kind = ViewModel.AggregateKind;
            InvalidateInactiveAggregateCharts(kind);
            InvalidateActiveAggregateChart(kind);
            EnsureAggregateChartsLoaded(kind);
            GetActivityByListGame();
        }

        private void InitializePeriodPresetCombo()
        {
            _periodPresetReady = false;
            List<PeriodPresetItem> items = new List<PeriodPresetItem>
            {
                new PeriodPresetItem { Kind = ActivityPeriodKind.Last7Days, Label = GameActivityViewModel.GetPeriodKindDisplayName(ActivityPeriodKind.Last7Days) },
                new PeriodPresetItem { Kind = ActivityPeriodKind.Month, Label = GameActivityViewModel.GetPeriodKindDisplayName(ActivityPeriodKind.Month) },
                new PeriodPresetItem { Kind = ActivityPeriodKind.Last3Months, Label = GameActivityViewModel.GetPeriodKindDisplayName(ActivityPeriodKind.Last3Months) },
                new PeriodPresetItem { Kind = ActivityPeriodKind.Year, Label = GameActivityViewModel.GetPeriodKindDisplayName(ActivityPeriodKind.Year) }
            };

            PART_PeriodPreset.ItemsSource = items;
            SyncPeriodPresetComboSelection();
            _periodPresetReady = true;
        }

        private void SyncPeriodPresetComboSelection()
        {
            if (PART_PeriodPreset == null)
            {
                return;
            }

            PeriodPresetItem match = null;
            for (int i = 0; i < PART_PeriodPreset.Items.Count; i++)
            {
                PeriodPresetItem item = PART_PeriodPreset.Items[i] as PeriodPresetItem;
                if (item != null && item.Kind == ViewModel.PeriodKind)
                {
                    match = item;
                    break;
                }
            }

            if (match != null && !ReferenceEquals(PART_PeriodPreset.SelectedItem, match))
            {
                bool wasReady = _periodPresetReady;
                _periodPresetReady = false;
                PART_PeriodPreset.SelectedItem = match;
                _periodPresetReady = wasReady;
            }
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
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to switch GameActivity chart mode to Time", true, PluginDatabase.PluginName);
                }
            }

            try
            {
                if (ToggleButtonLog.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to validate GameActivity toggle buttons state (Time)", true, PluginDatabase.PluginName);
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
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to switch GameActivity chart mode to Log", true, PluginDatabase.PluginName);
                }
            }

            try
            {
                if (ToggleButtonTime.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to validate GameActivity toggle buttons state (Log)", true, PluginDatabase.PluginName);
            }
        }


        private void SyncAggregateModeTabs()
        {
            if (PART_AggregateModeTabs == null)
            {
                return;
            }

            int index = (int)ViewModel.AggregateKind;
            if (PART_AggregateModeTabs.SelectedIndex == index)
            {
                return;
            }

            _aggregateTabsSyncing = true;
            try
            {
                PART_AggregateModeTabs.SelectedIndex = index;
            }
            finally
            {
                _aggregateTabsSyncing = false;
            }
        }

        private void AggregateModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_aggregateTabsSyncing || PART_AggregateModeTabs == null)
            {
                return;
            }

            int index = PART_AggregateModeTabs.SelectedIndex;
            if (index < 0 || index > (int)AggregateKind.Tags)
            {
                return;
            }

            AggregateKind kind = (AggregateKind)index;
            if (ViewModel.AggregateKind == kind)
            {
                return;
            }

            try
            {
                Common.LogDebug($"PeriodView: AggregateTab → {kind}");
                ViewModel.SetAggregateKind(kind);
                InvalidateInactiveAggregateCharts(kind);
                EnsureAggregateChartsLoaded(kind);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to switch aggregate mode tab", true, PluginDatabase.PluginName);
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


        /// <summary>
        /// Selects the period session matching a ChartTime data-point click.
        /// </summary>
        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {
            if (chartPoint?.SeriesView?.Values == null)
            {
                return;
            }

            int index = (int)chartPoint.X;
            IChartValues data = chartPoint.SeriesView.Values;
            if (index < 0 || index >= data.Count)
            {
                return;
            }

            CustomerForTime point = data[index] as CustomerForTime;
            if (point == null || point.Values == 0)
            {
                return;
            }

            DateTime sessionDate = point.SessionDate;
            if (sessionDate == default(DateTime))
            {
                if (!TryResolveChartPointDay(point.Name, out sessionDate))
                {
                    Common.LogDebug($"PeriodView: ChartTime click unresolved name={point.Name}");
                    return;
                }
            }

            ListActivities matched = FindPeriodSessionForChartPoint(sessionDate, chartPoint.SeriesView.Title);
            if (matched == null)
            {
                Common.LogDebug($"PeriodView: ChartTime click no session for {sessionDate:yyyy-MM-dd HH:mm:ss} series={chartPoint.SeriesView.Title}");
                return;
            }

            Common.LogDebug($"PeriodView: ChartTime click → session={matched.GameLastActivity:yyyy-MM-dd HH:mm:ss}");
            lvSessions.SelectedItem = matched;
            lvSessions.ScrollIntoView(matched);
        }

        /// <summary>
        /// Tries to parse a chart label into a local day (ISO or culture date).
        /// </summary>
        private static bool TryResolveChartPointDay(string name, out DateTime day)
        {
            day = default(DateTime);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            DateTime parsed;
            if (DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                day = parsed.Date;
                return true;
            }

            if (DateTime.TryParse(name, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                day = parsed.Date;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the period session for a chart point (exact timestamp, else nth session of the day by series title).
        /// </summary>
        private ListActivities FindPeriodSessionForChartPoint(DateTime sessionDate, string seriesTitle)
        {
            List<ListActivities> sessions = ViewModel.PeriodSessionList;
            if (sessions == null || sessions.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].GameLastActivity == sessionDate)
                {
                    return sessions[i];
                }
            }

            // Tolerance for tick differences (sub-second / UTC conversion).
            for (int i = 0; i < sessions.Count; i++)
            {
                TimeSpan delta = sessions[i].GameLastActivity - sessionDate;
                if (Math.Abs(delta.TotalSeconds) < 2)
                {
                    return sessions[i];
                }
            }

            List<ListActivities> sameDay = new List<ListActivities>();
            DateTime day = sessionDate.Date;
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].GameLastActivity.Date == day)
                {
                    sameDay.Add(sessions[i]);
                }
            }

            if (sameDay.Count == 0)
            {
                return null;
            }

            int seriesIndex = 0;
            if (!string.IsNullOrEmpty(seriesTitle))
            {
                int parsedIndex;
                if (int.TryParse(seriesTitle, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedIndex))
                {
                    seriesIndex = parsedIndex - 1;
                }
            }

            if (seriesIndex < 0)
            {
                seriesIndex = 0;
            }

            if (seriesIndex >= sameDay.Count)
            {
                seriesIndex = sameDay.Count - 1;
            }

            return sameDay[seriesIndex];
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

    /// <summary>
    /// ComboBox entry for home-view period presets (#241).
    /// </summary>
    public class PeriodPresetItem
    {
        /// <summary>Period kind represented by this item.</summary>
        public ActivityPeriodKind Kind { get; set; }

        /// <summary>Localized display label.</summary>
        public string Label { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }
}
