using GameActivity.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityGameGraphicLog.xaml
    /// </summary>
    public partial class GameActivityGameGraphicLog : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public int variateurLog = 0;
        public int variateurLogTemp = 0;

        public GameActivityGameGraphicLog(GameActivitySettings settings, GameActivityClass gameActivity, string dateSelected = "", string title = "", int variateurLog = 0, bool withTitle = true)
        {
            InitializeComponent();

            this.variateurLog = variateurLog;
            GetActivityForGamesLogGraphics(gameActivity, withTitle, dateSelected, title);

            if (!settings.IgnoreSettings)
            {
                gameLabelsX.ShowLabels = settings.EnableIntegrationAxisGraphicLog;
                gameLabelsY.ShowLabels = settings.EnableIntegrationOrdinatesGraphicLog;
            }
        }

        public void GetActivityForGamesLogGraphics(GameActivityClass gameActivity, bool withTitle, string dateSelected = "", string title = "")
        {
            List<ActivityDetailsData> gameActivitiesDetails = gameActivity.GetSessionActivityDetails(dateSelected, title);

            string[] activityForGameLogLabels = new string[0];
            List<ActivityDetailsData> gameLogsDefinitive = new List<ActivityDetailsData>();
            if (gameActivitiesDetails.Count > 0)
            {
                if (gameActivitiesDetails.Count > 10)
                {
                    // Variateur
                    int conteurEnd = gameActivitiesDetails.Count + variateurLog;
                    int conteurStart = conteurEnd - 10;

                    if (conteurEnd > gameActivitiesDetails.Count)
                    {
                        int temp = conteurEnd - gameActivitiesDetails.Count;
                        conteurEnd = gameActivitiesDetails.Count;
                        conteurStart = conteurEnd - 10;

                        variateurLog = variateurLogTemp;
                    }

                    if (conteurStart < 0)
                    {
                        conteurStart = 0;
                        conteurEnd = 10;

                        variateurLog = variateurLogTemp;
                    }

                    variateurLogTemp = variateurLog;

                    // Create data
                    int sCount = 0;
                    activityForGameLogLabels = new string[10];
                    for (int iLog = conteurStart; iLog < conteurEnd; iLog++)
                    {
                        gameLogsDefinitive.Add(gameActivitiesDetails[iLog]);
                        activityForGameLogLabels[sCount] = Convert.ToDateTime(gameActivitiesDetails[iLog].Datelog).ToLocalTime().ToString(Constants.TimeUiFormat);
                        sCount += 1;
                    }
                }
                else
                {
                    gameLogsDefinitive = gameActivitiesDetails;

                    activityForGameLogLabels = new string[gameActivitiesDetails.Count];
                    for (int iLog = 0; iLog < gameActivitiesDetails.Count; iLog++)
                    {
                        activityForGameLogLabels[iLog] = Convert.ToDateTime(gameActivitiesDetails[iLog].Datelog).ToLocalTime().ToString(Constants.TimeUiFormat);
                    }
                }
            }
            else
            {
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

            SeriesCollection activityForGameLogSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "cpu usage (%)",
                    Values = CPUseries
                },
                new ColumnSeries
                {
                    Title = "gpu usage (%)",
                    Values = GPUseries
                },
                new ColumnSeries
                {
                    Title = "ram usage (%)",
                    Values = RAMseries
                },
                new LineSeries
                {
                    Title = "fps",
                    Values = FPSseries
                }
            };
            Func<double, string> activityForGameLogFormatter = value => value.ToString("N");

            gameSeriesLog.DataTooltip = new LiveCharts.Wpf.DefaultTooltip();
            gameSeriesLog.DataTooltip.Background = (Brush)resources.GetResource("CommonToolTipBackgroundBrush");
            gameSeriesLog.DataTooltip.Padding = new Thickness(10);
            gameSeriesLog.DataTooltip.BorderThickness = (Thickness)resources.GetResource("CommonToolTipBorderThickness");
            gameSeriesLog.DataTooltip.BorderBrush = (Brush)resources.GetResource("CommonToolTipBorderBrush");
            gameSeriesLog.DataTooltip.Foreground = (Brush)resources.GetResource("CommonToolTipForeground");

            gameSeriesLog.Series = activityForGameLogSeries;
            gameLabelsY.MinValue = 0;
            gameLabelsX.Labels = activityForGameLogLabels;
            gameLabelsY.LabelFormatter = activityForGameLogFormatter;

            if (withTitle)
            {
                lGameSeriesLog.Visibility = Visibility.Visible;
                lGameSeriesLog.Content = resources.GetString("LOCGameActivityLogTitleDate") + " "
                    + ((DateTime)gameActivitiesDetails[0].Datelog).ToString(Constants.DateUiFormat);
            }
        }

        private void GameSeriesLog_Loaded(object sender, RoutedEventArgs e)
        {
            // Define height & width
            var parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)gameSeriesLog.Parent).Parent).Parent);

#if DEBUG
            logger.Debug($"SuccessStory - GameActivityGameGraphicLog() - parent.name: {parent.Name} - parent.Height: {parent.Height} - parent.Width: {parent.Width} -  - lGameSeriesLog.ActualHeight: {lGameSeriesLog.ActualHeight}");
#endif

            if (!double.IsNaN(parent.Height))
            {
                gameSeriesLog.Height = parent.Height - lGameSeriesLog.ActualHeight;
            }
            else
            {
                gameSeriesLog.Height = parent.ActualHeight - lGameSeriesLog.ActualHeight;
            }

            if (!double.IsNaN(parent.Width))
            {
                gameSeriesLog.Width = parent.Width;
            }
            else
            {
                gameSeriesLog.Width = parent.ActualWidth;
            }
        }
    }
}
