using CommonPlayniteShared.Common;
using CommonPluginsShared;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityBackup.xaml
    /// </summary>
    public partial class GameActivityBackup : UserControl
    {
        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private ViewDataContext ViewDataContext { get; set; } = new ViewDataContext();
        private ActivityBackup ActivityBackup { get; set; }

        private Guid Id { get; set; }


        public GameActivityBackup(ActivityBackup activityBackup)
        {
            this.ActivityBackup = activityBackup;
            Game game = PluginDatabase.PlayniteApi.Database.Games.Get(activityBackup.Id);
            this.Id = game.Id;

            InitializeComponent();
            this.DataContext = ViewDataContext;

            ViewDataContext.Name = activityBackup.Name;
            ViewDataContext.DateSession = activityBackup.DateSession;
            ViewDataContext.ElapsedSeconds = activityBackup.ElapsedSeconds;

            if (!game.CoverImage.IsNullOrEmpty())
            {
                ViewDataContext.Cover = PluginDatabase.PlayniteApi.Database.GetFullFilePath(game.CoverImage);
            }
            ViewDataContext.DateLastPlayed = (DateTime)game?.LastActivity;
            ViewDataContext.Playtime = game.Playtime;

            if (activityBackup.ItemsDetailsDatas?.Count == 0)
            {
                PART_ChartLogContener.Visibility = Visibility.Collapsed;
            }
            else
            {
                PluginChartLog PART_ChartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
                PART_ChartLog.SetDefaultDataContext();

                GameActivities pluginData = PluginDatabase.GetDefault(activityBackup.Id);
                pluginData.Items.Add(new Activity
                {
                    IdConfiguration = activityBackup.IdConfiguration,
                    GameActionName = activityBackup.GameActionName,
                    DateSession = activityBackup.DateSession,
                    SourceID = activityBackup.SourceID,
                    PlatformIDs = activityBackup.PlatformIDs,
                    ElapsedSeconds = activityBackup.ElapsedSeconds
                });
                pluginData.ItemsDetails.Items.TryAdd(activityBackup.DateSession, activityBackup.ItemsDetailsDatas);

                PART_ChartLog.GetActivityForGamesLogGraphics(pluginData, 0, 10, activityBackup.DateSession, "1");
            }
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Game game = PluginDatabase.PlayniteApi.Database.Games.Get(ActivityBackup.Id);
                GameActivities pluginData = PluginDatabase.Get(ActivityBackup.Id);

                game.Playtime += ActivityBackup.ElapsedSeconds;
                pluginData.Items.Add(new Activity
                {
                    IdConfiguration = ActivityBackup.IdConfiguration,
                    GameActionName = ActivityBackup.GameActionName,
                    DateSession = ActivityBackup.DateSession,
                    SourceID = ActivityBackup.SourceID,
                    PlatformIDs = ActivityBackup.PlatformIDs,
                    ElapsedSeconds = ActivityBackup.ElapsedSeconds
                });
                pluginData.ItemsDetails.Items.TryAdd(ActivityBackup.DateSession, ActivityBackup.ItemsDetailsDatas);

                PluginDatabase.PlayniteApi.Database.Games.Update(game);
                PluginDatabase.Update(pluginData);


                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{this.Id}.json");
                FileSystem.DeleteFile(PathFileBackup);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)this.Parent).Close();
        }

        private void PART_BtRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{this.Id}.json");
                FileSystem.DeleteFile(PathFileBackup);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)this.Parent).Close();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            PluginChartLog PART_ChartLog = (PluginChartLog)PART_ChartLogContener.Children[0];
            PART_ChartLog.Width = PART_ChartLogContener.ActualWidth;
            PART_ChartLog.Height = PART_ChartLogContener.ActualHeight;
            ((PluginChartLogDataContext)PART_ChartLog.DataContext).UseControls = false;
        }
    }


    public class ViewDataContext : ObservableObject
    {
        private string _Name;
        public string Name { get => _Name; set => SetValue(ref _Name, value); }

        private string _Cover;
        public string Cover { get => _Cover; set => SetValue(ref _Cover, value); }

        private DateTime _DateSession;
        public DateTime DateSession { get => _DateSession; set => SetValue(ref _DateSession, value); }

        private ulong _ElapsedSeconds;
        public ulong ElapsedSeconds { get => _ElapsedSeconds; set => SetValue(ref _ElapsedSeconds, value); }

        private DateTime? _DateLastPlayed;
        public DateTime? DateLastPlayed { get => _DateLastPlayed; set => SetValue(ref _DateLastPlayed, value); }

        private ulong _Playtime;
        public ulong Playtime { get => _Playtime; set => SetValue(ref _Playtime, value); }
    }
}
