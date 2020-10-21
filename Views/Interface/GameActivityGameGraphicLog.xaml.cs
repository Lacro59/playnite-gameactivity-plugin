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

        private string _dateSelected;
        private string _title;
        private int _variateurLogInitial = 0;
        private int _variateurLog = 0;
        private int _variateurLogTemp = 0;
        private bool _withTitle;
        private int _limit;

        public GameActivityGameGraphicLog(GameActivitySettings settings, GameActivityClass gameActivity, string dateSelected = "", string title = "", int variateurLog = 0, bool withTitle = true, int limit = 10)
        {
            InitializeComponent();

            _dateSelected = dateSelected;
            _title = title;
            _variateurLogInitial = variateurLog;
            _variateurLog = variateurLog;
            _withTitle = withTitle;
            _limit = limit;

            GetActivityForGamesLogGraphics(gameActivity, _withTitle, _dateSelected, _title, _limit);

            if (!settings.IgnoreSettings)
            {
                gameLabelsX.ShowLabels = settings.EnableIntegrationAxisGraphicLog;
                gameLabelsY.ShowLabels = settings.EnableIntegrationOrdinatesGraphicLog;
            }
        }

        public void GetActivityForGamesLogGraphics(GameActivityClass gameActivity, bool withTitle, string dateSelected = "", string title = "", int limit = 10)
        {
            List<ActivityDetailsData> gameActivitiesDetails = gameActivity.GetSessionActivityDetails(dateSelected, title);

            string[] activityForGameLogLabels = new string[0];
            List<ActivityDetailsData> gameLogsDefinitive = new List<ActivityDetailsData>();
            if (gameActivitiesDetails.Count > 0)
            {
                if (gameActivitiesDetails.Count > limit)
                {
                    // Variateur
                    int conteurEnd = gameActivitiesDetails.Count + _variateurLog;
                    int conteurStart = conteurEnd - limit;

                    if (conteurEnd > gameActivitiesDetails.Count)
                    {
                        int temp = conteurEnd - gameActivitiesDetails.Count;
                        conteurEnd = gameActivitiesDetails.Count;
                        conteurStart = conteurEnd - limit;

                        _variateurLog = _variateurLogTemp;
                    }

                    if (conteurStart < 0)
                    {
                        conteurStart = 0;
                        conteurEnd = limit;

                        _variateurLog = _variateurLogTemp;
                    }

                    _variateurLogTemp = _variateurLog;

                    // Create data
                    int sCount = 0;
                    activityForGameLogLabels = new string[limit];
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
            logger.Debug($"GameActivity - GameActivityGameGraphicLog() - parent.name: {parent.Name} - parent.Height: {parent.Height} - parent.Width: {parent.Width} -  - lGameSeriesLog.ActualHeight: {lGameSeriesLog.ActualHeight}");
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

        public void SetGaData(GameActivityClass gameActivity)
        {
            _variateurLog = _variateurLogInitial;
            GetActivityForGamesLogGraphics(gameActivity, _withTitle, _dateSelected, _title, _limit);
        }
    }
}
