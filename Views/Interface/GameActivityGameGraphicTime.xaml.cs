using GameActivity.Models;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Playnite.Converters;
using PluginCommon;
using PluginCommon.LiveChartsCommon;
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
        public GameActivityGameGraphicTime(GameActivitySettings settings, GameActivityClass gameActivity)
        {
            InitializeComponent();

            GetActivityForGamesTimeGraphics(gameActivity);
        }

        public void GetActivityForGamesTimeGraphics(GameActivityClass gameActivity, int variateurTime = 0)
        {
            //DateTime dateStart = DateTime.Now.AddDays(variateurTime);
            string[] listDate = new string[10];
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();

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
                series.Add(new CustomerForTime
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
                        string tempName = series[iDay].Name;
                        long tempElapsed = series[iDay].Values + elapsedSeconds;
                        series[iDay] = new CustomerForTime
                        {
                            Name = tempName,
                            Values = tempElapsed,
                        };
                    }
                }
            }


            // Set data in graphic.
            SeriesCollection activityForGameSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "",
                    Values = series
                }
            };
            for (int iDay = 0; iDay < listDate.Length; iDay++)
            {
                listDate[iDay] = Convert.ToDateTime(listDate[iDay]).ToString(Playnite.Common.Constants.DateUiFormat);
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

        private void GameSeries_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
            {
                if (sp.Name == "PART_GameActivty_Graphic")
                {
                    gameSeries.Height = sp.MaxHeight;
                }
            }
        }
    }
}
