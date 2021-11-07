using CommonPluginsControls.Controls;
using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
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
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityViewSingle.xaml
    /// </summary>
    public partial class GameActivityViewSingle : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private PluginChartTime PART_ChartTime;
        private PluginChartLog PART_ChartLog;

        private GameActivities gameActivities;


        public GameActivityViewSingle(Game game)
        {
            InitializeComponent();

            ButtonShowConfig.IsChecked = false;

            // Cover
            if (!game.CoverImage.IsNullOrEmpty())
            {
                string CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(game.CoverImage);
                PART_ImageCover.Source = BitmapExtensions.BitmapFromFile(CoverImage);
            }

            // Game sessions infos
            gameActivities = PluginDatabase.Get(game);

            PlayTimeToStringConverter longToTimePlayedConverter = new PlayTimeToStringConverter();
            PART_TimeAvg.Text = (string)longToTimePlayedConverter.Convert(gameActivities.avgPlayTime(), null, null, CultureInfo.CurrentCulture);


            LocalDateConverter localDateConverter = new LocalDateConverter();
            PlayTimeToStringConverter converter = new PlayTimeToStringConverter();

            PART_FirstSession.Text = (string)localDateConverter.Convert(gameActivities.GetFirstSession(), null, null, CultureInfo.CurrentCulture);
            PART_FirstSessionElapsedTime.Text = (string)converter.Convert((long)gameActivities.GetFirstSessionactivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);

            PART_LastSession.Text = (string)localDateConverter.Convert(gameActivities.GetLastSession(), null, null, CultureInfo.CurrentCulture);
            PART_LastSessionElapsedTime.Text = (string)converter.Convert((long)gameActivities.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);


            // Game session time line
            PART_ChartTime = (PluginChartTime)PART_ChartTimeContener.Children[0];
            PART_ChartTime.GameContext = game;


            GridView lvView = (GridView)lvSessions.View;

            // Game logs
            // Add column if log details enable.
            if (!PluginDatabase.PluginSettings.Settings.EnableLogging)
            {
                lvView.Columns.RemoveAt(10);
                lvView.Columns.RemoveAt(9);
                lvView.Columns.RemoveAt(8);
                lvView.Columns.RemoveAt(7);
                lvView.Columns.RemoveAt(6);
                lvView.Columns.RemoveAt(5);

                PART_BtLogContener.Visibility = Visibility.Collapsed;
                PART_ChartLogContener.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (!PluginDatabase.PluginSettings.Settings.lvAvgGpuT)
                {
                    lvView.Columns.RemoveAt(10);
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgCpuT)
                {
                    lvView.Columns.RemoveAt(9);
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgFps)
                {
                    lvView.Columns.RemoveAt(8);
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgRam)
                {
                    lvView.Columns.RemoveAt(7);
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgGpu)
                {
                    lvView.Columns.RemoveAt(6);
                }
                if (!PluginDatabase.PluginSettings.Settings.lvAvgCpu)
                {
                    lvView.Columns.RemoveAt(5);
                }
            }

            if (!PluginDatabase.PluginSettings.Settings.lvGamesSource)
            {
                lvView.Columns.RemoveAt(4);
            }
            if (!PluginDatabase.PluginSettings.Settings.lvGamesName)
            {
                lvView.Columns.RemoveAt(3);
            }

            getActivityByListGame(gameActivities);

            PART_ChartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
            PART_ChartLog.GameContext = game;

            if (((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Count > 0)
            {
                lvSessions.SelectedItem = ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).OrderByDescending(x => x.DateActivity).LastOrDefault();
            }

            this.DataContext = new
            {
                GameDisplayName = game.Name
            };
        }


        private void Bt_PrevTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Prev();
        }

        private void Bt_NextTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Next();
        }


        private void LvSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvSessions.SelectedItem == null)
            {
                return;
            }

            string titleChart = "1";
            DateTime dateSelected = ((ListActivities)lvSessions.SelectedItem).GameLastActivity;
            
            PART_ChartLog.DateSelected = dateSelected;
            PART_ChartLog.TitleChart = titleChart;
            PART_ChartLog.AxisVariator = 0;


            int index = ((ListActivities)lvSessions.SelectedItem).PCConfigurationId;
            if (index != -1 && index < PluginDatabase.LocalSystem.GetConfigurations().Count)
            {
                var Configuration = PluginDatabase.LocalSystem.GetConfigurations()[index];

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

        private void Bt_PrevLog(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.Prev();
        }

        private void Bt_NextLog(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.Next();
        }


        public void getActivityByListGame(GameActivities gameActivities)
        {
            ObservableCollection<ListActivities> activityListByGame = new ObservableCollection<ListActivities>();

            for (int iItem = 0; iItem < gameActivities.FilterItems.Count; iItem++)
            {
                try
                {
                    ulong elapsedSeconds = gameActivities.FilterItems[iItem].ElapsedSeconds;
                    DateTime dateSession = Convert.ToDateTime(gameActivities.FilterItems[iItem].DateSession).ToLocalTime();
                    string sourceName = gameActivities.FilterItems[iItem].SourceName;
                    var ModeSimple = (PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1) ? TextBlockWithIconMode.IconTextFirstOnly : TextBlockWithIconMode.IconFirstOnly;

                    activityListByGame.Add(new ListActivities()
                    {
                        GameLastActivity = dateSession,
                        GameElapsedSeconds = elapsedSeconds,
                        AvgCPU = gameActivities.avgCPU(dateSession.ToUniversalTime()) + "%",
                        AvgGPU = gameActivities.avgGPU(dateSession.ToUniversalTime()) + "%",
                        AvgRAM = gameActivities.avgRAM(dateSession.ToUniversalTime()) + "%",
                        AvgFPS = gameActivities.avgFPS(dateSession.ToUniversalTime()) + "",
                        AvgCPUT = gameActivities.avgCPUT(dateSession.ToUniversalTime()) + "°",
                        AvgGPUT = gameActivities.avgGPUT(dateSession.ToUniversalTime()) + "°",

                        GameSourceName = sourceName,
                        TypeStoreIcon = ModeSimple,
                        SourceIcon = PlayniteTools.GetPlatformIcon(sourceName),
                        SourceIconText = TransformIcon.Get(sourceName),

                        EnableWarm = PluginDatabase.PluginSettings.Settings.EnableWarning,
                        MaxCPUT = PluginDatabase.PluginSettings.Settings.MaxCpuTemp.ToString(),
                        MaxGPUT = PluginDatabase.PluginSettings.Settings.MaxGpuTemp.ToString(),
                        MinFPS = PluginDatabase.PluginSettings.Settings.MinFps.ToString(),
                        MaxCPU = PluginDatabase.PluginSettings.Settings.MaxCpuUsage.ToString(),
                        MaxGPU = PluginDatabase.PluginSettings.Settings.MaxGpuUsage.ToString(),
                        MaxRAM = PluginDatabase.PluginSettings.Settings.MaxRamUsage.ToString(),

                        PCConfigurationId = gameActivities.FilterItems[iItem].IdConfiguration,
                        PCName = gameActivities.FilterItems[iItem].Configuration.Name,
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


        private void PART_Delete_Click(object sender, RoutedEventArgs e)
        {
            var GameLastActivity = ((FrameworkElement)sender).Tag;
            var activity = ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Where(x => x.GameLastActivity == (DateTime)GameLastActivity).FirstOrDefault();

            gameActivities.DeleteActivity(activity.GameLastActivity);
            PluginDatabase.Update(gameActivities);

            lvSessions.SelectedIndex = -1;
            ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Remove(activity);
        }
    }
}
