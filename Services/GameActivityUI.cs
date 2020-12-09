using GameActivity.Models;
using GameActivity.Views.Interface;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameActivity.Services
{
    public class GameActivityUI : PlayniteUiHelper
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public override string _PluginUserDataPath { get; set; } = string.Empty;

        public override bool IsFirstLoad { get; set; } = true;

        public override string BtActionBarName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBar { get; set; }

        public override string SpDescriptionName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpDescription { get; set; }


        public override string SpInfoBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpInfoBarFS { get; set; }

        public override string BtActionBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBarFS { get; set; }


        public override List<CustomElement> ListCustomElements { get; set; } = new List<CustomElement>();


        public GameActivityUI(IPlayniteAPI PlayniteApi, GameActivitySettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _PluginUserDataPath = PluginUserDataPath;

            BtActionBarName = "PART_GaButton";
            SpDescriptionName = "PART_GaDescriptionIntegration";

            SpInfoBarFSName = "PART_GaSpInfoBar";
        }


        #region BtHeader
        public void AddBtHeader()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (PluginDatabase.PluginSettings.EnableIntegrationButtonHeader)
                {
                    logger.Info("GameActivity - Add Header button");
                    Button btHeader = new GameActivityButtonHeader(TransformIcon.Get("GameActivity"));
                    btHeader.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                    btHeader.Click += OnBtHeaderClick;
                    ui.AddButtonInWindowsHeader(btHeader);
                }
            }
        }

        public void OnBtHeaderClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"GameActivity - OnBtHeaderClick()");
#endif

            GameActivity.DatabaseReference = _PlayniteApi.Database;
            var ViewExtension = new GameActivityView(PluginDatabase.PluginSettings, _PlayniteApi, _PluginUserDataPath);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }
        #endregion


        public override void Initial()
        {
            
        }

        public override DispatcherOperation AddElements()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"GameActivity - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_HtmlDescription") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    CheckTypeView();

                    if (PluginDatabase.PluginSettings.EnableIntegrationButton)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - AddBtActionBar()");
#endif
                        AddBtActionBar();
                    }

                    if (PluginDatabase.PluginSettings.EnableIntegrationInDescription)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - AddSpDescription()");
#endif
                        AddSpDescription();
                    }

                    if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - AddCustomElements()");
#endif
                        AddCustomElements();
                    }
                }));
            }

            return null;
        }

        public override void RefreshElements(Game GameSelected, bool force = false)
        {
#if DEBUG
            logger.Debug($"GameActivity - RefreshElements({GameSelected.Name})");
#endif
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefresh = Task.Run(() =>
            {
                try
                {
                    Initial();

                    // Reset resources
                    List<ResourcesList> resourcesLists = new List<ResourcesList>();
                    resourcesLists.Add(new ResourcesList { Key = "Ga_HasData", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Ga_HasDataLog", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Ga_LastDateSession", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Ga_LastDateTimeSession", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Ga_LastPlaytimeSession", Value = string.Empty });

                    resourcesLists.Add(new ResourcesList { Key = "Ga_IntegrationShowGraphic", Value = PluginDatabase.PluginSettings.IntegrationShowGraphic });
                    resourcesLists.Add(new ResourcesList { Key = "Ga_IntegrationShowGraphicLog", Value = PluginDatabase.PluginSettings.IntegrationShowGraphicLog });
                    ui.AddResources(resourcesLists);


                    // Load data
                    if (!GameActivity.PluginDatabase.IsLoaded)
                    {
                        return;
                    }
                    GameActivities gameActivities = GameActivity.PluginDatabase.Get(GameSelected);

                    if (gameActivities.HasData)
                    {
                        resourcesLists = new List<ResourcesList>();
                        resourcesLists.Add(new ResourcesList { Key = "Ga_IntegrationShowGraphic", Value = PluginDatabase.PluginSettings.IntegrationShowGraphic });
                        resourcesLists.Add(new ResourcesList { Key = "Ga_IntegrationShowGraphicLog", Value = PluginDatabase.PluginSettings.IntegrationShowGraphicLog });

                        resourcesLists.Add(new ResourcesList { Key = "Ga_HasData", Value = gameActivities.HasData });

                        try
                        {
                            var data = gameActivities.GetSessionActivityDetails();
                            resourcesLists.Add(new ResourcesList { Key = "Ga_HasDataLog", Value = gameActivities.HasDataDetails() });
                        }
                        catch
                        {
                        }

                        try
                        {
                            resourcesLists.Add(new ResourcesList { Key = "Ga_LastDateSession", Value = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat) });
                            resourcesLists.Add(new ResourcesList
                            {
                                Key = "Ga_LastDateTimeSession",
                                Value = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat)
                                    + " " + gameActivities.GetLastSession().ToLocalTime().ToString(Constants.TimeUiFormat)
                            });
                        }
                        catch
                        {
                        }

                        try
                        {
                            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                            string playtime = (string)converter.Convert((long)gameActivities.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
                            resourcesLists.Add(new ResourcesList { Key = "Ga_LastPlaytimeSession", Value = playtime });
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        logger.Warn($"GameActivity - No data for {GameSelected.Name}");
                    }

                    // If not cancel, show
                    if (!ct.IsCancellationRequested && GameSelected.Id == GameActivity.GameSelected.Id)
                    {
                        ui.AddResources(resourcesLists);

                        if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                        {
                            PluginDatabase.SetCurrent(gameActivities);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }, ct);

            taskHelper.Add(TaskRefresh, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {

        }

        public override void AddBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_BtActionBar allready insert");
#endif
                return;
            }

            FrameworkElement BtActionBar;

            if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle)
            {
                if (PluginDatabase.PluginSettings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new GameActivityToggleButtonDetails();
                }
                else
                {
                    BtActionBar = new GameActivityToggleButton();
                }

                ((ToggleButton)BtActionBar).Click += OnBtActionBarToggleButtonClick;
            }
            else
            {
                if (PluginDatabase.PluginSettings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new GameActivityButtonDetails();
                }
                else
                {
                    BtActionBar = new GameActivityButton();
                }

                ((Button)BtActionBar).Click += OnBtActionBarClick;
            }

            if (!PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon)
            {
                BtActionBar.MinWidth = 150;
            }

            BtActionBar.Name = BtActionBarName;
            BtActionBar.Margin = new Thickness(10, 0, 0, 0);

            try
            {
                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBar);
                PART_BtActionBar = IntegrationUI.SearchElementByName(BtActionBarName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }

        public override void RefreshBtActionBar()
        {

        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"GameActivity - OnBtActionBarClick()");
#endif

            GameActivity.DatabaseReference = _PlayniteApi.Database;
            var ViewExtension = new GameActivityView(PluginDatabase.PluginSettings, _PlayniteApi, _PluginUserDataPath, GameActivity.GameSelected);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
            {
                string ButtonName = string.Empty;
                try
                {
                    ButtonName = ((Button)sender).Name;
                    if (ButtonName == "PART_GaCustomButton")
                    {
#if DEBUG
                        logger.Debug($"GameActivity - OnCustomThemeButtonClick()");
#endif
                        OnBtActionBarClick(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "OnCustomThemeButtonClick() error");
                }
            }
        }
        #endregion


        #region SpDescription
        public override void InitialSpDescription()
        {

        }

        public override void AddSpDescription()
        {
            if (PART_SpDescription != null)
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_SpDescription allready insert");
#endif
                return;
            }

            try
            {
                GaDescriptionIntegration SpDescription = new GaDescriptionIntegration();
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, PluginDatabase.PluginSettings.IntegrationTopGameDetails);
                PART_SpDescription = IntegrationUI.SearchElementByName(SpDescriptionName);

                if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle && PART_SpDescription != null)
                {
                    if (PART_BtActionBar != null && PART_BtActionBar is ToggleButton)
                    {
                        ((ToggleButton)PART_BtActionBar).IsChecked = false;
                    }
                    else
                    {
                        logger.Warn($"GameActivity - PART_BtActionBar is null or not ToggleButton");
                    }

                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }

        public override void RefreshSpDescription()
        {

        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {

        }

        public override void AddCustomElements()
        {
            if (ListCustomElements.Count > 0)
            {
#if DEBUG
                logger.Debug($"GameActivity - CustomElements allready insert - {ListCustomElements.Count}");
#endif
                return;
            }

            FrameworkElement PART_GaButtonWithJustIcon = null;
            FrameworkElement PART_GaButtonWithTitle = null;
            FrameworkElement PART_GaButtonWithTitleAndDetails = null;

            FrameworkElement PART_GameActivity_Graphic = null;
            FrameworkElement PART_GameActivity_GraphicLog = null;
            try
            {
                PART_GaButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_GaButtonWithJustIcon", false, true);
                PART_GaButtonWithTitle = IntegrationUI.SearchElementByName("PART_GaButtonWithTitle", false, true);
                PART_GaButtonWithTitleAndDetails = IntegrationUI.SearchElementByName("PART_GaButtonWithTitleAndDetails", false, true);

                PART_GameActivity_Graphic = IntegrationUI.SearchElementByName("PART_GameActivity_Graphic", false, true);
                PART_GameActivity_GraphicLog = IntegrationUI.SearchElementByName("PART_GameActivity_GraphicLog", false, true);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"Error on find custom element");
            }


            if (PART_GaButtonWithJustIcon != null)
            {
                PART_GaButtonWithJustIcon = new GameActivityButton(true);
                ((Button)PART_GaButtonWithJustIcon).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_GaButtonWithJustIcon, "PART_GaButtonWithJustIcon");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GaButtonWithJustIcon", Element = PART_GaButtonWithJustIcon });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GaButtonWithJustIcon not find");
#endif
            }

            if (PART_GaButtonWithTitle != null)
            {
                PART_GaButtonWithTitle = new GameActivityButton(false);
                ((Button)PART_GaButtonWithTitle).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_GaButtonWithTitle, "PART_GaButtonWithTitle");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GaButtonWithTitle", Element = PART_GaButtonWithTitle });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GaButtonWithTitle not find");
#endif
            }

            if (PART_GaButtonWithTitleAndDetails != null)
            {
                PART_GaButtonWithTitleAndDetails = new GameActivityButtonDetails();
                ((Button)PART_GaButtonWithTitleAndDetails).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_GaButtonWithTitleAndDetails, "PART_GaButtonWithTitleAndDetails");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GaButtonWithTitleAndDetails", Element = PART_GaButtonWithTitleAndDetails });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GaButtonWithTitleAndDetails not find");
#endif
            }


            if (PART_GameActivity_Graphic != null && PluginDatabase.PluginSettings.IntegrationShowGraphic)
            {
                PART_GameActivity_Graphic = new GameActivityGameGraphicTime(0, PluginDatabase.PluginSettings.IntegrationGraphicOptionsCountAbscissa);
                ((GameActivityGameGraphicTime)PART_GameActivity_Graphic).DisableAnimations(true);

                PART_GameActivity_Graphic.Name = "GameActivity_Graphic";
                try
                {
                    ui.AddElementInCustomTheme(PART_GameActivity_Graphic, "PART_GameActivity_Graphic");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GameActivity_Graphic", Element = PART_GameActivity_Graphic });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GameActivity_Graphic not find");
#endif
            }

            if (PART_GameActivity_GraphicLog != null && PluginDatabase.PluginSettings.IntegrationShowGraphicLog)
            {
                PART_GameActivity_GraphicLog = new GameActivityGameGraphicLog(null, string.Empty, 0, !PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme, PluginDatabase.PluginSettings.IntegrationGraphicLogOptionsCountAbscissa);
                ((GameActivityGameGraphicLog)PART_GameActivity_GraphicLog).DisableAnimations(true);

                PART_GameActivity_GraphicLog.Name = "GameActivity_GraphicLog";
                try
                {
                    ui.AddElementInCustomTheme(PART_GameActivity_GraphicLog, "PART_GameActivity_GraphicLog");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GameActivity_GraphicLog", Element = PART_GameActivity_GraphicLog });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GameActivity_GraphicLog not find");
#endif
            }
        }

        public override void RefreshCustomElements()
        {

        }
        #endregion




        public override DispatcherOperation AddElementsFS()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"GameActivity - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_ButtonContext") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    if (PluginDatabase.PluginSettings.EnableIntegrationFS)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - AddBtInfoBarFS()");
#endif
                        AddSpInfoBarFS();
                        AddSpInfoBarFS();
                    }
                }));
            }

            return null;
        }


        #region SpInfoBarFS
        public override void InitialSpInfoBarFS()
        {

        }

        public override void AddSpInfoBarFS()
        {

        }

        public override void RefreshSpInfoBarFS()
        {

        }
        #endregion


        #region BtActionBarFS
        public override void InitialBtActionBarFS()
        {

        }

        public override void AddBtActionBarFS()
        {

        }

        public override void RefreshBtActionBarFS()
        {

        }
        #endregion
    }
}
