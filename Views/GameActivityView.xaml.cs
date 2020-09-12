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
using PluginCommon.LiveChartsCommon;
using Playnite.Controls;
using Playnite.Converters;
using System.Globalization;
using GameActivity.Views.Interface;
using LiveCharts.Events;
using System.Windows.Forms;

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

        private LongToTimePlayedConverter converter = new LongToTimePlayedConverter();

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

        GameActivitySettings settings { get; set; }

        public bool isMonthSources = true;
        public bool isGameTime = true;

        public bool ShowIcon { get; set; }

        // Variables api.
        public readonly IPlayniteAPI PlayniteApi;
        public readonly IGameDatabaseAPI dbPlaynite;
        public readonly IPlaynitePathsAPI pathsPlaynite;
        public readonly string pathExtentionData;


        public GameActivityView(GameActivitySettings settings, IPlayniteAPI PlayniteApi, string pathExtData, Game GameSelected = null)
        {
            this.PlayniteApi = PlayniteApi;
            dbPlaynite = PlayniteApi.Database;
            pathsPlaynite = PlayniteApi.Paths;
            this.settings = settings;
            pathExtentionData = pathExtData;
            
            pathActivityDB = pathExtentionData + "\\activity\\";
            pathActivityDetailsDB = pathExtentionData + "\\activityDetails\\";

            // Set dates variables
            yearCurrent = DateTime.Now.Year;
            monthCurrent = DateTime.Now.Month;

            // Initialization components
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            if (!settings.EnableLogging)
            {
                ToggleButtonTime.Visibility = Visibility.Hidden;
                ToggleButtonLog.Visibility = Visibility.Hidden;
            }


            // Block hidden column.
            lvElapsedSeconds.IsEnabled = false;

            // Add column if log details enable.
            if (!settings.EnableLogging)
            {
                GridView lvView = (GridView)lvGames.View;

                lvView.Columns.RemoveAt(10);
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


            // Set game selected
            if (GameSelected != null)
            {
                for (int i = 0; i < lvGames.Items.Count; i++)
                {
                    if (((listGame)lvGames.Items[i]).listGameTitle == GameSelected.Name)
                    {
                        lvGames.SelectedIndex = i;
                    }
                }
            }
            lvGames.ScrollIntoView(lvGames.SelectedItem);


            // Set Binding data
            ShowIcon = this.settings.showLauncherIcons;
            DataContext = this;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
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
                    try
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
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "GameActivity", $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].GameName}");
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
                    try
                    {
                        List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                        List<Activity> gameActivities = listGameActivities[iGame].Activities;
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
                    catch(Exception ex)
                    {
                        Common.LogError(ex, "GameActivity", $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].GameName}");
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
                    Values = (long)item.Value,
                });
                labels[compteur] = item.Key;
                if (settings.showLauncherIcons)
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

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((long)value, null, null, CultureInfo.CurrentCulture);
            acmLabelsY.LabelFormatter = activityForGameLogFormatter;

            acmSeries.Series = ActivityByMonthSeries;
            acmLabelsY.MinValue = 0;
            ((CustomerToolTipForTime)acmSeries.DataTooltip).ShowIcon = settings.showLauncherIcons;
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
            JObject activityByWeek5 = new JObject();

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
                    activityByWeek5.Add((string)listSources[iSource], 0);
                }

                activityByWeek.Add(activityByWeek1);
                activityByWeek.Add(activityByWeek2);
                activityByWeek.Add(activityByWeek3);
                activityByWeek.Add(activityByWeek4);
                activityByWeek.Add(activityByWeek5);

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
                JArray listNoDelete = new JArray();
                for (int i = 0; i < activityByWeek.Count; i++) {
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
                    if (settings.showLauncherIcons)
                        labels[iSource] = TransformIcon.Get((string)listNoDelete[iSource]);

                    IChartValues Values = new ChartValues<CustomerForTime>() {
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
            ((CustomerToolTipForMultipleTime)acwSeries.DataTooltip).ShowIcon = settings.showLauncherIcons;
            acwLabelsX.Labels = activityByWeekLabels;
        }


        /// <summary>
        /// Get list games with an activities.
        /// </summary>
        public void getActivityByListGame()
        {
            activityListByGame = new List<listGame>();

            List<GameActivityClass> listGameActivities = GameActivityDatabases.GetListGameActivity();
            string gameID = "";
            for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
            {
                try
                {
                    gameID = listGameActivities[iGame].GameID.ToString();
                    if (!listGameActivities[iGame].GameName.IsNullOrEmpty())
                    {
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
                            avgCPU = listGameActivities[iGame].avgCPU(listGameActivities[iGame].GetLastSession()) + "%",
                            avgGPU = listGameActivities[iGame].avgGPU(listGameActivities[iGame].GetLastSession()) + "%",
                            avgRAM = listGameActivities[iGame].avgRAM(listGameActivities[iGame].GetLastSession()) + "%",
                            avgFPS = listGameActivities[iGame].avgFPS(listGameActivities[iGame].GetLastSession()) + "",
                            avgCPUT = listGameActivities[iGame].avgCPUT(listGameActivities[iGame].GetLastSession()) + "°",
                            avgGPUT = listGameActivities[iGame].avgGPUT(listGameActivities[iGame].GetLastSession()) + "°",

                            enableWarm = settings.EnableWarning,
                            maxCPUT = "" + settings.MaxCpuTemp,
                            maxGPUT = "" + settings.MaxGpuTemp,
                            minFPS = "" + settings.MinFps,
                            maxCPU = "" + settings.MaxCpuUsage,
                            maxGPU = "" + settings.MaxGpuUsage,
                            maxRAM = "" + settings.MaxRamUsage
                        });

                        iconImage = null;
                    }
                    // Game is deleted
                    else
                    {
                        logger.Warn($"GameActivity - Failed to load GameActivities from {gameID} because the game is deleted");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", $"Failed to load GameActivities from {gameID}");
                    PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, $"GameActivity error on {gameID}");
                }
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
            gameSeriesContener.Children.Clear();
            GameActivityClass gameActivity = GameActivityDatabases.Get(Guid.Parse(gameID));
            List<Activity> gameActivities = gameActivity.Activities;

            var graph = new GameActivityGameGraphicTime(settings, gameActivity, variateurTime);
            graph.gameSeriesDataClick += new DataClickHandler(GameSeries_DataClick);
            gameSeriesContener.Children.Add(graph);
            gameSeriesContener.UpdateLayout();

            gameLabel.Content = resources.GetString("LOCGameActivityTimeTitle");
        }

        /// <summary>
        /// Get data detail for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        public void getActivityForGamesLogGraphics(string gameID, string dateSelected = "", string title = "")
        {
            gameSeriesContener.Children.Clear();
            GameActivityClass gameActivity = GameActivityDatabases.Get(Guid.Parse(gameID));
            List<Activity> gameActivities = gameActivity.Activities;
            gameSeriesContener.Children.Add(new GameActivityGameGraphicLog(settings, gameActivity, dateSelected, title, variateurLog, false));
            gameSeriesContener.UpdateLayout();

            if (dateSelected == "")
            {
                gameLabel.Content = resources.GetString("LOCGameActivityLogTitle") + " (" 
                    + Convert.ToDateTime(gameActivity.GetLastSession()).ToString(Playnite.Common.Constants.DateUiFormat) + ")";
            }
            else
            {
                gameLabel.Content = resources.GetString("LOCGameActivityLogTitleDate") + " "
                    + Convert.ToDateTime(dateSelected).ToString(Playnite.Common.Constants.DateUiFormat);
            }
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
            if (settings.EnableLogging)
            {
                int index = (int)chartPoint.X;
                string title = chartPoint.SeriesView.Title;
                var data = chartPoint.SeriesView.Values;

                string LabelDataSelected = ((CustomerForTime)data[index]).Name;

                isGameTime = false;
                ToggleButtonTime.IsChecked = false;
                ToggleButtonLog.IsChecked = true;

                gameSeries.HideTooltip();

                getActivityForGamesLogGraphics(gameIDCurrent, LabelDataSelected, title);
            }
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
