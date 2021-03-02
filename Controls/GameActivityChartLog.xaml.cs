using CommonPluginsPlaynite.Common;
using CommonPluginsShared;
using CommonPluginsShared.Controls;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Logique d'interaction pour GameActivityChartLog.xaml
    /// </summary>
    public partial class GameActivityChartLog : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        #region Property
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(GameActivityChartLog),
            new FrameworkPropertyMetadata(false, SettingsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public static readonly DependencyProperty DateSelectedProperty;
        public DateTime? DateSelected { get; set; } = null;

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
            typeof(GameActivityChartLog),
            new FrameworkPropertyMetadata(0, AxisVariatoryPropertyChangedCallback));
        #endregion


        public GameActivityChartLog()
        {
            InitializeComponent();

            PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
            PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
            PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

            // Apply settings
            PluginSettings_PropertyChanged(null, null);
        }


        #region OnPropertyChange
        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            GameActivityChartLog obj = sender as GameActivityChartLog;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }

        private static void AxisVariatoryPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            GameActivityChartLog obj = sender as GameActivityChartLog;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.GameContextChanged(null, obj.GameContext);
            }
        }

        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Apply settings
            if (IgnoreSettings)
            {
                this.DataContext = new
                {
                    DisableAnimations,
                    ChartLogHeight = double.NaN,
                    ChartLogAxis = true,
                    ChartLogOrdinates = true
                };
            }
            else
            {
                this.DataContext = new
                {
                    DisableAnimations,
                    PluginDatabase.PluginSettings.Settings.ChartLogHeight,
                    PluginDatabase.PluginSettings.Settings.ChartLogAxis,
                    PluginDatabase.PluginSettings.Settings.ChartLogOrdinates
                };
            }

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (IgnoreSettings)
            {
                MustDisplay = true;
            }
            else
            {
                MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartLog;

                // When control is not used
                if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationChartLog)
                {
                    return;
                }
            }

            if (newContext != null)
            {
                GameActivities gameActivities = PluginDatabase.Get(newContext);

                if (!gameActivities.HasDataDetails() && !PluginDatabase.PluginSettings.Settings.ChartLogVisibleEmpty)
                {
                    MustDisplay = false;
                    return;
                }

                int Limit = PluginDatabase.PluginSettings.Settings.ChartTimeCountAbscissa;
                if (AxisLimit != 0)
                {
                    Limit = AxisLimit;
                }

                GetActivityForGamesLogGraphics(gameActivities, AxisVariator, Limit, DateSelected, TitleChart);
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

                                //variateurLog = _variateurLogTemp;
                                this.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    AxisVariator++;
                                });
                            }

                            //_variateurLogTemp = variateurLog;

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

    }
}
