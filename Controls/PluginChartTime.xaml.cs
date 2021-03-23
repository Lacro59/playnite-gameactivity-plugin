using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsPlaynite.Common;
using CommonPluginsPlaynite.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Controls;
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

        public event DataClickHandler GameSeriesDataClick;


        #region Property
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, SettingsPropertyChangedCallback));

        public bool LabelsRotation
        {
            get { return (bool)GetValue(LabelsRotationProperty); }
            set { SetValue(LabelsRotationProperty, value); }
        }

        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation),
            typeof(bool),
            typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, SettingsPropertyChangedCallback));

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
            new FrameworkPropertyMetadata(0, AxisVariatoryPropertyChangedCallback));
        #endregion


        public PluginChartTime()
        {
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


        #region OnPropertyChange
        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PluginChartTime obj = sender as PluginChartTime;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }

        private static void AxisVariatoryPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PluginChartTime obj = sender as PluginChartTime;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.GameContextChanged(null, obj.GameContext);
            }
        }

        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            double LabelsRotationValue = 0;
            if (LabelsRotation)
            {
                LabelsRotationValue = 160;
            }

            // Apply settings
            if (IgnoreSettings)
            {
                this.DataContext = new
                {
                    DisableAnimations,
                    ChartTimeHeight = double.NaN,
                    ChartTimeAxis = true,
                    ChartTimeOrdinates = true,
                    LabelsRotationValue
                };
            }
            else
            {
                this.DataContext = new
                {
                    DisableAnimations,
                    PluginDatabase.PluginSettings.Settings.ChartTimeHeight,
                    PluginDatabase.PluginSettings.Settings.ChartTimeAxis,
                    PluginDatabase.PluginSettings.Settings.ChartTimeOrdinates,
                    LabelsRotationValue
                };
            }

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (!PluginDatabase.IsLoaded)
            {
                return;
            }

            if (IgnoreSettings)
            {
                MustDisplay = true;
            }
            else
            {
                MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartTime;

                // When control is not used
                if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationChartTime)
                {
                    return;
                }
            }

            if (newContext != null)
            {
                GameActivities gameActivities = PluginDatabase.Get(newContext);

                if (!gameActivities.HasData && !PluginDatabase.PluginSettings.Settings.ChartTimeVisibleEmpty)
                {
                    MustDisplay = false;
                    return;
                }

                int Limit = PluginDatabase.PluginSettings.Settings.ChartTimeCountAbscissa;
                if (AxisLimit != 0)
                {
                    Limit = AxisLimit;
                }

                GetActivityForGamesTimeGraphics(gameActivities, AxisVariator, Limit);
            }
            else
            {
       
            }
        }
        #endregion


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
                        long elapsedSeconds = Activities[iActivity].ElapsedSeconds;
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
                                        Values = series1[iDay].Values + elapsedSeconds,
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
                                            Values = elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series2[iDay].Values == 0)
                                    {
                                        HasData2 = true;
                                        series2[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series3[iDay].Values == 0)
                                    {
                                        HasData3 = true;
                                        series3[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series4[iDay].Values == 0)
                                    {
                                        HasData4 = true;
                                        series4[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = elapsedSeconds,
                                        };
                                        continue;
                                    }

                                    if (series5[iDay].Values == 0)
                                    {
                                        HasData5 = true;
                                        series5[iDay] = new CustomerForTime
                                        {
                                            Name = tempName,
                                            Values = elapsedSeconds,
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

                        LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                        Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((long)value, null, null, CultureInfo.CurrentCulture);
                        PART_ChartTimeActivityLabelsY.LabelFormatter = activityForGameLogFormatter;

                        PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForTime();
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
}
