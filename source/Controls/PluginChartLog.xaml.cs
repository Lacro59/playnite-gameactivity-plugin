using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameActivity.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginChartLog.xaml
    /// </summary>
    public partial class PluginChartLog : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (ActivityDatabase)_PluginDatabase;
            }
        }

        private PluginChartLogDataContext ControlDataContext;
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginChartLogDataContext)_ControlDataContext;
            }
        }

        private ColumnSeries CpuSeries;
        private ColumnSeries GpuSeries;
        private ColumnSeries RamSeries;
        private LineSeries FpsSeries;


        #region Properties
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public DateTime? DateSelected
        {
            get { return (DateTime?)GetValue(DateSelectedProperty); }
            set { SetValue(DateSelectedProperty, value); }
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
            get { return (int)GetValue(AxisVariatoryProperty); }
            set { SetValue(AxisVariatoryProperty, value); }
        }

        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator),
            typeof(int),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));

        public bool DisplayCpu
        {
            get { return (bool)GetValue(DisplayCpuProperty); }
            set { SetValue(DisplayCpuProperty, value); }
        }

        public static readonly DependencyProperty DisplayCpuProperty = DependencyProperty.Register(
            nameof(DisplayCpu),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayGpu
        {
            get { return (bool)GetValue(DisplayGpuProperty); }
            set { SetValue(DisplayGpuProperty, value); }
        }

        public static readonly DependencyProperty DisplayGpuProperty = DependencyProperty.Register(
            nameof(DisplayGpu),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayRam
        {
            get { return (bool)GetValue(DisplayRamProperty); }
            set { SetValue(DisplayRamProperty, value); }
        }

        public static readonly DependencyProperty DisplayRamProperty = DependencyProperty.Register(
            nameof(DisplayRam),
            typeof(bool),
            typeof(PluginChartLog),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool DisplayFps
        {
            get { return (bool)GetValue(DisplayFpsProperty); }
            set { SetValue(DisplayFpsProperty, value); }
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

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

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

            ControlDataContext = new PluginChartLogDataContext
            {
                IsActivated = IsActivated,
                ChartLogHeight = ChartLogHeight,
                ChartLogAxis = ChartLogAxis,
                ChartLogOrdinates = ChartLogOrdinates,
                ChartLogVisibleEmpty = PluginDatabase.PluginSettings.Settings.ChartLogVisibleEmpty,
                UseControls = UseControls,

                DisableAnimations = DisableAnimations,
                DisplayCpu = DisplayCpu,
                DisplayGpu = DisplayGpu,
                DisplayRam = DisplayRam,
                DisplayFps = DisplayFps
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            bool IgnoreSettings = this.IgnoreSettings;
            bool MustDisplay = this.MustDisplay;

            int Limit = PluginDatabase.PluginSettings.Settings.ChartLogCountAbscissa;
            if (AxisLimit != 0)
            {
                Limit = AxisLimit;
            }

            int AxisVariator = this.AxisVariator;
            DateTime? DateSelected = this.DateSelected;
            string TitleChart = this.TitleChart;

            return Task.Run(() =>
            {
                GameActivities gameActivities = (GameActivities)PluginGameData;

                if (!IgnoreSettings && !ControlDataContext.ChartLogVisibleEmpty)
                {
                    MustDisplay = gameActivities.HasDataDetails();
                }
                else
                {
                    MustDisplay = true;
                }

                if (MustDisplay)
                {
                    GetActivityForGamesLogGraphics(gameActivities, AxisVariator, Limit, DateSelected, TitleChart);
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.MustDisplay = MustDisplay;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }


        #region Public methods
        public void Next()
        {
            AxisVariator += 1;
        }

        public void Prev()
        {
            AxisVariator -= 1;
        }
        #endregion


        public void GetActivityForGamesLogGraphics(GameActivities gameActivities, int variateurLog = 0, int limit = 10, DateTime? dateSelected = null, string titleChart = "")
        {
            Task.Run(() =>
            {
                try
                {
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
                        Func<double, string> activityForGameLogFormatter = value => value.ToString("N");

                        PART_ChartLogActivity.DataTooltip = new LiveCharts.Wpf.DefaultTooltip();
                        PART_ChartLogActivity.DataTooltip.FontSize = 16;
                        PART_ChartLogActivity.DataTooltip.Background = (Brush)resources.GetResource("CommonToolTipBackgroundBrush");
                        PART_ChartLogActivity.DataTooltip.Padding = new Thickness(10);
                        PART_ChartLogActivity.DataTooltip.BorderThickness = (Thickness)resources.GetResource("CommonToolTipBorderThickness");
                        PART_ChartLogActivity.DataTooltip.BorderBrush = (Brush)resources.GetResource("CommonToolTipBorderBrush");
                        PART_ChartLogActivity.DataTooltip.Foreground = (Brush)resources.GetResource("CommonToolTipForeground");

                        PART_ChartLogActivity.Series = activityForGameLogSeries;
                        PART_ChartLogActivityLabelsY.MinValue = 0;
                        PART_ChartLogActivityLabelsX.Labels = activityForGameLogLabels;
                        PART_ChartLogActivityLabelsY.LabelFormatter = activityForGameLogFormatter;
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
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
                CpuSeries.Visibility = (DisplayCpu) ? Visibility.Visible : Visibility.Collapsed;
            }
            if (GpuSeries != null)
            {
                GpuSeries.Visibility = (DisplayGpu) ? Visibility.Visible : Visibility.Collapsed;
            }
            if (RamSeries != null)
            {
                RamSeries.Visibility = (DisplayRam) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (FpsSeries != null)
            {
                FpsSeries.Visibility = (DisplayFps) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void PART_ChartLogActivity_UpdaterTick(object sender)
        {
            SetChartVisibility();
        }
        #endregion
    }


    public class PluginChartLogDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double ChartLogHeight { get; set; }
        public bool ChartLogAxis { get; set; }
        public bool ChartLogOrdinates { get; set; }
        public bool ChartLogVisibleEmpty { get; set; }
        public bool UseControls { get; set; }

        public bool DisableAnimations { get; set; }
        public bool DisplayCpu { get; set; }
        public bool DisplayGpu { get; set; }
        public bool DisplayRam { get; set; }
        public bool DisplayFps { get; set; }
    }
}
