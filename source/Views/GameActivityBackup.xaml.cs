using CommonPlayniteShared.Common;
using CommonPluginsShared;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
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
        private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private ViewDataContext ViewDataContext { get; set; } = new ViewDataContext();
        private ActivityBackup ActivityBackup { get; set; }

        private Guid Id { get; set; }


        public GameActivityBackup(ActivityBackup activityBackup)
        {
            ActivityBackup = activityBackup;
            Game game = API.Instance.Database.Games.Get(activityBackup.Id);
            Id = game.Id;

            InitializeComponent();
            DataContext = ViewDataContext;

            ViewDataContext.Name = activityBackup.Name;
            ViewDataContext.DateSession = activityBackup.DateSession;
            ViewDataContext.ElapsedSeconds = activityBackup.ElapsedSeconds;

            if (!game.CoverImage.IsNullOrEmpty())
            {
                ViewDataContext.Cover = API.Instance.Database.GetFullFilePath(game.CoverImage);
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
            ((Window)Parent).Close();
        }

        private void PART_BtAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Game game = API.Instance.Database.Games.Get(ActivityBackup.Id);
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

                API.Instance.Database.Games.Update(game);
                PluginDatabase.Update(pluginData);


                string PathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{this.Id}.json");
                FileSystem.DeleteFile(PathFileBackup);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)Parent).Close();
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

            ((Window)Parent).Close();
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
        private string _name;
        public string Name { get => _name; set => SetValue(ref _name, value); }

        private string _cover;
        public string Cover { get => _cover; set => SetValue(ref _cover, value); }

        private DateTime _dateSession;
        public DateTime DateSession { get => _dateSession; set => SetValue(ref _dateSession, value); }

        private ulong _elapsedSeconds;
        public ulong ElapsedSeconds { get => _elapsedSeconds; set => SetValue(ref _elapsedSeconds, value); }

        private DateTime? _dateLastPlayed;
        public DateTime? DateLastPlayed { get => _dateLastPlayed; set => SetValue(ref _dateLastPlayed, value); }

        private ulong _playtime;
        public ulong Playtime { get => _playtime; set => SetValue(ref _playtime, value); }
    }
}
