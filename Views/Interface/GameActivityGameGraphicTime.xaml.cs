using GameActivity.Models;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Events;
using LiveCharts.Wpf;
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
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityGameGraphicTime.xaml
    /// </summary>
    public partial class GameActivityGameGraphicTime : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public event DataClickHandler gameSeriesDataClick;


        public GameActivityGameGraphicTime(GameActivitySettings settings, GameActivityClass gameActivity, int variateurTime = 0)
        {
            InitializeComponent();

            GetActivityForGamesTimeGraphics(gameActivity, variateurTime);

            gameLabelsX.ShowLabels = settings.EnableIntegrationAxisGraphic;
            gameLabelsY.ShowLabels = settings.EnableIntegrationOrdinatesGraphic;
        }

        public void GetActivityForGamesTimeGraphics(GameActivityClass gameActivity, int variateurTime)
        {
            string[] listDate = new string[10];
            ChartValues<CustomerForTime> series1 = new ChartValues<CustomerForTime>();
            ChartValues<CustomerForTime> series2 = new ChartValues<CustomerForTime>();
            ChartValues<CustomerForTime> series3 = new ChartValues<CustomerForTime>();
            ChartValues<CustomerForTime> series4 = new ChartValues<CustomerForTime>();
            ChartValues<CustomerForTime> series5 = new ChartValues<CustomerForTime>();

            bool HasData2 = false;
            bool HasData3 = false;
            bool HasData4 = false;
            bool HasData5 = false;

            List<Activity> gameActivities = gameActivity.Activities;

            // Find last activity date
            DateTime dateStart = new DateTime(1982, 12, 15, 0, 0, 0);
            for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
            {
                DateTime dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).ToLocalTime();
                if (dateSession > dateStart)
                {
                    dateStart = dateSession;
                }
            }
            dateStart = dateStart.AddDays(variateurTime);

            // Periode data showned
            for (int iDay = 0; iDay < 10; iDay++)
            {
                listDate[iDay] = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd");
                series1.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                });
                series2.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                });
                series3.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                });
                series4.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                });
                series5.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                });
            }


            // Search data in periode
            for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
            {
                long elapsedSeconds = gameActivities[iActivity].ElapsedSeconds;
                string dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).ToLocalTime().ToString("yyyy-MM-dd");

                for (int iDay = 0; iDay < 10; iDay++)
                {
                    if (listDate[iDay] == dateSession)
                    {
                        string tempName = series1[iDay].Name;

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


            // Set data in graphic.
            SeriesCollection activityForGameSeries = new SeriesCollection();
            activityForGameSeries.Add (new ColumnSeries { Title = "1", Values = series1 });
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
            gameLabelsY.LabelFormatter = activityForGameLogFormatter;

            gameSeries.DataTooltip = new CustomerToolTipForTime();
            gameLabelsY.MinValue = 0;
            gameSeries.Series = activityForGameSeries;
            gameLabelsX.Labels = activityForGameLabels;
        }

        private void GameSeries_Loaded(object sender, RoutedEventArgs e)
        {
            // Define height & width
            var parent = ((FrameworkElement)((FrameworkElement)gameSeries.Parent).Parent);

            if (!double.IsNaN(parent.Height))
            {
                gameSeries.Height = parent.Height;
            }
            else
            {
                gameSeries.Height = parent.ActualHeight;
            }

            if (!double.IsNaN(parent.Width))
            {
                gameSeries.Width = parent.Width;
            }
            else
            {
                gameSeries.Width = parent.ActualWidth;
            }
        }

        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {
            this.gameSeriesDataClick?.Invoke(this, chartPoint);
        }
    }
}
