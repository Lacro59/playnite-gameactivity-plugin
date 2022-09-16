using System;
using System.Collections.Generic;
using System.Windows;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Controls.Primitives;
using GameActivity.Models;
using LiveCharts;
using CommonPluginsShared;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Globalization;
using System.Threading.Tasks;
using CommonPlayniteShared.Converters;
using CommonPluginsControls.LiveChartsCommon;
using CommonPlayniteShared.Common;
using GameActivity.Services;
using GameActivity.Controls;
using CommonPluginsControls.Controls;
using System.Windows.Media;
using System.IO;
using CommonPluginsShared.Extensions;
using Playnite.SDK.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityView.xaml.
    /// </summary>
    public partial class GameActivityView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private GameActivity plugin;
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private List<string> listSources { get; set; }
        private DateTime LabelDataSelected { get; set; }

        private PluginChartTime PART_GameActivityChartTime;
        private PluginChartLog PART_GameActivityChartLog;

        private PlayTimeToStringConverter converter = new PlayTimeToStringConverter();

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
        public TextBlockWithIconMode ModeComplet { get; set; }
        public TextBlockWithIconMode ModeSimple { get; set; }

        // Variables api.
        public readonly IPlayniteAPI _PlayniteApi;
        public readonly IGameDatabaseAPI dbPlaynite;
        public readonly IPlaynitePathsAPI pathsPlaynite;
        public readonly string pathExtentionData;


        public GameActivityView(GameActivity plugin, Game GameSelected = null)
        {
            this.plugin = plugin;

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


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_DataTop.Visibility = Visibility.Hidden;
            PART_DataBottom.Visibility = Visibility.Hidden;


            PART_Truncate.IsChecked = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate;
            ButtonShowConfig.IsChecked = false;


            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                ToggleButtonTime.Visibility = Visibility.Hidden;
                ToggleButtonLog.Visibility = Visibility.Hidden;
            }


            PART_GameActivityChartTime = new PluginChartTime();
            PART_GameActivityChartTime.Truncate = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate;
            PART_GameActivityChartTime.IgnoreSettings = true;
            PART_GameActivityChartTime.LabelsRotation = true;
            PART_GameActivityChartTime.GameSeriesDataClick += GameSeries_DataClick;
            PART_GameActivityChartTime_Contener.Children.Add(PART_GameActivityChartTime);


            PART_GameActivityChartLog = new PluginChartLog();
            PART_GameActivityChartLog.IgnoreSettings = true;
            PART_GameActivityChartLog.AxisLimit = 10;
            PART_GameActivityChartLog_Contener.Children.Add(PART_GameActivityChartLog);


            lvGames.SaveColumn = PluginDatabase.PluginSettings.Settings.SaveColumnOrder;
            lvGames.SaveColumnFilePath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "lvGames.json");

            GridView lvView = (GridView)lvGames.View;

            // Add column if log details enable.
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                lvAvgGpuP.Width = 0;
                lvAvgGpuPHeader.IsHitTestVisible = false;
                lvAvgCpuP.Width = 0;
                lvAvgCpuPHeader.IsHitTestVisible = false;
                lvAvgGpuT.Width = 0;
                lvAvgGpuTHeader.IsHitTestVisible = false;
                lvAvgCpuT.Width = 0;
                lvAvgCpuTHeader.IsHitTestVisible = false;
                lvAvgFps.Width = 0;
                lvAvgFpsHeader.IsHitTestVisible = false;
                lvAvgRam.Width = 0;
                lvAvgRamHeader.IsHitTestVisible = false;
                lvAvgGpu.Width = 0;
                lvAvgGpuHeader.IsHitTestVisible = false;
                lvAvgCpu.Width = 0;
                lvAvgCpuHeader.IsHitTestVisible = false;
            }
            else
            {
                if (!PluginDatabase.PluginSettings.Settings.lvAvgGpuP)
                {
                    lvAvgGpuP.Width = 0;
                    lvAvgGpuPHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgCpuP)
                {
                    lvAvgCpuP.Width = 0;
                    lvAvgCpuPHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgGpuT)
                {
                    lvAvgGpuT.Width = 0;
                    lvAvgGpuTHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgCpuT)
                {
                    lvAvgCpuT.Width = 0;
                    lvAvgCpuTHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgFps)
                {
                    lvAvgFps.Width = 0;
                    lvAvgFpsHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgRam)
                {
                    lvAvgRam.Width = 0;
                    lvAvgRamHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgGpu)
                {
                    lvAvgGpu.Width = 0;
                    lvAvgGpuHeader.IsHitTestVisible = false;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgCpu)
                {
                    lvAvgCpu.Width = 0;
                    lvAvgCpuHeader.IsHitTestVisible = false;
                }
            }

            if (!PluginDatabase.PluginSettings.Settings.lvGamesPlayAction)
            {
                lvGamesPlayAction.Width = 0;
                lvGamesPlayActionHeader.IsHitTestVisible = false;
            }
            if (!PluginDatabase.PluginSettings.Settings.lvGamesPcName)
            {
                lvGamesPcName.Width = 0;
                lvGamesPcNameHeader.IsHitTestVisible = false;
            }
            if (!PluginDatabase.PluginSettings.Settings.lvGamesSource)
            {
                lvGamesSource.Width = 0;
                lvGamesSourceHeader.IsHitTestVisible = false;
            }
            if (!PluginDatabase.PluginSettings.Settings.lvGamesIcon)
            {
                lvGamesIcon.Width = 0;
                lvGamesIconHeader.IsHitTestVisible = false;
            }
            
            if (!PluginDatabase.PluginSettings.Settings.lvGamesIcon)
            {
                lvGamesIcon.Width = 0;
                lvGamesIconHeader.IsHitTestVisible = false;
            }

            // Graphics game details activities.
            activityForGamesGraphics.Visibility = Visibility.Hidden;

            activityLabel.Content = new DateTime(yearCurrent, monthCurrent, 1).ToString("MMMM yyyy");

            
            #region Get & set datas
            listSources = GetListSourcesName();

            Task.Run(() => {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    getActivityByMonth(yearCurrent, monthCurrent);
                    getActivityByWeek(yearCurrent, monthCurrent);
                }).Wait();


                getActivityByDay(yearCurrent, monthCurrent);
                getActivityByListGame();
                SetSourceFilter();

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
                        PART_ChartTotalHoursSource.Visibility = Visibility.Hidden;
                        PART_ChartTotalHoursSource_Label.Visibility = Visibility.Hidden;

                        Grid.SetColumn(GridDay, 0);
                        Grid.SetColumnSpan(GridDay, 3);
                    }
                }).Wait();

            }).ContinueWith(antecedent =>
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_DataTop.Visibility = Visibility.Visible;
                    PART_DataBottom.Visibility = Visibility.Visible;
                });
            });
            #endregion


            // Set Binding data
            ShowIcon = this._settings.ShowLauncherIcons;
            ModeComplet = (PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstWithText : TextBlockWithIconMode.IconFirstWithText;
            ModeSimple = (PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstOnly : TextBlockWithIconMode.IconFirstOnly;


            PART_ChartTotalHoursSource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartTotalHoursSource_ToolTip.Mode = ModeComplet;

            PART_ChartHoursByDaySource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartHoursByDaySource_ToolTip.Mode = ModeComplet;

            PART_ChartHoursByWeekSource_ToolTip.ShowIcon = ShowIcon;
            PART_ChartHoursByWeekSource_ToolTip.Mode = ModeComplet;
            PART_ChartHoursByWeekSource_ToolTip.ShowWeekPeriode = true;


            DataContext = this;
        }


        private void SetSourceFilter()
        {
            IEnumerable<string> ListSourceName = activityListByGame.Select(x => x.GameSourceName).Distinct();
            foreach (string sourcename in ListSourceName)
            {
                string Icon = PlayniteTools.GetPlatformIcon(sourcename);
                string IconText = TransformIcon.Get(sourcename);

                FilterSourceItems.Add(new ListSource
                {
                    TypeStoreIcon = ModeComplet,
                    SourceIcon = Icon,
                    SourceIconText = IconText,
                    SourceName = sourcename,
                    SourceNameShort = sourcename,
                    IsCheck = false
                });
            }

            FilterSourceItems.Sort((x, y) => x.SourceNameShort.CompareTo(y.SourceNameShort));

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                FilterSource.ItemsSource = FilterSourceItems;
            });
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

            Dictionary<string, ulong> activityByMonth = new Dictionary<string, ulong>();

            // Total hours by source.
            if (isMonthSources)
            {
                if (_settings.ShowLauncherIcons)
                {
                    PART_ChartTotalHoursSource_X.LabelsRotation = 0;
                    PART_ChartTotalHoursSource_X.FontSize = 30;
                }
                else
                {
                    PART_ChartTotalHoursSource_X.LabelsRotation = 160;
                    PART_ChartTotalHoursSource_X.FontSize = (double)resources.GetResource("FontSize");
                }

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Activity> Activities = listGameActivities[iGame].FilterItems;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                            string sourceName = Activities[iActivity].SourceName;

                            // Cumul data
                            if (activityByMonth.ContainsKey(sourceName))
                            {
                                if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                {
                                    activityByMonth[sourceName] = (ulong)activityByMonth[sourceName] + elapsedSeconds;
                                }
                            }
                            else
                            {
                                if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                {
                                    activityByMonth.Add(sourceName, elapsedSeconds);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
                    }
                }

                PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime { ShowIcon = ShowIcon, Mode = ModeComplet };
            }
            // Total hours by genres.
            else
            {
                PART_ChartTotalHoursSource_X.LabelsRotation = 160;
                PART_ChartTotalHoursSource_X.FontSize = (double)resources.GetResource("FontSize");

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    try
                    {
                        List<Genre> listGameListGenres = listGameActivities[iGame].Genres;
                        List<Activity> Activities = listGameActivities[iGame].FilterItems;
                        for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                        {
                            ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                            DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).AddSeconds(-(double)elapsedSeconds).ToLocalTime();

                            for (int iGenre = 0; iGenre < listGameListGenres?.Count; iGenre++)
                            {
                                // Cumul data
                                if (activityByMonth.ContainsKey(listGameListGenres[iGenre].Name))
                                {
                                    if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                    {
                                        activityByMonth[listGameListGenres[iGenre].Name] = (ulong)activityByMonth[listGameListGenres[iGenre].Name] + elapsedSeconds;
                                    }
                                }
                                else
                                {
                                    if (startOfMonth <= dateSession && dateSession <= endOfMonth)
                                    {
                                        activityByMonth.Add(listGameListGenres[iGenre].Name, elapsedSeconds);
                                    }
                                }
                            }
                        }

                        PART_ChartTotalHoursSource.DataTooltip = new CustomerToolTipForTime { ShowIcon = false, Mode = TextBlockWithIconMode.TextOnly };
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error in getActivityByMonth({year}, {month}) with {listGameActivities[iGame].Name}", true, PluginDatabase.PluginName);
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
                    Icon = PlayniteTools.GetPlatformIcon(item.Key),
                    IconText = TransformIcon.Get(item.Key),

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
                        Values = series,
                        Fill = PluginDatabase.PluginSettings.Settings.ChartColors
                    }
                };
            string[] ActivityByMonthLabels = labels;

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

            if (isMonthSources)
            {
                if (_settings.CumulPlaytimeStore)
                {
                    PART_ChartTotalHoursSource.Visibility = Visibility.Hidden;
                    PART_ChartTotalHoursSource_Label.Visibility = Visibility.Hidden;

                    Grid.SetColumn(GridDay, 0);
                    Grid.SetColumnSpan(GridDay, 3);
                }

                Grid.SetColumnSpan(gridMonth, 1);
                PART_ChartHoursByDaySource.Visibility = Visibility.Visible;
                actLabel.Visibility = Visibility.Visible;
                PART_ChartHoursByWeekSource.Visibility = Visibility.Visible;
                acwLabel.Visibility = Visibility.Visible;
            }
            else
            {
                if (_settings.CumulPlaytimeStore)
                {
                    PART_ChartTotalHoursSource.Visibility = Visibility.Visible;
                    PART_ChartTotalHoursSource_Label.Visibility = Visibility.Visible;
                }

                Grid.SetColumnSpan(gridMonth, 5);
                PART_ChartHoursByDaySource.Visibility = Visibility.Hidden;
                actLabel.Visibility = Visibility.Hidden;
                PART_ChartHoursByWeekSource.Visibility = Visibility.Hidden;
                acwLabel.Visibility = Visibility.Hidden;
            }

            PART_ChartTotalHoursSource_Y.LabelFormatter = activityForGameLogFormatter;
            PART_ChartTotalHoursSource.Series = ActivityByMonthSeries;
            PART_ChartTotalHoursSource_Y.MinValue = 0;
            ((CustomerToolTipForTime)PART_ChartTotalHoursSource.DataTooltip).ShowIcon = _settings.ShowLauncherIcons;
            PART_ChartTotalHoursSource_X.Labels = ActivityByMonthLabels;
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
                    List<Activity> Activities = listGameActivities[iGame].FilterItems;
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        string dateSession = Convert.ToDateTime(((DateTime)Activities[iActivity].DateSession).ToLocalTime()).ToString(Constants.DateUiFormat);

                        if (dateSession == activityByDateLabels[iDay])
                        {
                            series[iDay].Values += (long)elapsedSeconds;
                        }
                    }
                }
            }

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                activityByDaySeries.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = series,
                    Fill = PluginDatabase.PluginSettings.Settings.ChartColors
                });

                //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                var customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);

                //lets save the mapper globally
                Charting.For<CustomerForTime>(customerVmMapper);

                Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartHoursByDaySource_Y.LabelFormatter = activityForGameLogFormatter;
                PART_ChartHoursByDaySource.DataTooltip = new CustomerToolTipForTime { ShowIcon = ShowIcon, Mode = ModeComplet };
                PART_ChartHoursByDaySource.Series = activityByDaySeries;
                PART_ChartHoursByDaySource_Y.MinValue = 0;
                PART_ChartHoursByDaySource_X.Labels = activityByDateLabels;
            });
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

            if (firstMonday > StartDate)
            {
                firstMonday = Enumerable.Range(-6, 7)
                    .SkipWhile(x => StartDate.AddDays(x).DayOfWeek != DayOfWeek.Monday)
                    .Select(x => StartDate.AddDays(x))
                    .First();
            }

            //get count of days
            TimeSpan ts = (TimeSpan)(SeriesEndDate - firstMonday);
            //create new list of WeekStartEnd class
            List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();
            //add dates to list
            int iDays = 0;
            for (iDays = 0; iDays < ts.Days; iDays += 7)
            {
                datesPeriodes.Add(new WeekStartEnd() { Monday = firstMonday.AddDays(iDays), Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59) });
            }

            if (datesPeriodes.Last().Sunday < SeriesEndDate)
            {
                datesPeriodes.Add(new WeekStartEnd() { Monday = firstMonday.AddDays(iDays), Sunday = firstMonday.AddDays(iDays + 6).AddHours(23).AddMinutes(59).AddSeconds(59) });
            }

            // Source activty by month
            Dictionary<string, long> activityByWeek1 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek2 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek3 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek4 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek5 = new Dictionary<string, long>();
            Dictionary<string, long> activityByWeek6 = new Dictionary<string, long>();

            List<Dictionary<string, long>> activityByWeek = new List<Dictionary<string, long>>();
            SeriesCollection activityByWeekSeries = new SeriesCollection();
            IChartValues Values = new ChartValues<CustomerForTime>();

            if (isMonthSources)
            {
                // Insert sources
                for (int iSource = 0; iSource < listSources.Count; iSource++)
                {
                    activityByWeek1.Add(listSources[iSource], 0);
                    activityByWeek2.Add(listSources[iSource], 0);
                    activityByWeek3.Add(listSources[iSource], 0);
                    activityByWeek4.Add(listSources[iSource], 0);
                    activityByWeek5.Add(listSources[iSource], 0);
                    activityByWeek6.Add(listSources[iSource], 0);
                }

                activityByWeek.Add(activityByWeek1);
                activityByWeek.Add(activityByWeek2);
                activityByWeek.Add(activityByWeek3);
                activityByWeek.Add(activityByWeek4);
                activityByWeek.Add(activityByWeek5);
                activityByWeek.Add(activityByWeek6);

                List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
                for (int iGame = 0; iGame < listGameActivities.Count; iGame++)
                {
                    List<Activity> Activities = listGameActivities[iGame].FilterItems;
                    for (int iActivity = 0; iActivity < Activities.Count; iActivity++)
                    {
                        ulong elapsedSeconds = Activities[iActivity].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(Activities[iActivity].DateSession).ToLocalTime();
                        string sourceName = Activities[iActivity].SourceName;

                        // Cumul data
                        for (int iWeek = 0; iWeek < datesPeriodes.Count; iWeek++)
                        {
                            if (datesPeriodes[iWeek].Monday <= dateSession && dateSession <= datesPeriodes[iWeek].Sunday)
                            {
                                // Add source by platform
                                if (!activityByWeek[iWeek].ContainsKey(sourceName))
                                {
                                    activityByWeek1.Add(sourceName, 0);
                                    activityByWeek2.Add(sourceName, 0);
                                    activityByWeek3.Add(sourceName, 0);
                                    activityByWeek4.Add(sourceName, 0);
                                    activityByWeek5.Add(sourceName, 0);
                                    activityByWeek6.Add(sourceName, 0);
                                }

                                activityByWeek[iWeek][sourceName] = activityByWeek[iWeek][sourceName] + (long)elapsedSeconds;
                            }
                        }
                    }
                }


                // Check source with data (only view this)
                List<string> listNoDelete = new List<string>();
                for (int i = 0; i < activityByWeek.Count; i++)
                {
                    foreach (var item in activityByWeek[i])
                    {
                        if (item.Value != 0 && listNoDelete.TakeWhile(x => x.ToString() == item.Key).Count() != 1)
                        {
                            listNoDelete.Add(item.Key);
                        }
                    }
                }
                listNoDelete = listNoDelete.Select(x => x).Distinct().ToList();


                // Prepare data.
                string[] labels = new string[listNoDelete.Count];
                for (int iSource = 0; iSource < listNoDelete.Count; iSource++)
                {
                    labels[iSource] = (string)listNoDelete[iSource];
                    if (_settings.ShowLauncherIcons)
                    {
                        labels[iSource] = TransformIcon.Get((string)listNoDelete[iSource]);
                    }


                    Brush Fill = null;
                    if (PluginDatabase.PluginSettings.Settings.StoreColors.Count == 0)
                    {
                        PluginDatabase.PluginSettings.Settings.StoreColors = GameActivitySettingsViewModel.GetDefaultStoreColors();
                    }
                    Fill = PluginDatabase.PluginSettings.Settings.StoreColors
                                .Where(x => x.Name.Contains((string)listNoDelete[iSource], StringComparison.InvariantCultureIgnoreCase))?.FirstOrDefault()?.Fill;


                    Values = new ChartValues<CustomerForTime>();
                    for (int i = 0; i < datesPeriodes.Count; i++)
                    {
                        Values.Add(new CustomerForTime { Name = (string)listNoDelete[iSource], Values = (int)activityByWeek[i][(string)listNoDelete[iSource]] });
                    }

                    activityByWeekSeries.Add(new StackedColumnSeries
                    {
                        Title = labels[iSource],
                        Values = Values,
                        StackMode = StackMode.Values,
                        DataLabels = false,
                        Fill = Fill
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
            if (datesPeriodes.Count == 6)
            {
                activityByWeekLabels = new[]
                {
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[0].Monday),
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[1].Monday),
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[2].Monday),
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[3].Monday),
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[4].Monday),
                    resources.GetString("LOCGameActivityWeekLabel") + " " + Tools.WeekOfYearISO8601(datesPeriodes[5].Monday)
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
                    Values = series,
                    Fill = PluginDatabase.PluginSettings.Settings.ChartColors
                });

                PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForTime
                {
                    ShowIcon = ShowIcon,
                    ShowTitle = true,
                    Mode = ModeComplet,
                    ShowWeekPeriode = true,
                    DatesPeriodes = datesPeriodes
                };
            }
            else
            {
                PART_ChartHoursByWeekSource.DataTooltip = new CustomerToolTipForMultipleTime
                {
                    ShowIcon = ShowIcon,
                    ShowTitle = true,
                    Mode = ModeComplet,
                    ShowWeekPeriode = true,
                    DatesPeriodes = datesPeriodes
                };
            }

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForTime>(customerVmMapper);

            Func<double, string> activityForGameLogFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

            PART_ChartHoursByWeekSource_Y.LabelFormatter = activityForGameLogFormatter;
            PART_ChartHoursByWeekSource.Series = activityByWeekSeries;
            PART_ChartHoursByWeekSource_Y.MinValue = 0;
            PART_ChartHoursByWeekSource_X.Labels = activityByWeekLabels;
        }


        /// <summary>
        /// Get list games with an activities.
        /// </summary>
        public void getActivityByListGame()
        {
            activityListByGame = new List<ListActivities>();

            List<GameActivities> listGameActivities = GameActivity.PluginDatabase.GetListGameActivity();
            listGameActivities = listGameActivities.Where(x => x.FilterItems.Count > 0 && !x.IsDeleted).ToList();

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
                            sourceName = listGameActivities[iGame].GetLastSessionActivity().SourceName;
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, "Error to get SourceName", true, PluginDatabase.PluginName);
                        }

                        Activity lastSessionActivity = listGameActivities[iGame].GetLastSessionActivity();
                        ulong elapsedSeconds = lastSessionActivity.ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(lastSessionActivity.DateSession).ToLocalTime();

                        string GameIcon = listGameActivities[iGame].Icon;
                        if (!GameIcon.IsNullOrEmpty())
                        {
                            GameIcon = dbPlaynite.GetFullFilePath(GameIcon);
                        }

                        activityListByGame.Add(new ListActivities()
                        {
                            Id = listGameActivities[iGame].Id,
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
                            AvgCPUP = listGameActivities[iGame].avgCPUP(listGameActivities[iGame].GetLastSession()) + "W",
                            AvgGPUP = listGameActivities[iGame].avgGPUP(listGameActivities[iGame].GetLastSession()) + "W",

                            EnableWarm = _settings.EnableWarning,
                            MaxCPUT = _settings.MaxCpuTemp.ToString(),
                            MaxGPUT = _settings.MaxGpuTemp.ToString(),
                            MinFPS = _settings.MinFps.ToString(),
                            MaxCPU = _settings.MaxCpuUsage.ToString(),
                            MaxGPU = _settings.MaxGpuUsage.ToString(),
                            MaxRAM = _settings.MaxRamUsage.ToString(),

                            PCConfigurationId = listGameActivities[iGame].GetLastSessionActivity()?.IdConfiguration ?? -1,
                            PCName = listGameActivities[iGame].GetLastSessionActivity()?.Configuration.Name,

                            TypeStoreIcon = ModeSimple,
                            SourceIcon = PlayniteTools.GetPlatformIcon(sourceName),
                            SourceIconText = TransformIcon.Get(sourceName),

                            GameActionName = listGameActivities[iGame].GetLastSessionActivity()?.GameActionName
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
                    Common.LogError(ex, false, $"Failed to load GameActivities from {gameID}", true, PluginDatabase.PluginName);
                }
            }

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                lvGames.ItemsSource = activityListByGame;

                lvGames.Sorting();
                Filter();
            });
        }


        /// <summary>
        /// Get data for the selected game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="variateur"></param>
        public void getActivityForGamesTimeGraphics(string gameID, bool isNavigation = false)
        {
            PART_GameActivityChartTime.GameContext = _PlayniteApi.Database.Games.Get(Guid.Parse(gameID));
            PART_GameActivityChartTime.DisableAnimations = true;
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
            PART_GameActivityChartLog.DisableAnimations = true;
            PART_GameActivityChartLog.DateSelected = dateSelected;
            PART_GameActivityChartLog.TitleChart = title;
            PART_GameActivityChartLog.AxisVariator = variateurLog;

            if (!isNavigation)
            {
                if (dateSelected == null || dateSelected == default(DateTime))
                {
                    gameLabel.Content = resources.GetString("LOCGameActivityLogTitleDate") + " ("
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
        public List<string> GetListSourcesName()
        {
            List<string> arrayReturn = new List<string>();
            foreach (GameSource source in dbPlaynite.Sources)
            {
                if (arrayReturn.Find(x => x.IsEqual(source.Name)) == null)
                {
                    arrayReturn.AddMissing(source.Name);
                }
            }
            
            // Source for game add manually.
            arrayReturn.AddMissing("Playnite");

            Common.LogDebug(true, Serialization.ToJson(arrayReturn));
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
                ListBox item = (ListBox)sender;
                if (((List<ListActivities>)item.ItemsSource)?.Count > 0)
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


                int index = -1;
                if (lvGames.SelectedItem != null)
                {
                    index = ((ListActivities)lvGames.SelectedItem).PCConfigurationId;
                }

                if (index != -1 && index < PluginDatabase.LocalSystem.GetConfigurations().Count)
                {
                    SystemConfiguration Configuration = PluginDatabase.LocalSystem.GetConfigurations()[index];

                    PART_PcName.Content = Configuration.Name;
                    PART_Os.Content = Configuration.Os;
                    PART_CpuName.Content = Configuration.Cpu;
                    PART_GpuName.Content = Configuration.GpuName;
                    PART_Ram.Content = Configuration.RamUsage;
                }
                else
                {
                    PART_PcName.Content = string.Empty;
                    PART_Os.Content = string.Empty;
                    PART_CpuName.Content = string.Empty;
                    PART_GpuName.Content = string.Empty;
                    PART_Ram.Content = string.Empty;
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

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;

            DateTime dateNew = (DateTime)control.SelectedDate;
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
                    PART_Truncate.Visibility = Visibility.Visible;
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
                    PART_Truncate.Visibility = Visibility.Collapsed;
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


        private void Bt_Truncate(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                PART_GameActivityChartTime.Truncate = (bool)((ToggleButton)sender).IsChecked;
                PART_GameActivityChartTime.AxisVariator = 0;
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
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = titleChart;
                PART_GameActivityChartLog.Next();
            }
        }

        private void Button_Click_prevGamePlus(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Prev(PluginDatabase.PluginSettings.Settings.VariatorTime);
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = titleChart;
                PART_GameActivityChartLog.Prev(PluginDatabase.PluginSettings.Settings.VariatorLog);
            }
        }

        private void Button_Click_nextGamePlus(object sender, RoutedEventArgs e)
        {
            if (isGameTime)
            {
                PART_GameActivityChartTime.DisableAnimations = true;
                PART_GameActivityChartTime.Next(PluginDatabase.PluginSettings.Settings.VariatorTime);
            }
            else
            {
                PART_GameActivityChartLog.DisableAnimations = true;
                PART_GameActivityChartLog.DateSelected = LabelDataSelected;
                PART_GameActivityChartLog.TitleChart = titleChart;
                PART_GameActivityChartLog.Next(PluginDatabase.PluginSettings.Settings.VariatorLog);
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
                IChartValues data = chartPoint.SeriesView.Values;

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


        private void PART_CbSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }
        private void PART_CbSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }
        private void FilterCbSource(CheckBox sender)
        {
            FilterSource.Text = string.Empty;

            if ((bool)sender.IsChecked)
            {
                SearchSources.Add(((ListSource)sender.Tag).SourceNameShort);
            }
            else
            {
                SearchSources.Remove(((ListSource)sender.Tag).SourceNameShort);
            }

            if (SearchSources.Count != 0)
            {
                FilterSource.Text = string.Join(", ", SearchSources);
            }
            
            Filter();
        }
        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            Game game = API.Instance.Database.Games.Get((Guid)bt.Tag);
            GameActivityViewSingle ViewExtension = new GameActivityViewSingle(plugin, game);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(API.Instance, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }
    }

    public class ListSource
    {
        public TextBlockWithIconMode TypeStoreIcon { get; set; }

        public string SourceIcon { get; set; }
        public string SourceIconText { get; set; }
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }
}
