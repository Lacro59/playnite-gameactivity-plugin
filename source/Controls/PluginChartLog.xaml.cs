using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GameActivity.Controls
{
    public partial class PluginChartLog : PluginUserControlExtend
    {
        // ──────────────────────────────────────────────────────────────────────
        // Plugin database wiring
        // ──────────────────────────────────────────────────────────────────────

        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartLogDataContext ControlDataContext = new PluginChartLogDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartLogDataContext)value;
        }

        // ── LiveCharts series fields ───────────────────────────────────────────
        private LineSeries _cpuSeries;
        private LineSeries _gpuSeries;
        private LineSeries _ramSeries;
        private LineSeries _fpsSeries;
        private LineSeries _cpuTSeries;
        private LineSeries _gpuTSeries;
        private LineSeries _cpuPSeries;
        private LineSeries _gpuPSeries;

        // ──────────────────────────────────────────────────────────────────────
        // Dependency properties
        // ──────────────────────────────────────────────────────────────────────

        #region Properties

        /// <summary>When true, disables LiveCharts entry animations.</summary>
        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>When true, X-axis labels are rotated 160° to avoid overlap on dense charts.</summary>
        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }
        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// Maximum number of X-axis log entries rendered in one page.
        /// When 0, falls back to <c>PluginSettings.ChartLogCountAbscissa</c>.
        /// Registered as a real DP so that external XAML bindings and
        /// <see cref="ControlsPropertyChangedCallback"/> react to changes correctly.
        /// </summary>
        public int AxisLimit
        {
            get => (int)GetValue(AxisLimitProperty);
            set => SetValue(AxisLimitProperty, value);
        }
        public static readonly DependencyProperty AxisLimitProperty = DependencyProperty.Register(
            nameof(AxisLimit), typeof(int), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        /// <summary>Selected session date used to filter the log data shown by the chart.</summary>
        public DateTime? DateSelected
        {
            get => (DateTime?)GetValue(DateSelectedProperty);
            set => SetValue(DateSelectedProperty, value);
        }
        public static readonly DependencyProperty DateSelectedProperty = DependencyProperty.Register(
            nameof(DateSelected), typeof(DateTime?), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        /// <summary>Optional title injected into the chart header / tooltip.</summary>
        public string TitleChart
        {
            get => (string)GetValue(TitleChartProperty);
            set => SetValue(TitleChartProperty, value);
        }
        public static readonly DependencyProperty TitleChartProperty = DependencyProperty.Register(
            nameof(TitleChart), typeof(string), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));

        /// <summary>
        /// Signed offset applied to the session window anchor.
        /// Negative values scroll toward older entries; 0 shows the most recent page.
        /// Modified by the nav bar buttons.
        /// </summary>
        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }
        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator), typeof(int), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        // ── Sensor toggles ────────────────────────────────────────────────────

        /// <summary>Controls visibility of the CPU usage series.</summary>
        public bool DisplayCpu
        {
            get => (bool)GetValue(DisplayCpuProperty);
            set => SetValue(DisplayCpuProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuProperty = DependencyProperty.Register(
            nameof(DisplayCpu), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the GPU usage series.</summary>
        public bool DisplayGpu
        {
            get => (bool)GetValue(DisplayGpuProperty);
            set => SetValue(DisplayGpuProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuProperty = DependencyProperty.Register(
            nameof(DisplayGpu), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the RAM usage series.</summary>
        public bool DisplayRam
        {
            get => (bool)GetValue(DisplayRamProperty);
            set => SetValue(DisplayRamProperty, value);
        }
        public static readonly DependencyProperty DisplayRamProperty = DependencyProperty.Register(
            nameof(DisplayRam), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the FPS series.</summary>
        public bool DisplayFps
        {
            get => (bool)GetValue(DisplayFpsProperty);
            set => SetValue(DisplayFpsProperty, value);
        }
        public static readonly DependencyProperty DisplayFpsProperty = DependencyProperty.Register(
            nameof(DisplayFps), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the CPU temperature series.</summary>
        public bool DisplayCpuT
        {
            get => (bool)GetValue(DisplayCpuTProperty);
            set => SetValue(DisplayCpuTProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuTProperty = DependencyProperty.Register(
            nameof(DisplayCpuT), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the GPU temperature series.</summary>
        public bool DisplayGpuT
        {
            get => (bool)GetValue(DisplayGpuTProperty);
            set => SetValue(DisplayGpuTProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuTProperty = DependencyProperty.Register(
            nameof(DisplayGpuT), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the CPU power series.</summary>
        public bool DisplayCpuP
        {
            get => (bool)GetValue(DisplayCpuPProperty);
            set => SetValue(DisplayCpuPProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuPProperty = DependencyProperty.Register(
            nameof(DisplayCpuP), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>Controls visibility of the GPU power series.</summary>
        public bool DisplayGpuP
        {
            get => (bool)GetValue(DisplayGpuPProperty);
            set => SetValue(DisplayGpuPProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuPProperty = DependencyProperty.Register(
            nameof(DisplayGpuP), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// When true: bypasses AxisVariator/limit pagination and renders the entire
        /// session dataset. Also forces all 8 series visible regardless of individual
        /// Display* flags. Driven by the nav bar toggle or by an external XAML binding.
        /// </summary>
        public bool ShowAllData
        {
            get => (bool)GetValue(ShowAllDataProperty);
            set => SetValue(ShowAllDataProperty, value);
        }
        public static readonly DependencyProperty ShowAllDataProperty = DependencyProperty.Register(
            nameof(ShowAllData), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// Shows or hides the <see cref="PluginChartNavBar"/> above the filter bar.
        /// Defaults to false so existing usages without a nav bar are unaffected.
        /// </summary>
        public bool ShowNavBar
        {
            get => (bool)GetValue(ShowNavBarProperty);
            set => SetValue(ShowNavBarProperty, value);
        }
        public static readonly DependencyProperty ShowNavBarProperty = DependencyProperty.Register(
            nameof(ShowNavBar), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage nav bar buttons.
        /// Should match the effective abscissa limit (<see cref="AxisLimit"/> when set,
        /// otherwise <c>PluginSettings.ChartLogCountAbscissa</c>).
        /// When &lt;= 0, PrevPage/NextPage buttons are hidden in the nav bar.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize), typeof(int), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────────────────────────────

        public PluginChartLog()
        {
            AlwaysShow = true;
            InitializeComponent();
            ControlDataContext.SetControl(this);
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            ControlDataContext.PropertyChanged += OnDataContextPropertyChanged;

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

        // ──────────────────────────────────────────────────────────────────────
        // Static event registration
        // ──────────────────────────────────────────────────────────────────────

        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();

                DisplayCpu = PluginDatabase.PluginSettings.DisplayCpu;
                DisplayGpu = PluginDatabase.PluginSettings.DisplayGpu;
                DisplayRam = PluginDatabase.PluginSettings.DisplayRam;
                DisplayFps = PluginDatabase.PluginSettings.DisplayFps;

                DisplayCpuT = false;
                DisplayGpuT = false;
                DisplayCpuP = false;
                DisplayGpuP = false;

                // ShowAllData is not reset from plugin settings — it is owned by the
                // nav bar toggle or by the caller's XAML binding.
            });
        }

        // ── Filter bar slide animation triggered by UseControls changes ────────

        private void OnDataContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PluginChartLogDataContext.UseControls))
            {
                return;
            }

            string storyboardKey = ControlDataContext.UseControls
                ? "FilterBarShowAnimation"
                : "FilterBarHideAnimation";

            if (Resources[storyboardKey] is Storyboard storyboard)
            {
                storyboard.Begin(PART_FilterBar);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Default DataContext initialisation
        // ──────────────────────────────────────────────────────────────────────

        public override void SetDefaultDataContext()
        {
            bool isActivated = PluginDatabase.PluginSettings.EnableIntegrationChartLog;
            double chartLogHeight = PluginDatabase.PluginSettings.ChartLogHeight;
            bool chartLogAxis = PluginDatabase.PluginSettings.ChartLogAxis;
            bool chartLogOrdinates = PluginDatabase.PluginSettings.ChartLogOrdinates;
            bool useControls = PluginDatabase.PluginSettings.UseControls;
            bool displayMoreData = PluginDatabase.PluginSettings.DisplayMoreData;

            if (IgnoreSettings)
            {
                isActivated = true;
                chartLogHeight = double.NaN;
                chartLogAxis = true;
                chartLogOrdinates = true;
                useControls = true;
                displayMoreData = true;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ChartLogHeight = chartLogHeight;
            ControlDataContext.ChartLogAxis = chartLogAxis;
            ControlDataContext.ChartLogOrdinates = chartLogOrdinates;
            ControlDataContext.ChartLogVisibleEmpty = PluginDatabase.PluginSettings.ChartLogVisibleEmpty;
            ControlDataContext.UseControls = useControls;
            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotation ? 160d : 0d;
            ControlDataContext.DisplayMoreData = displayMoreData;

            ControlDataContext.DisplayCpu = DisplayCpu;
            ControlDataContext.DisplayGpu = DisplayGpu;
            ControlDataContext.DisplayRam = DisplayRam;
            ControlDataContext.DisplayFps = DisplayFps;
            ControlDataContext.DisplayCpuT = DisplayCpuT;
            ControlDataContext.DisplayGpuT = DisplayGpuT;
            ControlDataContext.DisplayCpuP = DisplayCpuP;
            ControlDataContext.DisplayGpuP = DisplayGpuP;

            // Always reset ShowAllData on game context change so the nav bar
            // starts in a clean paginated state for the new game.
            ControlDataContext.ShowAllData = false;
            ShowAllData = false;

            // ── Nav bar defaults ───────────────────────────────────────────
            bool showNavBar = ShowNavBar;
            if (IgnoreSettings) { showNavBar = true; }

            ControlDataContext.ShowNavBar = showNavBar;
            // NavLabel has no period concept for ChartLog (labels are timestamps).
            // The builder will push "first – last" timestamps after each render.
            ControlDataContext.NavLabel = string.Empty;

            // ── Resolve effective abscissa limit and push to nav bar ───────
            // Priority: explicit AxisLimit DP → plugin setting.
            int effectivePageSize = AxisLimit > 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;

            ControlDataContext.PageSize = effectivePageSize;
            ControlDataContext.AxisLimit = AxisLimit;

            // Seed the nav bar AxisLimit so its AxisLimitDecrease button starts
            // with the correct floor check and tooltip text.
            if (PART_NavBar != null)
            {
                PART_NavBar.AxisLimit = AxisLimit;
            }
            // ── End nav bar defaults ───────────────────────────────────────

            PART_ChartLogActivity.Series = null;
            PART_ChartLogActivityLabelsX.Labels = null;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Data loading
        // ──────────────────────────────────────────────────────────────────────

        public override void SetData(Game newContext, PluginGameEntry pluginGameData)
        {
            GameActivities gameActivities = (GameActivities)pluginGameData;

            MustDisplay = !IgnoreSettings && !ControlDataContext.ChartLogVisibleEmpty
                ? gameActivities.HasDataDetails()
                : true;

            if (!MustDisplay) { return; }

            int limit = AxisLimit != 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;

            // Capture all UI-thread DP values before entering the background thread.
            int axisVariator = AxisVariator;
            DateTime? dateSelected = DateSelected;
            string titleChart = TitleChart;
            bool showAllData = ShowAllData;

            ControlDataContext.ChartLogAxis = !ShowAllData;

            GetActivityForGamesLogGraphics(gameActivities, axisVariator, limit, dateSelected, titleChart, showAllData);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Public navigation helpers
        // ──────────────────────────────────────────────────────────────────────

        #region Public methods

        /// <summary>Advances the session window forward by <paramref name="value"/> steps.</summary>
        public void Next(int value = 1) { AxisVariator += value; }

        /// <summary>Moves the session window backward by <paramref name="value"/> steps.</summary>
        public void Prev(int value = 1) { AxisVariator -= value; }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        // Nav bar event handlers
        // ──────────────────────────────────────────────────────────────────────

        #region Nav bar event handlers

        // Each handler translates a PluginChartNavBar RoutedEvent into a chart action.
        // Next() / Prev() modify AxisVariator, which triggers ControlsPropertyChangedCallback
        // → GameContextChanged → SetData — no explicit refresh call is needed there.

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
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;
            Prev(pageSize);
        }

        private void NavBar_PrevClicked(object sender, RoutedEventArgs e)
        {
            Prev();
        }

        private void NavBar_ShowAllToggled(object sender, RoutedEventArgs e)
        {
            // Mirror the nav bar's new ShowAllData state into both the DP and the DataContext,
            // then trigger a full reload so the chart re-renders with the complete dataset
            // (or returns to the paginated view when ShowAllData is turned off).
            ShowAllData = PART_NavBar.ShowAllData;
            ControlDataContext.ShowAllData = ShowAllData;
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
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;
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

        // ──────────────────────────────────────────────────────────────────────
        // Chart data construction
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and assigns the LiveCharts series collection for the current session window.
        /// Runs on a background thread; all chart assignments are marshalled via Dispatcher.
        /// </summary>
        /// <param name="showAll">
        /// When true: bypasses <see cref="AxisVariator"/> and <see cref="AxisLimit"/>
        /// and renders the complete dataset. All 8 series are forced visible.
        /// </param>
        public void GetActivityForGamesLogGraphics(
            GameActivities gameActivities,
            int variateurLog = 0,
            int limit = 10,
            DateTime? dateSelected = null,
            string titleChart = "",
            bool showAll = false)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    List<ActivityDetailsData> activitiesDetails =
                        gameActivities.GetSessionActivityDetails(dateSelected, titleChart);

                    if (activitiesDetails == null) { return; }

                    if (activitiesDetails.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            PART_ChartLogActivity.Series = null;
                            PART_ChartLogActivityLabelsX.Labels = null;
                        }));
                        return;
                    }

                    string[] activityForGameLogLabels;
                    List<ActivityDetailsData> gameLogsDefinitive;

                    if (showAll)
                    {
                        // Pagination bypassed — render the entire dataset.
                        // AxisVariator is NOT touched so disabling ShowAllData restores
                        // the previous paginated window position.
                        gameLogsDefinitive = activitiesDetails;
                        activityForGameLogLabels = new string[activitiesDetails.Count];

                        for (int iLog = 0; iLog < activitiesDetails.Count; iLog++)
                        {
                            activityForGameLogLabels[iLog] = Convert.ToDateTime(activitiesDetails[iLog].Datelog)
                                .ToLocalTime()
                                .ToString(Constants.TimeUiFormat);
                        }
                    }
                    else if (activitiesDetails.Count > limit)
                    {
                        int conteurEnd = activitiesDetails.Count + variateurLog;
                        int conteurStart = conteurEnd - limit;

                        // Clamp locally — never write back to AxisVariator from a builder.
                        // Writing to a DP from inside SetData re-fires ControlsPropertyChangedCallback
                        // → GameContextChanged → infinite render loop (visible as UI flickering).
                        if (conteurEnd > activitiesDetails.Count)
                        {
                            conteurEnd = activitiesDetails.Count;
                            conteurStart = conteurEnd - limit;
                        }

                        if (conteurStart < 0)
                        {
                            conteurStart = 0;
                            conteurEnd = limit;
                        }

                        activityForGameLogLabels = new string[limit];
                        gameLogsDefinitive = new List<ActivityDetailsData>(limit);

                        int labelIndex = 0;
                        for (int iLog = conteurStart; iLog < conteurEnd; iLog++)
                        {
                            gameLogsDefinitive.Add(activitiesDetails[iLog]);
                            activityForGameLogLabels[labelIndex++] = Convert.ToDateTime(activitiesDetails[iLog].Datelog)
                                .ToLocalTime()
                                .ToString(Constants.TimeUiFormat);
                        }
                    }
                    else
                    {
                        gameLogsDefinitive = activitiesDetails;
                        activityForGameLogLabels = new string[activitiesDetails.Count];

                        for (int iLog = 0; iLog < activitiesDetails.Count; iLog++)
                        {
                            activityForGameLogLabels[iLog] = Convert.ToDateTime(activitiesDetails[iLog].Datelog)
                                .ToLocalTime()
                                .ToString(Constants.TimeUiFormat);
                        }
                    }

                    // ── Collect values for all 8 series ───────────────────────────────
                    ChartValues<double> cpuValues = new ChartValues<double>();
                    ChartValues<double> gpuValues = new ChartValues<double>();
                    ChartValues<double> ramValues = new ChartValues<double>();
                    ChartValues<double> fpsValues = new ChartValues<double>();
                    ChartValues<double> cpuTValues = new ChartValues<double>();
                    ChartValues<double> gpuTValues = new ChartValues<double>();
                    ChartValues<double> cpuPValues = new ChartValues<double>();
                    ChartValues<double> gpuPValues = new ChartValues<double>();

                    foreach (ActivityDetailsData log in gameLogsDefinitive)
                    {
                        cpuValues.Add(log.CPU);
                        gpuValues.Add(log.GPU);
                        ramValues.Add(log.RAM);
                        fpsValues.Add(log.FPS);
                        cpuTValues.Add(log.CPUT);
                        gpuTValues.Add(log.GPUT);
                        cpuPValues.Add(log.CPUP);
                        gpuPValues.Add(log.GPUP);
                    }

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        Brush cpuBrush = TryGetThemeBrush("GameActivityCpuBrush", "#FF2979FF");
                        Brush gpuBrush = TryGetThemeBrush("GameActivityGpuBrush", "#FFFF5252");
                        Brush ramBrush = TryGetThemeBrush("GameActivityRamBrush", "#FFFFD740");
                        Brush fpsBrush = TryGetThemeBrush("GameActivityFpsBrush", "#FF69F0AE");
                        Brush cpuTBrush = TryGetThemeBrush("GameActivityCpuTBrush", "#FF82B1FF");
                        Brush gpuTBrush = TryGetThemeBrush("GameActivityGpuTBrush", "#FFFF8A80");
                        Brush cpuPBrush = TryGetThemeBrush("GameActivityCpuPBrush", "#FFBBDEFB");
                        Brush gpuPBrush = TryGetThemeBrush("GameActivityGpuPBrush", "#FFFFCDD2");

                        _cpuSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelCpu") + " (%)",
                            Stroke = cpuBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 2,
                            PointGeometrySize = 6,
                            Values = cpuValues,
                            ScalesYAt = 0
                        };
                        _gpuSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelGpu") + " (%)",
                            Stroke = gpuBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 2,
                            PointGeometrySize = 6,
                            Values = gpuValues,
                            ScalesYAt = 0
                        };
                        _ramSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelRam") + " (%)",
                            Stroke = ramBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 2,
                            PointGeometrySize = 6,
                            Values = ramValues,
                            ScalesYAt = 0
                        };
                        _fpsSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelFps"),
                            Stroke = fpsBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 2,
                            PointGeometrySize = 6,
                            Values = fpsValues,
                            ScalesYAt = 1
                        };
                        _cpuTSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelCpuT"),
                            Stroke = cpuTBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                            PointGeometrySize = 4,
                            Values = cpuTValues,
                            ScalesYAt = 1
                        };
                        _gpuTSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelGpuT"),
                            Stroke = gpuTBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                            PointGeometrySize = 4,
                            Values = gpuTValues,
                            ScalesYAt = 1
                        };
                        _cpuPSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelCpuP"),
                            Stroke = cpuPBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 },
                            PointGeometrySize = 4,
                            Values = cpuPValues,
                            ScalesYAt = 1
                        };
                        _gpuPSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelGpuP"),
                            Stroke = gpuPBrush,
                            Fill = Brushes.Transparent,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 },
                            PointGeometrySize = 4,
                            Values = gpuPValues,
                            ScalesYAt = 1
                        };

                        try
                        {
                            PART_ChartLogActivity.DataTooltip = new DefaultTooltip
                            {
                                FontSize = 13,
                                Background = (Brush)ResourceProvider.GetResource("CommonToolTipBackgroundBrush"),
                                Padding = new Thickness(8),
                                BorderThickness = (Thickness)ResourceProvider.GetResource("CommonToolTipBorderThickness"),
                                BorderBrush = (Brush)ResourceProvider.GetResource("CommonToolTipBorderBrush"),
                                Foreground = (Brush)ResourceProvider.GetResource("CommonToolTipForeground")
                            };
                        }
                        catch (Exception ex) { Common.LogError(ex, false); }

                        SeriesCollection series = new SeriesCollection
                        {
                            _cpuSeries, _gpuSeries, _ramSeries, _fpsSeries,
                            _cpuTSeries, _gpuTSeries, _cpuPSeries, _gpuPSeries
                        };

                        // Update the nav bar range badge with the visible time window.
                        // Labels are already localised via Constants.TimeUiFormat.
                        ControlDataContext.NavLabel = PluginChartNavBar.BuildRangeLabel(activityForGameLogLabels);

                        PART_ChartLogActivity.Series = series;
                        PART_ChartLogActivityLabelsY.MinValue = 0;
                        PART_ChartLogActivityLabelsY.LabelFormatter = v => v.ToString("N0") + "%";
                        PART_ChartLogActivityLabelsX.Labels = activityForGameLogLabels;

                        SetChartVisibility();
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Chart series visibility
        // ──────────────────────────────────────────────────────────────────────

        #region Chart visibility

        /// <summary>Toggles CPU series visibility.</summary>
        public void ToggleCpu() { DisplayCpu = !DisplayCpu; }
        /// <summary>Toggles GPU series visibility.</summary>
        public void ToggleGpu() { DisplayGpu = !DisplayGpu; }
        /// <summary>Toggles RAM series visibility.</summary>
        public void ToggleRam() { DisplayRam = !DisplayRam; }
        /// <summary>Toggles FPS series visibility.</summary>
        public void ToggleFps() { DisplayFps = !DisplayFps; }
        /// <summary>Toggles CPU temperature series visibility.</summary>
        public void ToggleCpuT() { DisplayCpuT = !DisplayCpuT; }
        /// <summary>Toggles GPU temperature series visibility.</summary>
        public void ToggleGpuT() { DisplayGpuT = !DisplayGpuT; }
        /// <summary>Toggles CPU power series visibility.</summary>
        public void ToggleCpuP() { DisplayCpuP = !DisplayCpuP; }
        /// <summary>Toggles GPU power series visibility.</summary>
        public void ToggleGpuP() { DisplayGpuP = !DisplayGpuP; }

        /// <summary>
        /// Applies visibility to all 8 series according to Display* flags.
        /// </summary>
        /// <param name="forceAllVisible">
        /// When true (ShowAllData mode), every series is forced to <see cref="Visibility.Visible"/> —
        /// the filter bar checkboxes are irrelevant because the caller wants a complete overview.
        /// </param>
        private void SetChartVisibility(bool forceAllVisible = false)
        {
            if (_cpuSeries != null) { _cpuSeries.Visibility = (forceAllVisible || DisplayCpu) ? Visibility.Visible : Visibility.Collapsed; }
            if (_gpuSeries != null) { _gpuSeries.Visibility = (forceAllVisible || DisplayGpu) ? Visibility.Visible : Visibility.Collapsed; }
            if (_ramSeries != null) { _ramSeries.Visibility = (forceAllVisible || DisplayRam) ? Visibility.Visible : Visibility.Collapsed; }
            if (_fpsSeries != null) { _fpsSeries.Visibility = (forceAllVisible || DisplayFps) ? Visibility.Visible : Visibility.Collapsed; }
            if (_cpuTSeries != null) { _cpuTSeries.Visibility = (forceAllVisible || DisplayCpuT) ? Visibility.Visible : Visibility.Collapsed; }
            if (_gpuTSeries != null) { _gpuTSeries.Visibility = (forceAllVisible || DisplayGpuT) ? Visibility.Visible : Visibility.Collapsed; }
            if (_cpuPSeries != null) { _cpuPSeries.Visibility = (forceAllVisible || DisplayCpuP) ? Visibility.Visible : Visibility.Collapsed; }
            if (_gpuPSeries != null) { _gpuPSeries.Visibility = (forceAllVisible || DisplayGpuP) ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// LiveCharts render tick — re-reads Display* flags each tick so that toggling a series
        /// on an already-rendered chart takes effect without a full data reload.
        /// </summary>
        private void PART_ChartLogActivity_UpdaterTick(object sender)
        {
            SetChartVisibility();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the theme brush identified by <paramref name="resourceKey"/>,
        /// or a fallback solid colour brush when the resource is absent or of the wrong type.
        /// </summary>
        private static Brush TryGetThemeBrush(string resourceKey, string fallbackHex)
        {
            object resource = ResourceProvider.GetResource(resourceKey);
            if (resource is Brush brush)
            {
                return brush;
            }
            return new BrushConverter().ConvertFromString(fallbackHex) as SolidColorBrush;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DataContext
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Observable ViewModel backing <see cref="PluginChartLog"/>.
    /// Commands are wired in <see cref="PluginChartLogDataContext()"/> via
    /// <see cref="SetControl"/>.
    /// </summary>
    public class PluginChartLogDataContext : ObservableObject, IDataContext
    {
        // ── Activation / layout ───────────────────────────────────────────────

        private bool _isActivated;
        /// <summary>Controls whether the chart control is visible.</summary>
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private double _chartLogHeight;
        /// <summary>Explicit pixel height of the chart; <see cref="double.NaN"/> for auto-size.</summary>
        public double ChartLogHeight { get => _chartLogHeight; set => SetValue(ref _chartLogHeight, value); }

        private bool _chartLogAxis;
        /// <summary>When true, the X-axis labels strip is visible.</summary>
        public bool ChartLogAxis { get => _chartLogAxis; set => SetValue(ref _chartLogAxis, value); }

        private bool _chartLogOrdinates;
        /// <summary>When true, the Y-axis labels strip is visible.</summary>
        public bool ChartLogOrdinates { get => _chartLogOrdinates; set => SetValue(ref _chartLogOrdinates, value); }

        private bool _chartLogVisibleEmpty;
        /// <summary>When true, the chart placeholder is shown even when there is no data.</summary>
        public bool ChartLogVisibleEmpty { get => _chartLogVisibleEmpty; set => SetValue(ref _chartLogVisibleEmpty, value); }

        private bool _useControls;
        /// <summary>When true, the filter bar is expanded (slide animation triggered on change).</summary>
        public bool UseControls { get => _useControls; set => SetValue(ref _useControls, value); }

        // ── Chart options ─────────────────────────────────────────────────────

        private bool _disableAnimations = true;
        /// <summary>Mirrors <see cref="PluginChartLog.DisableAnimations"/>.</summary>
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        private double _labelsRotationValue;
        /// <summary>Rotation angle applied to X-axis labels (0 or 160 degrees).</summary>
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }

        // ── Sensor display flags ──────────────────────────────────────────────

        private bool _displayCpu;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayCpu"/> — drives CPU series Visibility.</summary>
        public bool DisplayCpu { get => _displayCpu; set => SetValue(ref _displayCpu, value); }

        private bool _displayGpu;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayGpu"/> — drives GPU series Visibility.</summary>
        public bool DisplayGpu { get => _displayGpu; set => SetValue(ref _displayGpu, value); }

        private bool _displayRam;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayRam"/> — drives RAM series Visibility.</summary>
        public bool DisplayRam { get => _displayRam; set => SetValue(ref _displayRam, value); }

        private bool _displayFps;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayFps"/> — drives FPS series Visibility.</summary>
        public bool DisplayFps { get => _displayFps; set => SetValue(ref _displayFps, value); }

        private bool _displayMoreData;
        /// <summary>When true, the extended sensor group (CpuT, GpuT, CpuP, GpuP) toggle buttons are shown.</summary>
        public bool DisplayMoreData { get => _displayMoreData; set => SetValue(ref _displayMoreData, value); }

        private bool _displayCpuT;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayCpuT"/>.</summary>
        public bool DisplayCpuT { get => _displayCpuT; set => SetValue(ref _displayCpuT, value); }

        private bool _displayGpuT;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayGpuT"/>.</summary>
        public bool DisplayGpuT { get => _displayGpuT; set => SetValue(ref _displayGpuT, value); }

        private bool _displayCpuP;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayCpuP"/>.</summary>
        public bool DisplayCpuP { get => _displayCpuP; set => SetValue(ref _displayCpuP, value); }

        private bool _displayGpuP;
        /// <summary>Mirror of <see cref="PluginChartLog.DisplayGpuP"/>.</summary>
        public bool DisplayGpuP { get => _displayGpuP; set => SetValue(ref _displayGpuP, value); }

        // ── ShowAllData ────────────────────────────────────────────────────────

        private bool _showAllData;
        /// <summary>
        /// Mirror of <see cref="PluginChartLog.ShowAllData"/> DP.
        /// Written by <see cref="PluginChartLog.SetDefaultDataContext"/> (reset to false)
        /// and by <see cref="PluginChartLog.NavBar_ShowAllToggled"/>.
        /// Read by XAML bindings (e.g. to disable filter bar checkboxes in ShowAllData mode).
        /// </summary>
        public bool ShowAllData { get => _showAllData; set => SetValue(ref _showAllData, value); }

        // ── Nav bar state ─────────────────────────────────────────────────────

        private bool _showNavBar;
        /// <summary>Drives <see cref="PluginChartNavBar.ShowNavBar"/> binding in XAML.</summary>
        public bool ShowNavBar { get => _showNavBar; set => SetValue(ref _showNavBar, value); }

        private string _navLabel = string.Empty;
        /// <summary>
        /// Badge text shown on the right of the nav bar representing the visible X-axis time range.
        /// Format: "first – last" using the current UI culture (e.g. "14:00 – 17:30").
        /// Reset to <see cref="string.Empty"/> on every game context change.
        /// </summary>
        public string NavLabel { get => _navLabel; set => SetValue(ref _navLabel, value); }

        private int _pageSize;
        /// <summary>
        /// Mirror of the effective chart abscissa limit pushed by <see cref="PluginChartLog.SetDefaultDataContext"/>.
        /// Bound to <see cref="PluginChartNavBar.PageSize"/> so the nav bar can show/hide
        /// PrevPage/NextPage buttons and build their tooltips.
        /// </summary>
        public int PageSize { get => _pageSize; set => SetValue(ref _pageSize, value); }

        private int _axisLimit;
        /// <summary>
        /// Mirror of <see cref="PluginChartLog.AxisLimit"/>.
        /// Kept in sync by <see cref="PluginChartLog.SetDefaultDataContext"/> and
        /// <see cref="PluginChartLog.ApplyAxisLimitFromNavBar"/> so XAML bindings can
        /// display or react to the current limit.
        /// </summary>
        public int AxisLimit { get => _axisLimit; set => SetValue(ref _axisLimit, value); }

        // ── RelayCommands ─────────────────────────────────────────────────────

        /// <summary>Bound to the CPU toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleCpu { get; }
        /// <summary>Bound to the GPU toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleGpu { get; }
        /// <summary>Bound to the RAM toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleRam { get; }
        /// <summary>Bound to the FPS toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleFps { get; }
        /// <summary>Bound to the CPU temperature toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleCpuT { get; }
        /// <summary>Bound to the GPU temperature toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleGpuT { get; }
        /// <summary>Bound to the CPU power toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleCpuP { get; }
        /// <summary>Bound to the GPU power toggle button in the filter bar.</summary>
        public RelayCommand CmdToggleGpuP { get; }

        public PluginChartLogDataContext()
        {
            // Commands delegate to the control instance set via SetControl.
            // Using null-conditional on _control guards against designer-time instantiation
            // where SetControl is never called.
            CmdToggleCpu = new RelayCommand(() => _control?.ToggleCpu());
            CmdToggleGpu = new RelayCommand(() => _control?.ToggleGpu());
            CmdToggleRam = new RelayCommand(() => _control?.ToggleRam());
            CmdToggleFps = new RelayCommand(() => _control?.ToggleFps());
            CmdToggleCpuT = new RelayCommand(() => _control?.ToggleCpuT());
            CmdToggleGpuT = new RelayCommand(() => _control?.ToggleGpuT());
            CmdToggleCpuP = new RelayCommand(() => _control?.ToggleCpuP());
            CmdToggleGpuP = new RelayCommand(() => _control?.ToggleGpuP());
        }

        private PluginChartLog _control;

        /// <summary>
        /// Wires command delegates to the owning <see cref="PluginChartLog"/> instance.
        /// Must be called from <see cref="PluginChartLog"/>'s constructor before any command fires.
        /// </summary>
        public void SetControl(PluginChartLog control)
        {
            _control = control;
        }
    }
}
