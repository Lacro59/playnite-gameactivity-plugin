using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Globalization;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityToggleButtonDetails.xaml
    /// </summary>
    public partial class GameActivityToggleButtonDetails : ToggleButton
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        public GameActivityToggleButtonDetails(long Playtime)
        {
            InitializeComponent();

            GameActivity.PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    long ElapsedSeconds = 0;
#if DEBUG
                    logger.Debug($"OnPropertyChanged: {JsonConvert.SerializeObject(GameActivity.PluginDatabase.GameSelectedData)}");
#endif

                    if (GameActivity.PluginDatabase.GameIsLoaded)
                    {
                        if (GameActivity.PluginDatabase.GameSelectedData.Items.Count == 0)
                        {

                        }
                        else
                        {
                            ElapsedSeconds = GameActivity.PluginDatabase.GameSelectedData.GetLastSessionActivity().ElapsedSeconds;
                        }
                    }
                    else
                    {

                    }

                    LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                    string PlaytimeString = (string)converter.Convert(ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
                    PART_GaButtonPlaytime.Content = PlaytimeString;
                }));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
    }
}
