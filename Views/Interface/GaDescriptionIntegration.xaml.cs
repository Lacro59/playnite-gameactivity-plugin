using GameActivity.Models;
using GameActivity.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GaDescriptionIntegration.xaml
    /// </summary>
    public partial class GaDescriptionIntegration : StackPanel
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private GameActivityGameGraphicTime gameActivityGameGraphicTime;
        private GameActivityGameGraphicLog gameActivityGameGraphicLog;

        //private bool _IsCustom;
        //private bool _OnlyGraphic;


        //public GaDescriptionIntegration(bool IsCustom = false, bool OnlyGraphic = true)
        public GaDescriptionIntegration()
        {
            //_IsCustom = IsCustom;
            //_OnlyGraphic = OnlyGraphic;

            InitializeComponent();

            gameActivityGameGraphicTime = new GameActivityGameGraphicTime(0, PluginDatabase.PluginSettings.IntegrationGraphicOptionsCountAbscissa);
            gameActivityGameGraphicTime.DisableAnimations(true);

            gameActivityGameGraphicLog = new GameActivityGameGraphicLog(null, string.Empty, 0, !PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme, PluginDatabase.PluginSettings.IntegrationGraphicLogOptionsCountAbscissa);
            gameActivityGameGraphicLog.DisableAnimations(true);


            PART_GameActivity_Graphic.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicHeight;
            PART_GameActivity_Graphic.Children.Add(gameActivityGameGraphicTime);

            PART_GameActivity_GraphicLog.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicLogHeight;
            PART_GameActivity_GraphicLog.Children.Add(gameActivityGameGraphicLog);


            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"GaDescriptionIntegration.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        // ToggleButton
                        if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle && PluginDatabase.PluginSettings.EnableIntegrationButton)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // No data
                            if (!PluginDatabase.GameSelectedData.HasData)
                            {
                                this.Visibility = Visibility.Collapsed;
                                return;
                            }
                            else
                            {
                                this.Visibility = Visibility.Visible;
                            }
                        }

                        // Margin with title
                        if (PluginDatabase.PluginSettings.IntegrationShowTitle && !PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
                        {
                            PART_GameActivity_Graphic.Margin = new Thickness(0, 5, 0, 5);
                            PART_GameActivity_GraphicLog.Margin = new Thickness(0, 5, 0, 0);
                        }
                        // Without title
                        else
                        {
                            PART_GameActivity_Graphic.Margin = new Thickness(0, 0, 0, 0);
                            PART_GameActivity_GraphicLog.Margin = new Thickness(0, 0, 0, 0);
                        
                            if (!PluginDatabase.PluginSettings.IntegrationTopGameDetails)
                            {
                                if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
                                {
                                    PART_GameActivity_Graphic.Margin = new Thickness(0, 15, 0, 0);
                                }
                                else if(PluginDatabase.PluginSettings.IntegrationShowGraphicLog)
                                {
                                    PART_GameActivity_GraphicLog.Margin = new Thickness(0, 15, 0, 0);
                                }
                            }
                        }


                        bool IntegrationShowTitle = PluginDatabase.PluginSettings.IntegrationShowTitle;
                        if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle)
                        {
                            IntegrationShowTitle = true;
                        }


                        this.DataContext = new
                        {
                            IntegrationShowTitle = IntegrationShowTitle,
                            IntegrationShowGraphic = PluginDatabase.PluginSettings.IntegrationShowGraphic,
                            IntegrationShowGraphicLog = PluginDatabase.PluginSettings.IntegrationShowGraphicLog,
                            HasData = PluginDatabase.GameSelectedData.HasData,
                            HasDataDetails = PluginDatabase.GameSelectedData.HasDataDetails()
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
