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


        public GaDescriptionIntegration(GameActivitySettings settings, GameActivityClass gameActivity, bool IsCustom = false, bool OnlyGraphic = true)
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

#if DEBUG
                logger.Debug($"GameActivity - PART_GameActivity_Graphic - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                if (Show && gameActivity != null)
                {
                    gameActivityGameGraphicTime = new GameActivityGameGraphicTime(settings, gameActivity, 0, settings.IntegrationGraphicOptionsCountAbscissa);
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

#if DEBUG
                logger.Debug($"GameActivity - PART_GameActivity_GraphicLog - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                if (Show && gameActivity != null)
                {
                    gameActivityGameGraphicLog = new GameActivityGameGraphicLog(settings, gameActivity, string.Empty, string.Empty, 0, !settings.EnableIntegrationInCustomTheme, settings.IntegrationGraphicLogOptionsCountAbscissa);
                    PART_GameActivity_GraphicLog.Visibility = Visibility.Visible;
                    PART_GameActivity_GraphicLog.Height = settings.IntegrationShowGraphicLogHeight;
                    PART_GameActivity_GraphicLog.Children.Add(gameActivityGameGraphicLog);

                }
            }
        }


        public void SetGaData(GameActivitySettings settings, GameActivityClass gameActivity)
        {
#if DEBUG
            logger.Debug($"GameActivity - SetGaData() - _IsCustom: {_IsCustom} - _OnlyGraphic: {_OnlyGraphic}");
#endif
            bool Show = true;

            if (settings.IntegrationShowGraphic)
            {
                if (_IsCustom && !_OnlyGraphic)
                {
                    Show = false;
                }

                if (Show && gameActivity != null)
                {
                    if (gameActivityGameGraphicTime == null)
                    {
                        gameActivityGameGraphicTime = new GameActivityGameGraphicTime(settings, gameActivity, 0, settings.IntegrationGraphicOptionsCountAbscissa);
                        PART_GameActivity_Graphic.Visibility = Visibility.Visible;
                        PART_GameActivity_Graphic.Height = settings.IntegrationShowGraphicHeight;
                        PART_GameActivity_Graphic.Children.Add(gameActivityGameGraphicTime);
                    }
                    gameActivityGameGraphicTime.SetGaData(gameActivity);
#if DEBUG
                    logger.Debug($"GameActivity - gameActivityGameGraphicTime - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                }
                else
                {
#if DEBUG
                    logger.Debug($"GameActivity - gameActivityGameGraphicTime - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                }
            }

            Show = true;

            if (settings.IntegrationShowGraphicLog)
            {
                if (_IsCustom && _OnlyGraphic)
                {
                    Show = false;
                }

                if (Show && gameActivity != null)
                {
                    if (gameActivityGameGraphicLog == null)
                    {
                        gameActivityGameGraphicLog = new GameActivityGameGraphicLog(settings, gameActivity, string.Empty, string.Empty, 0, !settings.EnableIntegrationInCustomTheme, settings.IntegrationGraphicLogOptionsCountAbscissa);
                        PART_GameActivity_GraphicLog.Visibility = Visibility.Visible;
                        PART_GameActivity_GraphicLog.Height = settings.IntegrationShowGraphicLogHeight;
                        PART_GameActivity_GraphicLog.Children.Add(gameActivityGameGraphicLog);
                    }
                    gameActivityGameGraphicLog.SetGaData(gameActivity);
#if DEBUG
                    logger.Debug($"GameActivity - gameActivityGameGraphicLog - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                }
                else
                {
#if DEBUG
                    logger.Debug($"GameActivity - gameActivityGameGraphicLog - Show: {Show} - gameActivity: {JsonConvert.SerializeObject(gameActivity)}");
#endif
                }
            }

            PART_GameActivity_Graphic.UpdateLayout();
            PART_GameActivity_GraphicLog.UpdateLayout();
        }
    }
}
