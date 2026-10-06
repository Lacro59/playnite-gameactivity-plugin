using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Views;
using Playnite.SDK;

namespace GameActivity.Services.AlarmToast
{
    /// <summary>
    /// Shows a short-lived in-game overlay listing warm (breached) sensors.
    /// Placement uses the game process HWND when available, otherwise the primary monitor work area.
    /// </summary>
    public sealed class AlarmToastService
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly object _sync = new object();
        private Window _activeWindow;
        private DispatcherTimer _closeTimer;

        /// <summary>
        /// Displays a toast for <paramref name="warning"/> using alarm appearance settings.
        /// </summary>
        /// <param name="warning">Threshold snapshot (warm sensors only are shown).</param>
        /// <param name="startedProcessId">Optional pid from <c>OnGameStarted</c> for HWND anchoring.</param>
        /// <param name="settingsOverride">
        /// Optional settings (e.g. unsaved settings UI values). When <c>null</c>, uses persisted plugin settings.
        /// </param>
        public void Show(WarningData warning, int? startedProcessId, GameActivitySettings settingsOverride = null)
        {
            if (warning == null)
            {
                return;
            }

            GameActivitySettings settings = settingsOverride ?? GameActivity.PluginDatabase?.PluginSettings;
            if (settings == null)
            {
                Logger.Warn("AlarmToastService.Show: settings unavailable");
                return;
            }

            List<AlarmToastItem> items = BuildWarmItems(warning);
            if (items.Count == 0)
            {
                Common.LogDebug("AlarmToastService.Show: no warm sensors");
                return;
            }

            Application app = Application.Current;
            if (app?.Dispatcher == null)
            {
                Logger.Warn("AlarmToastService.Show: no WPF dispatcher");
                return;
            }

            // Resolve HWND off the UI thread (anchor may briefly poll for MainWindowHandle).
            IntPtr gameHwnd = GameWindowAnchor.TryResolve(startedProcessId);

            if (app.Dispatcher.CheckAccess())
            {
                ShowCore(items, settings, gameHwnd);
            }
            else
            {
                app.Dispatcher.BeginInvoke(
                    new Action(() => ShowCore(items, settings, gameHwnd)),
                    DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Closes any visible toast immediately.
        /// </summary>
        public void Close()
        {
            Application app = Application.Current;
            if (app?.Dispatcher == null)
            {
                CloseCore();
                return;
            }

            if (app.Dispatcher.CheckAccess())
            {
                CloseCore();
            }
            else
            {
                app.Dispatcher.BeginInvoke(new Action(CloseCore), DispatcherPriority.Normal);
            }
        }

        private void ShowCore(List<AlarmToastItem> items, GameActivitySettings settings, IntPtr gameHwnd)
        {
            try
            {
                CloseCore();

                string title = ResourceProvider.GetString("LOCGameActivityAlarmToastTitle")
                    ?? "Hardware alert";
                Window window = AlarmToastWindowFactory.Create(title);
                AlarmToastView view = new AlarmToastView();
                view.DataContext = new AlarmToastViewModel
                {
                    Title = title,
                    Timestamp = items.Count > 0 ? items[0].At : string.Empty,
                    Items = items
                };
                window.Content = view;

                int opacityPercent = settings.AlarmToastOpacityPercent;
                if (opacityPercent < 1)
                {
                    opacityPercent = 1;
                }
                else if (opacityPercent > 100)
                {
                    opacityPercent = 100;
                }

                window.Opacity = opacityPercent / 100d;

                window.Show();
                window.UpdateLayout();

                int corner = settings.AlarmToastCorner;
                if (corner < 0 || corner > 3)
                {
                    corner = 0;
                }

                if (!AlarmToastPlacer.Place(window, gameHwnd, corner))
                {
                    Logger.Warn("AlarmToastService: placement failed; toast may be off-screen");
                }

                int durationSeconds = settings.AlarmToastDurationSeconds;
                if (durationSeconds < 1)
                {
                    durationSeconds = 1;
                }

                lock (_sync)
                {
                    _activeWindow = window;
                    _closeTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(durationSeconds)
                    };
                    _closeTimer.Tick += OnCloseTimerTick;
                    _closeTimer.Start();
                }

                Logger.Info(
                    $"AlarmToastService: shown - Sensors:{items.Count} Corner:{corner} Duration:{durationSeconds}s Opacity:{opacityPercent}% Anchored:{(gameHwnd != IntPtr.Zero)}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AlarmToastService.Show failed");
                CloseCore();
            }
        }

        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            CloseCore();
        }

        private void CloseCore()
        {
            lock (_sync)
            {
                if (_closeTimer != null)
                {
                    _closeTimer.Stop();
                    _closeTimer.Tick -= OnCloseTimerTick;
                    _closeTimer = null;
                }

                if (_activeWindow != null)
                {
                    try
                    {
                        Common.LogDebug("AlarmToastService: closing active toast");
                        _activeWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "AlarmToastService: close failed");
                    }

                    _activeWindow = null;
                }
            }
        }

        private static List<AlarmToastItem> BuildWarmItems(WarningData warning)
        {
            List<AlarmToastItem> items = new List<AlarmToastItem>();
            AddIfWarm(items, warning.At, warning.FpsData);
            AddIfWarm(items, warning.At, warning.CpuTempData);
            AddIfWarm(items, warning.At, warning.GpuTempData);
            AddIfWarm(items, warning.At, warning.CpuUsageData);
            AddIfWarm(items, warning.At, warning.GpuUsageData);
            AddIfWarm(items, warning.At, warning.RamUsageData);
            AddIfWarm(items, warning.At, warning.CpuPowerData);
            AddIfWarm(items, warning.At, warning.GpuPowerData);
            return items;
        }

        private static void AddIfWarm(List<AlarmToastItem> items, string at, Data data)
        {
            if (data == null || !data.IsWarm)
            {
                return;
            }

            items.Add(new AlarmToastItem
            {
                At = at ?? string.Empty,
                Name = data.Name ?? string.Empty,
                ValueText = data.Value.ToString()
            });
        }
    }

    /// <summary>
    /// View model for <see cref="AlarmToastView"/>.
    /// </summary>
    public sealed class AlarmToastViewModel
    {
        /// <summary>Toast title.</summary>
        public string Title { get; set; }

        /// <summary>Local time label from the warning snapshot.</summary>
        public string Timestamp { get; set; }

        /// <summary>Warm sensor rows.</summary>
        public List<AlarmToastItem> Items { get; set; }
    }

    /// <summary>
    /// One warm sensor line shown in the toast.
    /// </summary>
    public sealed class AlarmToastItem
    {
        /// <summary>Snapshot time.</summary>
        public string At { get; set; }

        /// <summary>Sensor display name.</summary>
        public string Name { get; set; }

        /// <summary>Formatted value.</summary>
        public string ValueText { get; set; }
    }
}
