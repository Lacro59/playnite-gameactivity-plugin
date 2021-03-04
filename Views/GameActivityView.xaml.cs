using System;
using System.Collections.Generic;
using System.Windows;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.ComponentModel;
using GameActivity.Models;
using LiveCharts;
using CommonPluginsShared;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Globalization;
using LiveCharts.Events;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Threading.Tasks;
using CommonPluginsPlaynite.Converters;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsPlaynite.Common;
using GameActivity.Services;
using GameActivity.Controls;

namespace GameActivity
{
    /// <summary>
    /// Logique d'interaction pour GameActivity.xaml.
    /// </summary>
    public partial class GameActivityView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private JArray listSources { get; set; }
        private DateTime LabelDataSelected { get; set; }

        private GameActivityChartTime PART_GameActivityChartTime;
        private GameActivityChartLog PART_GameActivityChartLog;

        private LongToTimePlayedConverter converter = new LongToTimePlayedConverter();

        public int yearCurrent;
        public int monthCurrent;
        public string gameIDCurrent;
        public int variateurTime = 0;
        public int variateurLog = 0;
        public int variateurLogTemp = 0;
        public string titleChart;

        private List<ListSource> FilterSourceItems = new List<ListSource>();
        private List<string> SearchSources = new List<string>();
        public List<ListActivities> activityListByGame { get; set; }

        GameActivitySettings _settings { get; set; }

        public bool isMonthSources = true;
        public bool isGameTime = true;

        public bool ShowIcon { get; set; }

        // Variables api.
        public readonly IPlayniteAPI _PlayniteApi;
        public readonly IGameDatabaseAPI dbPlaynite;
        public readonly IPlaynitePathsAPI pathsPlaynite;
        public readonly string pathExtentionData;


        public GameActivityView(Game GameSelected = null)
        {
            _PlayniteApi = PluginDatabase.PlayniteApi;
            dbPlaynite = PluginDatabase.PlayniteApi.Database;
            pathsPlaynite = PluginDatabase.PlayniteApi.Paths;
            _settings = PluginDatabase.PluginSettings.Settings;
            pathExtentionData = PluginDatabase.Paths.PluginUserDataPath;

            // Set dates variables
            yearCurrent = DateTime.Now.Year;
            monthCurrent = DateTime.Now.Month;

            // Initialization components
            InitializeComponent();

            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                ToggleButtonTime.Visibility = Visibility.Hidden;
                ToggleButtonLog.Visibility = Visibility.Hidden;
            }


            PART_GameActivityChartTime = new GameActivityChartTime();
            PART_GameActivityChartTime.IgnoreSettings = true;
            PART_GameActivityChartTime.GameSeriesDataClick += GameSeries_DataClick;
            PART_GameActivityChartTime_Contener.Children.Add(PART_GameActivityChartTime);


            PART_GameActivityChartLog = new GameActivityChartLog();
            PART_GameActivityChartLog.IgnoreSettings = true;
            PART_GameActivityChartLog_Contener.Children.Add(PART_GameActivityChartLog);


            // Add column if log details enable.
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                GridView lvView = (GridView)lvGames.View;

                lvView.Columns.RemoveAt(12);
                lvView.Columns.RemoveAt(11);
                lvView.Columns.RemoveAt(10);
                lvView.Columns.RemoveAt(9);
                lvView.Columns.RemoveAt(8);
                lvView.Columns.RemoveAt(7);

                lvGames.View = lvView;
            }


            // Graphics game details activities.
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");

            
            #region Get & set datas
            listSources = GetListSourcesName();


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_DataTop.Visibility = Visibility.Hidden;
            PART_DataBottom.Visibility = Visibility.Hidden;

            var task = Task.Run(() =>
            {
                try
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        getActivityByMonth(yearCurrent, monthCurrent);
                    });
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        getActivityByWeek(yearCurrent, monthCurrent);
                    });
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        getActivityByDay(yearCurrent, monthCurrent);
                    });
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        getActivityByListGame();
                    });
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        SetSourceFilter();
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Error on task");
                }
            })
            .ContinueWith(antecedent =>
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                { 
                    // Set game selected
                    if (GameSelected != null)
                    {
                        for (int i = 0; i < lvGames.Items.Count; i++)
                        {
                            if (((ListActivities)lvGames.Items[i]).GameTitle == GameSelected.Name)
                            {
                                lvGames.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    lvGames.ScrollIntoView(lvGames.SelectedItem);

                    if (_settings.CumulPlaytimeStore)
                    {
                        acmSeries.Visibility = Visibility.Hidden;
                        acmLabel.Visibility = Visibility.Hidden;

                        Grid.SetColumn(GridDay, 0);
                        Grid.SetColumnSpan(GridDay, 3);
                    }

                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_DataTop.Visibility = Visibility.Visible;
                    PART_DataBottom.Visibility = Visibility.Visible;
                });
            });
            #endregion


            // Set Binding data
            ShowIcon = this._settings.ShowLauncherIcons;
            DataContext = this;
        }


        private void SetSourceFilter()
        {
            var ListSourceName = activityListByGame.Select(x => x.GameSourceName).Distinct();

            foreach (var sourcename in ListSourceName)
            {
                string icon = TransformIcon.Get(sourcename) + " ";
                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + sourcename, SourceNameShort = sourcename, IsCheck = false });
            }

            FilterSourceItems.Sort((x, y) => x.SourceNameShort.CompareTo(y.SourceNameShort));
            FilterSource.ItemsSource = FilterSourceItems;
        }


        #region Generate graphics and list
        /// <summary>
        /// Get data graphic activity by month with time by source or by genre.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        public void getActivityByMonth(int year, int month)
        {
            DateTime startOfMonth = new DateTime(year, month, 1, 0, 0, 0);
            DateTime endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

            JObject activityByMonth = new JObject();

            // Total hours by source.
            if (isMonthSources)
            {
                if (_settings.ShowLauncherIcons)
                {
                    acmLabelsX.LabelsRotation = 0;
                }
                else
                {
                    acmLabelsX.LabelsRotation = 160;
                }

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Activity> Activities = listGameActivities[iGame].Items;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            long elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                            string sourceName = Activities[iActivity].SourceName;

                            // Cumul data
                            if (activityByMonth[sourceName] != null)
                            {
                                if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                {
                                    activityByMonth[sourceName] = (long)activityByMonth[sourceName] + elapsedSeconds;
                                }
                            }
                            else
                            {
                                if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                {
                                    activityByMonth.Add(new JProperty(sourceName, elapsedSeconds));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}");
                    }
                }
            }
            // Total hours by genres.
            else
            {
                acmLabelsX.LabelsRotation = 160;

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                        List<Activity> Activities = listGameActivities[iGame].Items;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            long elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).AddSeconds(-elapsedSeconds).ToLocalTime();

                            for (int iGenre = 0; iGenre < listGameListGenres.Count; iGenre++)
                            {
                                // Cumul data
                                if (activityByMonth[listGameListGenres[iGenre].Name] != null)
                                {
                                    if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                    {
                                        activityByMonth[listGameListGenres[iGenre].Name] = (long)activityByMonth[listGameListGenres[iGenre].Name] + elapsedSeconds;
                                    }
                                }
                                else
                                {
                                    if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                    {
                                        activityByMonth.Add(new JProperty(listGameListGenres[iGenre].Name, elapsedSeconds));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}");
                    }
                }
            }


            // Set data in graphic.
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
            string[] labels = new string[activityByMonth.Count];
            int compteur = 0;
            foreach (var item in activityByMonth)
            {
                series.Add(new CustomerForTime
                {
                    Name = item.Key,
                    Values = (long)item.Value,
                });
                labels[compteur] = item.Key;
                if (_settings.ShowLauncherIcons)
                {
                    labels[compteur] = TransformIcon.Get(labels[compteur]);
                }
                compteur = compteur + 1;
            }


            SeriesCollection ActivityByMonthSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = string.Empty,
                        Values = series
                    }
                };
            string[] ActivityByMonthLabels = labels;

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((long)value, null, null, CultureInfo.CurrentCulture);

            if (isMonthSources)
            {
                if (_settings.CumulPlaytimeStore)
                {
                    acmSeries.Visibility = Visibility.Hidden;
                    acmLabel.Visibility = Visibility.Hidden;

                    Grid.SetColumn(GridDay, 0);
                    Grid.SetColumnSpan(GridDay, 3);
                }

                Grid.SetColumnSpan(gridMonth, 1);
                actSeries.Visibility = Visibility.Visible;
                actLabel.Visibility = Visibility.Visible;
                acwSeries.Visibility = Visibility.Visible;
                acwLabel.Visibility = Visibility.Visible;
            }
            else
            {
                if (_settings.CumulPlaytimeStore)
                {
                    acmSeries.Visibility = Visibility.Visible;
                    acmLabel.Visibility = Visibility.Visible;
                }

                Grid.SetColumnSpan(gridMonth, 5);
                actSeries.Visibility = Visibility.Hidden;
                actLabel.Visibility = Visibility.Hidden;
                acwSeries.Visibility = Visibility.Hidden;
                acwLabel.Visibility = Visibility.Hidden;
            }

            acmLabelsY.LabelFormatter = activityForGameLogFormatter;
            acmSeries.Series = ActivityByMonthSeries;
            acmLabelsY.MinValue = 0;
            ((CustomerToolTipForTime)acmSeries.DataTooltip).ShowIcon = _settings.ShowLauncherIcons;
            acmLabelsX.Labels = ActivityByMonthLabels;
        }

        public void getActivityByDay(int year, int month)
        {
            DateTime StartDate = new DateTime(year, month, 1, 0, 0, 0);
            int NumberDayInMonth = DateTime.DaysInMonth(year, month);

            string[] activityByDateLabels = new string[NumberDayInMonth];
            SeriesCollection activityByDaySeries = new SeriesCollection();
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();

            for (int iDay = 0; iDay < NumberDayInMonth; iDay++)
            {
                activityByDateLabels[iDay] = Convert.ToDateTime(StartDate.AddDays(iDay)).ToString(Constants.DateUiFormat);

                series.Add(new CustomerForTime
                {
                    Name = activityByDateLabels[iDay],
                    Values = 0
                });

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> Activities = listGameActivities[iGame].Items;
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        long elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        string dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToString(Constants.DateUiFormat);

                        if (dateSession == activityByDateLabels[iDay])
                        {
                            series[iDay].Values += elapsedSeconds;
                        }
                    }
                }
            }

            activityByDaySeries.Add(new ColumnSeries
            {
                Title = string.Empty,
                Values = series
            });

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((long)value, null, null, CultureInfo.CurrentCulture);

            actLabelsY.LabelFormatter = activityForGameLogFormatter;
            actSeries.DataTooltip = new CustomerToolTipForTime();
            actSeries.Series = activityByDaySeries;
            actLabelsY.MinValue = 0;
            actLabelsX.Labels = activityByDateLabels;
        }


        /// <summary>
        /// Get data graphic activity by week.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>        
        public void getActivityByWeek(int year, int month)
        {
            //https://www.codeproject.com/Questions/1276907/Get-every-weeks-start-and-end-date-from-series-end
            //usage:
            DateTime StartDate = new DateTime(year, month, 1, 0, 0, 0);
            DateTime SeriesEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
            //find first monday
            DateTime firstMonday = Enumerable.Range(0, 7)
                .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                .Select(x => StartDate.AddDays(x))
                .First();
            //get count of days
            TimeSpan ts = (TimeSpan)(SeriesEndDate - firstMonday);
            //create new list of WeekStartEnd class
            List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();
            //add dates to list
            for (int i = 0; i < ts.Days; i += 7)
            {
                datesPeriodes.Add(new WeekStartEnd() { Monday = firstMonday.AddDays(i), Sunday = firstMonday.AddDays(i + 6).AddHours(23).AddMinutes(59).AddSeconds(59) });
            }

            // Source activty by month
            JObject activityByWeek1 = new JObject();
            JObject activityByWeek2 = new JObject();
            JObject activityByWeek3 = new JObject();
            JObject activityByWeek4 = new JObject();
            JObject activityByWeek5 = new JObject();

            JArray activityByWeek = new JArray();
            SeriesCollection activityByWeekSeries = new SeriesCollection();
            IChartValues Values = new ChartValues<CustomerForTime>();

            if (isMonthSources)
            {
                // Insert sources
                for (int iSource = 0; iSource < listSources.Count; iSource++)
                {
                    activityByWeek1.Add((string)listSources[iSource], 0);
                    activityByWeek2.Add((string)listSources[iSource], 0);
                    activityByWeek3.Add((string)listSources[iSource], 0);
                    activityByWeek4.Add((string)listSources[iSource], 0);
                    activityByWeek5.Add((string)listSources[iSource], 0);
                }

                activityByWeek.Add(activityByWeek1);
                activityByWeek.Add(activityByWeek2);
                activityByWeek.Add(activityByWeek3);
                activityByWeek.Add(activityByWeek4);
                activityByWeek.Add(activityByWeek5);

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> Activities = listGameActivities[iGame].Items;
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        long elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                        string sourceName = Activities[iActivity].SourceName;

                        // Cumul data
                        for (int iWeek = 0; iWeek < datesPeriodes.Count; iWeek++)
                        {
                            if (datesPeriodes[iWeek].Monday <= dateSession && dateSession <= datesPeriodes[iWeek].Sunday)
                            {
                                // Add source by platform
                                if (activityByWeek[iWeek][sourceName] == null)
                                {
                                    activityByWeek1.Add(sourceName, 0);
                                    activityByWeek2.Add(sourceName, 0);
                                    activityByWeek3.Add(sourceName, 0);
                                    activityByWeek4.Add(sourceName, 0);
                                    activityByWeek5.Add(sourceName, 0);
                                }

                                activityByWeek[iWeek][sourceName] = (long)activityByWeek[iWeek][sourceName] + elapsedSeconds;
                            }
                        }
                    }
                }


                // Check source with data (only view this)
                JArray listNoDelete = new JArray();
                for (int i = 0; i < activityByWeek.Count; i++)
                {
                    foreach (var item in (JObject)activityByWeek[i])
                    {
                        if ((long)item.Value != 0 && listNoDelete.TakeWhile(x => x.ToString() == item.Key).Count() != 1)
                        {
                            listNoDelete.Add(item.Key);
                        }
                    }
                }
                listNoDelete = JArray.FromObject(listNoDelete.Distinct().ToArray());


                // Prepare data.
                string[] labels = new string[listNoDelete.Count];
                for (int iSource = 0; iSource < listNoDelete.Count; iSource++)
                {
                    labels[iSource] = (string)listNoDelete[iSource];
                    if (_settings.ShowLauncherIcons)
                    {
                        labels[iSource] = TransformIcon.Get((string)listNoDelete[iSource]);
                    }

                    Values = new ChartValues<CustomerForTime>() {
                            new CustomerForTime{Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[0][(string)listNoDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[1][(string)listNoDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[2][(string)listNoDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[3][(string)listNoDelete[iSource]]}
                        };

                    if (datesPeriodes.Count == 5)
                    {
                        Values.Add(new CustomerForTime { Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[4][(string)listNoDelete[iSource]] });
                    }

                    activityByWeekSeries.Add(new StackedColumnSeries
                    {
                        Title = labels[iSource],
                        Values = Values,
                        StackMode = StackMode.Values,
                        DataLabels = false
                    });
                }
            }


            // Set data in graphics.
            string[] activityByWeekLabels = new[]
            {
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[3].Monday)
            };
            if (datesPeriodes.Count == 5)
            {
                activityByWeekLabels = new[]
            {
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[3].Monday),
                resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[4].Monday)
            };
            }


            if (_settings.CumulPlaytimeStore)
            {
                ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();
                for(int i = 0; i < activityByWeekSeries.Count; i++)
                {
                    for (int j = 0; j < activityByWeekSeries[i].Values.Count; j++)
                    {
                        if (series.Count == j)
                        {
                            series.Add(new CustomerForTime
                            {
                                Name = activityByWeekLabels[j],
                                Values = ((CustomerForTime)activityByWeekSeries[i].Values[j]).Values
                            });
                        }
                        else
                        {
                            series[j].Values += ((CustomerForTime)activityByWeekSeries[i].Values[j]).Values;
                        }
                    }
                }

                activityByWeekSeries = new SeriesCollection();
                activityByWeekSeries.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = series
                });

                acwSeries.DataTooltip = new CustomerToolTipForTime();
            }
            else
            {
                acwSeries.DataTooltip = new CustomerToolTipForMultipleTime();
                ((CustomerToolTipForMultipleTime)acwSeries.DataTooltip).ShowIcon = _settings.ShowLauncherIcons;
            }

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((long)value, null, null, CultureInfo.CurrentCulture);

            acwLabelsY.LabelFormatter = activityForGameLogFormatter;
            acwSeries.Series = activityByWeekSeries;
            acwLabelsY.MinValue = 0;
            acwLabelsX.Labels = activityByWeekLabels;
        }


        /// <summary>
        /// Get list games with an activities.
        /// </summary>
        public void getActivityByListGame()
        {
            activityListByGame = new List<ListActivities>();

            List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
            listGameActivities = listGameActivities.Where(x => x.Items.Count > 0 && !x.IsDeleted).ToList();

            string gameID = string.Empty;
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                try
                {
                    gameID = listGameActivities[iGame].Id.ToString();
                    if (!listGameActivities[iGame].Name.IsNullOrEmpty())
                    {
                        string gameTitle = listGameActivities[iGame].Name;

                        string sourceName = string.Empty;
                        try
                        {
                            sourceName = listGameActivities[iGame].Items[0].SourceName;
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, "Error to get SourceName");
                        }

                        Activity lastSessionActivity = listGameActivities[iGame].GetLastSessionActivity();
                        long elapsedSeconds = lastSessionActivity.ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(lastSessionActivity.DateSession).ToLocalTime();

                        string GameIcon = listGameActivities[iGame].Icon;
                        if (!GameIcon.IsNullOrEmpty())
                        {
                            GameIcon = dbPlaynite.GetFullFilePath(GameIcon);
                        }

                        activityListByGame.Add(new ListActivities()
                        {
                            GameId = gameID,
                            GameTitle = gameTitle,
                            GameIcon = GameIcon,
                            GameLastActivity = dateSession,
                            GameElapsedSeconds = elapsedSeconds,
                            GameSourceName = sourceName,
                            GameSourceIcon = TransformIcon.Get(sourceName),
                            DateActivity = listGameActivities[iGame].GetListDateActivity(),
                            AvgCPU = listGameActivities[iGame].avgCPU(listGameActivities[iGame].GetLastSession()) + "%",
                            AvgGPU = listGameActivities[iGame].avgGPU(listGameActivities[iGame].GetLastSession()) + "%",
                            AvgRAM = listGameActivities[iGame].avgRAM(listGameActivities[iGame].GetLastSession()) + "%",
                            AvgFPS = listGameActivities[iGame].avgFPS(listGameActivities[iGame].GetLastSession()) + "",
                            AvgCPUT = listGameActivities[iGame].avgCPUT(listGameActivities[iGame].GetLastSession()) + "°",
                            AvgGPUT = listGameActivities[iGame].avgGPUT(listGameActivities[iGame].GetLastSession()) + "°",

                            EnableWarm = _settings.EnableWarning,
                            MaxCPUT = _settings.MaxCpuTemp.ToString(),
                            MaxGPUT = _settings.MaxGpuTemp.ToString(),
                            MinFPS = _settings.MinFps.ToString(),
                            MaxCPU = _settings.MaxCpuUsage.ToString(),
                            MaxGPU = _settings.MaxGpuUsage.ToString(),
                            MaxRAM = _settings.MaxRamUsage.ToString(),
                        });
                    }
                    // Game is deleted
                    else
                    {
                        logger.Warn($"Failed to load GameActivities from {gameID} because the game is deleted");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load GameActivities from {gameID}");
                    _PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, $"GameActivity error on {gameID}");
                }
            }

            lvGames.ItemsSource = activityListByGame;

            lvGames.Sorting();
            Filter();
        }



        /// <summary>
        /// Get data for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="variateur"></param>
        public void getActivityForGamesTimeGraphics(string gameID, bool isNavigation = false)
        {
            PART_GameActivityChartTime.GameContext = _PlayniteApi.Database.Games.Get(Guid.Parse(gameID));
            PART_GameActivityChartTime.DisableAnimations = isNavigation;
            PART_GameActivityChartTime.AxisVariator = variateurTime;

            if (!isNavigation)
            {
                gameLabel.Content = resources.GetString("LOCGameActivityTimeTitle");
            }
        }

        /// <summary>
        /// Get data detail for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        public void getActivityForGamesLogGraphics(string gameID, DateTime? dateSelected = null, string title = "", bool isNavigation = false)
        {
            GameActivities gameActivities = GameActivity.PluginDatabase.Get(Guid.Parse(gameID));

            PART_GameActivityChartLog.GameContext = _PlayniteApi.Database.Games.Get(Guid.Parse(gameID));
            PART_GameActivityChartLog.DisableAnimations = isNavigation;
            PART_GameActivityChartLog.DateSelected = dateSelected;
            PART_GameActivityChartLog.TitleChart = title;
            PART_GameActivityChartLog.AxisVariator = variateurLog;

            if (!isNavigation)
            {
                if (dateSelected == null || dateSelected == default(DateTime))
                {
                    gameLabel.Content = resources.GetString("LOCGameActivityLogTitle") + " ("
                        + Convert.ToDateTime(gameActivities.GetLastSession()).ToString(Constants.DateUiFormat) + ")";
                }
                else
                {
                    gameLabel.Content = resources.GetString("LOCGameActivityLogTitleDate") + " "
                        + Convert.ToDateTime(dateSelected).ToString(Constants.DateUiFormat);
                }
            }
        }
        #endregion


        /// <summary>
        /// Get list sources name in database.
        /// </summary>
        /// <returns></returns>
        public JArray GetListSourcesName()
        {
            JArray arrayReturn = new JArray();
            foreach (GameSource source in dbPlaynite.Sources)
            {
                if (!arrayReturn.Contains(source.Name))
                {
                    arrayReturn.Add(source.Name);
                }
            }
            // Source for game add manually.
            arrayReturn.Add("Playnite");
            return arrayReturn;
        }


        /// <summary>
        /// Get details game activity on selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            variateurTime = 0;
            variateurLog = 0;
            variateurLogTemp = 0;

            if (sender != null)
            {
                var item = (ListBox)sender;
                if (item.ItemsSource != null && ((List<ListActivities>)item.ItemsSource).Count > 0)
                {
                    ListActivities gameItem = (ListActivities)item.SelectedItem;
                    gameIDCurrent = gameItem.GameId;

                    if (isGameTime)
                    {
                        getActivityForGamesTimeGraphics(gameIDCurrent);
                    }
                    else
                    {
                        getActivityForGamesLogGraphics(gameIDCurrent);
                    }

                    activityForGamesGraphics.Visibility = Visibility.Visible;
                }
            }
        }


        #region Butons click event
        private void Button_Click_PrevMonth(object sender, RoutedEventArgs e)
        {
            DateTime dateNew = new DateTime(yearCurrent, monthCurrent, 1).AddMonths(-1);
            yearCurrent = dateNew.Year;
            monthCurrent = dateNew.Month;

            // get data
            getActivityByMonth(yearCurrent, monthCurrent);
            getActivityByWeek(yearCurrent, monthCurrent);
            getActivityByDay(yearCurrent, monthCurrent);

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");


            Filter();
        }

        private void Button_Click_NextMonth(object sender, RoutedEventArgs e)
        {
            DateTime dateNew = new DateTime(yearCurrent, monthCurrent, 1).AddMonths(1);
            yearCurrent = dateNew.Year;
            monthCurrent = dateNew.Month;

            // get data
            getActivityByMonth(yearCurrent, monthCurrent);
            getActivityByWeek(yearCurrent, monthCurrent);
            getActivityByDay(yearCurrent, monthCurrent);

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");


            Filter();
        }


        private void ToggleButtonTime_Checked(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    isGameTime = true;
                    ToggleButtonLog.IsChecked = false;
                    getActivityForGamesTimeGraphics(gameIDCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (ToggleButtonLog.IsChecked == false && toggleButton.IsChecked == false)
                    toggleButton.IsChecked = true;
            }
            catch
            {
            }
        }

        private void ToggleButtonLog_Checked(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    isGameTime = false;
                    ToggleButtonTime.IsChecked = false;
                    getActivityForGamesLogGraphics(gameIDCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (ToggleButtonTime.IsChecked == false && toggleButton.IsChecked == false)
                    toggleButton.IsChecked = true;
            }
            catch
            {
            }
        }


        private void ToggleButtonSources_Checked(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    isMonthSources = true;
                    tbMonthGenres.IsChecked = false;
                    getActivityByMonth(yearCurrent, monthCurrent);
                    getActivityByWeek(yearCurrent, monthCurrent);
                    getActivityByDay(yearCurrent, monthCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthGenres.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }

        private void ToggleButtonGenres_Checked(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                try
                {
                    isMonthSources = false;
                    tbMonthSources.IsChecked = false;
                    getActivityByMonth(yearCurrent, monthCurrent);
                    getActivityByWeek(yearCurrent, monthCurrent);
                    getActivityByDay(yearCurrent, monthCurrent);
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthSources.IsChecked == false && toggleButton.IsChecked == false)
                {
                    toggleButton.IsChecked = true;
                }
            }
            catch
            {
            }
        }


        private void Button_Click_prevGame(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Prev();
            }
            else
            {
                //variateurLog = variateurLog - 1;
                //getActivityForGamesLogGraphics(gameIDCurrent, LabelDataSelected, titleChart, true);
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = titleChart;
                PART_GameActivityChartLog.Prev();
            }
        }

        private void Button_Click_nextGame(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Next();
            }
            else
            {
                //variateurLog = variateurLog + 1;
                //getActivityForGamesLogGraphics(gameIDCurrent, LabelDataSelected, titleChart, true);
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = titleChart;
                PART_GameActivityChartLog.Next();
            }
        }
        #endregion


        // TODO Show stack time for can select details data
        // TODO Select details data
        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {
            if (_settings.EnableLogging)
            {
                int index = (int)chartPoint.X;
                titleChart = chartPoint.SeriesView.Title;
                var data = chartPoint.SeriesView.Values;

                LabelDataSelected = Convert.ToDateTime(((CustomerForTime)data[index]).Name);

                isGameTime = false;
                ToggleButtonTime.IsChecked = false;
                ToggleButtonLog.IsChecked = true;

                getActivityForGamesLogGraphics(gameIDCurrent, LabelDataSelected, titleChart);
            }
        }


        #region Filter
        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }

        private void Filter()
        {
            lvGames.ItemsSource = null;

            if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
            {
                lvGames.ItemsSource = activityListByGame.FindAll(
                    x => x.GameTitle.ToLower().IndexOf(TextboxSearch.Text) > -1 
                         && SearchSources.Contains(x.GameSourceName)
                         && x.DateActivity.Contains(yearCurrent + "-" + ((monthCurrent > 9) ? monthCurrent.ToString() : "0" + monthCurrent))
                );
                lvGames.Sorting();
                return;
            }

            if (!TextboxSearch.Text.IsNullOrEmpty())
            {
                lvGames.ItemsSource = activityListByGame.FindAll(
                    x => x.GameTitle.ToLower().IndexOf(TextboxSearch.Text) > -1
                         && x.DateActivity.Contains(yearCurrent + "-" + ((monthCurrent > 9) ? monthCurrent.ToString() : "0" + monthCurrent))
                );
                lvGames.Sorting();
                return;
            }

            if (SearchSources.Count != 0)
            {
                lvGames.ItemsSource = activityListByGame.FindAll(
                    x => SearchSources.Contains(x.GameSourceName) 
                         && x.DateActivity.Contains(yearCurrent + "-" + ((monthCurrent > 9) ? monthCurrent.ToString() : "0" + monthCurrent))
                );
                lvGames.Sorting();
                return;
            }

            lvGames.ItemsSource = activityListByGame.FindAll(x => x.DateActivity.Contains(yearCurrent + "-" + ((monthCurrent > 9) ? monthCurrent.ToString() : "0" + monthCurrent)));
            lvGames.Sorting();
        }

        private void ChkSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }
        private void ChkSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }
        private void FilterCbSource(CheckBox sender)
        {
            FilterSource.Text = string.Empty;

            if ((bool)sender.IsChecked)
            {
                SearchSources.Add((string)sender.Tag);
            }
            else
            {
                SearchSources.Remove((string)sender.Tag);
            }

            if (SearchSources.Count != 0)
            {
                FilterSource.Text = String.Join(", ", SearchSources);
            }
            
            Filter();
        }
        #endregion
    }

    public class ListSource
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }

    public class WeekStartEnd
    {
        public DateTime Monday;
        public DateTime Sunday;
    }
}
