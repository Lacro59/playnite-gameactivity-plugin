using GameActivity.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using System;
using System.Globalization;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using CommonPluginsPlaynite.Converters;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityToggleButtonDetails.xaml
    /// </summary>
    public partial class GameActivityToggleButtonDetails : ToggleButton
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        public GameActivityToggleButtonDetails()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        long ElapsedSeconds = PluginDatabase.GameSelectedData.GetLastSessionActivity().ElapsedSeconds;

                        LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                        string PlaytimeString = (string)converter.Convert(ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
                        PART_GaButtonPlaytime.Content = PlaytimeString;
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        long ElapsedSeconds = 0;

                        LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                        string PlaytimeString = (string)converter.Convert(ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
                        PART_GaButtonPlaytime.Content = PlaytimeString;
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
    }
}
