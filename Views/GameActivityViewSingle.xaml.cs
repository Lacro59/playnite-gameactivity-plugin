using CommonPluginsPlaynite.Converters;
using CommonPluginsShared;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
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

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityViewSingle.xaml
    /// </summary>
    public partial class GameActivityViewSingle : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private GameActivityChartTime PART_ChartTime;
        private GameActivityChartLog PART_ChartLog;


        public GameActivityViewSingle(Game game)
        {
            InitializeComponent();

            // Cover
            if (!game.CoverImage.IsNullOrEmpty())
            {
                string CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(game.CoverImage);
                PART_ImageCover.Source = BitmapExtensions.BitmapFromFile(CoverImage);
            }

            // Game sessions infos
            GameActivities gameActivities = PluginDatabase.Get(game);

            LongToTimePlayedConverter longToTimePlayedConverter = new LongToTimePlayedConverter();
            PART_TimeAvg.Text = (string)longToTimePlayedConverter.Convert(gameActivities.avgPlayTime(), null, null, CultureInfo.CurrentCulture);


            LocalDateConverter localDateConverter = new LocalDateConverter();

            PART_FirstSession.Text = (string)localDateConverter.Convert(gameActivities.GetFirstSession(), null, null, CultureInfo.CurrentCulture);
            PART_LastSession.Text = (string)localDateConverter.Convert(gameActivities.GetLastSession(), null, null, CultureInfo.CurrentCulture);


            // Game session time line
            PART_ChartTime = new GameActivityChartTime
            {
                IgnoreSettings = true,
                LabelsRotation = true,
                AxisLimit = 15
            };
            PART_ChartTime.GameContext = game;

            PART_ChartTimeContener.Children.Add(PART_ChartTime);

            // Game logs
            // Add column if log details enable.
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                GridView lvView = (GridView)lvSessions.View;

                lvView.Columns.RemoveAt(8);
                lvView.Columns.RemoveAt(7);
                lvView.Columns.RemoveAt(6);
                lvView.Columns.RemoveAt(5);
                lvView.Columns.RemoveAt(4);
                lvView.Columns.RemoveAt(3);

                lvSessions.View = lvView;

                PART_LogContener.Visibility = Visibility.Collapsed;
            }

            getActivityByListGame(gameActivities);

            PART_ChartLog = new GameActivityChartLog
            {
                IgnoreSettings = true,
                AxisLimit = 15
            };
            PART_ChartLog.GameContext = game;

            PART_ChartLogContener.Children.Add(PART_ChartLog);

            if (((List<listGame>)lvSessions.ItemsSource).Count > 0)
            {
                lvSessions.SelectedIndex = 0;
            }

            this.DataContext = new
            {
                GameDisplayName = game.Name
            };
        }


        private void PART_ChartTimeContener_Loaded(object sender, RoutedEventArgs e)
        { 
            PART_ChartTime.Height = PART_ChartTimeContener.ActualHeight;
        }

        private void PART_ChartLogContener_Loaded(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.Height = PART_ChartLogContener.ActualHeight;
        }


        private void Bt_PrevTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.DisableAnimations = true;
            PART_ChartTime.Prev();
        }

        private void Bt_NextTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.DisableAnimations = true;
            PART_ChartTime.Next();
        }


        private void LvSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string titleChart = "1";
            DateTime dateSelected = ((listGame)lvSessions.SelectedItem).listGameLastActivity;
            
            PART_ChartLog.DateSelected = dateSelected;
            PART_ChartLog.TitleChart = titleChart;
            PART_ChartLog.AxisVariator = 0;
        }

        private void Bt_PrevLog(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.DisableAnimations = true;
            PART_ChartLog.Prev();
        }

        private void Bt_NextLog(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.DisableAnimations = true;
            PART_ChartLog.Next();
        }


        public void getActivityByListGame(GameActivities gameActivities)
        {
            List<listGame> activityListByGame = new List<listGame>();

            for (int iItem = 0; iItem < gameActivities.Items.Count; iItem++)
            {
                try
                {
                    long elapsedSeconds = gameActivities.Items[iItem].ElapsedSeconds;
                    DateTime dateSession = Convert.ToDateTime(gameActivities.Items[iItem].DateSession).ToLocalTime();

                    activityListByGame.Add(new listGame()
                    {
                        listGameLastActivity = dateSession,
                        listGameElapsedSeconds = elapsedSeconds,
                        avgCPU = gameActivities.avgCPU(dateSession.ToUniversalTime()) + "%",
                        avgGPU = gameActivities.avgGPU(dateSession.ToUniversalTime()) + "%",
                        avgRAM = gameActivities.avgRAM(dateSession.ToUniversalTime()) + "%",
                        avgFPS = gameActivities.avgFPS(dateSession.ToUniversalTime()) + "",
                        avgCPUT = gameActivities.avgCPUT(dateSession.ToUniversalTime()) + "°",
                        avgGPUT = gameActivities.avgGPUT(dateSession.ToUniversalTime()) + "°",

                        enableWarm = PluginDatabase.PluginSettings.Settings.EnableWarning,
                        maxCPUT = PluginDatabase.PluginSettings.Settings.MaxCpuTemp.ToString(),
                        maxGPUT = PluginDatabase.PluginSettings.Settings.MaxGpuTemp.ToString(),
                        minFPS = PluginDatabase.PluginSettings.Settings.MinFps.ToString(),
                        maxCPU = PluginDatabase.PluginSettings.Settings.MaxCpuUsage.ToString(),
                        maxGPU = PluginDatabase.PluginSettings.Settings.MaxGpuUsage.ToString(),
                        maxRAM = PluginDatabase.PluginSettings.Settings.MaxRamUsage.ToString(),
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load GameActivities for {gameActivities.Name}");
                    PluginDatabase.PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, $"GameActivity error on {gameActivities.Name}");
                }
            }

            lvSessions.ItemsSource = activityListByGame;
            lvSessions.Sorting();
        }
    }


    public class listGame
    {
        public string listGameTitle { get; set; }
        public string listGameID { get; set; }
        public string listGameIcon { get; set; }
        public DateTime listGameLastActivity { get; set; }
        public long listGameElapsedSeconds { get; set; }

        public List<string> listDateActivity { get; set; }

        public string listGameSourceName { get; set; }
        public string listGameSourceIcon { get; set; }

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
}
