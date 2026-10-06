using System;
using System.Diagnostics;
using System.Threading;
using CommonPluginsShared;
using Playnite.SDK;

namespace GameActivity.Services.AlarmToast
{
    /// <summary>
    /// Minimal game HWND resolver: Playnite <c>StartedProcessId</c> → main window handle.
    /// Launcher-aware tracking is deferred (Playnite 11).
    /// </summary>
    internal static class GameWindowAnchor
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        /// <summary>
        /// Tries to resolve a usable main-window HWND for <paramref name="processId"/>.
        /// Polls briefly when the process exists but has not created a window yet.
        /// </summary>
        /// <param name="processId">Process id from <c>OnGameStarted</c>, or <c>null</c>.</param>
        /// <returns>HWND when available; otherwise <see cref="IntPtr.Zero"/>.</returns>
        public static IntPtr TryResolve(int? processId)
        {
            if (!processId.HasValue || processId.Value <= 0)
            {
                Common.LogDebug("GameWindowAnchor: no StartedProcessId");
                return IntPtr.Zero;
            }

            try
            {
                Process process = Process.GetProcessById(processId.Value);
                if (process == null || process.HasExited)
                {
                    Logger.Warn($"GameWindowAnchor: process {processId.Value} exited or missing");
                    return IntPtr.Zero;
                }

                IntPtr handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    Common.LogDebug($"GameWindowAnchor: resolved HWND for pid {processId.Value}");
                    return handle;
                }

                // Short poll: some titles create their HWND a few hundred ms after start.
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(200);
                    process.Refresh();
                    if (process.HasExited)
                    {
                        break;
                    }

                    handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        Common.LogDebug($"GameWindowAnchor: resolved HWND for pid {processId.Value} after poll");
                        return handle;
                    }
                }

                Logger.Warn($"GameWindowAnchor: no MainWindowHandle for pid {processId.Value}");
                return IntPtr.Zero;
            }
            catch (ArgumentException)
            {
                Logger.Warn($"GameWindowAnchor: process {processId.Value} not found");
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"GameWindowAnchor: failed for pid {processId}");
                return IntPtr.Zero;
            }
        }
    }
}
