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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace GameActivity.Controls
{
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
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>When true, data is aggregated and displayed by calendar week instead of by day.</summary>
        public bool ShowByWeeks
        {
            get => (bool)GetValue(ShowByWeeksProperty);
            set => SetValue(ShowByWeeksProperty, value);
        }
        public static readonly DependencyProperty ShowByWeeksProperty = DependencyProperty.Register(
            nameof(ShowByWeeks), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

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
            nameof(Truncate), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>When true, X-axis labels are rotated 160° to avoid overlap on dense charts.</summary>
        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }
        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// Maximum number of X-axis items (days or weeks) rendered in one page.
        /// When 0, falls back to <c>PluginSettings.ChartTimeCountAbscissa</c>.
        /// Registered as a real DP (not a plain property) so that external XAML bindings
        /// and <see cref="ControlsPropertyChangedCallback"/> react to changes correctly.
        /// </summary>
        public int AxisLimit
        {
            get => (int)GetValue(AxisLimitProperty);
            set => SetValue(AxisLimitProperty, value);
        }
        public static readonly DependencyProperty AxisLimitProperty = DependencyProperty.Register(
            nameof(AxisLimit), typeof(int), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        /// <summary>
        /// Signed offset applied to the most-recent-window anchor.
        /// Negative values scroll the view toward older data; 0 always shows the most recent page.
        /// Modified by mouse-wheel and by the nav bar buttons.
        /// </summary>
        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }
        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator), typeof(int), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

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
            nameof(ShowNavBar), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage nav bar buttons.
        /// Should be bound to the same value used as the chart's abscissa limit
        /// (<see cref="AxisLimit"/> when set, otherwise <c>PluginSettings.ChartTimeCountAbscissa</c>).
        /// When &lt;= 0, the PrevPage/NextPage buttons are hidden in the nav bar.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize), typeof(int), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────────────

        public PluginChartTime()
        {
            AlwaysShow = true;
            InitializeComponent();
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (PART_NavBar != null)
            {
                // Observe AxisLimit on the nav bar directly — more reliable than RoutedEvents
                // because no XAML event wiring is required in the consumer's template.
                DependencyPropertyDescriptor
                    .FromProperty(PluginChartNavBar.AxisLimitProperty, typeof(PluginChartNavBar))
                    .AddValueChanged(PART_NavBar, OnNavBarAxisLimitChanged);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (PART_NavBar != null)
            {
                // Unsubscribe to prevent the descriptor from keeping a reference to this
                // control alive after it has been removed from the visual tree.
                DependencyPropertyDescriptor
                    .FromProperty(PluginChartNavBar.AxisLimitProperty, typeof(PluginChartNavBar))
                    .RemoveValueChanged(PART_NavBar, OnNavBarAxisLimitChanged);
            }
        }

        private void OnNavBarAxisLimitChanged(object sender, EventArgs e)
        {
            ApplyAxisLimitFromNavBar();
        }

        // ────────────────────────────────────────────────────────────────────
        // Static event registration
        // ────────────────────────────────────────────────────────────────────

        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();
                // NOTE: Games.ItemUpdated intentionally absent — handled by base via OnStaticGamesItemUpdated.
            });
        }

        protected override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Truncate = PluginDatabase.PluginSettings.ChartTimeTruncate;
            GameContextChanged(null, GameContext);
        }

        // ────────────────────────────────────────────────────────────────────
        // DataContext initialisation
        // ────────────────────────────────────────────────────────────────────

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
            ControlDataContext.ChartTimeVisibleEmpty = PluginDatabase.PluginSettings.ChartTimeVisibleEmpty;
            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotation ? 160d : 0d;

            // ── Nav bar defaults ───────────────────────────────────────────
            bool showNavBar = ShowNavBar;
            if (IgnoreSettings) { showNavBar = true; }

            ControlDataContext.ShowNavBar = showNavBar;
            ControlDataContext.ShowAllData = false; // Always reset on context change.
            ControlDataContext.NavLabel = string.Empty;

            // ── Resolve effective abscissa limit ───────────────────────────
            // Priority: explicit AxisLimit DP → plugin setting.
            int effectivePageSize = AxisLimit > 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;

            ControlDataContext.PageSize = effectivePageSize;
            ControlDataContext.AxisLimit = AxisLimit;

            // Push the current AxisLimit into the nav bar so its AxisLimitDecrease
            // button starts with the correct floor check and tooltip.
            if (PART_NavBar != null)
            {
                PART_NavBar.AxisLimit = AxisLimit;
            }
            // ── End nav bar defaults ───────────────────────────────────────

            PART_ChartTimeActivity.Series = null;
            PART_ChartTimeActivityLabelsX.Labels = null;
        }

        // ────────────────────────────────────────────────────────────────────
        // Data loading
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the display limit and delegates to the appropriate chart builder.
        /// When <see cref="PluginChartTimeDataContext.ShowAllData"/> is active the axis window is
        /// bypassed and the full dataset is rendered regardless of <see cref="AxisLimit"/> /
        /// <see cref="AxisVariator"/>.
        /// </summary>
        public override void SetData(Game newContext, PluginGameEntry pluginGameData)
        {
            GameActivities gameActivities = (GameActivities)pluginGameData;

            MustDisplay = !IgnoreSettings && !ControlDataContext.ChartTimeVisibleEmpty
                ? gameActivities.HasData
                : true;

            if (!MustDisplay) { return; }

            ControlDataContext.ChartTimeAxis = !ControlDataContext.ShowAllData;

            // ── ShowAllData mode: render the complete dataset ──────────────
            if (ControlDataContext.ShowAllData)
            {
                if (ShowByWeeks)
                {
                    GetActivityForGamesChartByWeek(gameActivities, 0, Convert.ToInt32(gameActivities.Count));
                }
                else
                {
                    GetActivityForGamesTimeGraphics(gameActivities, 0, Convert.ToInt32(gameActivities.Count));
                }

                return;
            }
            // ── End ShowAllData ────────────────────────────────────────────

            int limit = AxisLimit != 0 ? AxisLimit : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            int axisVariator = AxisVariator;

            if (ShowByWeeks)
                GetActivityForGamesChartByWeek(gameActivities, axisVariator, limit);
            else
                GetActivityForGamesTimeGraphics(gameActivities, axisVariator, limit);
        }

        // ────────────────────────────────────────────────────────────────────
        // Public navigation API
        // ────────────────────────────────────────────────────────────────────

        #region Public methods

        /// <summary>Advances the axis window forward by <paramref name="value"/> steps.</summary>
        public void Next(int value = 1) { AxisVariator += value; }

        /// <summary>Moves the axis window backward by <paramref name="value"/> steps.</summary>
        public void Prev(int value = 1) { AxisVariator -= value; }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Nav bar event handlers
        // ────────────────────────────────────────────────────────────────────

        #region Nav bar event handlers

        // Each handler translates a PluginChartNavBar RoutedEvent into a chart action.
        // Next() / Prev() modify AxisVariator which triggers ControlsPropertyChangedCallback
        // → GameContextChanged → SetData, so no explicit refresh call is needed here.

        private void NavBar_FirstClicked(object sender, RoutedEventArgs e)
        {
            // Large negative value; the builder clamps it locally to the actual
            // leftmost valid position without writing back to AxisVariator.
            AxisVariator = int.MinValue / 2;
        }

        private void NavBar_PagePrevClicked(object sender, RoutedEventArgs e)
        {
            // Skip back by a full page. Delta resolved here — the nav bar carries
            // PageSize only for display/tooltip purposes.
            int pageSize = AxisLimit > 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            Prev(pageSize);
        }

        private void NavBar_PrevClicked(object sender, RoutedEventArgs e)
        {
            Prev();
        }

        private void NavBar_ShowAllToggled(object sender, RoutedEventArgs e)
        {
            // Mirror the nav bar's new ShowAllData state into the DataContext, then
            // trigger a full reload so the chart re-renders with the complete dataset
            // (or returns to the windowed view when ShowAllData is turned off).
            ControlDataContext.ShowAllData = PART_NavBar.ShowAllData;
            GameContextChanged(null, GameContext);
        }

        private void NavBar_NextClicked(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void NavBar_PageNextClicked(object sender, RoutedEventArgs e)
        {
            // Skip forward by a full page.
            int pageSize = AxisLimit > 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartTimeCountAbscissa;
            Next(pageSize);
        }

        private void NavBar_LastClicked(object sender, RoutedEventArgs e)
        {
            // AxisVariator = 0 always shows the most recent window.
            AxisVariator = 0;
        }

        /// <summary>
        /// Reads the updated <see cref="PluginChartNavBar.AxisLimit"/> from the nav bar,
        /// pushes it into the control's own <see cref="AxisLimit"/> DP and refreshes
        /// <see cref="ControlDataContext.PageSize"/> so the nav bar tooltip stays accurate.
        /// </summary>
        /// <remarks>
        /// We cannot rely on ControlsPropertyChangedCallback to trigger the refresh here:
        /// if the nav bar AxisLimit and the local AxisLimit DP already hold the same integer
        /// value (e.g. both 0→1→1), WPF considers the DP unchanged and skips the callback.
        /// GameContextChanged is therefore called explicitly after every limit mutation.
        /// </remarks>
        private void ApplyAxisLimitFromNavBar()
        {
            int newLimit = PART_NavBar.AxisLimit;

            // Resolve effective page size before touching the DP so we can compare.
            int effectivePageSize = newLimit > 0
                ? newLimit
                : PluginDatabase.PluginSettings.ChartTimeCountAbscissa; // ChartLogCountAbscissa pour ChartLog

            // Update DataContext and nav bar PageSize regardless of whether AxisLimit changed.
            ControlDataContext.PageSize = effectivePageSize;
            ControlDataContext.AxisLimit = newLimit;
            PART_NavBar.PageSize = effectivePageSize;

            // Assign the DP. If the value is genuinely new this also fires
            // ControlsPropertyChangedCallback, but we call GameContextChanged
            // unconditionally below to handle the equal-value edge case.
            AxisLimit = newLimit;

            // Force chart refresh — SetData will pick up the new AxisLimit from the DP.
            GameContextChanged(null, GameContext);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Chart builders
        // ────────────────────────────────────────────────────────────────────

        private void GetActivityForGamesTimeGraphics(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            try
            {
                if (gameActivities?.FilterItems == null) { return; }

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

                    if (dtList.Count > limit && variateurTime != 0)
                    {
                        if (variateurTime > 0)
                        {
                            // Clamp locally: variator past the right edge → pin to most recent page.
                            // Do NOT write back to AxisVariator — that would re-trigger GameContextChanged.
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
                    int newLimit = (countActivities - limit - 1) >= 0 ? countActivities - limit - 1 : 0;
                    listDate = new string[countActivities - newLimit];

                    for (int i = newLimit; i < countActivities; i++)
                    {
                        string dt = dtList[i];
                        listDate[i - newLimit] = dt;

                        CustomerForTime placeholder = new CustomerForTime { Name = dt, Values = 0, HideIsZero = true };
                        series1.Add(placeholder);
                        series2.Add(placeholder);
                        series3.Add(placeholder);
                        series4.Add(placeholder);
                        series5.Add(placeholder);
                    }
                }
                else
                {
                    // Find the most recent session date, then offset it by variateurTime days.
                    DateTime dateStart = new DateTime(1982, 12, 15, 0, 0, 0);
                    foreach (Activity activity in activities)
                    {
                        DateTime dateSession = Convert.ToDateTime(activity.DateSession?.ToLocalTime());
                        if (dateSession > dateStart) { dateStart = dateSession; }
                    }
                    dateStart = dateStart.AddDays(variateurTime);

                    // ShowAllData passes limit = Count (large value): span the entire date range.
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
                            CustomerForTime placeholder = new CustomerForTime { Name = dateStr, Values = 0, HideIsZero = true };
                            series1.Add(placeholder);
                            series2.Add(placeholder);
                            series3.Add(placeholder);
                            series4.Add(placeholder);
                            series5.Add(placeholder);
                        }
                    }
                    else
                    {
                        listDate = new string[limit + 1];
                        for (int i = limit; i >= 0; i--)
                        {
                            string dateStr = dateStart.AddDays(-i).ToString("yyyy-MM-dd");
                            listDate[limit - i] = dateStr;

                            CustomerForTime placeholder = new CustomerForTime { Name = dateStr, Values = 0, HideIsZero = true };
                            series1.Add(placeholder);
                            series2.Add(placeholder);
                            series3.Add(placeholder);
                            series4.Add(placeholder);
                            series5.Add(placeholder);
                        }
                    }
                }

                LocalDateConverter localDateConverter = new LocalDateConverter();
                bool cumulSessions = PluginDatabase.PluginSettings.CumulPlaytimeSession;
                int effectiveLimit = listDate.Length - 1;

                for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                {
                    ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                    string dateSession = Convert.ToDateTime(activities[iActivity].DateSession)
                        .ToLocalTime().ToString("yyyy-MM-dd");

                    for (int iDay = effectiveLimit; iDay >= 0; iDay--)
                    {
                        if (listDate[iDay] != dateSession) { continue; }

                        string displayName = series1[iDay].Name;
                        try
                        {
                            displayName = (string)localDateConverter.Convert(
                                DateTime.ParseExact(series1[iDay].Name, "yyyy-MM-dd", null).ToLocalTime(),
                                null, null, CultureInfo.CurrentCulture);
                        }
                        catch { }

                        if (cumulSessions)
                        {
                            series1[iDay] = new CustomerForTime { Name = displayName, Values = series1[iDay].Values + (long)elapsedSeconds };
                            continue;
                        }

                        if (series1[iDay].Values == 0) { series1[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series2[iDay].Values == 0) { hasData2 = true; series2[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series3[iDay].Values == 0) { hasData3 = true; series3[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series4[iDay].Values == 0) { hasData4 = true; series4[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series5[iDay].Values == 0) { hasData5 = true; series5[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                    }
                }

                SeriesCollection activityForGameSeries = new SeriesCollection();

                if (cumulSessions)
                {
                    activityForGameSeries.Add(new ColumnSeries
                    {
                        Title = "1",
                        Values = series1,
                        Fill = PluginDatabase.PluginSettings.ChartColors
                    });
                }
                else
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = series1 });
                }

                if (hasData2) { activityForGameSeries.Add(new ColumnSeries { Title = "2", Values = series2 }); }
                if (hasData3) { activityForGameSeries.Add(new ColumnSeries { Title = "3", Values = series3 }); }
                if (hasData4) { activityForGameSeries.Add(new ColumnSeries { Title = "4", Values = series4 }); }
                if (hasData5) { activityForGameSeries.Add(new ColumnSeries { Title = "5", Values = series5 }); }

                // Convert ISO internal dates to localised short date strings for the X-axis.
                for (int iDay = 0; iDay < listDate.Length; iDay++)
                {
                    listDate[iDay] = Convert.ToDateTime(listDate[iDay]).ToString(Constants.DateUiFormat);
                }

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter =
                    value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = cumulSessions
                    ? (System.Windows.Controls.UserControl)new CustomerToolTipForTime
                    {
                        ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons,
                        Mode = PluginDatabase.PluginSettings.ModeStoreIcon == 1
                            ? TextBlockWithIconMode.IconTextFirstWithText
                            : TextBlockWithIconMode.IconFirstWithText
                    }
                    : new CustomerToolTipForMultipleTime { ShowTitle = false };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = listDate;

                // Update the nav bar range badge with the now-localised X-axis labels.
                ControlDataContext.NavLabel = PluginChartNavBar.BuildRangeLabel(listDate);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void GetActivityForGamesChartByWeek(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            try
            {
                List<Activity> activities = Serialization.GetClone(gameActivities.FilterItems);

                DateTime dtLastActivity = activities
                    .Where(x => x.DateSession != null)
                    .Select(x => (DateTime)x.DateSession?.ToLocalTime())
                    .Max();

                int resolvedLimit;

                // ShowAllData passes limit = Count: span every week from first to last session.
                if (limit == int.MaxValue || limit >= activities.Count)
                {
                    DateTime dtFirstActivity = activities
                        .Where(x => x.DateSession != null)
                        .Select(x => (DateTime)x.DateSession?.ToLocalTime())
                        .Min();

                    resolvedLimit = (int)Math.Ceiling((dtLastActivity - dtFirstActivity).TotalDays / 7.0);
                    resolvedLimit = resolvedLimit < 1 ? 1 : resolvedLimit;
                    // variateurTime intentionally ignored in ShowAllData mode.
                }
                else
                {
                    resolvedLimit = limit;

                    // Clamp variateurTime locally — writing back to AxisVariator from inside
                    // a builder would re-fire ControlsPropertyChangedCallback → infinite loop.
                    int minVariator = -resolvedLimit;
                    int maxVariator = 0;
                    variateurTime = Math.Max(minVariator, Math.Min(maxVariator, variateurTime));

                    dtLastActivity = dtLastActivity.AddDays(7 * variateurTime);
                }

                List<string> labels = new List<string>();
                ChartValues<CustomerForTime> seriesData = new ChartValues<CustomerForTime>();
                List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();

                for (int i = resolvedLimit; i >= 0; i--)
                {
                    DateTime dt = dtLastActivity.AddDays(-7 * i).ToLocalTime();
                    int weekNumber = UtilityTools.WeekOfYearISO8601(dt);
                    DateTime first = dt.StartOfWeek(DayOfWeek.Monday);

                    string dataTitleInfo = string.Format("{0} {1}",
                        ResourceProvider.GetString("LOCGameActivityWeekLabel"),
                        weekNumber);

                    labels.Add(dataTitleInfo);
                    datesPeriodes.Add(new WeekStartEnd
                    {
                        Monday = first,
                        Sunday = first.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59)
                    });
                    seriesData.Add(new CustomerForTime { Name = dataTitleInfo, Values = 0, HideIsZero = true });
                }

                activities.ForEach(x =>
                {
                    DateTime sessionTime = ((DateTime)x.DateSession).ToLocalTime();
                    int idx = datesPeriodes.FindIndex(y => y.Monday <= sessionTime && y.Sunday >= sessionTime);
                    if (idx > -1) { seriesData[idx].Values += (long)x.ElapsedSeconds; }
                });

                SeriesCollection activityForGameSeries = new SeriesCollection();
                activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = seriesData });

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter =
                    value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForTime
                {
                    ShowIcon = PluginDatabase.PluginSettings.ShowLauncherIcons,
                    Mode = PluginDatabase.PluginSettings.ModeStoreIcon == 1
                        ? TextBlockWithIconMode.IconTextFirstWithText
                        : TextBlockWithIconMode.IconFirstWithText,
                    DatesPeriodes = datesPeriodes,
                    ShowWeekPeriode = true,
                    ShowTitle = true
                };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = labels;

                // Update the nav bar range badge (first week – last week).
                ControlDataContext.NavLabel = PluginChartNavBar.BuildRangeLabel(labels.ToArray());
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Chart events
        // ────────────────────────────────────────────────────────────────────

        #region Events

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
        // ── Activation / layout ───────────────────────────────────────────────

        private bool _isActivated;
        /// <summary>Controls whether the chart control is visible.</summary>
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private double _chartTimeHeight;
        /// <summary>Explicit pixel height of the chart; <see cref="double.NaN"/> for auto-size.</summary>
        public double ChartTimeHeight { get => _chartTimeHeight; set => SetValue(ref _chartTimeHeight, value); }

        private bool _chartTimeAxis;
        /// <summary>When true, the X-axis labels strip is visible.</summary>
        public bool ChartTimeAxis { get => _chartTimeAxis; set => SetValue(ref _chartTimeAxis, value); }

        private bool _chartTimeOrdinates;
        /// <summary>When true, the Y-axis labels strip is visible.</summary>
        public bool ChartTimeOrdinates { get => _chartTimeOrdinates; set => SetValue(ref _chartTimeOrdinates, value); }

        private bool _chartTimeVisibleEmpty;
        /// <summary>When true, the chart placeholder is shown even when there is no data.</summary>
        public bool ChartTimeVisibleEmpty { get => _chartTimeVisibleEmpty; set => SetValue(ref _chartTimeVisibleEmpty, value); }

        // ── Chart options ─────────────────────────────────────────────────────

        private bool _disableAnimations = true;
        /// <summary>Mirrors <see cref="PluginChartTime.DisableAnimations"/>.</summary>
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        private double _labelsRotationValue;
        /// <summary>Rotation angle applied to X-axis labels (0 or 160 degrees).</summary>
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }

        // ── Nav bar state ─────────────────────────────────────────────────────

        private bool _showNavBar;
        /// <summary>Drives <see cref="PluginChartNavBar.ShowNavBar"/> binding in XAML.</summary>
        public bool ShowNavBar { get => _showNavBar; set => SetValue(ref _showNavBar, value); }

        private bool _showAllData;
        /// <summary>
        /// Mirrored into <see cref="PluginChartNavBar.ShowAllData"/>.
        /// When true, the chart bypasses the axis window and renders the full dataset.
        /// Reset to false on every game context change.
        /// </summary>
        public bool ShowAllData { get => _showAllData; set => SetValue(ref _showAllData, value); }

        private string _navLabel = string.Empty;
        /// <summary>
        /// Badge text shown on the right of the nav bar representing the visible X-axis period.
        /// In day mode: localised short dates. In week mode: week labels.
        /// Reset to <see cref="string.Empty"/> on every game context change.
        /// </summary>
        public string NavLabel { get => _navLabel; set => SetValue(ref _navLabel, value); }

        private int _pageSize;
        /// <summary>
        /// Mirror of the effective chart abscissa limit.
        /// Bound to <see cref="PluginChartNavBar.PageSize"/> so the nav bar can show/hide
        /// PrevPage/NextPage buttons and build their tooltips.
        /// </summary>
        public int PageSize { get => _pageSize; set => SetValue(ref _pageSize, value); }

        private int _axisLimit;
        /// <summary>
        /// Mirror of <see cref="PluginChartTime.AxisLimit"/>.
        /// Kept in sync by <see cref="PluginChartTime.SetDefaultDataContext"/> and
        /// <see cref="PluginChartTime.ApplyAxisLimitFromNavBar"/> so XAML bindings can
        /// display or react to the current limit.
        /// </summary>
        public int AxisLimit { get => _axisLimit; set => SetValue(ref _axisLimit, value); }
    }
}
