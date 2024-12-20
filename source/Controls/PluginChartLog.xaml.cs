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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameActivity.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginChartLog.xaml
    /// </summary>
    public partial class PluginChartLog : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase { get; set; } = GameActivity.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartLogDataContext ControlDataContext { get; set; } = new PluginChartLogDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartLogDataContext)controlDataContext;
        }

        private ColumnSeries CpuSeries;
        private ColumnSeries GpuSeries;
        private ColumnSeries RamSeries;
        private LineSeries FpsSeries;


        #region Properties
        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }

        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public DateTime? DateSelected
        {
            get => (DateTime?)GetValue(DateSelectedProperty);
            set => SetValue(DateSelectedProperty, value);
        }

        public static readonly DependencyProperty DateSelectedProperty = DependencyProperty.Register(
            nameof(DateSelected),
            typeof(DateTime?),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty TitleChartProperty;
        public string TitleChart { get; set; } = string.Empty;

        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }

        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator),
            typeof(int),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        public bool DisplayCpu
        {
            get => (bool)GetValue(DisplayCpuProperty);
            set => SetValue(DisplayCpuProperty, value);
        }

        public static readonly DependencyProperty DisplayCpuProperty = DependencyProperty.Register(
            nameof(DisplayCpu),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayGpu
        {
            get => (bool)GetValue(DisplayGpuProperty);
            set => SetValue(DisplayGpuProperty, value);
        }

        public static readonly DependencyProperty DisplayGpuProperty = DependencyProperty.Register(
            nameof(DisplayGpu),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayRam
        {
            get => (bool)GetValue(DisplayRamProperty);
            set => SetValue(DisplayRamProperty, value);
        }

        public static readonly DependencyProperty DisplayRamProperty = DependencyProperty.Register(
            nameof(DisplayRam),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayFps
        {
            get => (bool)GetValue(DisplayFpsProperty);
            set => SetValue(DisplayFpsProperty, value);
        }

        public static readonly DependencyProperty DisplayFpsProperty = DependencyProperty.Register(
            nameof(DisplayFps),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));
        #endregion


        public PluginChartLog()
        {
            AlwaysShow = true;

            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    DisplayCpu = PluginDatabase.PluginSettings.Settings.DisplayCpu;
                    DisplayGpu = PluginDatabase.PluginSettings.Settings.DisplayGpu;
                    DisplayRam = PluginDatabase.PluginSettings.Settings.DisplayRam;
                    DisplayFps = PluginDatabase.PluginSettings.Settings.DisplayFps;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartTime;
            double ChartLogHeight = PluginDatabase.PluginSettings.Settings.ChartLogHeight;
            bool ChartLogAxis = PluginDatabase.PluginSettings.Settings.ChartLogAxis;
            bool ChartLogOrdinates = PluginDatabase.PluginSettings.Settings.ChartLogOrdinates;
            bool UseControls = PluginDatabase.PluginSettings.Settings.UseControls;
            if (IgnoreSettings)
            {
                IsActivated = true;
                ChartLogHeight = double.NaN;
                ChartLogAxis = true;
                ChartLogOrdinates = true;
                UseControls = true;
            }

            double LabelsRotationValue = 0;
            if (LabelsRotation)
            {
                LabelsRotationValue = 160;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.ChartLogHeight = ChartLogHeight;
            ControlDataContext.ChartLogAxis = ChartLogAxis;
            ControlDataContext.ChartLogOrdinates = ChartLogOrdinates;
            ControlDataContext.ChartLogVisibleEmpty = PluginDatabase.PluginSettings.Settings.ChartLogVisibleEmpty;
            ControlDataContext.UseControls = UseControls;

            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotationValue;

            ControlDataContext.DisplayCpu = DisplayCpu;
            ControlDataContext.DisplayGpu = DisplayGpu;
            ControlDataContext.DisplayRam = DisplayRam;
            ControlDataContext.DisplayFps = DisplayFps;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            int Limit = PluginDatabase.PluginSettings.Settings.ChartLogCountAbscissa;
            if (AxisLimit != 0)
            {
                Limit = AxisLimit;
            }

            int AxisVariator = this.AxisVariator;
            DateTime? DateSelected = this.DateSelected;
            string TitleChart = this.TitleChart;

            GameActivities gameActivities = (GameActivities)PluginGameData;

            MustDisplay = !IgnoreSettings && !ControlDataContext.ChartLogVisibleEmpty ? gameActivities.HasDataDetails() : true;
            if (MustDisplay)
            {
                GetActivityForGamesLogGraphics(gameActivities, AxisVariator, Limit, DateSelected, TitleChart);
            }
        }


        #region Public methods
        public void Next(int value = 1)
        {
            AxisVariator += value;
        }

        public void Prev(int value = 1)
        {
            AxisVariator -= value;
        }
        #endregion


        public void GetActivityForGamesLogGraphics(GameActivities gameActivities, int variateurLog = 0, int limit = 10, DateTime? dateSelected = null, string titleChart = "")
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (gameActivities?.GetSessionActivityDetails(dateSelected, titleChart) == null)
                    {
                        return;
                    }

                    List<ActivityDetailsData> ActivitiesDetails = gameActivities.GetSessionActivityDetails(dateSelected, titleChart);
                    string[] activityForGameLogLabels = new string[0];
                    List<ActivityDetailsData> gameLogsDefinitive = new List<ActivityDetailsData>();
                    if (ActivitiesDetails.Count > 0)
                    {
                        if (ActivitiesDetails.Count > limit)
                        {
                            // Variateur
                            int conteurEnd = ActivitiesDetails.Count + variateurLog;
                            int conteurStart = conteurEnd - limit;

                            if (conteurEnd > ActivitiesDetails.Count)
                            {
                                conteurEnd = ActivitiesDetails.Count;
                                conteurStart = conteurEnd - limit;

                                //variateurLog = _variateurLogTemp;
                                this.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    AxisVariator--;
                                });
                            }

                            if (conteurStart < 0)
                            {
                                conteurStart = 0;
                                conteurEnd = limit;

                                this.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    AxisVariator++;
                                });
                            }

                            // Create data
                            int sCount = 0;
                            activityForGameLogLabels = new string[limit];
                            for (int iLog = conteurStart; iLog < conteurEnd; iLog++)
                            {
                                gameLogsDefinitive.Add(ActivitiesDetails[iLog]);
                                activityForGameLogLabels[sCount] = Convert.ToDateTime(ActivitiesDetails[iLog].Datelog).ToLocalTime().ToString(Constants.TimeUiFormat);
                                sCount += 1;
                            }
                        }
                        else
                        {
                            gameLogsDefinitive = ActivitiesDetails;

                            activityForGameLogLabels = new string[ActivitiesDetails.Count];
                            for (int iLog = 0; iLog < ActivitiesDetails.Count; iLog++)
                            {
                                activityForGameLogLabels[iLog] = Convert.ToDateTime(ActivitiesDetails[iLog].Datelog).ToLocalTime().ToString(Constants.TimeUiFormat);
                            }
                        }
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            PART_ChartLogActivity.Series = null;
                            PART_ChartLogActivityLabelsX.Labels = null;
                        }));

                        return;
                    }

                    // Set data in graphic.
                    ChartValues<int> CPUseries = new ChartValues<int>();
                    ChartValues<int> GPUseries = new ChartValues<int>();
                    ChartValues<int> RAMseries = new ChartValues<int>();
                    ChartValues<int> FPSseries = new ChartValues<int>();
                    for (int iLog = 0; iLog < gameLogsDefinitive.Count; iLog++)
                    {
                        CPUseries.Add(gameLogsDefinitive[iLog].CPU);
                        GPUseries.Add(gameLogsDefinitive[iLog].GPU);
                        RAMseries.Add(gameLogsDefinitive[iLog].RAM);
                        FPSseries.Add(gameLogsDefinitive[iLog].FPS);
                    }

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        //#FF2195F2
                        CpuSeries = new ColumnSeries
                        {
                            Title = "cpu usage (%)",
                            Fill = new BrushConverter().ConvertFromString("#FF2195F2") as SolidColorBrush,
                            Values = CPUseries,
                            ScalesYAt = 0
                        };
                        //#FFF34336
                        GpuSeries = new ColumnSeries
                        {
                            Title = "gpu usage (%)",
                            Fill = new BrushConverter().ConvertFromString("#FFF34336") as SolidColorBrush,
                            Values = GPUseries,
                            ScalesYAt = 0
                        };
                        //#FFFEC007
                        RamSeries = new ColumnSeries
                        {
                            Title = "ram usage (%)",
                            Fill = new BrushConverter().ConvertFromString("#FFFEC007") as SolidColorBrush,
                            Values = RAMseries,
                            ScalesYAt = 0
                        };
                        //#FF607D8A
                        FpsSeries = new LineSeries
                        {
                            Title = "fps",
                            Stroke = new BrushConverter().ConvertFromString("#FF607D8A") as SolidColorBrush,
                            Values = FPSseries,
                            ScalesYAt = 1
                        };

                        SeriesCollection activityForGameLogSeries = new SeriesCollection
                        {
                            CpuSeries,
                            GpuSeries,
                            RamSeries,
                            FpsSeries
                        };
                        Func<double, string> activityForGameLogFormatter = value => value.ToString("N0") + "%";

                        try
                        {
                            PART_ChartLogActivity.DataTooltip = new LiveCharts.Wpf.DefaultTooltip();
                            PART_ChartLogActivity.DataTooltip.FontSize = 16;
                            PART_ChartLogActivity.DataTooltip.Background = (Brush)ResourceProvider.GetResource("CommonToolTipBackgroundBrush");
                            PART_ChartLogActivity.DataTooltip.Padding = new Thickness(10);
                            PART_ChartLogActivity.DataTooltip.BorderThickness = (Thickness)ResourceProvider.GetResource("CommonToolTipBorderThickness");
                            PART_ChartLogActivity.DataTooltip.BorderBrush = (Brush)ResourceProvider.GetResource("CommonToolTipBorderBrush");
                            PART_ChartLogActivity.DataTooltip.Foreground = (Brush)ResourceProvider.GetResource("CommonToolTipForeground");
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }

                        PART_ChartLogActivity.Series = activityForGameLogSeries;
                        PART_ChartLogActivityLabelsY.MinValue = 0;
                        PART_ChartLogActivityLabelsX.Labels = activityForGameLogLabels;
                        PART_ChartLogActivityLabelsY.LabelFormatter = activityForGameLogFormatter;
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }


        #region Chart visibility
        private void CheckBoxDisplayCpu_Click(object sender, RoutedEventArgs e)
        {
            DisplayCpu = (bool)((CheckBox)sender).IsChecked;
        }

        private void CheckBoxDisplayGpu_Click(object sender, RoutedEventArgs e)
        {
            DisplayGpu = (bool)((CheckBox)sender).IsChecked;
        }

        private void CheckBoxDisplayRam_Click(object sender, RoutedEventArgs e)
        {
            DisplayRam = (bool)((CheckBox)sender).IsChecked;
        }

        private void CheckBoxDisplayFps_Click(object sender, RoutedEventArgs e)
        {
            DisplayFps = (bool)((CheckBox)sender).IsChecked;
        }


        private void SetChartVisibility()
        {
            if (CpuSeries != null)
            {
                CpuSeries.Visibility = DisplayCpu ? Visibility.Visible : Visibility.Collapsed;
            }
            if (GpuSeries != null)
            {
                GpuSeries.Visibility = DisplayGpu ? Visibility.Visible : Visibility.Collapsed;
            }
            if (RamSeries != null)
            {
                RamSeries.Visibility = DisplayRam ? Visibility.Visible : Visibility.Collapsed;
            }

            if (FpsSeries != null)
            {
                FpsSeries.Visibility = DisplayFps ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void PART_ChartLogActivity_UpdaterTick(object sender)
        {
            SetChartVisibility();
        }
        #endregion
    }


    public class PluginChartLogDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        public double _chartLogHeight;
        public double ChartLogHeight { get => _chartLogHeight; set => SetValue(ref _chartLogHeight, value); }

        public bool _chartLogAxis;
        public bool ChartLogAxis { get => _chartLogAxis; set => SetValue(ref _chartLogAxis, value); }

        public bool _chartLogOrdinates;
        public bool ChartLogOrdinates { get => _chartLogOrdinates; set => SetValue(ref _chartLogOrdinates, value); }

        public bool _chartLogVisibleEmpty;
        public bool ChartLogVisibleEmpty { get => _chartLogVisibleEmpty; set => SetValue(ref _chartLogVisibleEmpty, value); }

        public bool _useControls;
        public bool UseControls { get => _useControls; set => SetValue(ref _useControls, value); }

        public bool _disableAnimations = true;
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        public double _labelsRotationValue;
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }

        public bool _displayCpu;
        public bool DisplayCpu { get => _displayCpu; set => SetValue(ref _displayCpu, value); }

        public bool _displayGpu;
        public bool DisplayGpu { get => _displayGpu; set => SetValue(ref _displayGpu, value); }

        public bool _displayRam;
        public bool DisplayRam { get => _displayRam; set => SetValue(ref _displayRam, value); }

        public bool _displayFps;
        public bool DisplayFps { get => _displayFps; set => SetValue(ref _displayFps, value); }
    }
}
