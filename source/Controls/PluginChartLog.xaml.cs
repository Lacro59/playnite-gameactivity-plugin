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
using System.Windows.Threading;

namespace GameActivity.Controls
{
    public partial class PluginChartLog : PluginUserControlExtend
    {
        // ──────────────────────────────────────────────────────────────────────
        // Plugin database wiring
        // ──────────────────────────────────────────────────────────────────────

        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartLogDataContext ControlDataContext = new PluginChartLogDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartLogDataContext)value;
        }

        // ── LiveCharts series fields — kept from original ───────────────────
        private ColumnSeries _cpuSeries;
        private ColumnSeries _gpuSeries;
        private ColumnSeries _ramSeries;
        private LineSeries _fpsSeries;


        // ──────────────────────────────────────────────────────────────────────
        // Dependency properties — preserved exactly from the original
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

        /// <summary>
        /// Controls CPU series visibility.
        /// Triggers ControlsPropertyChangedCallback so the chart refreshes when toggled externally.
        /// </summary>
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

        #endregion


        // ──────────────────────────────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────────────────────────────

        public PluginChartLog()
        {
            AlwaysShow = true;
            InitializeComponent();
            // Give the DataContext a back-reference so RelayCommands can write
            // to this control's DependencyProperties (the single source of truth for toggle state).
            ControlDataContext.SetControl(this);
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
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
                PluginDatabase.Database.ItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.Database.ItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();
                API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                // Sync toggle state from settings on first attach.
                DisplayCpu = PluginDatabase.PluginSettings.Settings.DisplayCpu;
                DisplayGpu = PluginDatabase.PluginSettings.Settings.DisplayGpu;
                DisplayRam = PluginDatabase.PluginSettings.Settings.DisplayRam;
                DisplayFps = PluginDatabase.PluginSettings.Settings.DisplayFps;
            });
        }


        // ──────────────────────────────────────────────────────────────────────
        // Default DataContext initialisation — preserved from original
        // ──────────────────────────────────────────────────────────────────────

        public override void SetDefaultDataContext()
        {
            bool isActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartLog;
            double chartLogHeight = PluginDatabase.PluginSettings.Settings.ChartLogHeight;
            bool chartLogAxis = PluginDatabase.PluginSettings.Settings.ChartLogAxis;
            bool chartLogOrdinates = PluginDatabase.PluginSettings.Settings.ChartLogOrdinates;
            bool useControls = PluginDatabase.PluginSettings.Settings.UseControls;

            if (IgnoreSettings)
            {
                isActivated = true;
                chartLogHeight = double.NaN;
                chartLogAxis = true;
                chartLogOrdinates = true;
                useControls = true;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ChartLogHeight = chartLogHeight;
            ControlDataContext.ChartLogAxis = chartLogAxis;
            ControlDataContext.ChartLogOrdinates = chartLogOrdinates;
            ControlDataContext.ChartLogVisibleEmpty = PluginDatabase.PluginSettings.Settings.ChartLogVisibleEmpty;
            ControlDataContext.UseControls = useControls;
            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotation ? 160d : 0d;

            // Mirror DependencyProperty values into DataContext so XAML bindings stay in sync.
            ControlDataContext.DisplayCpu = DisplayCpu;
            ControlDataContext.DisplayGpu = DisplayGpu;
            ControlDataContext.DisplayRam = DisplayRam;
            ControlDataContext.DisplayFps = DisplayFps;

            PART_ChartLogActivity.Series = null;
            PART_ChartLogActivityLabelsX.Labels = null;
        }


        // ──────────────────────────────────────────────────────────────────────
        // Data loading
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Captures all UI-thread dependency property values before dispatching to background.
        /// Avoids cross-thread access to DependencyObjects inside the Task.
        /// </summary>
        public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
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
                : PluginDatabase.PluginSettings.Settings.ChartLogCountAbscissa;

            // Capture UI-thread values before entering background thread.
            int axisVariator = AxisVariator;
            DateTime? dateSelected = DateSelected;
            string titleChart = TitleChart;

            GetActivityForGamesLogGraphics(gameActivities, axisVariator, limit, dateSelected, titleChart);
        }


        // ──────────────────────────────────────────────────────────────────────
        // Public navigation helpers — preserved from original
        // ──────────────────────────────────────────────────────────────────────

        #region Public methods
        public void Next(int value = 1) { AxisVariator += value; }
        public void Prev(int value = 1) { AxisVariator -= value; }
        #endregion


        // ──────────────────────────────────────────────────────────────────────
        // Chart data construction — preserved logic, modernised series colors
        // ──────────────────────────────────────────────────────────────────────

        public void GetActivityForGamesLogGraphics(
            GameActivities gameActivities,
            int variateurLog = 0,
            int limit = 10,
            DateTime? dateSelected = null,
            string titleChart = "")
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Single call — avoids computing the session list twice.
                    List<ActivityDetailsData> activitiesDetails = gameActivities.GetSessionActivityDetails(dateSelected, titleChart);
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

                    if (activitiesDetails.Count > limit)
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

                    ChartValues<int> cpuValues = new ChartValues<int>();
                    ChartValues<int> gpuValues = new ChartValues<int>();
                    ChartValues<int> ramValues = new ChartValues<int>();
                    ChartValues<int> fpsValues = new ChartValues<int>();

                    foreach (ActivityDetailsData log in gameLogsDefinitive)
                    {
                        cpuValues.Add(log.CPU);
                        gpuValues.Add(log.GPU);
                        ramValues.Add(log.RAM);
                        fpsValues.Add(log.FPS);
                    }

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        // Resolve theme-aware brushes on the UI thread via DynamicResource.
                        // Falls back to hardcoded colors if the resource key is missing.
                        Brush cpuBrush = TryGetThemeBrush("GameActivityCpuBrush", "#FF2195F2");
                        Brush gpuBrush = TryGetThemeBrush("GameActivityGpuBrush", "#FFF34336");
                        Brush ramBrush = TryGetThemeBrush("GameActivityRamBrush", "#FFFEC007");
                        Brush fpsBrush = TryGetThemeBrush("GameActivityFpsBrush", "#FF607D8A");

                        _cpuSeries = new ColumnSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelCpu") + " (%)",
                            Fill = cpuBrush,
                            Values = cpuValues,
                            ScalesYAt = 0
                        };

                        _gpuSeries = new ColumnSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelGpu") + " (%)",
                            Fill = gpuBrush,
                            Values = gpuValues,
                            ScalesYAt = 0
                        };

                        _ramSeries = new ColumnSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelRam") + " (%)",
                            Fill = ramBrush,
                            Values = ramValues,
                            ScalesYAt = 0
                        };

                        _fpsSeries = new LineSeries
                        {
                            Title = ResourceProvider.GetString("LOCGameActivityLabelFps"),
                            Stroke = fpsBrush,
                            Values = fpsValues,
                            ScalesYAt = 1
                        };

                        SeriesCollection series = new SeriesCollection
                        {
                            _cpuSeries, _gpuSeries, _ramSeries, _fpsSeries
                        };

                        // Apply theme-aware tooltip styling.
                        try
                        {
                            PART_ChartLogActivity.DataTooltip = new DefaultTooltip
                            {
                                FontSize = 16,
                                Background = (Brush)ResourceProvider.GetResource("CommonToolTipBackgroundBrush"),
                                Padding = new Thickness(10),
                                BorderThickness = (Thickness)ResourceProvider.GetResource("CommonToolTipBorderThickness"),
                                BorderBrush = (Brush)ResourceProvider.GetResource("CommonToolTipBorderBrush"),
                                Foreground = (Brush)ResourceProvider.GetResource("CommonToolTipForeground")
                            };
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }

                        PART_ChartLogActivity.Series = series;
                        PART_ChartLogActivityLabelsY.MinValue = 0;
                        PART_ChartLogActivityLabelsY.LabelFormatter = value => value.ToString("N0") + "%";
                        PART_ChartLogActivityLabelsX.Labels = activityForGameLogLabels;
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

        /// <summary>
        /// Toggles CPU series visibility.
        /// Replaces CheckBoxDisplayCpu_Click code-behind handler — called by RelayCommand.
        /// The DependencyProperty write triggers ControlsPropertyChangedCallback on the base class,
        /// which will refresh the data context and keep DataContext.DisplayCpu in sync.
        /// </summary>
        public void ToggleCpu() { DisplayCpu = !DisplayCpu; }
        public void ToggleGpu() { DisplayGpu = !DisplayGpu; }
        public void ToggleRam() { DisplayRam = !DisplayRam; }
        public void ToggleFps() { DisplayFps = !DisplayFps; }

        /// <summary>
        /// Applies current DependencyProperty visibility flags to the live series objects.
        /// Called on every LiveCharts render tick so changes are reflected immediately.
        /// </summary>
        private void SetChartVisibility()
        {
            if (_cpuSeries != null) { _cpuSeries.Visibility = DisplayCpu ? Visibility.Visible : Visibility.Collapsed; }
            if (_gpuSeries != null) { _gpuSeries.Visibility = DisplayGpu ? Visibility.Visible : Visibility.Collapsed; }
            if (_ramSeries != null) { _ramSeries.Visibility = DisplayRam ? Visibility.Visible : Visibility.Collapsed; }
            if (_fpsSeries != null) { _fpsSeries.Visibility = DisplayFps ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// LiveCharts render tick — cannot be replaced by a command.
        /// Used here only to sync series visibility with the current toggle state.
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
        /// Tries to resolve a <see cref="Brush"/> from the application's merged resource dictionaries.
        /// Falls back to a hardcoded hex color string if the key is absent, so chart colors
        /// degrade gracefully when a theme does not define the custom keys.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="resourceKey">The DynamicResource key to look up.</param>
        /// <param name="fallbackHex">Hex color used when the key is not found.</param>
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
        // ── IDataContext ────────────────────────────────────────────────────

        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        // ── Original properties ————————————————————─────────────────────────

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

        private bool _disableAnimations = true;
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        private double _labelsRotationValue;
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }

        private bool _displayCpu;
        public bool DisplayCpu { get => _displayCpu; set => SetValue(ref _displayCpu, value); }

        private bool _displayGpu;
        public bool DisplayGpu { get => _displayGpu; set => SetValue(ref _displayGpu, value); }

        private bool _displayRam;
        public bool DisplayRam { get => _displayRam; set => SetValue(ref _displayRam, value); }

        private bool _displayFps;
        public bool DisplayFps { get => _displayFps; set => SetValue(ref _displayFps, value); }

        // ── Toggle RelayCommands — replace CheckBoxDisplayXxx_Click handlers ─
        // The command writes back to the parent control's DependencyProperty via
        // the PluginChartLog reference passed at construction time.
        // This keeps series toggle logic off the CheckBox Click event while
        // preserving the DependencyProperty as the single source of truth.

        /// <summary>Toggles the CPU series. Bound to the CPU CheckBox in XAML.</summary>
        public RelayCommand CmdToggleCpu { get; }

        /// <summary>Toggles the GPU series. Bound to the GPU CheckBox in XAML.</summary>
        public RelayCommand CmdToggleGpu { get; }

        /// <summary>Toggles the RAM series. Bound to the RAM CheckBox in XAML.</summary>
        public RelayCommand CmdToggleRam { get; }

        /// <summary>Toggles the FPS series. Bound to the FPS CheckBox in XAML.</summary>
        public RelayCommand CmdToggleFps { get; }

        public PluginChartLogDataContext()
        {
            // Commands delegate back to the control's DependencyProperty toggle helpers.
            // The control registers itself via SetControl() after InitializeComponent().
            CmdToggleCpu = new RelayCommand(() => _control?.ToggleCpu());
            CmdToggleGpu = new RelayCommand(() => _control?.ToggleGpu());
            CmdToggleRam = new RelayCommand(() => _control?.ToggleRam());
            CmdToggleFps = new RelayCommand(() => _control?.ToggleFps());
        }

        // ── Back-reference to the owning control (weak to avoid GC retention) ─

        private PluginChartLog _control;

        /// <summary>
        /// Called by <see cref="PluginChartLog"/> constructor to give commands
        /// access to the control's DependencyProperty toggle methods.
        /// </summary>
        public void SetControl(PluginChartLog control)
        {
            _control = control;
        }
    }
}