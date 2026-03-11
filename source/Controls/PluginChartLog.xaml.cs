using CommonPlayniteShared.Common;
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

        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }
        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public DateTime? DateSelected
        {
            get => (DateTime?)GetValue(DateSelectedProperty);
            set => SetValue(DateSelectedProperty, value);
        }
        public static readonly DependencyProperty DateSelectedProperty = DependencyProperty.Register(
            nameof(DateSelected), typeof(DateTime?), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty TitleChartProperty;
        public string TitleChart { get; set; } = string.Empty;

        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }
        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator), typeof(int), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        // ── Sensor toggles ────────────────────────────────────────────────────

        public bool DisplayCpu
        {
            get => (bool)GetValue(DisplayCpuProperty);
            set => SetValue(DisplayCpuProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuProperty = DependencyProperty.Register(
            nameof(DisplayCpu), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayGpu
        {
            get => (bool)GetValue(DisplayGpuProperty);
            set => SetValue(DisplayGpuProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuProperty = DependencyProperty.Register(
            nameof(DisplayGpu), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayRam
        {
            get => (bool)GetValue(DisplayRamProperty);
            set => SetValue(DisplayRamProperty, value);
        }
        public static readonly DependencyProperty DisplayRamProperty = DependencyProperty.Register(
            nameof(DisplayRam), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayFps
        {
            get => (bool)GetValue(DisplayFpsProperty);
            set => SetValue(DisplayFpsProperty, value);
        }
        public static readonly DependencyProperty DisplayFpsProperty = DependencyProperty.Register(
            nameof(DisplayFps), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayCpuT
        {
            get => (bool)GetValue(DisplayCpuTProperty);
            set => SetValue(DisplayCpuTProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuTProperty = DependencyProperty.Register(
            nameof(DisplayCpuT), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public bool DisplayGpuT
        {
            get => (bool)GetValue(DisplayGpuTProperty);
            set => SetValue(DisplayGpuTProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuTProperty = DependencyProperty.Register(
            nameof(DisplayGpuT), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public bool DisplayCpuP
        {
            get => (bool)GetValue(DisplayCpuPProperty);
            set => SetValue(DisplayCpuPProperty, value);
        }
        public static readonly DependencyProperty DisplayCpuPProperty = DependencyProperty.Register(
            nameof(DisplayCpuP), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public bool DisplayGpuP
        {
            get => (bool)GetValue(DisplayGpuPProperty);
            set => SetValue(DisplayGpuPProperty, value);
        }
        public static readonly DependencyProperty DisplayGpuPProperty = DependencyProperty.Register(
            nameof(DisplayGpuP), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        // ── ShowAllData ────────────────────────────────────────────────────────
        /// <summary>
        /// When true: bypasses AxisVariator/limit pagination and renders the entire
        /// session dataset. Also forces all 8 series visible regardless of individual
        /// Display* flags.
        /// Driven by the nav bar toggle or by an external XAML binding.
        /// </summary>
        public bool ShowAllData
        {
            get => (bool)GetValue(ShowAllDataProperty);
            set => SetValue(ShowAllDataProperty, value);
        }
        public static readonly DependencyProperty ShowAllDataProperty = DependencyProperty.Register(
            nameof(ShowAllData), typeof(bool), typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        // ── ShowNavBar ────────────────────────────────────────────────────────
        /// <summary>
        /// Shows or hides the PluginChartNavBar above the filter bar.
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

        // ── PageSize ──────────────────────────────────────────────────────────
        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage nav bar buttons.
        /// Should be bound to the same value used as the chart's abscissa limit
        /// (AxisLimit when set, otherwise PluginSettings.ChartLogCountAbscissa).
        /// When &lt;= 0, the PrevPage/NextPage buttons are hidden in the nav bar.
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

            ControlDataContext.PropertyChanged += OnDataContextPropertyChanged;
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

            // ── ShowAllData: always reset on game context change ───────────────
            // Mirrors PluginChartTime behaviour: switching game resets the view to
            // the paginated window so the nav bar starts in a clean state.
            ControlDataContext.ShowAllData = false;
            ShowAllData = false;
            // ── End ShowAllData ────────────────────────────────────────────────

            // ── Nav bar defaults ───────────────────────────────────────────────
            bool showNavBar = ShowNavBar;
            if (IgnoreSettings) { showNavBar = true; }

            ControlDataContext.ShowNavBar = showNavBar;
            // NavLabel is empty for ChartLog (no week/period concept).
            // A parent view may push a session title here via NavLabel binding.
            ControlDataContext.NavLabel = string.Empty;

            // ── PageSize: resolve effective limit and push to nav bar ──────────
            // Priority: explicit AxisLimit DP → plugin setting.
            // The nav bar hides PrevPage/NextPage when PageSize <= 0.
            int effectivePageSize = AxisLimit > 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;
            ControlDataContext.PageSize = effectivePageSize;
            // ── End PageSize ───────────────────────────────────────────────────
            // ── End nav bar defaults ───────────────────────────────────────────

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

            if (!MustDisplay)
            {
                return;
            }

            int limit = AxisLimit != 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.ChartLogCountAbscissa;

            // Capture all UI-thread DP values before entering the background thread.
            int axisVariator = AxisVariator;
            DateTime? dateSelected = DateSelected;
            string titleChart = TitleChart;

            // Capture ShowAllData on the UI thread before the Task starts.
            bool showAllData = ShowAllData;

            ControlDataContext.ChartLogAxis = !ShowAllData;

            GetActivityForGamesLogGraphics(gameActivities, axisVariator, limit, dateSelected, titleChart, showAllData);
        }


        // ──────────────────────────────────────────────────────────────────────
        // Public navigation helpers
        // ──────────────────────────────────────────────────────────────────────

        #region Public methods

        public void Next(int value = 1) { AxisVariator += value; }
        public void Prev(int value = 1) { AxisVariator -= value; }

        #endregion


        // ──────────────────────────────────────────────────────────────────────
        // Nav bar event handlers
        // ──────────────────────────────────────────────────────────────────────

        #region Nav bar event handlers

        // Each handler translates a PluginChartNavBar RoutedEvent into a chart action.
        // Next() / Prev() modify AxisVariator, which triggers ControlsPropertyChangedCallback
        // → GameContextChanged → SetData, so no explicit refresh call is needed there.

        private void NavBar_FirstClicked(object sender, RoutedEventArgs e)
        {
            // Jump to the oldest data by moving the window far to the left.
            // GetActivityForGamesLogGraphics clamps automatically when conteurStart < 0.
            AxisVariator = -9999;
        }

        private void NavBar_PagePrevClicked(object sender, RoutedEventArgs e)
        {
            // Skip back by a full page (PageSize items).
            // The delta is resolved here in the parent where the limit is owned;
            // the nav bar itself carries PageSize only for display purposes.
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
            // Skip forward by a full page (PageSize items).
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

        #endregion


        // ──────────────────────────────────────────────────────────────────────
        // Chart data construction
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and assigns the LiveCharts series collection for the current session.
        /// </summary>
        /// <param name="showAll">
        /// When <c>true</c>: ignores <paramref name="variateurLog"/> and <paramref name="limit"/>
        /// and renders the complete dataset. All 8 series are forced visible.
        /// When <c>false</c>: standard windowed pagination behaviour.
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

                    if (activitiesDetails == null)
                    {
                        return;
                    }

                    string[] activityForGameLogLabels;
                    List<ActivityDetailsData> gameLogsDefinitive;

                    if (activitiesDetails.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            PART_ChartLogActivity.Series = null;
                            PART_ChartLogActivityLabelsX.Labels = null;
                        }));
                        return;
                    }

                    // ── ShowAllData: render the entire dataset ─────────────────────────
                    if (showAll)
                    {
                        // Pagination is intentionally bypassed.
                        // AxisVariator is NOT touched so that disabling ShowAllData returns
                        // the user to their previous paginated window position.
                        gameLogsDefinitive = activitiesDetails;
                        activityForGameLogLabels = new string[activitiesDetails.Count];

                        for (int iLog = 0; iLog < activitiesDetails.Count; iLog++)
                        {
                            activityForGameLogLabels[iLog] = Convert.ToDateTime(activitiesDetails[iLog].Datelog)
                                .ToLocalTime()
                                .ToString(Constants.TimeUiFormat);
                        }
                    }
                    // ── End ShowAllData ────────────────────────────────────────────────
                    else if (activitiesDetails.Count > limit)
                    {
                        int conteurEnd = activitiesDetails.Count + variateurLog;
                        int conteurStart = conteurEnd - limit;

                        if (conteurEnd > activitiesDetails.Count)
                        {
                            conteurEnd = activitiesDetails.Count;
                            conteurStart = conteurEnd - limit;
                            this.Dispatcher.BeginInvoke((Action)delegate { AxisVariator--; });
                        }

                        if (conteurStart < 0)
                        {
                            conteurStart = 0;
                            conteurEnd = limit;
                            this.Dispatcher.BeginInvoke((Action)delegate { AxisVariator++; });
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
                    ChartValues<int> cpuValues = new ChartValues<int>();
                    ChartValues<int> gpuValues = new ChartValues<int>();
                    ChartValues<int> ramValues = new ChartValues<int>();
                    ChartValues<int> fpsValues = new ChartValues<int>();
                    ChartValues<int> cpuTValues = new ChartValues<int>();
                    ChartValues<int> gpuTValues = new ChartValues<int>();
                    ChartValues<int> cpuPValues = new ChartValues<int>();
                    ChartValues<int> gpuPValues = new ChartValues<int>();

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
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }

                        SeriesCollection series = new SeriesCollection
                        {
                            _cpuSeries, _gpuSeries, _ramSeries, _fpsSeries,
                            _cpuTSeries, _gpuTSeries, _cpuPSeries, _gpuPSeries
                        };

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

        public void ToggleCpu() { DisplayCpu = !DisplayCpu; }
        public void ToggleGpu() { DisplayGpu = !DisplayGpu; }
        public void ToggleRam() { DisplayRam = !DisplayRam; }
        public void ToggleFps() { DisplayFps = !DisplayFps; }
        public void ToggleCpuT() { DisplayCpuT = !DisplayCpuT; }
        public void ToggleGpuT() { DisplayGpuT = !DisplayGpuT; }
        public void ToggleCpuP() { DisplayCpuP = !DisplayCpuP; }
        public void ToggleGpuP() { DisplayGpuP = !DisplayGpuP; }

        /// <summary>
        /// Applies visibility to all 8 series.
        /// </summary>
        /// <param name="forceAllVisible">
        /// When <c>true</c> (ShowAllData mode), every series is forced to
        /// <see cref="Visibility.Visible"/> — the filter bar checkboxes are irrelevant
        /// in that mode because the caller wants a complete overview.
        /// When <c>false</c>, standard per-series Display* flag logic applies.
        /// </param>
        /// <remarks>
        /// BUG FIX vs original: the original code declared <paramref name="forceAllVisible"/>
        /// but never used it — every branch evaluated only DisplayXxx.
        /// The corrected form uses <c>forceAllVisible || DisplayXxx</c> so ShowAllData
        /// actually overrides individual series visibility.
        /// </remarks>
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
        /// LiveCharts render tick — re-reads ShowAllData from the DP each tick so that
        /// toggling ShowAllData on an already-rendered chart takes effect without a
        /// full data reload.
        /// </summary>
        private void PART_ChartLogActivity_UpdaterTick(object sender)
        {
            SetChartVisibility();
        }

        #endregion


        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

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

    public class PluginChartLogDataContext : ObservableObject, IDataContext
    {
        // ── Activation / layout ───────────────────────────────────────────────

        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private double _chartLogHeight;
        public double ChartLogHeight { get => _chartLogHeight; set => SetValue(ref _chartLogHeight, value); }

        private bool _chartLogAxis;
        public bool ChartLogAxis { get => _chartLogAxis; set => SetValue(ref _chartLogAxis, value); }

        private bool _chartLogOrdinates;
        public bool ChartLogOrdinates { get => _chartLogOrdinates; set => SetValue(ref _chartLogOrdinates, value); }

        private bool _chartLogVisibleEmpty;
        public bool ChartLogVisibleEmpty { get => _chartLogVisibleEmpty; set => SetValue(ref _chartLogVisibleEmpty, value); }

        private bool _useControls;
        public bool UseControls { get => _useControls; set => SetValue(ref _useControls, value); }

        // ── Chart options ─────────────────────────────────────────────────────

        private bool _disableAnimations = true;
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        private double _labelsRotationValue;
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }

        // ── Sensor display flags ──────────────────────────────────────────────

        private bool _displayCpu;
        public bool DisplayCpu { get => _displayCpu; set => SetValue(ref _displayCpu, value); }

        private bool _displayGpu;
        public bool DisplayGpu { get => _displayGpu; set => SetValue(ref _displayGpu, value); }

        private bool _displayRam;
        public bool DisplayRam { get => _displayRam; set => SetValue(ref _displayRam, value); }

        private bool _displayFps;
        public bool DisplayFps { get => _displayFps; set => SetValue(ref _displayFps, value); }

        private bool _displayMoreData;
        public bool DisplayMoreData { get => _displayMoreData; set => SetValue(ref _displayMoreData, value); }

        private bool _displayCpuT;
        public bool DisplayCpuT { get => _displayCpuT; set => SetValue(ref _displayCpuT, value); }

        private bool _displayGpuT;
        public bool DisplayGpuT { get => _displayGpuT; set => SetValue(ref _displayGpuT, value); }

        private bool _displayCpuP;
        public bool DisplayCpuP { get => _displayCpuP; set => SetValue(ref _displayCpuP, value); }

        private bool _displayGpuP;
        public bool DisplayGpuP { get => _displayGpuP; set => SetValue(ref _displayGpuP, value); }

        // ── ShowAllData ────────────────────────────────────────────────────────
        /// <summary>
        /// Mirror of <see cref="PluginChartLog.ShowAllData"/> DP.
        /// Written by <see cref="PluginChartLog.SetDefaultDataContext"/> (reset to false)
        /// and by <see cref="PluginChartLog.NavBar_ShowAllToggled"/>.
        /// Read by XAML bindings (e.g. to disable filter bar checkboxes in ShowAllData mode).
        /// </summary>
        private bool _showAllData;
        public bool ShowAllData { get => _showAllData; set => SetValue(ref _showAllData, value); }

        // ── Nav bar state ─────────────────────────────────────────────────────

        private bool _showNavBar;
        /// <summary>Drives PluginChartNavBar.ShowNavBar binding in the XAML.</summary>
        public bool ShowNavBar { get => _showNavBar; set => SetValue(ref _showNavBar, value); }

        private string _navLabel = string.Empty;
        /// <summary>
        /// Badge text shown on the right of the nav bar.
        /// For ChartLog this is always empty (no week/period concept).
        /// Exposed so a parent view can push a session title if needed.
        /// </summary>
        public string NavLabel { get => _navLabel; set => SetValue(ref _navLabel, value); }

        private int _pageSize;
        /// <summary>
        /// Mirror of the effective chart abscissa limit pushed by PluginChartLog.SetDefaultDataContext.
        /// Bound to PluginChartNavBar.PageSize so the nav bar can show/hide the
        /// PrevPage/NextPage buttons and build their tooltips.
        /// </summary>
        public int PageSize { get => _pageSize; set => SetValue(ref _pageSize, value); }

        // ── RelayCommands ─────────────────────────────────────────────────────

        public RelayCommand CmdToggleCpu { get; }
        public RelayCommand CmdToggleGpu { get; }
        public RelayCommand CmdToggleRam { get; }
        public RelayCommand CmdToggleFps { get; }
        public RelayCommand CmdToggleCpuT { get; }
        public RelayCommand CmdToggleGpuT { get; }
        public RelayCommand CmdToggleCpuP { get; }
        public RelayCommand CmdToggleGpuP { get; }

        public PluginChartLogDataContext()
        {
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

        /// <summary>Called from <see cref="PluginChartLog"/> constructor to wire commands.</summary>
        public void SetControl(PluginChartLog control)
        {
            _control = control;
        }
    }
}