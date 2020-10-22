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

namespace GameActivity.Services
{
    public class GameActivityUI : PlayniteUiHelper
    {
        private readonly GameActivitySettings _Settings;


        public GameActivityUI(IPlayniteAPI PlayniteApi, GameActivitySettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _Settings = Settings;
            BtActionBarName = "PART_GaButton";
            SpDescriptionName = "PART_GaDescriptionIntegration";
        }


        #region BtHeader
        public void AddBtHeader()
        {
            if (_Settings.EnableIntegrationButtonHeader)
            {
                logger.Info("GameActivity - Add Header button");
                Button btHeader = new GameActivityButtonHeader(TransformIcon.Get("GameActivity"));
                btHeader.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                btHeader.Click += OnBtHeaderClick;
                ui.AddButtonInWindowsHeader(btHeader);
            }
        }

        public void OnBtHeaderClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"GameActivity - OnBtActionBarClick()");
#endif

            GameActivity.DatabaseReference = _PlayniteApi.Database;
            var ViewExtension = new GameActivityView(_Settings, _PlayniteApi, _PluginUserDataPath);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }
        #endregion


        public override void Initial()
        {
            if (_Settings.EnableIntegrationButton)
            {
#if DEBUG
                logger.Debug($"GameActivity - InitialBtActionBar()");
#endif
                InitialBtActionBar();
            }

            if (_Settings.EnableIntegrationInDescription)
            {
#if DEBUG
                logger.Debug($"GameActivity - InitialSpDescription()");
#endif
                InitialSpDescription();
            }

            if (_Settings.EnableIntegrationInCustomTheme)
            {
#if DEBUG
                logger.Debug($"GameActivity - InitialCustomElements()");
#endif
                InitialCustomElements();
            }
        }

        public override void AddElements()
        {
            if (IsFirstLoad)
            {
#if DEBUG
                logger.Debug($"GameActivity - IsFirstLoad");
#endif
                Thread.Sleep(1000);
                IsFirstLoad = false;
            }

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                CheckTypeView();

                if (_Settings.EnableIntegrationButton)
                {
#if DEBUG
                    logger.Debug($"GameActivity - AddBtActionBar()");
#endif
                    AddBtActionBar();
                }

                if (_Settings.EnableIntegrationInDescription)
                {
#if DEBUG
                    logger.Debug($"GameActivity - AddSpDescription()");
#endif
                    AddSpDescription();
                }

                if (_Settings.EnableIntegrationInCustomTheme)
                {
#if DEBUG
                    logger.Debug($"GameActivity - AddCustomElements()");
#endif
                    AddCustomElements();
                }
            }));
        }

        public override void RefreshElements(Game GameSelected, bool force = false)
        {
#if DEBUG
            logger.Debug($"GameActivity - RefreshElements({GameSelected.Name})");
#endif
            taskHelper.Check();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefresh = Task.Run(() => {
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

                    resourcesLists.Add(new ResourcesList { Key = "Ga_IntegrationShowGraphic", Value = _Settings.IntegrationShowGraphic });
                    resourcesLists.Add(new ResourcesList { Key = "true", Value = _Settings.IntegrationShowGraphicLog });
                    ui.AddResources(resourcesLists);


                    // Load data
                    GameActivity.SelectedGameGameActivity = null;
                    try
                    {
                        GameActivity.DatabaseReference = _PlayniteApi.Database;
                        GameActivity.SelectedGameGameActivity = GameActivity.GameActivityDatabases.Get(GameSelected.Id);
#if DEBUG
                        logger.Debug($"GameActivity - GameActivity.SelectedGameGameActivity: ({JsonConvert.SerializeObject(GameActivity.SelectedGameGameActivity)})");
#endif
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "GameActivity", "Error to load data");
                        _PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "GameActivity");
                    }

                    if (GameActivity.SelectedGameGameActivity != null)
                    {
                        resourcesLists.Add(new ResourcesList { Key = "Ga_HasData", Value = true });

                        try
                        {
                            var data = GameActivity.SelectedGameGameActivity.GetSessionActivityDetails();
                            resourcesLists.Add(new ResourcesList { Key = "Ga_HasDataLog", Value = (data.Count > 0) });
                        }
                        catch
                        {
                        }

                        try
                        {
                            resourcesLists.Add(new ResourcesList { Key = "Ga_LastDateSession", Value = Convert.ToDateTime(GameActivity.SelectedGameGameActivity.GetLastSession()).ToString(Constants.DateUiFormat) });
                            resourcesLists.Add(new ResourcesList
                            {
                                Key = "Ga_LastDateTimeSession",
                                Value = Convert.ToDateTime(GameActivity.SelectedGameGameActivity.GetLastSession()).ToString(Constants.DateUiFormat)
                                    + " " + Convert.ToDateTime(GameActivity.SelectedGameGameActivity.GetLastSession()).ToString(Constants.TimeUiFormat)
                            });
                        }
                        catch
                        {
                        }

                        try
                        {
                            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                            string playtime = (string)converter.Convert((long)GameActivity.SelectedGameGameActivity.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
                            resourcesLists.Add(new ResourcesList { Key = "Ga_LastPlaytimeSession", Value = playtime });
                        }
                        catch
                        {
                        }

                        ui.AddResources(resourcesLists);
                    }
                    else
                    {
                        logger.Warn("GameActivity - No data for " + GameSelected.Name);
                    }

                    // If not cancel, show
                    if (!ct.IsCancellationRequested)
                    {
                        ui.AddResources(resourcesLists);

                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (_Settings.EnableIntegrationButton)
                            {
#if DEBUG
                                logger.Debug($"GameActivity - RefreshBtActionBar()");
#endif
                                RefreshBtActionBar();
                            }

                            if (_Settings.EnableIntegrationInDescription)
                            {
#if DEBUG
                                logger.Debug($"GameActivity - RefreshSpDescription()");
#endif
                                RefreshSpDescription();
                            }

                            if (_Settings.EnableIntegrationInCustomTheme)
                            {
#if DEBUG
                                logger.Debug($"GameActivity - RefreshCustomElements()");
#endif
                                RefreshCustomElements();
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", $"Error on TaskRefreshBtActionBar()");
                }
            });

            taskHelper.Add(TaskRefresh, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (PART_BtActionBar != null)
                {
                    PART_BtActionBar.Visibility = Visibility.Collapsed;
                }
            }));
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

            if (_Settings.EnableIntegrationInDescriptionWithToggle)
            {
                if (_Settings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new GameActivityToggleButtonDetails(GameActivity.GameSelected.Playtime);
                }
                else
                {
                    BtActionBar = new GameActivityToggleButton(_Settings);
                }

                ((ToggleButton)BtActionBar).Click += OnBtActionBarToggleButtonClick;
            }
            else
            {
                if (_Settings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new GameActivityButtonDetails(GameActivity.GameSelected.Playtime);
                }
                else
                {
                    BtActionBar = new GameActivityButton(_Settings);
                }

                ((Button)BtActionBar).Click += OnBtActionBarClick;
            }
            
            if (!_Settings.EnableIntegrationInDescriptionOnlyIcon)
            {
                BtActionBar.Width = 150;
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
                Common.LogError(ex, "GameActivity", "Error on AddBtActionBar()");
            }
        }

        public override void RefreshBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
                PART_BtActionBar.Visibility = Visibility.Visible;

                long ElapsedSeconds = 0;
                if (GameActivity.SelectedGameGameActivity != null)
                {
                    ElapsedSeconds = GameActivity.SelectedGameGameActivity.GetLastSessionActivity().ElapsedSeconds;
                }

                if (PART_BtActionBar is GameActivityButtonDetails)
                {
                    ((GameActivityButtonDetails)PART_BtActionBar).SetGaData(ElapsedSeconds);
                }
                if (PART_BtActionBar is GameActivityToggleButtonDetails)
                {
                    ((GameActivityToggleButtonDetails)PART_BtActionBar).SetGaData(ElapsedSeconds);
                }
            }
            else
            {
                logger.Warn($"GameActivity - PART_BtActionBar is not defined");
            }
        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"GameActivity - OnBtActionBarClick()");
#endif

            GameActivity.DatabaseReference = _PlayniteApi.Database;
            var ViewExtension = new GameActivityView(_Settings, _PlayniteApi, _PluginUserDataPath, GameActivity.GameSelected);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCGameActivity"), ViewExtension);
            windowExtension.ShowDialog();
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_Settings.EnableIntegrationInCustomTheme)
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
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (PART_SpDescription != null)
                {
                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            }));
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
                GaDescriptionIntegration SpDescription = new GaDescriptionIntegration(_Settings, GameActivity.SelectedGameGameActivity);
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, _Settings.IntegrationTopGameDetails);
                PART_SpDescription = IntegrationUI.SearchElementByName(SpDescriptionName);

                if(_Settings.EnableIntegrationInDescriptionWithToggle)
                {
                    ((ToggleButton)PART_BtActionBar).IsChecked = false;
                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", "Error on AddSpDescription()");
            }
        }

        public override void RefreshSpDescription()
        {
            if (PART_SpDescription != null)
            {
                if (GameActivity.SelectedGameGameActivity != null)
                {
                    PART_SpDescription.Visibility = Visibility.Visible;

                    if (PART_SpDescription is GaDescriptionIntegration)
                    {
                        ((GaDescriptionIntegration)PART_SpDescription).SetGaData(_Settings, GameActivity.SelectedGameGameActivity);

                        if (_Settings.EnableIntegrationInDescriptionWithToggle)
                        {
                            ((ToggleButton)PART_BtActionBar).IsChecked = false;
                            PART_SpDescription.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"GameActivity - No data for {GameActivity.GameSelected.Name}");
#endif
                }
            }
            else
            {
                logger.Warn($"GameActivity - PART_SpDescription is not defined");
            }
        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (CustomElement customElement in ListCustomElements)
                {
                    customElement.Element.Visibility = Visibility.Collapsed;
                }
            }));
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

            FrameworkElement PART_GaCustomButton = null;
            FrameworkElement PART_GameActivity_Graphic = null;
            FrameworkElement PART_GameActivity_GraphicLog = null;
            try
            {
                PART_GaCustomButton = IntegrationUI.SearchElementByName("PART_GaCustomButton", false, true);
                PART_GameActivity_Graphic = IntegrationUI.SearchElementByName("PART_GameActivity_Graphic", false, true);
                PART_GameActivity_GraphicLog = IntegrationUI.SearchElementByName("PART_GameActivity_GraphicLog", false, true);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity", $"Error on find custom element");
            }

            if (PART_GaCustomButton != null)
            {
                PART_GaCustomButton = new GameActivityButton(_Settings);
                PART_GaCustomButton.Name = "GaCustomButton";
                ((Button)PART_GaCustomButton).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_GaCustomButton, "PART_GaCustomButton");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GaCustomButton", Element = PART_GaCustomButton });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GaCustomButton not find");
#endif
            }

            if (PART_GameActivity_Graphic != null && _Settings.IntegrationShowGraphic)
            {
                PART_GameActivity_Graphic = new GaDescriptionIntegration(_Settings, GameActivity.SelectedGameGameActivity, true);
                PART_GameActivity_Graphic.Name = "GameActivity_Graphic";
                try
                {
                    ui.AddElementInCustomTheme(PART_GameActivity_Graphic, "PART_GameActivity_Graphic");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GameActivity_Graphic", Element = PART_GameActivity_Graphic });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"GameActivity - PART_GameActivity_Graphic not find");
#endif
            }

            if (PART_GameActivity_GraphicLog != null && _Settings.IntegrationShowGraphicLog)
            {
                PART_GameActivity_GraphicLog = new GaDescriptionIntegration(_Settings, GameActivity.SelectedGameGameActivity, true, false);
                PART_GameActivity_GraphicLog.Name = "GameActivity_GraphicLog";
                try
                {
                    ui.AddElementInCustomTheme(PART_GameActivity_GraphicLog, "PART_GameActivity_GraphicLog");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_GameActivity_GraphicLog", Element = PART_GameActivity_GraphicLog });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", "Error on AddCustomElements()");
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
#if DEBUG
            logger.Debug($"GameActivity - ListCustomElements - {ListCustomElements.Count}");
#endif
            foreach (CustomElement customElement in ListCustomElements)
            {
                try
                {
                    bool isFind = false;

                    if (customElement.Element is GameActivityButton)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - customElement.Element is GameActivityButton");
#endif
                        customElement.Element.Visibility = Visibility.Visible;
                        isFind = true;
                    }

                    if (customElement.Element is GameActivityButtonDetails)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - customElement.Element is GameActivityButtonDetails");
#endif
                        customElement.Element.Visibility = Visibility.Visible;
                        ((GameActivityButtonDetails)customElement.Element).SetGaData(GameActivity.GameSelected.Playtime);
                        isFind = true;
                    }

                    if (customElement.Element is GaDescriptionIntegration)
                    {
#if DEBUG
                        logger.Debug($"GameActivity - customElement.Element is GaDescriptionIntegration");
#endif
                        isFind = true;
                        if (GameActivity.SelectedGameGameActivity != null)
                        {
                            customElement.Element.Visibility = Visibility.Visible;
                            ((GaDescriptionIntegration)customElement.Element).SetGaData(_Settings, GameActivity.SelectedGameGameActivity);
                        }
                        else
                        {
#if DEBUG
                            logger.Debug($"GameActivity - customElement.Element is GaDescriptionIntegration with no data");
#endif
                        }
                    }

                    if (!isFind)
                    {
                        logger.Warn($"GameActivity - RefreshCustomElements({customElement.ParentElementName})");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "GameActivity", $"Error on RefreshCustomElements()");
                }
            }
        }
        #endregion
    }
}
