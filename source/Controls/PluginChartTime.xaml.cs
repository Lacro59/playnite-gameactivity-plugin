using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Utilities;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Events;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;

namespace GameActivity.Controls
{
    /// <summary>
    /// User control providing charting functionality to visualize game activity over time.
    /// Supports aggregating time by day or week and displaying interactive columns.
    /// </summary>
    public partial class PluginChartTime : PluginUserControlExtend
    {
        // ── Plugin database wiring ────────────────────────────────────────────

        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartTimeDataContext ControlDataContext = new PluginChartTimeDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartTimeDataContext)value;
        }

        // ── Toggle ids ───────────────────────────────────────────────────────

        /// <summary>Nav bar toggle id for the ShowByWeeks feature.</summary>
        private const string ToggleIdShowByWeeks = "showByWeeks";

        /// <summary>Nav bar toggle id for the Truncate feature.</summary>
        private const string ToggleIdTruncate = "truncate";

        // ── Window tracking ──────────────────────────────────────────────────

        /// <summary>
        /// Total data points (days or weeks) available in the full dataset for the current game.
        /// Distinct from <see cref="_lastWindowSize"/>: this is the complete span, not the current page.
        /// Used by <see cref="Prev"/> and the nav bar to determine whether earlier data exists.
        /// </summary>
        private int _totalDataPoints;

        /// <summary>
        /// Effective number of columns rendered in the last chart build (current page size).
        /// Differs from <see cref="_totalDataPoints"/> when the dataset is larger than one page.
        /// Used by <see cref="Prev"/> and <see cref="NavBar_FirstClicked"/> to compute the leftmost valid AxisVariator.
        /// </summary>
        private int _lastWindowSize;

        /// <summary>Fired when the user clicks a data point on the chart.</summary>
        public event DataClickHandler GameSeriesDataClick;

        // ────────────────────────────────────────────────────────────────────
        // Dependency Properties
        // ────────────────────────────────────────────────────────────────────

        #region Properties

        /// <summary>When true, disables LiveCharts entry animations.</summary>
        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }
        public static readonly DependencyProperty DisableAnimationsProperty =
            DependencyProperty.Register(
                nameof(DisableAnimations),
                typeof(bool),
                typeof(PluginChartTime),
                new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback)
            );

        /// <summary>When true, data is aggregated and displayed by calendar week instead of by day.</summary>
        public bool ShowByWeeks
        {
            get => (bool)GetValue(ShowByWeeksProperty);
            set => SetValue(ShowByWeeksProperty, value);
        }
        public static readonly DependencyProperty ShowByWeeksProperty = DependencyProperty.Register(
            nameof(ShowByWeeks),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback)
        );

        /// <summary>
        /// When true, the X-axis is built from the distinct session dates that actually have data,
        /// instead of a continuous date range. Gaps between sessions are therefore hidden.
        /// </summary>
        public bool Truncate
        {
            get => (bool)GetValue(TruncateProperty);
            set => SetValue(TruncateProperty, value);
        }
        public static readonly DependencyProperty TruncateProperty = DependencyProperty.Register(
            nameof(Truncate),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback)
        );

        /// <summary>When true, X-axis labels are rotated 160° to avoid overlap on dense charts.</summary>
        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }
        public static readonly DependencyProperty LabelsRotationProperty =
            DependencyProperty.Register(
                nameof(LabelsRotation),
                typeof(bool),
                typeof(PluginChartTime),
                new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback)
            );

        /// <summary>
        /// Maximum number of X-axis items (days or weeks) rendered in one page.
        /// When 0, falls back to <c>PluginSettings.ChartTimeCountAbscissa</c>.
        /// </summary>
        public int AxisLimit
        {
            get => (int)GetValue(AxisLimitProperty);
            set => SetValue(AxisLimitProperty, value);
        }
        public static readonly DependencyProperty AxisLimitProperty = DependencyProperty.Register(
            nameof(AxisLimit),
            typeof(int),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback)
        );

        /// <summary>
        /// Signed offset applied to the most-recent-window anchor.
        /// Negative values scroll the view toward older data; 0 always shows the most recent page.
        /// </summary>
        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }
        public static readonly DependencyProperty AxisVariatoryProperty =
            DependencyProperty.Register(
                nameof(AxisVariator),
                typeof(int),
                typeof(PluginChartTime),
                new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback)
            );

        /// <summary>
        /// Shows or hides the <see cref="PluginChartNavBar"/> above the chart.
        /// Default is false so that existing usages without a nav bar are unaffected.
        /// </summary>
        public bool ShowNavBar
        {
            get => (bool)GetValue(ShowNavBarProperty);
            set => SetValue(ShowNavBarProperty, value);
        }
        public static readonly DependencyProperty ShowNavBarProperty = DependencyProperty.Register(
            nameof(ShowNavBar),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback)
        );

        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage nav bar buttons.
        /// When &lt;= 0, the PrevPage/NextPage buttons are hidden in the nav bar.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize),
            typeof(int),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback)
        );

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginChartTime"/> class.
        /// Sets up the data context and hooks up the navigation bar event handlers.
        /// </summary>
        public PluginChartTime()
        {
            AlwaysShow = true;
            InitializeComponent();
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (PART_NavBar != null)
            {
                DependencyPropertyDescriptor
                    .FromProperty(PluginChartNavBar.AxisLimitProperty, typeof(PluginChartNavBar))
                    .AddValueChanged(PART_NavBar, OnNavBarAxisLimitChanged);

                PART_NavBar.AxisLimitReset += NavBar_AxisLimitReset;
                PART_NavBar.NavBarToggleChanged += NavBar_ToggleChanged;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the control is unloaded from the visual tree.
        /// Detaches dependency property value changed listeners to prevent memory leaks.
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (PART_NavBar != null)
            {
                DependencyPropertyDescriptor
                    .FromProperty(PluginChartNavBar.AxisLimitProperty, typeof(PluginChartNavBar))
                    .RemoveValueChanged(PART_NavBar, OnNavBarAxisLimitChanged);

                PART_NavBar.AxisLimitReset -= NavBar_AxisLimitReset;
                PART_NavBar.NavBarToggleChanged -= NavBar_ToggleChanged;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Static event registration
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attaches static plugin events for handling plugin settings and database item updates.
        /// </summary>
        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(
                PluginDatabase.PluginName,
                () =>
                {
                    PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                    PluginDatabase.DatabaseItemUpdated +=
                        CreateDatabaseItemUpdatedHandler<GameActivities>();
                    PluginDatabase.DatabaseItemCollectionChanged +=
                        CreateDatabaseCollectionChangedHandler<GameActivities>();
                }
            );
        }

        /// <summary>
        /// Handles changes to the plugin settings and refreshes the chart context.
        /// </summary>
        protected override void PluginSettings_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e
        )
        {
            Truncate = PluginDatabase.PluginSettings.ChartTimeTruncate;
            GameContextChanged(null, GameContext);
        }

        // ────────────────────────────────────────────────────────────────────
        // DataContext initialisation
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the default data context for the chart, applying visibility and axis rules based on settings.
        /// </summary>
        public override void SetDefaultDataContext()
        {
            bool isActivated = PluginDatabase.PluginSettings.EnableIntegrationChartTime;
            double chartTimeHeight = PluginDatabase.PluginSettings.ChartTimeHeight;
            bool chartTimeAxis = PluginDatabase.PluginSettings.ChartTimeAxis;
            bool chartTimeOrdinates = PluginDatabase.PluginSettings.ChartTimeOrdinates;

            if (IgnoreSettings)
            {
                isActivated = true;
                chartTimeHeight = double.NaN;
                chartTimeAxis = true;
                chartTimeOrdinates = true;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ChartTimeHeight = chartTimeHeight;
            ControlDataContext.ChartTimeAxis = chartTimeAxis;
            ControlDataContext.ChartTimeOrdinates = chartTimeOrdinates;
            ControlDataContext.ChartTimeVisibleEmpty = PluginDatabase
                .PluginSettings
                .ChartTimeVisibleEmpty;
            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotation ? 160d : 0d;

            bool showNavBar = IgnoreSettings ? true : ShowNavBar;
            ControlDataContext.ShowNavBar = showNavBar;
            ControlDataContext.ShowAllData = false;
            ControlDataContext.NavLabel = string.Empty;

            int effectivePageSize =
                AxisLimit > 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;

            ControlDataContext.PageSize = effectivePageSize;
            ControlDataContext.AxisLimit = AxisLimit;

            if (PART_NavBar != null)
            {
                int defaultLimit =
                    AxisLimit > 0
                        ? AxisLimit
                        : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;

                PART_NavBar.AxisLimitDefault = defaultLimit;
                PART_NavBar.AxisLimit = AxisLimit;

                RegisterNavBarToggles();
            }

            PART_ChartTimeActivity.Series = null;
            PART_ChartTimeActivityLabelsX.Labels = null;
            _totalDataPoints = 0;
            _lastWindowSize = 0;
        }

        /// <summary>
        /// Registers the ShowByWeeks and Truncate toggles on the nav bar.
        /// Removes any previously registered instances first so this method is safe
        /// to call on every <see cref="SetDefaultDataContext"/> invocation.
        /// </summary>
        private void RegisterNavBarToggles()
        {
            // Remove before re-adding — SetDefaultDataContext can be called multiple times.
            PART_NavBar.RemoveToggle(ToggleIdShowByWeeks);
            PART_NavBar.RemoveToggle(ToggleIdTruncate);

            var toggleWeeks = new NavBarToggle(ToggleIdShowByWeeks, "\uEC45")
            {
                InactiveToolTip = ResourceProvider.GetString("LOCCommonNavShowByWeeks"),
                ActiveToolTip = ResourceProvider.GetString("LOCCommonNavShowByWeeksDisable"),
            };
            PART_NavBar.AddToggle(toggleWeeks);

            // Initialise state silently — must not fire NavBarToggleChanged during setup.
            PART_NavBar.SetToggleState(ToggleIdShowByWeeks, ShowByWeeks);

            var toggleTruncate = new NavBarToggle(ToggleIdTruncate, "\uEF29")
            {
                InactiveToolTip = ResourceProvider.GetString("LOCCommonNavTruncate"),
                ActiveToolTip = ResourceProvider.GetString("LOCCommonNavTruncateDisable"),
            };
            PART_NavBar.AddToggle(toggleTruncate);

            PART_NavBar.SetToggleState(ToggleIdTruncate, Truncate);
        }

        // ────────────────────────────────────────────────────────────────────
        // Data loading
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Invoked when the control receives new data context. Triggers data fetching and rendering.
        /// </summary>
        /// <param name="newContext">The newly selected game.</param>
        /// <param name="pluginGameData">The associated game activity data.</param>
        public override void SetData(Game newContext, PluginGameEntry pluginGameData)
        {
            GameActivities gameActivities = (GameActivities)pluginGameData;

            MustDisplay =
                !IgnoreSettings && !ControlDataContext.ChartTimeVisibleEmpty
                    ? gameActivities.HasData
                    : true;

            if (!MustDisplay)
            {
                return;
            }

            ControlDataContext.ChartTimeAxis = !ControlDataContext.ShowAllData;

            if (ControlDataContext.ShowAllData)
            {
                if (ShowByWeeks)
                {
                    GetActivityForGamesChartByWeek(
                        gameActivities,
                        0,
                        Convert.ToInt32(gameActivities.Count)
                    );
                }
                else
                {
                    GetActivityForGamesTimeGraphics(
                        gameActivities,
                        0,
                        Convert.ToInt32(gameActivities.Count)
                    );
                }

                return;
            }

            int limit =
                AxisLimit != 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            int axisVariator = AxisVariator;

            if (ShowByWeeks)
            {
                GetActivityForGamesChartByWeek(gameActivities, axisVariator, limit);
            }
            else
            {
                GetActivityForGamesTimeGraphics(gameActivities, axisVariator, limit);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Public navigation API
        // ────────────────────────────────────────────────────────────────────

        #region Public methods

        /// <summary>Advances the axis window forward by <paramref name="value"/> steps, clamped to 0.</summary>
        public void Next(int value = 1)
        {
            AxisVariator = Math.Min(0, AxisVariator + value);
        }

        /// <summary>
        /// Moves the axis window backward by <paramref name="value"/> steps,
        /// clamped so the window never starts before the first data point.
        /// </summary>
        public void Prev(int value = 1)
        {
            // _lastWindowSize reflects the actual rendered window size, which differs between
            // Truncate mode (≤ limit entries) and normal mode (limit + 1 entries).
            // Falls back to limit + 1 before the first render.
            int limit =
                AxisLimit > 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            int windowSize = _lastWindowSize > 0 ? _lastWindowSize : limit + 1;
            int minVariator = _totalDataPoints > windowSize ? -(_totalDataPoints - windowSize) : 0;
            AxisVariator = Math.Max(minVariator, AxisVariator - value);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Nav bar event handlers
        // ────────────────────────────────────────────────────────────────────

        #region Nav bar event handlers

        /// <summary>
        /// Jumps to the first page (oldest possible window entries) by computing
        /// the leftmost valid AxisVariator from the last known window size.
        /// </summary>
        private void NavBar_FirstClicked(object sender, RoutedEventArgs e)
        {
            int limit =
                AxisLimit > 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            int windowSize = _lastWindowSize > 0 ? _lastWindowSize : limit + 1;
            int minVariator = _totalDataPoints > windowSize ? -(_totalDataPoints - windowSize) : 0;
            AxisVariator = minVariator;
        }

        /// <summary>Moves the session window backward by a full page.</summary>
        private void NavBar_PagePrevClicked(object sender, RoutedEventArgs e)
        {
            int pageSize =
                AxisLimit > 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            Prev(pageSize);
        }

        /// <summary>Moves the session window backward by a single step.</summary>
        private void NavBar_PrevClicked(object sender, RoutedEventArgs e)
        {
            Prev();
        }

        /// <summary>
        /// Toggles between the paginated view and showing the entire session dataset.
        /// </summary>
        private void NavBar_ShowAllToggled(object sender, RoutedEventArgs e)
        {
            ControlDataContext.ShowAllData = PART_NavBar.ShowAllData;
            GameContextChanged(null, GameContext);
        }

        /// <summary>Moves the session window forward by a single step.</summary>
        private void NavBar_NextClicked(object sender, RoutedEventArgs e)
        {
            Next();
        }

        /// <summary>Moves the session window forward by a full page.</summary>
        private void NavBar_PageNextClicked(object sender, RoutedEventArgs e)
        {
            int pageSize =
                AxisLimit > 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            Next(pageSize);
        }

        /// <summary>Jumps to the most recent window by resetting AxisVariator to 0.</summary>
        private void NavBar_LastClicked(object sender, RoutedEventArgs e)
        {
            AxisVariator = 0;
        }

        /// <summary>
        /// Handles the reset event from the navigation bar's axis limit.
        /// </summary>
        private void NavBar_AxisLimitReset(object sender, RoutedEventArgs e)
        {
            ApplyAxisLimitFromNavBar();
        }

        /// <summary>
        /// Handles changes to the navigation bar's axis limit dependency property.
        /// </summary>
        private void OnNavBarAxisLimitChanged(object sender, EventArgs e)
        {
            ApplyAxisLimitFromNavBar();
        }

        /// <summary>
        /// Dispatches nav bar toggle state changes to the appropriate control property.
        /// A single handler for all toggles — identified by <see cref="NavBarToggleChangedEventArgs.ToggleId"/>.
        /// </summary>
        private void NavBar_ToggleChanged(object sender, NavBarToggleChangedEventArgs e)
        {
            switch (e.ToggleId)
            {
                case ToggleIdShowByWeeks:
                    ShowByWeeks = e.IsActive;
                    GameContextChanged(null, GameContext);
                    break;

                case ToggleIdTruncate:
                    Truncate = e.IsActive;
                    GameContextChanged(null, GameContext);
                    break;
            }
        }

        /// <summary>
        /// Reads the updated <see cref="PluginChartNavBar.AxisLimit"/> from the nav bar,
        /// pushes it into the control's own <see cref="AxisLimit"/> DP and refreshes
        /// <see cref="ControlDataContext.PageSize"/> so the nav bar tooltip stays accurate.
        /// </summary>
        /// <remarks>
        /// <see cref="GameContextChanged"/> is called explicitly because WPF skips
        /// <c>ControlsPropertyChangedCallback</c> when the DP value does not change (equal-value edge case).
        /// </remarks>
        private void ApplyAxisLimitFromNavBar()
        {
            int newLimit = PART_NavBar.AxisLimit;
            int effectivePageSize =
                newLimit > 0 ? newLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;

            ControlDataContext.PageSize = effectivePageSize;
            ControlDataContext.AxisLimit = newLimit;
            PART_NavBar.PageSize = effectivePageSize;
            AxisLimit = newLimit;

            GameContextChanged(null, GameContext);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Chart builders
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and assigns the LiveCharts series collection for the time-based session window (days).
        /// </summary>
        /// <param name="gameActivities">The game activities data object containing session logs.</param>
        /// <param name="variateurTime">The signed offset applied to the session window anchor.</param>
        /// <param name="limit">The maximum number of days to render in one page.</param>
        private void GetActivityForGamesTimeGraphics(
            GameActivities gameActivities,
            int variateurTime = 0,
            int limit = 9
        )
        {
            try
            {
                if (gameActivities?.FilterItems == null)
                {
                    return;
                }

                string[] listDate;
                ChartValues<CustomerForTime> series1 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series2 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series3 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series4 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series5 = new ChartValues<CustomerForTime>();

                bool hasData2 = false;
                bool hasData3 = false;
                bool hasData4 = false;
                bool hasData5 = false;

                List<Activity> activities = Serialization.GetClone(gameActivities.FilterItems);

                if (Truncate)
                {
                    activities = activities.OrderBy(x => x.DateSession).ToList();

                    List<string> dtList = activities
                        .Where(x => x.DateSession != null)
                        .Select(x => ((DateTime)x.DateSession).ToLocalTime().ToString("yyyy-MM-dd"))
                        .Distinct()
                        .ToList();

                    // Total distinct session days available — independent of the current window.
                    int totalDistinctDays = dtList.Count;

                    if (dtList.Count > limit && variateurTime != 0)
                    {
                        if (variateurTime > 0)
                        {
                            variateurTime = 0;
                        }
                        else if (dtList.Count + variateurTime - limit <= 0)
                        {
                            variateurTime = limit - dtList.Count;
                            while (dtList.Count > limit)
                            {
                                dtList.RemoveAt(dtList.Count - 1);
                            }
                        }
                        else
                        {
                            int max = dtList.Count + variateurTime - 1;
                            while (dtList.Count - 1 > max)
                            {
                                dtList.RemoveAt(dtList.Count - 1);
                            }
                        }
                    }

                    int countActivities = dtList.Count;
                    int newLimit =
                        (countActivities - limit - 1) >= 0 ? countActivities - limit - 1 : 0;
                    listDate = new string[countActivities - newLimit];

                    for (int i = newLimit; i < countActivities; i++)
                    {
                        string dt = dtList[i];
                        listDate[i - newLimit] = dt;

                        CustomerForTime placeholder = new CustomerForTime
                        {
                            Name = dt,
                            Values = 0,
                            HideIsZero = true,
                        };
                        series1.Add(placeholder);
                        series2.Add(placeholder);
                        series3.Add(placeholder);
                        series4.Add(placeholder);
                        series5.Add(placeholder);
                    }

                    _totalDataPoints = totalDistinctDays;
                    _lastWindowSize = listDate.Length;
                }
                else
                {
                    DateTime dateStart = new DateTime(1982, 12, 15, 0, 0, 0);
                    foreach (Activity activity in activities)
                    {
                        DateTime dateSession = Convert.ToDateTime(
                            activity.DateSession?.ToLocalTime()
                        );
                        if (dateSession > dateStart)
                        {
                            dateStart = dateSession;
                        }
                    }
                    dateStart = dateStart.AddDays(variateurTime);

                    // ShowAllData passes limit = Count: span the full date range.
                    if (limit == int.MaxValue || limit >= activities.Count)
                    {
                        DateTime dateMin = activities
                            .Where(x => x.DateSession != null)
                            .Min(x => Convert.ToDateTime(x.DateSession).ToLocalTime())
                            .Date;

                        DateTime dateMax = activities
                            .Where(x => x.DateSession != null)
                            .Max(x => Convert.ToDateTime(x.DateSession).ToLocalTime())
                            .Date;

                        int totalDays = (int)(dateMax - dateMin).TotalDays + 1;
                        totalDays = totalDays < 1 ? 1 : totalDays;

                        listDate = new string[totalDays];
                        for (int i = 0; i < totalDays; i++)
                        {
                            string dateStr = dateMin.AddDays(i).ToString("yyyy-MM-dd");
                            listDate[i] = dateStr;
                            CustomerForTime placeholder = new CustomerForTime
                            {
                                Name = dateStr,
                                Values = 0,
                                HideIsZero = true,
                            };
                            series1.Add(placeholder);
                            series2.Add(placeholder);
                            series3.Add(placeholder);
                            series4.Add(placeholder);
                            series5.Add(placeholder);
                        }

                        _totalDataPoints = totalDays;
                        _lastWindowSize = totalDays;
                    }
                    else
                    {
                        // Total continuous days spanned by the full dataset.
                        DateTime fullDateMin = activities
                            .Where(x => x.DateSession != null)
                            .Min(x => Convert.ToDateTime(x.DateSession).ToLocalTime())
                            .Date;

                        DateTime fullDateMax = activities
                            .Where(x => x.DateSession != null)
                            .Max(x => Convert.ToDateTime(x.DateSession).ToLocalTime())
                            .Date;

                        int fullTotalDays = (int)(fullDateMax - fullDateMin).TotalDays + 1;
                        fullTotalDays = fullTotalDays < 1 ? 1 : fullTotalDays;

                        listDate = new string[limit + 1];
                        for (int i = limit; i >= 0; i--)
                        {
                            string dateStr = dateStart.AddDays(-i).ToString("yyyy-MM-dd");
                            listDate[limit - i] = dateStr;

                            CustomerForTime placeholder = new CustomerForTime
                            {
                                Name = dateStr,
                                Values = 0,
                                HideIsZero = true,
                            };
                            series1.Add(placeholder);
                            series2.Add(placeholder);
                            series3.Add(placeholder);
                            series4.Add(placeholder);
                            series5.Add(placeholder);
                        }

                        _totalDataPoints = fullTotalDays;
                        _lastWindowSize = listDate.Length;
                    }
                }

                LocalDateConverter localDateConverter = new LocalDateConverter();
                bool cumulSessions = PluginDatabase.PluginSettings.CumulPlaytimeSession;
                int effectiveLimit = listDate.Length - 1;

                for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                {
                    ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                    string dateSession = Convert
                        .ToDateTime(activities[iActivity].DateSession)
                        .ToLocalTime()
                        .ToString("yyyy-MM-dd");

                    for (int iDay = effectiveLimit; iDay >= 0; iDay--)
                    {
                        if (listDate[iDay] != dateSession)
                        {
                            continue;
                        }

                        string displayName = series1[iDay].Name;
                        try
                        {
                            displayName = (string)
                                localDateConverter.Convert(
                                    DateTime
                                        .ParseExact(series1[iDay].Name, "yyyy-MM-dd", null)
                                        .ToLocalTime(),
                                    null,
                                    null,
                                    CultureInfo.CurrentCulture
                                );
                        }
                        catch { }

                        if (cumulSessions)
                        {
                            series1[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = series1[iDay].Values + (long)elapsedSeconds,
                            };
                            continue;
                        }

                        if (series1[iDay].Values == 0)
                        {
                            series1[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = (long)elapsedSeconds,
                            };
                            continue;
                        }
                        if (series2[iDay].Values == 0)
                        {
                            hasData2 = true;
                            series2[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = (long)elapsedSeconds,
                            };
                            continue;
                        }
                        if (series3[iDay].Values == 0)
                        {
                            hasData3 = true;
                            series3[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = (long)elapsedSeconds,
                            };
                            continue;
                        }
                        if (series4[iDay].Values == 0)
                        {
                            hasData4 = true;
                            series4[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = (long)elapsedSeconds,
                            };
                            continue;
                        }
                        if (series5[iDay].Values == 0)
                        {
                            hasData5 = true;
                            series5[iDay] = new CustomerForTime
                            {
                                Name = displayName,
                                Values = (long)elapsedSeconds,
                            };
                            continue;
                        }
                    }
                }

                SeriesCollection activityForGameSeries = new SeriesCollection();

                if (cumulSessions)
                {
                    activityForGameSeries.Add(
                        new ColumnSeries
                        {
                            Title = "1",
                            Values = series1,
                            Fill = PluginDatabase.PluginSettings.ChartColors,
                        }
                    );
                }
                else
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = series1 });
                }

                if (hasData2)
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "2", Values = series2 });
                }
                if (hasData3)
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "3", Values = series3 });
                }
                if (hasData4)
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "4", Values = series4 });
                }
                if (hasData5)
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "5", Values = series5 });
                }

                for (int iDay = 0; iDay < listDate.Length; iDay++)
                {
                    listDate[iDay] = Convert
                        .ToDateTime(listDate[iDay])
                        .ToString(Constants.DateUiFormat);
                }

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers
                    .Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter =
                    new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter = value =>
                    (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = cumulSessions
                    ? (System.Windows.Controls.UserControl)
                        new CustomerToolTipForTime
                        {
                            ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons,
                            Mode =
                                PluginDatabase.PluginSettings.ModeStoreIcon == 1
                                    ? TextBlockWithIconMode.IconTextFirstWithText
                                    : TextBlockWithIconMode.IconFirstWithText,
                        }
                    : new CustomerToolTipForMultipleTime { ShowTitle = false };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = listDate;

                UpdateNavBarBounds();

                ControlDataContext.NavLabel = PluginChartNavBar.BuildRangeLabel(listDate);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Builds and assigns the LiveCharts series collection for the weekly aggregated session window.
        /// </summary>
        /// <param name="gameActivities">The game activities data object containing session logs.</param>
        /// <param name="variateurTime">The signed offset applied to the session window anchor (in weeks).</param>
        /// <param name="limit">The maximum number of weeks to render in one page.</param>
        private void GetActivityForGamesChartByWeek(
            GameActivities gameActivities,
            int variateurTime = 0,
            int limit = 9
        )
        {
            try
            {
                List<Activity> activities = Serialization.GetClone(gameActivities.FilterItems);

                DateTime dtFirstActivity = activities
                    .Where(x => x.DateSession != null)
                    .Select(x => (DateTime)x.DateSession?.ToLocalTime())
                    .Min();

                DateTime dtLastActivity = activities
                    .Where(x => x.DateSession != null)
                    .Select(x => (DateTime)x.DateSession?.ToLocalTime())
                    .Max();

                // Anchor both extremes to their Monday so week boundaries are consistent
                // regardless of the day-of-week of the first/last session.
                DateTime firstMonday = dtFirstActivity.StartOfWeek(DayOfWeek.Monday);
                DateTime lastMonday = dtLastActivity.StartOfWeek(DayOfWeek.Monday);

                // Total weeks = number of distinct Monday-anchored weeks between first and last session.
                int totalWeeks = (int)Math.Round((lastMonday - firstMonday).TotalDays / 7.0) + 1;
                totalWeeks = totalWeeks < 1 ? 1 : totalWeeks;

                int resolvedLimit;
                DateTime pivotMonday;

                // ShowAllData passes limit = Count: span every week from first to last session.
                if (limit == int.MaxValue || limit >= activities.Count)
                {
                    resolvedLimit = totalWeeks - 1; // loop goes from resolvedLimit down to 0 → resolvedLimit+1 labels
                    pivotMonday = lastMonday;
                    // variateurTime intentionally ignored in ShowAllData mode.
                }
                else
                {
                    resolvedLimit = limit;

                    // Clamp only the positive side — the negative side is handled by Prev() via _totalDataPoints.
                    variateurTime = Math.Min(0, variateurTime);

                    // Shift the pivot week by variateurTime weeks (negative = older).
                    pivotMonday = lastMonday.AddDays(7 * variateurTime);
                }

                List<string> labels = new List<string>();
                ChartValues<CustomerForTime> seriesData = new ChartValues<CustomerForTime>();
                List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();

                for (int i = resolvedLimit; i >= 0; i--)
                {
                    // Each step is exactly one week back from the pivot Monday.
                    DateTime weekMonday = pivotMonday.AddDays(-7 * i);
                    DateTime weekSunday = weekMonday
                        .AddDays(6)
                        .AddHours(23)
                        .AddMinutes(59)
                        .AddSeconds(59);
                    int weekNumber = UtilityTools.WeekOfYearISO8601(weekMonday);

                    string label = string.Format(
                        "{0} {1}",
                        ResourceProvider.GetString("LOCGameActivityWeekLabel"),
                        weekNumber
                    );

                    labels.Add(label);
                    datesPeriodes.Add(
                        new WeekStartEnd { Monday = weekMonday, Sunday = weekSunday }
                    );
                    seriesData.Add(
                        new CustomerForTime
                        {
                            Name = label,
                            Values = 0,
                            HideIsZero = true,
                        }
                    );
                }

                // _totalDataPoints = navigable week count across the full dataset.
                // _lastWindowSize  = columns in the current page.
                _totalDataPoints = totalWeeks;
                _lastWindowSize = labels.Count;

                activities.ForEach(x =>
                {
                    DateTime sessionTime = ((DateTime)x.DateSession).ToLocalTime();
                    int idx = datesPeriodes.FindIndex(y =>
                        y.Monday <= sessionTime && y.Sunday >= sessionTime
                    );
                    if (idx > -1)
                    {
                        seriesData[idx].Values += (long)x.ElapsedSeconds;
                    }
                });

                SeriesCollection activityForGameSeries = new SeriesCollection();
                activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = seriesData });

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers
                    .Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter =
                    new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter = value =>
                    (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForTime
                {
                    ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons,
                    Mode =
                        PluginDatabase.PluginSettings.ModeStoreIcon == 1
                            ? TextBlockWithIconMode.IconTextFirstWithText
                            : TextBlockWithIconMode.IconFirstWithText,
                    DatesPeriodes = datesPeriodes,
                    ShowWeekPeriode = true,
                    ShowTitle = true,
                };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = labels;

                UpdateNavBarBounds();

                ControlDataContext.NavLabel = PluginChartNavBar.BuildRangeLabel(labels.ToArray());
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Pushes the current navigation bounds into the nav bar after a chart rebuild.
        /// Extracted to avoid duplicating the same three lines in both chart builders.
        /// </summary>
        private void UpdateNavBarBounds()
        {
            if (PART_NavBar == null)
            {
                return;
            }

            int windowSize = _lastWindowSize;
            int minVariator = _totalDataPoints > windowSize ? -(_totalDataPoints - windowSize) : 0;

            PART_NavBar.AxisLimitMaximum = _totalDataPoints;
            PART_NavBar.CanGoNext = AxisVariator < 0;
            PART_NavBar.CanGoPrev = AxisVariator > minVariator;
        }

        // ────────────────────────────────────────────────────────────────────
        // Chart events
        // ────────────────────────────────────────────────────────────────────

        #region Events

        /// <summary>
        /// Invokes the <see cref="GameSeriesDataClick"/> event when a chart data point is clicked.
        /// </summary>
        private void PART_ChartTimeActivity_DataClick(object sender, ChartPoint chartPoint)
        {
            GameSeriesDataClick?.Invoke(this, chartPoint);
        }

        #endregion
    }

    // ────────────────────────────────────────────────────────────────────────
    // DataContext
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Observable ViewModel backing <see cref="PluginChartTime"/>.
    /// </summary>
    public class PluginChartTimeDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;

        /// <summary>Controls whether the chart control is visible.</summary>
        public bool IsActivated
        {
            get => _isActivated;
            set => SetValue(ref _isActivated, value);
        }

        private double _chartTimeHeight;

        /// <summary>Explicit pixel height of the chart; <see cref="double.NaN"/> for auto-size.</summary>
        public double ChartTimeHeight
        {
            get => _chartTimeHeight;
            set => SetValue(ref _chartTimeHeight, value);
        }

        private bool _chartTimeAxis;

        /// <summary>When true, the X-axis labels strip is visible.</summary>
        public bool ChartTimeAxis
        {
            get => _chartTimeAxis;
            set => SetValue(ref _chartTimeAxis, value);
        }

        private bool _chartTimeOrdinates;

        /// <summary>When true, the Y-axis labels strip is visible.</summary>
        public bool ChartTimeOrdinates
        {
            get => _chartTimeOrdinates;
            set => SetValue(ref _chartTimeOrdinates, value);
        }

        private bool _chartTimeVisibleEmpty;

        /// <summary>When true, the chart placeholder is shown even when there is no data.</summary>
        public bool ChartTimeVisibleEmpty
        {
            get => _chartTimeVisibleEmpty;
            set => SetValue(ref _chartTimeVisibleEmpty, value);
        }

        private bool _disableAnimations = true;

        /// <summary>Mirrors <see cref="PluginChartTime.DisableAnimations"/>.</summary>
        public bool DisableAnimations
        {
            get => _disableAnimations;
            set => SetValue(ref _disableAnimations, value);
        }

        private double _labelsRotationValue;

        /// <summary>Rotation angle applied to X-axis labels (0 or 160 degrees).</summary>
        public double LabelsRotationValue
        {
            get => _labelsRotationValue;
            set => SetValue(ref _labelsRotationValue, value);
        }

        private bool _showNavBar;

        /// <summary>Drives <see cref="PluginChartNavBar.ShowNavBar"/> binding in XAML.</summary>
        public bool ShowNavBar
        {
            get => _showNavBar;
            set => SetValue(ref _showNavBar, value);
        }

        private bool _showAllData;

        /// <summary>
        /// Mirrored into <see cref="PluginChartNavBar.ShowAllData"/>.
        /// When true, the chart bypasses the axis window and renders the full dataset.
        /// Reset to false on every game context change.
        /// </summary>
        public bool ShowAllData
        {
            get => _showAllData;
            set => SetValue(ref _showAllData, value);
        }

        private string _navLabel = string.Empty;

        /// <summary>
        /// Badge text shown on the right of the nav bar representing the visible X-axis period.
        /// Reset to <see cref="string.Empty"/> on every game context change.
        /// </summary>
        public string NavLabel
        {
            get => _navLabel;
            set => SetValue(ref _navLabel, value);
        }

        private int _pageSize;

        /// <summary>
        /// Mirror of the effective chart abscissa limit.
        /// Bound to <see cref="PluginChartNavBar.PageSize"/> so the nav bar can show/hide
        /// PrevPage/NextPage buttons and build their tooltips.
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetValue(ref _pageSize, value);
        }

        private int _axisLimit;

        /// <summary>
        /// Mirror of <see cref="PluginChartTime.AxisLimit"/>.
        /// Kept in sync by <see cref="PluginChartTime.SetDefaultDataContext"/> and
        /// <see cref="PluginChartTime.ApplyAxisLimitFromNavBar"/>.
        /// </summary>
        public int AxisLimit
        {
            get => _axisLimit;
            set => SetValue(ref _axisLimit, value);
        }
    }
}