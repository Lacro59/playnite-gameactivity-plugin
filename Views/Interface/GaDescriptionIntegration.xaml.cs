using GameActivity.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GaDescriptionIntegration.xaml
    /// </summary>
    public partial class GaDescriptionIntegration : StackPanel
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GameActivityGameGraphicTime gameActivityGameGraphicTime;
        private GameActivityGameGraphicLog gameActivityGameGraphicLog;

        private bool _IsCustom;
        private bool _OnlyGraphic;


        public GaDescriptionIntegration(GameActivitySettings settings, bool IsCustom = false, bool OnlyGraphic = true)
        {
            InitializeComponent();

            _IsCustom = IsCustom;
            _OnlyGraphic = OnlyGraphic;

#if DEBUG
            logger.Debug($"GameActivity - GaDescriptionIntegration() - _IsCustom: {_IsCustom} - _OnlyGraphic: {_OnlyGraphic}");
#endif

            if (!settings.IntegrationShowTitle || IsCustom)
            {
                PART_Title.Visibility = Visibility.Collapsed;
                PART_Separator.Visibility = Visibility.Collapsed;
            }

            bool Show = true;

            PART_GameActivity_Graphic.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowGraphic)
            {
                if (_IsCustom && !_OnlyGraphic)
                {
                    Show = false;
                }

                if (Show)
                {
                    gameActivityGameGraphicTime = new GameActivityGameGraphicTime(settings, 0, settings.IntegrationGraphicOptionsCountAbscissa);
                    PART_GameActivity_Graphic.Visibility = Visibility.Visible;
                    PART_GameActivity_Graphic.Height = settings.IntegrationShowGraphicHeight;
                    PART_GameActivity_Graphic.Children.Add(gameActivityGameGraphicTime);

                }
            }

            Show = true;

            PART_GameActivity_GraphicLog.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowGraphicLog)
            {
                if (_IsCustom && _OnlyGraphic)
                {
                    Show = false;
                }

                if (Show)
                {
                    gameActivityGameGraphicLog = new GameActivityGameGraphicLog(settings, null, string.Empty, 0, !settings.EnableIntegrationInCustomTheme, settings.IntegrationGraphicLogOptionsCountAbscissa);
                    PART_GameActivity_GraphicLog.Visibility = Visibility.Visible;
                    PART_GameActivity_GraphicLog.Height = settings.IntegrationShowGraphicLogHeight;
                    PART_GameActivity_GraphicLog.Children.Add(gameActivityGameGraphicLog);
                }
            }
        }
    }
}
