using CommonPlayniteShared.Common;
using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.Controls;
using CommonPluginsShared.PlayniteExtended;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.Views;
using MoreLinq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using QuickSearch.SearchItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity
{
    public class GameActivity : PluginExtended<GameActivitySettingsViewModel, ActivityDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4");

        internal TopPanelItem TopPanelItem { get; set; }
        internal SidebarItem SidebarItem { get; set; }
        internal SidebarItemControl SidebarItemControl { get; set; }

		// Hardware monitoring system
		internal GameActivityMonitoring GameActivityMonitoring { get; set; }

		public GameActivity(IPlayniteAPI api) : base(api, "GameActivity")
        {
            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginButton", "PluginChartTime", "PluginChartLog" },
                SourceName = "GameActivity"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "GameActivity",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                TopPanelItem = new GameActivityTopPanelItem(this);
                SidebarItem = new GameActivityViewSidebar(this);
            }

			// Create the activity monitoring system
			GameActivityMonitoring = new GameActivityMonitoring(this);
		}


        #region Custom event

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomGameActivityButton")
                {
                    Common.LogDebug(true, $"OnCustomThemeButtonClick()");

                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true,
                        CanBeResizable = true,
                        Height = 740,
                        Width = 1280
                    };

                    GameActivityViewSingle ViewExtension = new GameActivityViewSingle(this, PluginDatabase.GameContext);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivity"), ViewExtension, windowOptions);
                    _ = windowExtension.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        #endregion

        #region Theme integration

        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield return TopPanelItem;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton(this);
            }

            if (args.Name == "PluginChartTime")
            {
                return new PluginChartTime { DisableAnimations = true, LabelsRotation = true, Truncate = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate };
            }

            if (args.Name == "PluginChartLog")
            {
                return new PluginChartLog { DisableAnimations = true, LabelsRotation = true };
            }

            return null;
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            List<SidebarItem> items = new List<SidebarItem> { SidebarItem };
            return items;
        }

        #endregion

        #region Menus

        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                // Show plugin view with all activities for all game in database with data of selected game
                new GameMenuItem
                {
                    //MenuSection = "",
                    Icon = Path.Combine(PluginFolder, "Resources", "chart-646.png"),
                    Description = ResourceProvider.GetString("LOCGameActivityViewGameActivity"),
                    Action = (gameMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            CanBeResizable = true,
                            Height = 740,
                            Width = 1280
                        };

                        GameActivityViewSingle ViewExtension = new GameActivityViewSingle(this, GameMenu);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGameActivity"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCGameActivity"),
                Description = "Test",
                Action = (mainMenuItem) =>
                {

                }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                // Show plugin view with all activities for all game in database
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCGameActivityViewGamesActivities"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            CanBeResizable = true,
                            Height = 740,
                            Width = 1280
                        };

                        GameActivityView ViewExtension = new GameActivityView(this);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGamesActivitiesTitle"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonExtractToCsv"),
                    Action = (mainMenuItem) =>
                    {
                        string path = API.Instance.Dialogs.SelectFolder();
                        if (Directory.Exists(path))
                        {
                            PluginDatabase.ExtractToCsv(path, true);
                        }
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonExtractAllToCsv"),
                    Action = (mainMenuItem) =>
                    {
                        string path = API.Instance.Dialogs.SelectFolder();
                        if (Directory.Exists(path))
                        {
                            PluginDatabase.ExtractToCsv(path, false);
                        }
                    }
                },

                // Database management
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCGaGamesDataMismatch"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            CanBeResizable = true,
                            WidthPercent = 70,
                            MaxWidth= 1500,
                            Height = 500
                        };

                        GamesDataMismatch ViewExtension = new GamesDataMismatch();
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCGaGamesDataMismatch"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonTransferPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        TransfertData ViewExtension = new TransfertData(PluginDatabase.GetDataGames().ToList(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCCommonSelectTransferData"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                    Description = ResourceProvider.GetString("LOCCommonIsolatedPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = false,
                            ShowCloseButton = true,
                        };

                        ListDataWithoutGame ViewExtension = new ListDataWithoutGame(PluginDatabase.GetIsolatedDataGames().ToList(), PluginDatabase);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCCommonIsolatedPluginData"), ViewExtension, windowOptions);
                        _ = windowExtension.ShowDialog();
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCGameActivity"),
                Description = "Test",
                Action = (mainMenuItem) =>
                {
					GameActivityMonitoring.GetCurrentMetrics();

				}
            });
#endif

            return mainMenuItems;
        }

        #endregion

        #region Game event

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
                else
                {
                    _ = Task.Run(() =>
                    {
                        SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            if (args.NewValue?.Count == 1)
                            {
                                PluginDatabase.GameContext = args.NewValue[0];
                                PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
			try
            {
                RunningActivity runningActivity = new RunningActivity
                {
                    Id = args.Game.Id,
                    PlaytimeOnStarted = args.Game.Playtime
                };
				GameActivityMonitoring.AddRunningActivity(runningActivity);

				GameActivityMonitoring.DataBackup_start(args.Game.Id);

                // start timer if log is enable.
                if (PluginSettings.Settings.EnableLogging)
                {
					GameActivityMonitoring.DataLogging_start(args.Game.Id);
                }

                DateTime DateSession = DateTime.Now.ToUniversalTime();

                runningActivity.GameActivitiesLog = PluginDatabase.Get(args.Game);
                runningActivity.GameActivitiesLog.Items.Add(new Activity
                {
                    IdConfiguration = PluginDatabase?.SystemConfigurationManager?.GetConfigurationIndex() ?? -1,
                    GameActionName = args.SourceAction?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction"),
                    DateSession = DateSession,
                    SourceID = args.Game.SourceId == null ? default : args.Game.SourceId,
                    PlatformIDs = args.Game.PlatformIds ?? new List<Guid>()
                });
                _ = runningActivity.GameActivitiesLog.ItemsDetails.Items.TryAdd(DateSession, new List<ActivityDetailsData>());

                runningActivity.ActivityBackup = new ActivityBackup
                {
                    Id = runningActivity.GameActivitiesLog.Id,
                    Name = runningActivity.GameActivitiesLog.Name,
                    ElapsedSeconds = 0,
                    GameActionName = args.SourceAction?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction"),
                    IdConfiguration = PluginDatabase?.SystemConfigurationManager?.GetConfigurationIndex() ?? -1,
                    DateSession = DateSession,
                    SourceID = args.Game.SourceId == null ? default : args.Game.SourceId,
                    PlatformIDs = args.Game.PlatformIds ?? new List<Guid>(),
                    ItemsDetailsDatas = new List<ActivityDetailsData>()
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);

				GameActivityMonitoring.DataBackup_stop(args.Game.Id);
                if (PluginSettings.Settings.EnableLogging)
                {
					GameActivityMonitoring.DataLogging_stop(args.Game.Id);
                }
            }
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    RunningActivity runningActivity = GameActivityMonitoring.GetRunningActivity(args.Game.Id);
                    GameActivityMonitoring.DataBackup_stop(args.Game.Id);

                    // Stop timer if log is enable.
                    if (PluginSettings.Settings.EnableLogging)
                    {
						GameActivityMonitoring.DataLogging_stop(args.Game.Id);
                    }

                    if (runningActivity == null)
                    {
                        return;
                    }

                    ulong elapsedSeconds = args.ElapsedSeconds;
                    if (elapsedSeconds == 0)
                    {
                        Thread.Sleep(5000);
                        // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                        elapsedSeconds = PluginSettings.Settings.SubstPlayStateTime && ExistsPlayStateInfoFile()
                            ? args.Game.Playtime - runningActivity.PlaytimeOnStarted - GetPlayStatePausedTimeInfo(args.Game)
                            : args.Game.Playtime - runningActivity.PlaytimeOnStarted;

                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}- noElapsedSeconds",
                            PluginDatabase.PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCGameActivityNoPlaytime"), args.Game.Name, elapsedSeconds),
                            NotificationType.Info
                        ));
                    }
                    else if (PluginSettings.Settings.SubstPlayStateTime && ExistsPlayStateInfoFile()) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
                    {
                        Thread.Sleep(10000); // Necessary since PlayState is executed after GameActivity.
                        elapsedSeconds -= GetPlayStatePausedTimeInfo(args.Game);
                    }

                    // Infos
                    runningActivity.GameActivitiesLog.GetLastSessionActivity(false).ElapsedSeconds = elapsedSeconds;
                    Common.LogDebug(true, Serialization.ToJson(runningActivity.GameActivitiesLog));
                    PluginDatabase.Update(runningActivity.GameActivitiesLog);

                    if (PluginDatabase.GameContext != null && args.Game.Id == PluginDatabase.GameContext.Id)
                    {
                        PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                    }

                    // Delete running data
                    GameActivityMonitoring.RemoveRunningActivity(runningActivity);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Delete backup
            string pathFileBackup = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, $"SaveSession_{args.Game.Id}.json");
            FileSystem.DeleteFile(pathFileBackup);
        }

		#endregion

		#region PlayState

		private bool ExistsPlayStateInfoFile() // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
        {
            // PlayState will write the Id and pausedTime to PlayState.txt file placed inside ExtensionsData Roaming Playnite folder
            // Check first if this file exists and if not return false to avoid executing unnecessary code.
            string PlayStateFile = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "PlayState.txt");
            return File.Exists(PlayStateFile);
        }

        private ulong GetPlayStatePausedTimeInfo(Game game) // Temporary workaround for PlayState paused time until Playnite allows to share data among extensions
        {
            // PlayState will write the Id and pausedTime to PlayState.txt file placed inside ExtensionsData Roaming Playnite folder
            // Check first if this file exists and if not return 0 as pausedTime.
            // This check is redundant with ExistsPlayStateInfoFile, but it's because the PlayState file will be modified after the first check, so added as a fallback to avoid exceptions.
            string PlayStateFile = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "PlayState.txt");
            if (!File.Exists(PlayStateFile))
            {
                return 0;
            }

            // The file is a simple txt, first line is GameId and second line the paused time.
            string[] PlayStateInfo = File.ReadAllLines(PlayStateFile);
            string Id = PlayStateInfo[0];
            ulong PausedSeconds = ulong.TryParse(PlayStateInfo[1], out ulong number) ? number : 0;

            // After retrieving the info restart the file in order to avoid reusing the same txt if PlayState crash / gets uninstalled.
            string[] Info = { " ", " " };

            File.WriteAllLines(PlayStateFile, Info);

            // Check that the GameId is the same as the paused game. If so, return the paused time. If not, return 0.
            return game.Id.ToString() == Id ? PausedSeconds : 0;
        }
        
        #endregion

        #region Application event

        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
			// Initialize hardware monitoring system if logging is enabled
			if (PluginSettings.Settings.EnableLogging)
			{
                GameActivityMonitoring.InitializeMonitoring();
                GameActivityMonitoring.CheckMonitoringReadiness();
			}

            #region QuickSearch support

            try
            {
                string icon = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "chart-646.png");
                SubItemsAction gaSubItemsAction = new SubItemsAction() { Action = () => { }, Name = "", CloseAfterExecute = false, SubItemSource = new QuickSearchItemSource() };
                CommandItem gaCommand = new CommandItem(PluginDatabase.PluginName, new List<CommandAction>(), ResourceProvider.GetString("LOCGaQuickSearchDescription"), icon);
                gaCommand.Keys.Add(new CommandItemKey() { Key = "ga", Weight = 1 });
                gaCommand.Actions.Add(gaSubItemsAction);
                _ = QuickSearch.QuickSearchSDK.AddCommand(gaCommand);
            }
            catch { }

            #endregion

            // Check backup
            GameActivityMonitoring.CheckBackup();

		}

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }
        
        #endregion

        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {

        }

        #region Settings
        
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GameActivitySettingsView();
        }
        
        #endregion
    }
}