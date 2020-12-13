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
using PluginCommon.PlayniteResources.Common;
using System.Windows.Threading;
using System.Threading;
using Newtonsoft.Json;
using GameActivity.Services;
using System.Threading.Tasks;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityGameGraphicLog.xaml
    /// </summary>
    public partial class GameActivityGameGraphicLog : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private DateTime? _dateSelected;
        private string _title;
        private int _variateurLogInitial = 0;
        private int _variateurLog = 0;
        private int _variateurLogTemp = 0;
        private bool _withTitle;
        private int _limit;

        public GameActivityGameGraphicLog(DateTime? dateSelected = null, string title = "", int variateurLog = 0, bool withTitle = true, int limit = 10)
        {
            InitializeComponent();

            _dateSelected = dateSelected;
            _title = title;
            _variateurLogInitial = variateurLog;
            _variateurLog = variateurLog;
            _withTitle = withTitle;
            _limit = limit;

            if (!PluginDatabase.PluginSettings.IgnoreSettings)
            {
                PART_ChartLogActivityLabelsX.ShowLabels = PluginDatabase.PluginSettings.EnableIntegrationAxisGraphicLog;
                PART_ChartLogActivityLabelsY.ShowLabels = PluginDatabase.PluginSettings.EnableIntegrationOrdinatesGraphicLog;
            }

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        GetActivityForGamesLogGraphics(PluginDatabase.GameSelectedData, _withTitle, _dateSelected, _title, _variateurLog, _limit);
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
        

        public void GetActivityForGamesLogGraphics(GameActivities gameActivities, bool withTitle, DateTime? dateSelected = null, string title = "", int variateurLog = 0, int limit = 10)
        {
            Task.Run(() =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (!gameActivities.HasDataDetails(dateSelected, title))
                        {
                            this.Visibility = Visibility.Collapsed;
                            return;
                        }
                        else
                        {
                            this.Visibility = Visibility.Visible;
                        }
                    }));


                    List<ActivityDetailsData> ActivitiesDetails = gameActivities.GetSessionActivityDetails(dateSelected, title);
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
                                int temp = conteurEnd - ActivitiesDetails.Count;
                                conteurEnd = ActivitiesDetails.Count;
                                conteurStart = conteurEnd - limit;

                                variateurLog = _variateurLogTemp;
                            }

                            if (conteurStart < 0)
                            {
                                conteurStart = 0;
                                conteurEnd = limit;

                                variateurLog = _variateurLogTemp;
                            }

                            _variateurLogTemp = variateurLog;

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

                        PART_ChartLogActivity.DataTooltip = new LiveCharts.Wpf.DefaultTooltip();
                        PART_ChartLogActivity.DataTooltip.Background = (Brush)resources.GetResource("CommonToolTipBackgroundBrush");
                        PART_ChartLogActivity.DataTooltip.Padding = new Thickness(10);
                        PART_ChartLogActivity.DataTooltip.BorderThickness = (Thickness)resources.GetResource("CommonToolTipBorderThickness");
                        PART_ChartLogActivity.DataTooltip.BorderBrush = (Brush)resources.GetResource("CommonToolTipBorderBrush");
                        PART_ChartLogActivity.DataTooltip.Foreground = (Brush)resources.GetResource("CommonToolTipForeground");

                        PART_ChartLogActivity.Series = activityForGameLogSeries;
                        PART_ChartLogActivityLabelsY.MinValue = 0;
                        PART_ChartLogActivityLabelsX.Labels = activityForGameLogLabels;
                        PART_ChartLogActivityLabelsY.LabelFormatter = activityForGameLogFormatter;

                        if (withTitle)
                        {
                            lGameSeriesLog.Visibility = Visibility.Visible;
                            lGameSeriesLog.Content = resources.GetString("LOCGameActivityLogTitleDate") + " "
                                + ((DateTime)ActivitiesDetails[0].Datelog).ToString(Constants.DateUiFormat);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            });
        }

        public void DisableAnimations(bool IsDisable)
        {
            PART_ChartLogActivity.DisableAnimations = IsDisable;
        }



        private void PART_ChartLogActivity_Loaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize(PART_ChartLogActivity, PluginDatabase.PluginSettings.IntegrationShowGraphicLogHeight, 0);

            if (lGameSeriesLog.Visibility == Visibility.Visible)
            {
                PART_ChartLogActivity.Height = PART_ChartLogActivity.Height - lGameSeriesLog.ActualHeight - 5;
            }
        }
    }
}
