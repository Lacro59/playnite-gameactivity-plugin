using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPlayniteShared.Converters;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using CommonPluginsShared.Plugins;
using GameActivity.Views;
using System.Drawing.Imaging;
using CommonPluginsControls.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using System.Threading;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// ViewModel for the GameActivityViewSingle view.
    /// Implements INotifyPropertyChanged via ObservableObject (Playnite SDK base).
    /// Exposes all data and commands; the code-behind is limited to UI-only wiring.
    /// </summary>
    public class GameActivityViewSingleViewModel : ObservableObject
    {
        // ─── Dependencies ────────────────────────────────────────────────────────────
        private static ILogger Logger => LogManager.GetLogger();
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        private readonly GameActivity _plugin;
        private readonly Game _gameContext;
        private GameActivities _gameActivities;

        // ─── Constructor ─────────────────────────────────────────────────────────────

        public GameActivityViewSingleViewModel(GameActivity plugin, Game game)
        {
            _plugin = plugin;
            _gameContext = game;

            // Initialise commands before loading data so bindings resolve immediately.
            InitCommands();
            LoadData();
        }

        // ─── Display Properties ───────────────────────────────────────────────────────

        /// <summary>Display name of the game.</summary>
        public string GameDisplayName => _gameContext?.Name ?? string.Empty;

        private BitmapImage _coverImage;
        /// <summary>Cover image loaded from the Playnite database file path.</summary>
        public BitmapImage CoverImage
        {
            get => _coverImage;
            private set => SetValue(ref _coverImage, value);
        }

        private string _timeAvg = string.Empty;
        /// <summary>Average play-time per session, formatted as human-readable string.</summary>
        public string TimeAvg
        {
            get => _timeAvg;
            private set => SetValue(ref _timeAvg, value);
        }

        private string _totalGaPlaytime = string.Empty;
        /// <summary>Total GameActivity playtime (sum of sessions), formatted.</summary>
        public string TotalGaPlaytime
        {
            get => _totalGaPlaytime;
            private set => SetValue(ref _totalGaPlaytime, value);
        }

        private string _totalPlaynitePlaytime = string.Empty;
        /// <summary>Total Playnite library playtime, formatted.</summary>
        public string TotalPlaynitePlaytime
        {
            get => _totalPlaynitePlaytime;
            private set => SetValue(ref _totalPlaynitePlaytime, value);
        }

        private bool _hasPlaytimeMismatch;
        /// <summary>True when GA session total/count diverges from Playnite playtime/play count.</summary>
        public bool HasPlaytimeMismatch
        {
            get => _hasPlaytimeMismatch;
            private set => SetValue(ref _hasPlaytimeMismatch, value);
        }

        private string _playtimeMismatchTooltip;
        /// <summary>Tooltip explaining the GA vs Playnite mismatch; null when values match.</summary>
        public string PlaytimeMismatchTooltip
        {
            get => _playtimeMismatchTooltip;
            private set => SetValue(ref _playtimeMismatchTooltip, value);
        }

        private string _recentActivity = string.Empty;
        /// <summary>Relative label for the most recent activity (e.g. "2 days ago").</summary>
        public string RecentActivity
        {
            get => _recentActivity;
            private set => SetValue(ref _recentActivity, value);
        }

        private string _firstSession = string.Empty;
        /// <summary>Local-formatted date of the first recorded session.</summary>
        public string FirstSession
        {
            get => _firstSession;
            private set => SetValue(ref _firstSession, value);
        }

        private string _firstSessionElapsedTime = string.Empty;
        /// <summary>Duration of the first session, formatted as human-readable string.</summary>
        public string FirstSessionElapsedTime
        {
            get => _firstSessionElapsedTime;
            private set => SetValue(ref _firstSessionElapsedTime, value);
        }

        private string _lastSession = string.Empty;
        /// <summary>Local-formatted date of the last recorded session.</summary>
        public string LastSession
        {
            get => _lastSession;
            private set => SetValue(ref _lastSession, value);
        }

        private string _lastSessionElapsedTime = string.Empty;
        /// <summary>Duration of the last session, formatted as human-readable string.</summary>
        public string LastSessionElapsedTime
        {
            get => _lastSessionElapsedTime;
            private set => SetValue(ref _lastSessionElapsedTime, value);
        }

        // ─── Session List ─────────────────────────────────────────────────────────────

        private bool _isLoading = true;
        /// <summary>True while the session list is still loading; drives the loading overlay.</summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetValue(ref _isLoading, value);
        }

        private ObservableCollection<ListActivities> _sessionItems = new ObservableCollection<ListActivities>();
        /// <summary>Bound to the ListView of recorded sessions.</summary>
        public ObservableCollection<ListActivities> SessionItems
        {
            get => _sessionItems;
            private set => SetValue(ref _sessionItems, value);
        }

        // ─── PC Configuration (displayed when a session is selected) ──────────────────

        public string PcSubtitle => string.IsNullOrEmpty(PcName) ? string.Empty : $"{PcName} · {CpuName} · {GpuName}";

        private string _pcName = string.Empty;
        public string PcName
        {
            get => _pcName;
            set => SetValue(ref _pcName, value);
        }

        private string _osName = string.Empty;
        public string OsName
        {
            get => _osName;
            set => SetValue(ref _osName, value);
        }

        private string _cpuName = string.Empty;
        public string CpuName
        {
            get => _cpuName;
            set => SetValue(ref _cpuName, value);
        }

        private string _gpuName = string.Empty;
        public string GpuName
        {
            get => _gpuName;
            set => SetValue(ref _gpuName, value);
        }

        private string _ramUsage = string.Empty;
        public string RamUsage
        {
            get => _ramUsage;
            set => SetValue(ref _ramUsage, value);
        }

        private ListActivities _selectedSession;
        public ListActivities SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                OnPropertyChanged();
            }
        }

        // ─── Settings pass-through ────────────────────────────────────────────────────

        /// <summary>Plugin settings forwarded to XAML bindings.</summary>
        public PluginSettings Settings => PluginDatabase.PluginSettings;

        // ─── Commands ─────────────────────────────────────────────────────────────────

        /// <summary>Deletes the activity identified by its DateTime tag.</summary>
        public RelayCommand<DateTime> DeleteActivityCommand { get; private set; }

        /// <summary>Opens the "Add session" window.</summary>
        public RelayCommand AddActivityCommand { get; private set; }

        /// <summary>Opens the "Edit session" window for the selected session row.</summary>
        public RelayCommand<ListActivities> EditActivityCommand { get; private set; }

        /// <summary>Opens the "Merge sessions" window.</summary>
        public RelayCommand MergeActivityCommand { get; private set; }

        /// <summary>Exports performance detail rows for the selected session to CSV.</summary>
        public RelayCommand<ListActivities> ExportSessionDetailsCommand { get; private set; }

        private void InitCommands()
        {
            // Delete command: removes a session by its exact timestamp.
            DeleteActivityCommand = new RelayCommand<DateTime>((dateTag) =>
            {
                MessageBoxResult result = API.Instance.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCConfirumationAskGeneric"),
                    PluginDatabase.PluginName,
                    MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    ListActivities activity = SessionItems.FirstOrDefault(x => x.GameLastActivity == dateTag);
                    if (activity == null)
                    {
                        return;
                    }

                    // Decrement playtime only when the elapsed value is positive.
                    if (activity.GameElapsedSeconds != 0)
                    {
                        if ((long)(_gameContext.Playtime - activity.GameElapsedSeconds) >= 0)
                        {
                            _gameContext.Playtime -= activity.GameElapsedSeconds;
                            if (_gameContext.PlayCount != 0)
                            {
                                _gameContext.PlayCount--;
                            }
                            else
                            {
                                Logger.Warn($"Play count already at 0 for {_gameContext.Name}");
                            }
                        }
                        else
                        {
                            Logger.Warn($"Cannot subtract ElapsedSeconds ({activity.GameElapsedSeconds}) from Playtime ({_gameContext.Playtime}) for {_gameContext.Name}");
                        }
                    }

                    _gameActivities.DeleteActivity(activity.GameLastActivity);

                    // Playnite expects a local-facing "Last played" value.
                    _gameContext.LastActivity = _gameActivities?.Items?.Any() == true
                        ? _gameActivities.Items.Max(x => x.DateSession).ToLocalTime()
                        : (DateTime?)null;

                    API.Instance.Database.Games.Update(_gameContext);
                    PluginDatabase.Update(_gameActivities);

                    // Refresh list on UI thread.
                    _ = SessionItems.Remove(activity);
                    RefreshPlaytimeTotals();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Add command: opens the dedicated add-time window.
            AddActivityCommand = new RelayCommand(() =>
            {
                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true,
                    MinHeight = 450,
                    Width = 500,
                    EnableWindowPersistence = false
                };

                try
                {
                    GameActivityAddTime viewExtension = new GameActivityAddTime(_plugin, _gameContext, null);
                    Window window = PlayniteUiHelper.CreateExtensionWindow(
                        ResourceProvider.GetString("LOCGaAddNewGameSession"), viewExtension, windowOptions);
                    _ = window.ShowDialog();

                    if (viewExtension.Activity != null)
                    {
                        _gameActivities.Items.Add(viewExtension.Activity);
                        LoadSessionsAsync();

                        if (viewExtension.Activity.ElapsedSeconds >= 0)
                        {
                            _gameContext.Playtime += viewExtension.Activity.ElapsedSeconds;
                            _gameContext.PlayCount++;
                        }

                        _gameContext.LastActivity = _gameActivities.Items.Max(x => x.DateSession).ToLocalTime();
                        API.Instance.Database.Games.Update(_gameContext);
                        PluginDatabase.Update(_gameActivities);
                        RefreshPlaytimeTotals();
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Edit command: opens the add-time window pre-populated with the chosen activity.
            EditActivityCommand = new RelayCommand<ListActivities>((sessionRow) =>
            {
                try
                {
                    int index = FindActivityIndex(sessionRow);
                    if (index < 0)
                    {
                        Logger.Warn($"Unable to find matching activity to edit for {_gameContext.Name}");
                        return;
                    }

                    Activity activity = _gameActivities.Items[index];
                    ulong originalElapsed = activity.ElapsedSeconds;

                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = true,
                        MinHeight = 450,
                        Width = 500,
                        EnableWindowPersistence = false
                    };

                    GameActivityAddTime viewExtension = new GameActivityAddTime(_plugin, _gameContext, activity);
                    Window window = PlayniteUiHelper.CreateExtensionWindow(
                        ResourceProvider.GetString("LOCGaAddNewGameSession"), viewExtension, windowOptions);
                    _ = window.ShowDialog();

                    if (viewExtension.Activity != null)
                    {
                        _gameActivities.Items[index] = viewExtension.Activity;
                        LoadSessionsAsync();

                        if (viewExtension.Activity.ElapsedSeconds >= 0)
                        {
                            _gameContext.Playtime += viewExtension.Activity.ElapsedSeconds - originalElapsed;
                        }

                        _gameContext.LastActivity = _gameActivities.Items.Max(x => x.DateSession).ToLocalTime();
                        API.Instance.Database.Games.Update(_gameContext);
                        PluginDatabase.Update(_gameActivities);
                        RefreshPlaytimeTotals();
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            // Merge command: opens the merge-sessions window.
            MergeActivityCommand = new RelayCommand(() =>
            {
                try
                {
                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = true,
                        Width = 800,
                        MinHeight = 100,
                        EnableWindowPersistence = false
                    };

                    GameActivityMergeTime viewExtension = new GameActivityMergeTime(_gameContext);
                    Window window = PlayniteUiHelper.CreateExtensionWindow(
                        ResourceProvider.GetString("LOCGaMergeSession"), viewExtension, windowOptions);
                    _ = window.ShowDialog();

                    // Reload data from database because merge is done in another view-model instance.
                    _gameActivities = PluginDatabase.Get(_gameContext);
                    LoadSessionsAsync();
                    RefreshPlaytimeTotals();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            ExportSessionDetailsCommand = new RelayCommand<ListActivities>((sessionRow) =>
            {
                try
                {
                    if (sessionRow == null || _gameActivities == null)
                    {
                        return;
                    }

                    int index = FindActivityIndex(sessionRow);
                    if (index < 0)
                    {
                        Logger.Warn($"Export session details: no matching activity for {_gameContext?.Name}");
                        return;
                    }

                    Activity activity = _gameActivities.Items[index];
                    var exporter = new GameActivityDetailsExport();
                    exporter.ExportSessionDetailsToCsv(PluginDatabase.PluginName, _gameActivities, activity.DateSession);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }

        /// <summary>
        /// Finds the activity index from a list row using robust matching.
        /// Handles local/UTC conversions and uses action/configuration/elapsed as tie-breakers.
        /// </summary>
        /// <param name="sessionRow">Session row selected in the list.</param>
        /// <returns>Index of the matching activity in <see cref="_gameActivities.Items"/>; -1 if not found.</returns>
        private int FindActivityIndex(ListActivities sessionRow)
        {
            if (sessionRow == null || _gameActivities == null || _gameActivities.Items == null || _gameActivities.Items.Count == 0)
            {
                return -1;
            }

            DateTime localDate = sessionRow.GameLastActivity;
            DateTime utcDate = localDate.ToUniversalTime();

            int exactUtcIndex = _gameActivities.Items.FindIndex(x =>
                x.ElapsedSeconds == sessionRow.GameElapsedSeconds &&
                x.IdConfiguration == sessionRow.PCConfigurationId &&
                string.Equals(x.GameActionName, sessionRow.GameActionName, StringComparison.Ordinal));
            if (exactUtcIndex >= 0)
            {
                return exactUtcIndex;
            }

            int exactLocalIndex = _gameActivities.Items.FindIndex(x =>
                x.ElapsedSeconds == sessionRow.GameElapsedSeconds &&
                x.IdConfiguration == sessionRow.PCConfigurationId &&
                string.Equals(x.GameActionName, sessionRow.GameActionName, StringComparison.Ordinal));
            if (exactLocalIndex >= 0)
            {
                return exactLocalIndex;
            }

            // Fallback: date-only match if one of the optional fields changed since list generation.
            return _gameActivities.Items.FindIndex(x => x.DateSession.ToLocalTime() == localDate);
        }

        // ─── Data Loading ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads session stats synchronously at construction time.
        /// Cover decode and session list population run in the background so ShowDialog is not blocked.
        /// </summary>
        private void LoadData()
        {
#if DEBUG
            DebugTimer loadTimer = new DebugTimer("GameActivityViewSingleViewModel.LoadData");
#endif
            // Session aggregate data.
            _gameActivities = PluginDatabase.Get(_gameContext);

            PlayTimeToStringConverter playTimeConverter = new PlayTimeToStringConverter();
            LocalDateConverter localDateConverter = new LocalDateConverter();

            TimeAvg = (string)playTimeConverter.Convert(
                _gameActivities.AvgPlayTime(), null, null, CultureInfo.CurrentCulture);

            RefreshPlaytimeTotals(playTimeConverter);

            RecentActivity = _gameActivities.GetRecentActivity();

            FirstSession = (string)localDateConverter.Convert(
                _gameActivities.GetFirstSession(), null, null, CultureInfo.CurrentCulture);
            FirstSessionElapsedTime = (string)playTimeConverter.Convert(
                _gameActivities.GetFirstSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);

            LastSession = (string)localDateConverter.Convert(
                _gameActivities.GetLastSession(), null, null, CultureInfo.CurrentCulture);
            LastSessionElapsedTime = (string)playTimeConverter.Convert(
                _gameActivities.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
#if DEBUG
            loadTimer.Step("Get + stats done");
#endif

            // Kick off background loading of the session list and cover (cover was ~2.6s sync).
            LoadSessionsAsync();
            LoadCoverAsync();
#if DEBUG
            loadTimer.Stop("LoadSessionsAsync + LoadCoverAsync kicked");
#endif
        }

        /// <summary>
        /// Decodes the cover off the UI thread (scaled to the 220px display slot) and assigns it when ready.
        /// </summary>
        private void LoadCoverAsync()
        {
            if (_gameContext == null || _gameContext.CoverImage.IsNullOrEmpty())
            {
                return;
            }

            string coverPath;
            try
            {
                coverPath = API.Instance.Database.GetFullFilePath(_gameContext.CoverImage);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to resolve cover path for {_gameContext.Name}", false, PluginDatabase.PluginName);
                return;
            }

            if (string.IsNullOrEmpty(coverPath))
            {
                Common.LogDebug($"No cover file path for {_gameContext.Name}");
                return;
            }

            // PART_ImageCover is 220×220; decode at 2× for common DPI scaling.
            BitmapLoadProperties loadProperties = new BitmapLoadProperties(440, 0);
            string gameName = _gameContext.Name;

            // BitmapImage.EndInit requires STA — Task.Run (MTA) fails after a long decode.
            Thread coverThread = new Thread(() =>
            {
#if DEBUG
                DebugTimer coverTimer = new DebugTimer("GameActivityViewSingleViewModel.LoadCoverAsync");
#endif
                try
                {
                    BitmapImage image = BitmapExtensions.BitmapFromFile(coverPath, loadProperties);
                    if (image == null)
                    {
                        image = BitmapExtensions.BitmapFromFile(coverPath);
                    }
#if DEBUG
                    coverTimer.Step(image == null ? "decode null" : "decode done");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
                    {
                        CoverImage = image;
#if DEBUG
                        coverTimer.Stop("assigned");
#endif
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load cover for {gameName}", false, PluginDatabase.PluginName);
#if DEBUG
                    coverTimer.Stop("error");
#endif
                }
            });
            coverThread.IsBackground = true;
            coverThread.SetApartmentState(ApartmentState.STA);
            coverThread.Start();
        }

        /// <summary>
        /// Refreshes dual playtime totals and the mismatch warning used by the stats card.
        /// </summary>
        private void RefreshPlaytimeTotals()
        {
            RefreshPlaytimeTotals(new PlayTimeToStringConverter());
        }

        /// <summary>
        /// Refreshes dual playtime totals and the mismatch warning used by the stats card.
        /// </summary>
        /// <param name="playTimeConverter">Shared duration formatter.</param>
        private void RefreshPlaytimeTotals(PlayTimeToStringConverter playTimeConverter)
        {
            ulong gaSeconds = _gameActivities?.SessionPlaytime ?? 0UL;
            ulong pnSeconds = _gameContext?.Playtime ?? 0UL;
            ulong gaCount = _gameActivities?.Count ?? 0UL;
            ulong pnCount = _gameContext?.PlayCount ?? 0UL;

            TotalGaPlaytime = (string)playTimeConverter.Convert(
                gaSeconds, null, null, CultureInfo.CurrentCulture);
            TotalPlaynitePlaytime = (string)playTimeConverter.Convert(
                pnSeconds, null, null, CultureInfo.CurrentCulture);

            bool mismatch = gaSeconds != pnSeconds || gaCount != pnCount;
            HasPlaytimeMismatch = mismatch;
            if (mismatch)
            {
                PlaytimeMismatchTooltip = string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceProvider.GetString("LOCGaPlaytimeMismatchTooltip"),
                    TotalGaPlaytime,
                    gaCount,
                    TotalPlaynitePlaytime,
                    pnCount);
                Common.LogDebug(
                    $"Playtime mismatch for {_gameContext?.Name}: GA={gaSeconds}s/{gaCount}, Playnite={pnSeconds}s/{pnCount}");
            }
            else
            {
                PlaytimeMismatchTooltip = null;
            }
        }

        /// <summary>
        /// Builds the session list on a background thread and marshals the result
        /// back to the UI thread via Dispatcher.BeginInvoke.
        /// </summary>
        public void LoadSessionsAsync()
        {
            _ = Task.Run(() =>
            {
#if DEBUG
                DebugTimer sessionsTimer = new DebugTimer("GameActivityViewSingleViewModel.LoadSessionsAsync");
#endif
                ObservableCollection<ListActivities> items = new ObservableCollection<ListActivities>();
                bool enableLogging = PluginDatabase.PluginSettings.EnableLogging;

                for (int i = 0; i < _gameActivities.FilterItems.Count; i++)
                {
                    try
                    {
                        ulong elapsed = _gameActivities.FilterItems[i].ElapsedSeconds;
                        DateTime dateSession = Convert.ToDateTime(_gameActivities.FilterItems[i].DateSession).ToLocalTime();
                        DateTime dateSessionUtc = dateSession.ToUniversalTime();
                        string sourceName = _gameActivities.FilterItems[i].SourceName;

                        TextBlockWithIconMode iconMode = (PluginDatabase.PluginSettings.ModeStoreIcon == 1)
                            ? TextBlockWithIconMode.IconTextFirstOnly
                            : TextBlockWithIconMode.IconFirstOnly;

                        SessionHardwareMetrics hw = enableLogging
                            ? _gameActivities.GetSessionHardwareMetrics(dateSessionUtc)
                            : new SessionHardwareMetrics();

                        items.Add(new ListActivities
                        {
                            GameLastActivity = dateSession,
                            GameElapsedSeconds = elapsed,
                            AvgCPU = hw.AvgCpu + "%",
                            AvgGPU = hw.AvgGpu + "%",
                            AvgRAM = hw.AvgRam + "%",
                            AvgFPS = hw.AvgFps.ToString(),
                            LoggedFpsMin = hw.MinFps.ToString(),
                            LoggedFpsMax = hw.MaxFps.ToString(),
                            LoggedFpsMedian = hw.MedianFps.ToString(),
                            LoggedFpsStdDev = hw.StdDevFps.ToString(),
                            LoggedFps1PercentLowAvg = hw.AvgFps1PercentLow.ToString(),
                            LoggedFps1PercentLowMin = hw.MinFps1PercentLow.ToString(),
                            LoggedFps0Point1PercentLowAvg = hw.AvgFps0Point1PercentLow.ToString(),
                            LoggedFps0Point1PercentLowMin = hw.MinFps0Point1PercentLow.ToString(),
                            AvgCPUT = hw.AvgCpuT + "°",
                            AvgGPUT = hw.AvgGpuT + "°",
                            AvgCPUP = hw.AvgCpuP + "W",
                            AvgGPUP = hw.AvgGpuP + "W",

                            GameSourceName = sourceName,
                            TypeStoreIcon = iconMode,
                            SourceIcon = PlayniteTools.GetPlatformIcon(sourceName),
                            SourceIconText = TransformIcon.Get(sourceName),

                            EnableWarm = PluginDatabase.PluginSettings.HasActiveChartThresholdIndicators,
                            MaxCPUT = PluginDatabase.PluginSettings.MaxCpuTemp.ToString(),
                            MaxGPUT = PluginDatabase.PluginSettings.MaxGpuTemp.ToString(),
                            MinFPS = PluginDatabase.PluginSettings.MinFps.ToString(),
                            MaxCPU = PluginDatabase.PluginSettings.MaxCpuUsage.ToString(),
                            MaxGPU = PluginDatabase.PluginSettings.MaxGpuUsage.ToString(),
                            MaxRAM = PluginDatabase.PluginSettings.MaxRamUsage.ToString(),
                            MaxCPUP = PluginDatabase.PluginSettings.MaxCpuPower.ToString(),
                            MaxGPUP = PluginDatabase.PluginSettings.MaxGpuPower.ToString(),

                            PCConfigurationId = _gameActivities.FilterItems[i].IdConfiguration,
                            PCName = _gameActivities.FilterItems[i].Configuration.Name,
                            GameActionName = _gameActivities.FilterItems[i].GameActionName
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false,
                            $"Failed to build ListActivities row #{i} for {_gameActivities.Name}",
                            true, PluginDatabase.PluginName);
                    }
                }

#if DEBUG
                sessionsTimer.Step(string.Format("build done sessions={0} logging={1}", items.Count, enableLogging));
#endif

                // Marshal back to the UI thread.
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    SessionItems = items;
                    IsLoading = false;
#if DEBUG
                    sessionsTimer.Step("SessionItems bound");
#endif

                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)delegate
                    {
                        ICollectionView view = CollectionViewSource.GetDefaultView(SessionItems);
                        if (view != null && view.MoveCurrentToFirst())
                        {
                            SelectedSession = view.CurrentItem as ListActivities;
                        }
#if DEBUG
                        sessionsTimer.Stop("SelectedSession set");
#endif
                    });
                });
            });
        }

        /// <summary>
        /// Updates the PC configuration panel when a session row is selected.
        /// Called from the view's SelectionChanged handler (minimal code-behind).
        /// </summary>
        /// <param name="configurationIndex">IdConfiguration from the selected ListActivities row.</param>
        public void UpdatePcConfiguration(int configurationIndex)
        {
            if (configurationIndex != -1
                && configurationIndex < PluginDatabase.SystemConfigurationManager.GetConfigurations().Count)
            {
                var cfg = PluginDatabase.SystemConfigurationManager.GetConfigurations()[configurationIndex];
                PcName = cfg.Name;
                OsName = cfg.Os;
                CpuName = cfg.Cpu;
                GpuName = cfg.GpuName;
                RamUsage = cfg.RamUsage;
            }
            else
            {
                PcName = OsName = CpuName = GpuName = RamUsage = string.Empty;
            }

            OnPropertyChanged(nameof(PcSubtitle));
        }
    }
}