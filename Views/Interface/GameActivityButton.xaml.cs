using GameActivity.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButton.xaml
    /// </summary>
    public partial class GameActivityButton : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        bool? _JustIcon = null;


        public GameActivityButton(bool? JustIcon = null)
        {
            _JustIcon = JustIcon;

            InitializeComponent();


            bool EnableIntegrationButtonJustIcon;
            if (_JustIcon == null)
            {
                EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon;
            }
            else
            {
                EnableIntegrationButtonJustIcon = (bool)_JustIcon;
            }

            this.DataContext = new
            {
                EnableIntegrationButtonJustIcon = EnableIntegrationButtonJustIcon
            };


            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"GameActivityButton.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        bool EnableIntegrationButtonJustIcon;
                        if (_JustIcon == null)
                        {
                            EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon;
                        }
                        else
                        {
                            EnableIntegrationButtonJustIcon = (bool)_JustIcon;
                        }

                        this.DataContext = new
                        {
                            EnableIntegrationButtonJustIcon = EnableIntegrationButtonJustIcon
                        };
#if DEBUG
                        logger.Debug($"GameActivity - DataContext: {JsonConvert.SerializeObject(DataContext)}");
#endif
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
