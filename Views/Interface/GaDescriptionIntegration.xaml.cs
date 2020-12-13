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


        public GaDescriptionIntegration()
        {
            InitializeComponent();

            if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
            {
                GameActivityGameGraphicTime gameActivityGameGraphicTime = new GameActivityGameGraphicTime(0, PluginDatabase.PluginSettings.IntegrationGraphicOptionsCountAbscissa);
                gameActivityGameGraphicTime.DisableAnimations(true);
                PART_GameActivity_Graphic.Children.Add(gameActivityGameGraphicTime);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowGraphicLog)
            {
                GameActivityGameGraphicLog gameActivityGameGraphicLog = new GameActivityGameGraphicLog(null, string.Empty, 0, !PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme, PluginDatabase.PluginSettings.IntegrationGraphicLogOptionsCountAbscissa);
                gameActivityGameGraphicLog.DisableAnimations(true);
                PART_GameActivity_GraphicLog.Children.Add(gameActivityGameGraphicLog);
            }


            PART_GameActivity_Graphic.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicHeight;
            PART_GameActivity_GraphicLog.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicLogHeight;


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
                        if (PluginDatabase.PluginSettings.IntegrationShowTitle)
                        {
                            PART_GameActivity_Graphic.Margin = new Thickness(0, 5, 0, 5);
                            PART_GameActivity_GraphicLog.Margin = new Thickness(0, 5, 0, 5);
                        }
                        // Without title
                        else
                        {
                            if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
                            {
                                PART_GameActivity_Graphic.Margin = new Thickness(0, 5, 0, 5);
                            }
                            else if (PluginDatabase.PluginSettings.IntegrationShowGraphicLog)
                            {
                                PART_GameActivity_GraphicLog.Margin = new Thickness(0, 5, 0, 5);
                            }
                        }


                        bool IntegrationShowTitle = PluginDatabase.PluginSettings.IntegrationShowTitle;
                        if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle)
                        {
                            IntegrationShowTitle = true;
                        }


                        PART_GameActivity_Graphic.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicHeight;
                        PART_GameActivity_GraphicLog.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicLogHeight;


                        this.DataContext = new
                        {
                            IntegrationShowTitle = IntegrationShowTitle,
                            IntegrationShowGraphic = PluginDatabase.PluginSettings.IntegrationShowGraphic,
                            IntegrationShowGraphicLog = PluginDatabase.PluginSettings.IntegrationShowGraphicLog,
                            HasData = PluginDatabase.GameSelectedData.HasData,
                            HasDataDetails = PluginDatabase.GameSelectedData.HasDataDetails()
                        };
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
