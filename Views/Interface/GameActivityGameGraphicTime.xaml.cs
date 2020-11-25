using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Events;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using PluginCommon.LiveChartsCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityGameGraphicTime.xaml
    /// </summary>
    public partial class GameActivityGameGraphicTime : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public event DataClickHandler gameSeriesDataClick;

        private int _variateurTimeInitial = 0;
        private int _variateurTime = 0;
        private int _limit;


        public GameActivityGameGraphicTime(int variateurTime = 0, int limit = 9)
        {
            InitializeComponent();

            _variateurTimeInitial = variateurTime;
            _variateurTime = variateurTime;
            _limit = limit;

            if (!PluginDatabase.PluginSettings.IgnoreSettings)
            {
                PART_ChartTimeActivityLabelsX.ShowLabels = PluginDatabase.PluginSettings.EnableIntegrationAxisGraphic;
                PART_ChartTimeActivityLabelsY.ShowLabels = PluginDatabase.PluginSettings.EnableIntegrationOrdinatesGraphic;
            }

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"GameActivityGameGraphicTime.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        GetActivityForGamesTimeGraphics(PluginDatabase.GameSelectedData, _variateurTime, _limit);
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }


        public void GetActivityForGamesTimeGraphics(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            try
            {
                if (!gameActivities.HasData)
                {
                    this.Visibility = Visibility.Collapsed;
                    return;
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                }


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

                            if (PluginDatabase.PluginSettings.CumulPlaytimeSession)
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
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }

        private void PART_ChartTimeActivity_DataClick(object sender, ChartPoint chartPoint)
        {
            this.gameSeriesDataClick?.Invoke(this, chartPoint);
        }

        public void DisableAnimations(bool IsDisable)
        {
            PART_ChartTimeActivity.DisableAnimations = IsDisable;
        }



        private void PART_ChartTimeActivity_Loaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize(PART_ChartTimeActivity, PluginDatabase.PluginSettings.IntegrationShowGraphicHeight, 0);
        }

    }
}
