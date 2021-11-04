using CommonPluginsControls.Controls;
using CommonPluginsControls.LiveChartsCommon;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Interfaces;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Events;
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
    /// Logique d'interaction pour PluginChartTime.xaml
    /// </summary>
    public partial class PluginChartTime : PluginUserControlExtend
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

        private PluginChartTimeDataContext ControlDataContext;
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginChartTimeDataContext)_ControlDataContext;
            }
        }

        public event DataClickHandler GameSeriesDataClick;


        #region Properties
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool LabelsRotation
        {
            get { return (bool)GetValue(LabelsRotationProperty); }
            set { SetValue(LabelsRotationProperty, value); }
        }

        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public int AxisVariator
        {
            get { return (int)GetValue(AxisVariatoryProperty); }
            set { SetValue(AxisVariatoryProperty, value); }
        }

        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator),
            typeof(int),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));
        #endregion


        public PluginChartTime()
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

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartTime;
            double ChartTimeHeight = PluginDatabase.PluginSettings.Settings.ChartTimeHeight;
            bool ChartTimeAxis = PluginDatabase.PluginSettings.Settings.ChartTimeAxis;
            bool ChartTimeOrdinates = PluginDatabase.PluginSettings.Settings.ChartTimeOrdinates;
            if (IgnoreSettings)
            {
                IsActivated = true;
                ChartTimeHeight = double.NaN;
                ChartTimeAxis = true;
                ChartTimeOrdinates = true;
            }

            double LabelsRotationValue = 0;
            if (LabelsRotation)
            {
                LabelsRotationValue = 160;
            }

            ControlDataContext = new PluginChartTimeDataContext
            {
                IsActivated = IsActivated,
                ChartTimeHeight = ChartTimeHeight,
                ChartTimeAxis = ChartTimeAxis,
                ChartTimeOrdinates = ChartTimeOrdinates,
                ChartTimeVisibleEmpty = PluginDatabase.PluginSettings.Settings.ChartTimeVisibleEmpty,

                DisableAnimations = DisableAnimations,
                LabelsRotationValue = LabelsRotationValue
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            bool IgnoreSettings = this.IgnoreSettings;
            bool MustDisplay = this.MustDisplay;

            int Limit = PluginDatabase.PluginSettings.Settings.ChartTimeCountAbscissa;
            if (AxisLimit != 0)
            {
                Limit = AxisLimit;
            }

            int AxisVariator = this.AxisVariator;

            return Task.Run(() =>
            {
                GameActivities gameActivities = (GameActivities)PluginGameData;

                if (!IgnoreSettings && !ControlDataContext.ChartTimeVisibleEmpty)
                {
                    MustDisplay = gameActivities.HasData;
                }
                else
                {
                    MustDisplay = true;
                }

                if (MustDisplay)
                {
                    GetActivityForGamesTimeGraphics(gameActivities, AxisVariator, Limit);
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.MustDisplay = MustDisplay;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }


        #region Public method
        public void Next()
        {
            AxisVariator += 1;
        }

        public void Prev()
        {
            AxisVariator -= 1;
        }
        #endregion


        private void GetActivityForGamesTimeGraphics(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            Task.Run(() =>
            {
                try
                {
                    string[] listDate = new string[limit + 1];
                    ChartValues<CustomerForTime> series1 = new ChartValues<CustomerForTime>();
                    ChartValues<CustomerForTime> series2 = new ChartValues<CustomerForTime>();
                    ChartValues<CustomerForTime> series3 = new ChartValues<CustomerForTime>();
                    ChartValues<CustomerForTime> series4 = new ChartValues<CustomerForTime>();
                    ChartValues<CustomerForTime> series5 = new ChartValues<CustomerForTime>();

                    bool HasData2 = false;
                    bool HasData3 = false;
                    bool HasData4 = false;
                    bool HasData5 = false;

                    List<Activity> Activities = gameActivities.Items;

                    // Find last activity date
                    DateTime dateStart = new DateTime(1982, 12, 15, 0, 0, 0);
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                        if (dateSession > dateStart)
                        {
                            dateStart = dateSession;
                        }
                    }
                    dateStart = dateStart.AddDays(variateurTime);

                    // Periode data showned
                    for (int i = limit; i >= 0; i--)
                    {
                        listDate[(limit - i)] = dateStart.AddDays(-i).ToString("yyyy-MM-dd");
                        CustomerForTime customerForTime = new CustomerForTime
                        {
                            Name = dateStart.AddDays(-i).ToString("yyyy-MM-dd"),
                            Values = 0
                        };
                        series1.Add(customerForTime);
                        series2.Add(customerForTime);
                        series3.Add(customerForTime);
                        series4.Add(customerForTime);
                        series5.Add(customerForTime);
                    }

                    LocalDateConverter localDateConverter = new LocalDateConverter();

                    // Search data in periode
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        string dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime().ToString("yyyy-MM-dd");

                        //for (int iDay = 0; iDay < 10; iDay++)
                        for (int iDay = limit; iDay >= 0; iDay--)
                        {
                            if (listDate[iDay] == dateSession)
                            {
                                string tempName = series1[iDay].Name;
                                try
                                {
                                    tempName = (string)localDateConverter.Convert(DateTime.ParseExact(series1[iDay].Name, "yyyy-MM-dd", null), null, null, null);
                                }
                                catch
                                {
                                }

                                if (PluginDatabase.PluginSettings.Settings.CumulPlaytimeSession)
                                {
                                    series1[iDay] = new CustomerForTime
                                    {
                                        Name = tempName,
                                        Values = series1[iDay].Values + (long)elapsedSeconds,
                                    };
                                    continue;
                                }
                                else
                                {
                                    if (series1[iDay].Values == 0)
                                    {
                                        series1[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = (long)elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series2[iDay].Values == 0)
                                    {
                                        HasData2 = true;
                                        series2[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = (long)elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series3[iDay].Values == 0)
                                    {
                                        HasData3 = true;
                                        series3[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = (long)elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series4[iDay].Values == 0)
                                    {
                                        HasData4 = true;
                                        series4[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = (long)elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series5[iDay].Values == 0)
                                    {
                                        HasData5 = true;
                                        series5[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = (long)elapsedSeconds,
                                        };
                                        continue;
                                    }
                                }
                            }
                        }
                    }


                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        // Set data in graphic.
                        SeriesCollection activityForGameSeries = new SeriesCollection();
                        activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = series1 });
                        if (HasData2)
                        {
                            activityForGameSeries.Add(new ColumnSeries { Title = "2", Values = series2 });
                        }
                        if (HasData3)
                        {
                            activityForGameSeries.Add(new ColumnSeries { Title = "3", Values = series3 });
                        }
                        if (HasData4)
                        {
                            activityForGameSeries.Add(new ColumnSeries { Title = "4", Values = series4 });
                        }
                        if (HasData5)
                        {
                            activityForGameSeries.Add(new ColumnSeries { Title = "5", Values = series5 });
                        }

                        for (int iDay = 0; iDay < listDate.Length; iDay++)
                        {
                            listDate[iDay] = Convert.ToDateTime(listDate[iDay]).ToString(Constants.DateUiFormat);
                        }
                        string[] activityForGameLabels = listDate;

                        //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                        var customerVmMapper = Mappers.Xy<CustomerForTime>()
                        .X((value, index) => index)
                        .Y(value => value.Values);


                        //lets save the mapper globally
                        Charting.For<CustomerForTime>(customerVmMapper);

                        PlayTimeToStringConverter converter = new PlayTimeToStringConverter();
                        Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);
                        PART_ChartTimeActivityLabelsY.LabelFormatter = activityForGameLogFormatter;

                        if (PluginDatabase.PluginSettings.Settings.CumulPlaytimeSession)
                        {
                            PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForTime
                            {
                                ShowIcon = PluginDatabase.PluginSettings.Settings.ShowLauncherIcons,
                                Mode = (PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstWithText : TextBlockWithIconMode.IconFirstWithText
                            };
                        }
                        else
                        {
                            PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForMultipleTime { ShowTitle = false };
                        }

                        PART_ChartTimeActivityLabelsY.MinValue = 0;
                        PART_ChartTimeActivity.Series = activityForGameSeries;
                        PART_ChartTimeActivityLabelsX.Labels = activityForGameLabels;
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            });
        }


        #region Events
        private void PART_ChartTimeActivity_DataClick(object sender, ChartPoint chartPoint)
        {
            this.GameSeriesDataClick?.Invoke(this, chartPoint);
        }
        #endregion
    }


    public class PluginChartTimeDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double ChartTimeHeight { get; set; }
        public bool ChartTimeAxis { get; set; }
        public bool ChartTimeOrdinates { get; set; }
        public bool ChartTimeVisibleEmpty { get; set; }

        public bool DisableAnimations { get; set; }
        public double LabelsRotationValue { get; set; }
    }
}
