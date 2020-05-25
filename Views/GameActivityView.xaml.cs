using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.ComponentModel;
using GameActivity.Database.Collections;
using GameActivity.Models;
using LiveCharts;
using LiveCharts.Wpf;
using PluginCommon;

namespace GameActivity
{
    /// <summary>
    /// Logique d'interaction pour GameActivity.xaml.
    /// </summary>
    public partial class GameActivityView : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public JArray listSources { get; set; }

        private GameActivityCollection GameActivityDatabases { get; set; }

        public int yearCurrent;
        public int monthCurrent;
        public string gameIDCurrent;
        public int variateurTime = 0;
        public int variateurLog = 0;
        public int variateurLogTemp = 0;

        public List<listGame> activityListByGame { get; set; }

        // Variables graphics activities.
        public string activityByMonthTitle { get; set; }
        public string activityByWeekTitle { get; set; }
        public string activityForGameTimeTitle { get; set; }
        public string activityForGameLogTitle { get; set; }

        // Variables list games activities.
        public string lvGamesID { get; set; }
        public string lvGamesIcon { get; set; }
        public string lvGamesTitle { get; set; }
        public string lvGamesLastActivity { get; set; }
        public string lvGamesElapsedSeconds { get; set; }

        // Application variables paths.
        public string pathFileActivityDB { get; set; }
        public string pathActivityDB { get; set; }
        public string pathActivityDetailsDB { get; set; }

        GameActivitySettings settingsPlaynite { get; set; }

        bool isMonthSources = true;
        bool isGameTime = true;

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

            #region text localization
            // listViewGames
            lvGamesIcon = "Icon";
            lvGamesTitle = "Name";
            lvGamesLastActivity = "Last session start";
            lvGamesElapsedSeconds = "Elapsed Time";

            // Graphics title
            activityByMonthTitle = "Total hours";
            activityByWeekTitle = "Total hours by weeks";
            activityForGameTimeTitle = "Total hours by day";
            activityForGameLogTitle = "Last session details";
            #endregion

            // Initialization components
            InitializeComponent();

            // Add column if log details enable.
            if (!settings.HWiNFO_enable)
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
            DataContext = this;
        }


        #region Generate graphics and list
        /// <summary>
        /// Get data graphic activity by month.
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
                        if (activityByMonth.ContainsKey(sourceName))
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
                            if (activityByMonth.ContainsKey(listGameListGenres[iGenre].Name))
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
            ChartValues<double> series = new ChartValues<double>();
            string[] labels = new string[activityByMonth.Count];
            int compteur = 0;
            foreach (var item in activityByMonth)
            {
                series.Add((double)item.Value);
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
            Func<double, string> ActivityByMonthFormatter = value => (int)TimeSpan.FromSeconds(value).TotalHours + "h " + TimeSpan.FromSeconds(value).ToString(@"mm") + "min";

            acmLabel.Content = activityByMonthTitle;
            acmSeries.Series = ActivityByMonthSeries;
            acmLabelsX.Labels = ActivityByMonthLabels;
            acmLabelsY.LabelFormatter = ActivityByMonthFormatter;
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
                //if(firstMonday.AddDays(i+6)<SeriesEndDate) //uncomment this line if you would like to get last sunday before SeriesEndDate
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


                // Prepare data.
                string[] labels = new string[listSources.Count];
                for (int iSource = 0; iSource < listSources.Count; iSource++)
                {
                    labels[iSource] = (string)listSources[iSource];
                    if (settingsPlaynite.showLauncherIcons)
                        labels[iSource] = TransformIcon.Get((string)listSources[iSource]);

                    activityByWeekSeries.Add(new StackedColumnSeries
                    {
                        Title = labels[iSource],
                        Values = new ChartValues<double>() {
                            (double)activityByWeek[0][(string)listSources[iSource]],
                            (double)activityByWeek[1][(string)listSources[iSource]],
                            (double)activityByWeek[2][(string)listSources[iSource]],
                            (double)activityByWeek[3][(string)listSources[iSource]]
                        },
                        StackMode = StackMode.Values,
                        DataLabels = false
                    });
                }
            }
            else
            {
            }


            // Set data in graphics.
            string[] activityByWeekLabels = new[] 
            {
                "week " + WeekOfYearISO8601(datesPeriodes[0].Monday),
                "week " + WeekOfYearISO8601(datesPeriodes[1].Monday),
                "week " + WeekOfYearISO8601(datesPeriodes[2].Monday),
                "week " + WeekOfYearISO8601(datesPeriodes[3].Monday),
            };
            Func<double, string> activityByWeekFormatter = value => (int)TimeSpan.FromSeconds(value).TotalHours + "h " + TimeSpan.FromSeconds(value).ToString(@"mm") + "min";

            acwLabel.Content = activityByWeekTitle;
            acwSeries.Series = activityByWeekSeries;
            acwLabelsX.Labels = activityByWeekLabels;
            acwLabelsY.LabelFormatter = activityByWeekFormatter;
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
                    listGameElapsedSeconds = (int)TimeSpan.FromSeconds(elapsedSeconds).TotalHours + "h " + TimeSpan.FromSeconds(elapsedSeconds).ToString(@"mm") + "min",
                    avgCPU = listGameActivities[iGame].avgCPU(listGameActivities[iGame].GetLastSession()) + "%",
                    avgGPU = listGameActivities[iGame].avgGPU(listGameActivities[iGame].GetLastSession()) + "%",
                    avgRAM = listGameActivities[iGame].avgRAM(listGameActivities[iGame].GetLastSession()) + "%",
                    avgFPS = listGameActivities[iGame].avgFPS(listGameActivities[iGame].GetLastSession()) + "",
                    avgCPUT = listGameActivities[iGame].avgCPUT(listGameActivities[iGame].GetLastSession()) + "°",
                    avgGPUT = listGameActivities[iGame].avgGPUT(listGameActivities[iGame].GetLastSession()) + "°"
                });
            
                iconImage = null;
            }

            // Sorting default.
            lvGames.ItemsSource = activityListByGame;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvGames.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("listGameLastActivity", ListSortDirection.Descending));
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
            ChartValues<double> series = new ChartValues<double>();

            // Periode data showned
            for (int iDay = 0; iDay < 10; iDay++)
            {
                listDate[iDay] = dateStart.AddDays(iDay - 9).ToString("yyyy-MM-dd");
                series.Add(0);
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
                        series[iDay] = series[iDay] + elapsedSeconds;
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
            Func<double, string> activityForGameFormatter = value => (int)TimeSpan.FromSeconds(value).TotalHours + "h " + TimeSpan.FromSeconds(value).ToString(@"mm") + "min";

            gameLabel.Content = activityForGameTimeTitle;
            gameSeries.Series = activityForGameSeries;
            gameLabelsX.Labels = activityForGameLabels;
            gameLabelsY.LabelFormatter = activityForGameFormatter;
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

            //logger.Info(JsonConvert.SerializeObject(gameActivity.ActivitiesDetails));
            //logger.Info(JsonConvert.SerializeObject(gameActivitiesDetails));

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
            else
            {

            }

            // Set data in graphic.
            ChartValues<double> CPUseries = new ChartValues<double>();
            ChartValues<double> GPUseries = new ChartValues<double>();
            ChartValues<double> RAMseries = new ChartValues<double>();
            ChartValues<double> FPSseries = new ChartValues<double>();
            for (int iLog = 0; iLog < gameLogsDefinitive.Count; iLog++)
            {
                CPUseries.Add((double)gameLogsDefinitive[iLog].CPU);
                GPUseries.Add((double)gameLogsDefinitive[iLog].GPU);
                RAMseries.Add((double)gameLogsDefinitive[iLog].RAM);
                FPSseries.Add((double)gameLogsDefinitive[iLog].FPS);
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
            //string[] activityForGameLogLabels = listDate;
            Func<double, string> activityForGameLogFormatter = value => value.ToString("N");

            gameLabel.Content = activityForGameLogTitle;
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




        /// <summary>
        /// Get number week.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int WeekOfYearISO8601(DateTime date)
        {
            var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

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



        #region Functions sorting lvGames.
        //https://stackoverflow.com/questions/30787068/wpf-listview-sorting-on-column-click
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        private void onHeaderClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader ch)) return;
            var dir = ListSortDirection.Ascending;
            if (ch == lastHeaderClicked && lastDirection == ListSortDirection.Ascending)
                dir = ListSortDirection.Descending;
            sort(ch, dir);
            lastHeaderClicked = ch; lastDirection = dir;
        }

        private void sort(GridViewColumnHeader ch, ListSortDirection dir)
        {
            var bn = (ch.Column.DisplayMemberBinding as Binding)?.Path.Path;
            bn = bn ?? ch.Column.Header as string;
            var dv = CollectionViewSource.GetDefaultView(lvGames.ItemsSource);
            dv.SortDescriptions.Clear();
            var sd = new SortDescription(bn, dir);
            dv.SortDescriptions.Add(sd);
            dv.Refresh();
        }
        #endregion

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





        private void GameSeries_DataClick(object sender, ChartPoint chartPoint)
        {

        }
    }

    // Listview games
    public class listGame
    {
        public string listGameTitle { get; set; }
        public string listGameID { get; set; }
        public BitmapImage listGameIcon { get; set; }
        public DateTime listGameLastActivity { get; set; }
        public string listGameElapsedSeconds { get; set; }

        public string avgCPU { get; set; }
        public string avgGPU { get; set; }
        public string avgRAM { get; set; }
        public string avgFPS { get; set; }
        public string avgCPUT { get; set; }
        public string avgGPUT { get; set; }
    }

    public class WeekStartEnd
    {
        public DateTime Monday;
        public DateTime Sunday;
    }
}
