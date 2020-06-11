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
using GameActivity.Database.Collections;
using GameActivity.Models;
using LiveCharts;
using PluginCommon;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Windows.Media;
using PluginCommon.LiveChartsCommon;
using Playnite.Controls;

namespace GameActivity
{
    /// <summary>
    /// Logique d'interaction pour GameActivity.xaml.
    /// </summary>
    public partial class GameActivityView : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public JArray listSources { get; set; }

        private GameActivityCollection GameActivityDatabases { get; set; }

        public int yearCurrent;
        public int monthCurrent;
        public string gameIDCurrent;
        public int variateurTime = 0;
        public int variateurLog = 0;
        public int variateurLogTemp = 0;

        public List<listGame> activityListByGame { get; set; }

        // Application variables paths.
        public string pathFileActivityDB { get; set; }
        public string pathActivityDB { get; set; }
        public string pathActivityDetailsDB { get; set; }

        GameActivitySettings settingsPlaynite { get; set; }

        public bool isMonthSources = true;
        public bool isGameTime = true;

        public bool ShowIcon { get; set; }

        // Variables api.
        public readonly IGameDatabaseAPI dbPlaynite;
        public readonly IPlaynitePathsAPI pathsPlaynite;
        public readonly string pathExtentionData;


        public GameActivityView(GameActivitySettings settings, IGameDatabaseAPI dbAPI, IPlaynitePathsAPI pathsAPI, string pathExtData)
        {
            dbPlaynite = dbAPI;
            pathsPlaynite = pathsAPI;
            settingsPlaynite = settings;
            pathExtentionData = pathExtData;
            
            pathActivityDB = pathExtentionData + "\\activity\\";
            pathActivityDetailsDB = pathExtentionData + "\\activityDetails\\";

            // Set dates variables
            yearCurrent = DateTime.Now.Year;
            monthCurrent = DateTime.Now.Month;

            // Initialization components
            InitializeComponent();

            // Block hidden column.
            lvElapsedSeconds.IsEnabled = false;

            // Add column if log details enable.
            if (!settings.EnableLogging)
            {
                GridView lvView = (GridView)lvGames.View;

                lvView.Columns.RemoveAt(9);
                lvView.Columns.RemoveAt(8);
                lvView.Columns.RemoveAt(7);
                lvView.Columns.RemoveAt(6);
                lvView.Columns.RemoveAt(5);
                lvView.Columns.RemoveAt(4);

                lvGames.View = lvView;
            }

            // Sorting default.
            _lastDirection = ListSortDirection.Descending;
            _lastHeaderClicked = lvLastActivity;
            _lastHeaderClicked.Content += " ▼";


            // Graphics game details activities.
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");

            #region Get Datas
            listSources = getListSourcesName();

            GameActivityDatabases = new GameActivityCollection();
            GameActivityDatabases.InitializeCollection(pathExtData);
            #endregion

            #region Get & set datas
            getActivityByMonth(yearCurrent, monthCurrent);
            getActivityByWeek(yearCurrent, monthCurrent);

            getActivityByListGame();
            #endregion


            // Set Binding data
            ShowIcon = settingsPlaynite.showLauncherIcons;
            DataContext = this;
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
                List<GameActivityClass> listGameActivities = GameActivityDatabases.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> gameActivities = listGameActivities[iGame].Activities;
                    for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
                    {
                        long elapsedSeconds = gameActivities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).AddSeconds(-elapsedSeconds).ToLocalTime();
                        string sourceName = gameActivities[iActivity].SourceName;

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

                gridMonth.Width = 605;
                acwSeries.Visibility = Visibility.Visible;
                acwLabel.Visibility = Visibility.Visible;
            }
            // Total hours by genres.
            else
            {
                List<GameActivityClass> listGameActivities = GameActivityDatabases.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                    List <Activity> gameActivities = listGameActivities[iGame].Activities;
                    for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
                    {
                        long elapsedSeconds = gameActivities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).AddSeconds(-elapsedSeconds).ToLocalTime();

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

                gridMonth.Width = 1223;
                acwSeries.Visibility = Visibility.Hidden;
                acwLabel.Visibility = Visibility.Hidden;
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
                    Values = (double)item.Value,
                });
                labels[compteur] = item.Key;
                if (settingsPlaynite.showLauncherIcons)
                    labels[compteur] = TransformIcon.Get(labels[compteur]);
                compteur = compteur + 1;
            }

            SeriesCollection ActivityByMonthSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "",
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

            acmSeries.Series = ActivityByMonthSeries;
            ((CustomerToolTipForTime)acmSeries.DataTooltip).ShowIcon = settingsPlaynite.showLauncherIcons;
            acmLabelsX.Labels = ActivityByMonthLabels;
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

            JArray activityByWeek = new JArray();
            SeriesCollection activityByWeekSeries = new SeriesCollection();

            if (isMonthSources)
            {
                // Insert sources
                for (int iSource = 0; iSource < listSources.Count; iSource++)
                {
                    activityByWeek1.Add((string)listSources[iSource], 0);
                    activityByWeek2.Add((string)listSources[iSource], 0);
                    activityByWeek3.Add((string)listSources[iSource], 0);
                    activityByWeek4.Add((string)listSources[iSource], 0);
                }

                activityByWeek.Add(activityByWeek1);
                activityByWeek.Add(activityByWeek2);
                activityByWeek.Add(activityByWeek3);
                activityByWeek.Add(activityByWeek4);


                List<GameActivityClass> listGameActivities = GameActivityDatabases.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> gameActivities = listGameActivities[iGame].Activities;
                    for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
                    {
                        long elapsedSeconds = gameActivities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).AddSeconds(-elapsedSeconds).ToLocalTime();
                        string sourceName = gameActivities[iActivity].SourceName;

                        // Cumul data
                        for (int iWeek = 0; iWeek < datesPeriodes.Count; iWeek++)
                        {
                            if (datesPeriodes[iWeek].Monday <= dateSession && dateSession <= datesPeriodes[iWeek].Sunday)
                            {
                                activityByWeek[iWeek][sourceName] = (long)activityByWeek[iWeek][sourceName] + elapsedSeconds;
                            }
                        }
                    }
                }


                // Check source with data (only view this)
                JArray listNotDelete = new JArray();
                for (int i = 0; i < 4; i++) {
                    foreach (var item in (JObject)activityByWeek[i])
                    {
                        if ((long)item.Value != 0 && listNotDelete.TakeWhile(x => x.ToString() == item.Key).Count() != 1)
                        {
                            listNotDelete.Add(item.Key);
                        }
                    }
                }


                // Prepare data.
                string[] labels = new string[listNotDelete.Count];
                for (int iSource = 0; iSource < listNotDelete.Count; iSource++)
                {
                    labels[iSource] = (string)listNotDelete[iSource];
                    if (settingsPlaynite.showLauncherIcons)
                        labels[iSource] = TransformIcon.Get((string)listNotDelete[iSource]);

                    activityByWeekSeries.Add(new StackedColumnSeries
                    {
                        Title = labels[iSource],
                        Values = new ChartValues<CustomerForTime>() {
                            new CustomerForTime{Name = (string)listNotDelete[iSource], Values = (int)activityByWeek[0][(string)listNotDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNotDelete[iSource], Values = (int)activityByWeek[1][(string)listNotDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNotDelete[iSource], Values = (int)activityByWeek[2][(string)listNotDelete[iSource]]},
                            new CustomerForTime{Name = (string)listNotDelete[iSource], Values = (int)activityByWeek[3][(string)listNotDelete[iSource]]}
                        },
                        StackMode = StackMode.Values,
                        DataLabels = false
                    });
                }
            }


            // Set data in graphics.
            string[] activityByWeekLabels = new[] 
            {
                "week " + Tools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                "week " + Tools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                "week " + Tools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                "week " + Tools.WeekOfYearISO8601(datesPeriodes[3].Monday),
            };

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            acwSeries.Series = activityByWeekSeries;
            ((CustomerToolTipForMultipleTime)acwSeries.DataTooltip).ShowIcon = settingsPlaynite.showLauncherIcons;
            acwLabelsX.Labels = activityByWeekLabels;
        }


        /// <summary>
        /// Get list games with an activities.
        /// </summary>
        public void getActivityByListGame()
        {
            activityListByGame = new List<listGame>();

            List<GameActivityClass> listGameActivities = GameActivityDatabases.GetListGameActivity();
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                string gameID = listGameActivities[iGame].GameID.ToString();
                string gameTitle = listGameActivities[iGame].GameName;
                string gameIcon;

                Activity lastSessionActivity = listGameActivities[iGame].GetLastSessionActivity();
                long elapsedSeconds = lastSessionActivity.ElapsedSeconds;
                DateTime dateSession = Convert.ToDateTime(lastSessionActivity.DateSession).AddSeconds(-elapsedSeconds).ToLocalTime();


                BitmapImage iconImage = new BitmapImage();
                if (String.IsNullOrEmpty(listGameActivities[iGame].GameIcon) == false)
                {
                    iconImage.BeginInit();
                    gameIcon = dbPlaynite.GetFullFilePath(listGameActivities[iGame].GameIcon);
                    iconImage.UriSource = new Uri(gameIcon, UriKind.RelativeOrAbsolute);
                    iconImage.EndInit();
                }

                activityListByGame.Add(new listGame()
                {
                    listGameID = gameID,
                    listGameTitle = gameTitle,
                    listGameIcon = iconImage,
                    listGameLastActivity = dateSession,
                    listGameElapsedSeconds = elapsedSeconds,
                    listGameElapsedSecondsFormat = (int)TimeSpan.FromSeconds(elapsedSeconds).TotalHours + "h " + TimeSpan.FromSeconds(elapsedSeconds).ToString(@"mm") + "min",
                    avgCPU = listGameActivities[iGame].avgCPU(listGameActivities[iGame].GetLastSession()) + "%",
                    avgGPU = listGameActivities[iGame].avgGPU(listGameActivities[iGame].GetLastSession()) + "%",
                    avgRAM = listGameActivities[iGame].avgRAM(listGameActivities[iGame].GetLastSession()) + "%",
                    avgFPS = listGameActivities[iGame].avgFPS(listGameActivities[iGame].GetLastSession()) + "",
                    avgCPUT = listGameActivities[iGame].avgCPUT(listGameActivities[iGame].GetLastSession()) + "°",
                    avgGPUT = listGameActivities[iGame].avgGPUT(listGameActivities[iGame].GetLastSession()) + "°",

                    enableWarm = settingsPlaynite.EnableWarning,
                    maxCPUT = "" + settingsPlaynite.MaxCpuTemp,
                    maxGPUT = "" + settingsPlaynite.MaxGpuTemp,
                    minFPS = "" + settingsPlaynite.MinFps,
                    maxCPU = "" + settingsPlaynite.MaxCpuUsage,
                    maxGPU = "" + settingsPlaynite.MaxGpuUsage,
                    maxRAM = "" + settingsPlaynite.MaxRamUsage
                });
            
                iconImage = null;
            }

            lvGames.ItemsSource = activityListByGame;

            // Sorting
            try
            {
                var columnBinding = _lastHeaderClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;

                // Specific sort with another column
                if (_lastHeaderClicked.Name == "lvElapsedSecondsFormat")
                {
                    columnBinding = lvElapsedSeconds.Column.DisplayMemberBinding as Binding;
                    sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;
                }
                Sort(sortBy, _lastDirection);
            }
            // If first view
            catch
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvGames.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("listGameLastActivity", ListSortDirection.Descending));
            }
        }



        /// <summary>
        /// Get data for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="variateur"></param>
        public void getActivityForGamesTimeGraphics(string gameID)
        {
            DateTime dateStart = DateTime.Now.AddDays(variateurTime);
            string[] listDate = new string[10];
            ChartValues<CustomerForTime> series = new ChartValues<CustomerForTime>();

            // Periode data showned
            for (int iDay = 0; iDay < 10; iDay++)
            {
                listDate[iDay] = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd");
                //series.Add(0);
                series.Add(new CustomerForTime
                {
                    Name = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd"),
                    Values = 0,
                    //ValuesFormat = (int)TimeSpan.FromSeconds(0).TotalHours + "h " + TimeSpan.FromSeconds(0).ToString(@"mm") + "min"
                });
            }


            // Search data in periode
            GameActivityClass gameActivity = GameActivityDatabases.Get(Guid.Parse(gameID));
            List<Activity> gameActivities = gameActivity.Activities;
            for (int iActivity = 0; iActivity < gameActivities.Count; iActivity++)
            {
                long elapsedSeconds = gameActivities[iActivity].ElapsedSeconds;
                string dateSession = Convert.ToDateTime(gameActivities[iActivity].DateSession).ToLocalTime().ToString("yyyy-MM-dd");

                for (int iDay = 0; iDay < 10; iDay++)
                {
                    if (listDate[iDay] == dateSession)
                    {
                        string tempName = series[iDay].Name;
                        double tempElapsed = series[iDay].Values + elapsedSeconds;
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
            string[] activityForGameLabels = listDate;

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            gameSeries.DataTooltip = new CustomerToolTipForTime();

            gameLabel.Content = resources.GetString("LOCGameActivityTimeTitle");
            gameSeries.Series = activityForGameSeries;
            gameLabelsX.Labels = activityForGameLabels;
        }

        /// <summary>
        /// Get data detail for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        public void getActivityForGamesLogGraphics(string gameID)
        {
            // TODO Get by date for click on time sessions
            GameActivityClass gameActivity = GameActivityDatabases.Get(Guid.Parse(gameID));
            List<ActivityDetailsData> gameActivitiesDetails = gameActivity.GetLastSessionActivityDetails();

            string[] activityForGameLogLabels = new string[0];
            List<ActivityDetailsData> gameLogsDefinitive = new List<ActivityDetailsData>();
            if (gameActivitiesDetails.Count > 0) {
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
                        activityForGameLogLabels[sCount] = Convert.ToDateTime(gameActivitiesDetails[iLog].Datelog).ToLocalTime().ToString("HH:mm");
                        sCount += 1;
                    }
                }
                else
                {
                    gameLogsDefinitive = gameActivitiesDetails;

                    activityForGameLogLabels = new string[gameActivitiesDetails.Count];
                    for (int iLog = 0; iLog < gameActivitiesDetails.Count; iLog++)
                    {
                        activityForGameLogLabels[iLog] = Convert.ToDateTime(gameActivitiesDetails[iLog].Datelog).ToLocalTime().ToString("HH:mm");
                    }
                }
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

            gameSeries.DataTooltip = new LiveCharts.Wpf.DefaultTooltip();
            gameSeries.DataTooltip.Background = (Brush)resources.GetResource("CommonToolTipBackgroundBrush");
            gameSeries.DataTooltip.Padding = new Thickness(10); 
            gameSeries.DataTooltip.BorderThickness = (Thickness)resources.GetResource("CommonToolTipBorderThickness"); 
            gameSeries.DataTooltip.BorderBrush = (Brush)resources.GetResource("CommonToolTipBorderBrush");
            gameSeries.DataTooltip.Foreground = (Brush)resources.GetResource("CommonToolTipForeground");

            gameLabel.Content = resources.GetString("LOCGameActivityLogTitle");
            gameSeries.Series = activityForGameLogSeries;
            gameLabelsX.Labels = activityForGameLogLabels;
            gameLabelsY.LabelFormatter = activityForGameLogFormatter;
        }
        #endregion


        /// <summary>
        /// Get source name by source id.
        /// </summary>
        /// <param name="sourceID"></param>
        /// <returns></returns>
        public string getSourceName(string sourceID)
        {
            if ("00000000-0000-0000-0000-000000000000" != sourceID)
            {
                return dbPlaynite.Sources.Get(Guid.Parse(sourceID)).Name;
            }
            else
            {
                return "Playnite";
            }
            
        }

        /// <summary>
        /// Get list sources name in database.
        /// </summary>
        /// <returns></returns>
        public JArray getListSourcesName()
        {
            JArray arrayReturn = new JArray();
            foreach (GameSource source in dbPlaynite.Sources)
            {
                arrayReturn.Add(source.Name);
            }
            // Source for game add manuelly.
            arrayReturn.Add("Playnite");
            return arrayReturn;
        }



        #region Functions sorting lvGames.
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection;

        private void lvGames_onHeaderClick(object sender, RoutedEventArgs e)
        {
            lvElapsedSeconds.IsEnabled = true;

            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            // No sort
            if (headerClicked.Name == "lvGameIcon")
            {
                headerClicked = null;
            }

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    // Specific sort with another column
                    if (headerClicked.Name == "lvElapsedSecondsFormat")
                    {
                        columnBinding = lvElapsedSeconds.Column.DisplayMemberBinding as Binding;
                        sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                    }


                    Sort(sortBy, direction);

                    if (_lastHeaderClicked != null)
                    {
                        _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▲", "");
                        _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▼", "");
                    }

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Content += " ▲";
                    }
                    else
                    {
                        headerClicked.Content += " ▼";
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }

            lvElapsedSeconds.IsEnabled = false;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(lvGames.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        #endregion



        /// <summary>
        /// Get details game activity on selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            variateurTime = 0;
            variateurLog = 0;
            variateurLogTemp = 0;

            var item = (ListBox)sender;
            listGame gameItem = (listGame)item.SelectedItem;
            gameIDCurrent = gameItem.listGameID;

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


        #region Butons click event
        private void Button_Click_PrevMonth(object sender, RoutedEventArgs e)
        {
            DateTime dateNew = new DateTime(yearCurrent, monthCurrent, 1).AddMonths(-1);
            yearCurrent = dateNew.Year;
            monthCurrent = dateNew.Month;

            // get data
            getActivityByMonth(yearCurrent, monthCurrent);
            getActivityByWeek(yearCurrent, monthCurrent);

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");
        }

        private void Button_Click_NextMonth(object sender, RoutedEventArgs e)
        {
            DateTime dateNew = new DateTime(yearCurrent, monthCurrent, 1).AddMonths(1);
            yearCurrent = dateNew.Year;
            monthCurrent = dateNew.Month;

            // get data
            getActivityByMonth(yearCurrent, monthCurrent);
            getActivityByWeek(yearCurrent, monthCurrent);

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");
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
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthGenres.IsChecked == false && toggleButton.IsChecked == false)
                    toggleButton.IsChecked = true;
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
                }
                catch
                {
                }
            }

            try
            {
                if (tbMonthSources.IsChecked == false && toggleButton.IsChecked == false)
                    toggleButton.IsChecked = true;
            }
            catch
            {
            }
        }


        private void Button_Click_prevGame(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                variateurTime = variateurTime - 1;
                getActivityForGamesTimeGraphics(gameIDCurrent);
            }
            else
            {
                variateurLog = variateurLog - 1;
                getActivityForGamesLogGraphics(gameIDCurrent);
            }
        }

        private void Button_Click_nextGame(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                variateurTime = variateurTime + 1;
                getActivityForGamesTimeGraphics(gameIDCurrent);
            }
            else
            {
                variateurLog = variateurLog + 1;
                getActivityForGamesLogGraphics(gameIDCurrent);
            }
        }
        #endregion


        // TODO Show stack time for can select details data
        // TODO Select details data
        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {

        }

        private void TbMonthSources_Loaded(object sender, RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }
    }

    // Listview games
    public class listGame
    {
        public string listGameTitle { get; set; }
        public string listGameID { get; set; }
        public BitmapImage listGameIcon { get; set; }
        public DateTime listGameLastActivity { get; set; }
        public long listGameElapsedSeconds { get; set; }
        public string listGameElapsedSecondsFormat { get; set; }

        public string avgCPU { get; set; }
        public string avgGPU { get; set; }
        public string avgRAM { get; set; }
        public string avgFPS { get; set; }
        public string avgCPUT { get; set; }
        public string avgGPUT { get; set; }

        public bool enableWarm { get; set; }
        public string maxCPUT { get; set; }
        public string maxGPUT { get; set; }
        public string minFPS { get; set; }
        public string maxCPU { get; set; }
        public string maxGPU { get; set; }
        public string maxRAM { get; set; }
    }

    public class WeekStartEnd
    {
        public DateTime Monday;
        public DateTime Sunday;
    }
}
