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
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls.Primitives;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityViewSingle.xaml
    /// </summary>
    public partial class GameActivityViewSingle : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private PluginChartTime PART_ChartTime;
        private PluginChartLog PART_ChartLog;

        private GameActivities gameActivities;
        private Game game;
        private GameActivity plugin;


        public GameActivityViewSingle(GameActivity plugin, Game game)
        {
            this.game = game;
            this.plugin = plugin;

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
            PART_ChartTime.Truncate = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate;
            PART_Truncate.IsChecked = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate;


            lvSessions.SaveColumn = PluginDatabase.PluginSettings.Settings.SaveColumnOrder;
            lvSessions.SaveColumnFilePath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "lvSessions.json");

            GridView lvView = (GridView)lvSessions.View;

            // Game logs
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

                PART_BtLogContener.Visibility = Visibility.Collapsed;
                PART_ChartLogContener.Visibility = Visibility.Collapsed;
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
            if (!PluginDatabase.PluginSettings.Settings.lvGamesPlayAction)
            {
                lvGamesPlayAction.Width = 0;
                lvGamesPlayActionHeader.IsHitTestVisible = false;
            }

            getActivityByListGame(gameActivities);

            PART_ChartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
            PART_ChartLog.GameContext = game;

            this.DataContext = new
            {
                GameDisplayName = game.Name,
                Settings = PluginDatabase.PluginSettings
            };
        }
        

        #region Time navigation 
        private void Bt_PrevTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Prev();
        }

        private void Bt_NextTime(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Next();
        }

        private void Bt_PrevTimePlus(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Prev(PluginDatabase.PluginSettings.Settings.VariatorTime);
        }

        private void Bt_NextTimePlus(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Next(PluginDatabase.PluginSettings.Settings.VariatorTime);
        }

        private void Bt_Truncate(object sender, RoutedEventArgs e)
        {
            PART_ChartTime.Truncate = (bool)((ToggleButton)sender).IsChecked;
            PART_ChartTime.AxisVariator = 0;
        }
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;
            if ((bool)tb.IsChecked)
            {
                PART_ChartTime.ShowByWeeks = true;
            }
            else
            {
                PART_ChartTime.ShowByWeeks = false;
            }
            PART_ChartTime.AxisVariator = 0;
        }
        #endregion


        #region Log navigation
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

        private void Bt_PrevLogPlus(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.Prev(PluginDatabase.PluginSettings.Settings.VariatorLog);
        }

        private void Bt_NextLogPlus(object sender, RoutedEventArgs e)
        {
            PART_ChartLog.Next(PluginDatabase.PluginSettings.Settings.VariatorLog);
        }
        #endregion


        public void getActivityByListGame(GameActivities gameActivities)
        {
            Task.Run(() => 
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
                            AvgCPUP = gameActivities.avgCPUP(dateSession.ToUniversalTime()) + "W",
                            AvgGPUP = gameActivities.avgGPUP(dateSession.ToUniversalTime()) + "W",

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

                            GameActionName = gameActivities.FilterItems[iItem].GameActionName
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Failed to load GameActivities for {gameActivities.Name}", true, PluginDatabase.PluginName);
                    }
                }

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    lvSessions.ItemsSource = activityListByGame;
                    lvSessions.Sorting();
                
                    if (((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Count > 0)
                    {
                        lvSessions.SelectedItem = ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).OrderByDescending(x => x.DateActivity).LastOrDefault();
                    }
                });
            });
        }


        #region Data actions
        private void PART_Delete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = PluginDatabase.PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCConfirumationAskGeneric"), PluginDatabase.PluginName, MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    object GameLastActivity = ((FrameworkElement)sender).Tag;
                    ListActivities activity = ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Where(x => x.GameLastActivity == (DateTime)GameLastActivity).FirstOrDefault();

                    // Delete playtime
                    if (activity.GameElapsedSeconds != 0)
                    {
                        if ((long)(game.Playtime - activity.GameElapsedSeconds) >= 0)
                        {
                            game.Playtime -= activity.GameElapsedSeconds;
                            if (game.PlayCount != 0)
                            {
                                game.PlayCount--;
                            }
                            else
                            {
                                logger.Warn($"Play count is already at 0 for {game.Name}");
                            }
                        }
                        else
                        {
                            logger.Warn($"Impossible to remove GameElapsedSeconds ({activity.GameElapsedSeconds}) in Playtime ({game.Playtime}) of {game.Name}");
                        }
                    }

                    gameActivities.DeleteActivity(activity.GameLastActivity);

                    // Set last played date
                    game.LastActivity = (DateTime)gameActivities.Items.Max(x => x.DateSession);

                    PluginDatabase.PlayniteApi.Database.Games.Update(game);
                    PluginDatabase.Update(gameActivities);

                    lvSessions.SelectedIndex = -1;
                    ((ObservableCollection<ListActivities>)lvSessions.ItemsSource).Remove(activity);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
        }

        private void PART_BtAdd_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true
            };

            try
            {
                GameActivityAddTime ViewExtension = new GameActivityAddTime(plugin, game, null);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGaAddNewGameSession"), ViewExtension, windowOptions);
                windowExtension.ShowDialog();

                if (ViewExtension.activity != null)
                {
                    gameActivities.Items.Add(ViewExtension.activity);
                    getActivityByListGame(gameActivities);

                    if (ViewExtension.activity.ElapsedSeconds >= 0)
                    {
                        game.Playtime += ViewExtension.activity.ElapsedSeconds;
                        game.PlayCount++;
                    }

                    // Set last played date
                    game.LastActivity = (DateTime)gameActivities.Items.Max(x => x.DateSession);

                    PluginDatabase.PlayniteApi.Database.Games.Update(game);
                    PluginDatabase.Update(gameActivities);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_BtEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                object GameLastActivity = ((FrameworkElement)sender).Tag;
                int index = gameActivities.Items.FindIndex(x => x.DateSession == ((DateTime)GameLastActivity).ToUniversalTime());
                Activity activity = gameActivities.Items[index];
                ulong ElapsedSeconds = activity.ElapsedSeconds;

                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true
                };

                GameActivityAddTime ViewExtension = new GameActivityAddTime(plugin, game, activity);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGaAddNewGameSession"), ViewExtension, windowOptions);
                windowExtension.ShowDialog();

                if (ViewExtension.activity != null)
                {
                    gameActivities.Items[index] = ViewExtension.activity;
                    getActivityByListGame(gameActivities);

                    if (ViewExtension.activity.ElapsedSeconds >= 0)
                    {
                        game.Playtime += ViewExtension.activity.ElapsedSeconds - ElapsedSeconds;
                    }

                    // Set last played date
                    game.LastActivity = (DateTime)gameActivities.Items.Max(x => x.DateSession);

                    PluginDatabase.PlayniteApi.Database.Games.Update(game);
                    PluginDatabase.Update(gameActivities);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_BtMerged_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true
                };

                GameActivityMergeTime ViewExtension = new GameActivityMergeTime(game);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCGaMergeSession"), ViewExtension, windowOptions);
                windowExtension.ShowDialog();

                gameActivities = PluginDatabase.Get(game);
                getActivityByListGame(gameActivities);

                // Set last played date
                game.LastActivity = (DateTime)gameActivities.Items.Max(x => x.DateSession);

                if (game.PlayCount != 0)
                {
                    game.PlayCount--;
                }
                else
                {
                    logger.Warn($"Play count is already at 0 for {game.Name}");
                }

                PluginDatabase.PlayniteApi.Database.Games.Update(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        #endregion
    }
}
