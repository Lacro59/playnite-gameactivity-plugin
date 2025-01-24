using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    /// Logique d'interaction pour GameDataMismatch.xaml
    /// </summary>
    public partial class GamesDataMismatch : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private GameDataMismatchDataContext ControlDataContext { get; set; } = new GameDataMismatchDataContext();

        public GamesDataMismatch()
        {
            InitializeComponent();

            DataContext = ControlDataContext;
            ControlDataContext.DataMismatch = PluginDatabase.GetGamesDataMismatch(false).ToObservable();

            Bt_GaToPlayniteAll.IsEnabled = ControlDataContext.DataMismatch?.Count > 0;
        }

        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }

        private void PlayniteToGa_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button bt = (Button)sender;
                Guid id = (Guid)bt.Tag;
                GameActivities gameActivities = PluginDatabase.Get(id);
                Game game = API.Instance.Database.Games.Get(id);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void GaToPlaynite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button bt = (Button)sender;
                Guid id = (Guid)bt.Tag;
                GameActivities gameActivities = PluginDatabase.Get(id);
                Game game = API.Instance.Database.Games.Get(id);

                game.PlayCount = gameActivities.Count;
                game.Playtime = gameActivities.SessionPlaytime;
                API.Instance.Database.Games.Update(game);

                ControlDataContext.DataMismatch = PluginDatabase.GetGamesDataMismatch(false).ToObservable();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void GaToPlayniteAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ControlDataContext.DataMismatch?.Count() == 0)
                {
                    return;
                }

                MessageBoxResult response = API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGaPlayniteToGaWarning"), PluginDatabase.PluginName, MessageBoxButton.OKCancel);
                if (response != MessageBoxResult.OK)
                {
                    return;
                }

                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
                {
                    Cancelable = true,
                    IsIndeterminate = false
                };

                _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
                {
                    API.Instance.Database.BeginBufferUpdate();

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    a.ProgressMaxValue = ControlDataContext.DataMismatch.Count();

                    ControlDataContext.DataMismatch.ForEach(x =>
                    {
                        if (a.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }

                        Guid id = x.Id;
                        GameActivities gameActivities = PluginDatabase.Get(id);
                        Game game = API.Instance.Database.Games.Get(id);

                        game.PlayCount = gameActivities.Count;
                        game.Playtime = gameActivities.SessionPlaytime;
                        API.Instance.Database.Games.Update(game);
                        a.CurrentProgressValue++;
                    });

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Logger.Info($"Task GaToPlayniteAll(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ControlDataContext.DataMismatch.Count()} items");

                    API.Instance.Database.EndBufferUpdate();
                }, globalProgressOptions);

                ControlDataContext.DataMismatch = PluginDatabase.GetGamesDataMismatch(false).ToObservable();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }

    public class GameDataMismatchDataContext: ObservableObject
    {
        private ObservableCollection<GameActivities> _dataMismatch;
        public ObservableCollection<GameActivities> DataMismatch { get => _dataMismatch; set => SetValue(ref _dataMismatch, value); }
    }
}
