using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CommonPluginsShared;
using Playnite.SDK;

namespace GameActivity.Services.AlarmToast
{
    /// <summary>
    /// Positions the alarm toast in a screen corner and inserts it above the game HWND when possible.
    /// </summary>
    internal static class AlarmToastPlacer
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private const int GapDip = 16;

        /// <summary>
        /// Places <paramref name="window"/> in the configured corner of the game (or monitor) bounds.
        /// </summary>
        /// <param name="window">Toast window (must be shown / laid out).</param>
        /// <param name="gameHwnd">Game HWND, or <see cref="IntPtr.Zero"/> for monitor fallback.</param>
        /// <param name="corner">0 bottom-right, 1 bottom-left, 2 top-right, 3 top-left.</param>
        /// <returns><c>true</c> when placement succeeded.</returns>
        public static bool Place(Window window, IntPtr gameHwnd, int corner)
        {
            if (window == null)
            {
                return false;
            }

            try
            {
                IntPtr toastHwnd = new WindowInteropHelper(window).Handle;
                if (toastHwnd == IntPtr.Zero)
                {
                    Logger.Warn("AlarmToastPlacer: toast HWND unavailable");
                    return false;
                }

                AlarmToastNative.RECT bounds;
                bool anchored = TryGetAnchorBounds(gameHwnd, out bounds);
                if (!anchored)
                {
                    Logger.Info("AlarmToastPlacer: using primary monitor work area fallback");
                    if (!TryGetPrimaryWorkArea(out bounds))
                    {
                        return false;
                    }

                    window.Topmost = true;
                }
                else
                {
                    // Stay just above the game rather than desktop-topmost.
                    window.Topmost = false;
                }

                double dpiScale = GetDpiScale(window);
                int width = Math.Max(1, (int)Math.Ceiling(Math.Max(window.ActualWidth, 1) * dpiScale));
                int height = Math.Max(1, (int)Math.Ceiling(Math.Max(window.ActualHeight, 1) * dpiScale));
                int gap = (int)Math.Round(GapDip * dpiScale);

                bool alignRight = corner == 0 || corner == 2;
                bool alignBottom = corner == 0 || corner == 1;

                int x = alignRight
                    ? bounds.Right - width - gap
                    : bounds.Left + gap;
                int y = alignBottom
                    ? bounds.Bottom - height - gap
                    : bounds.Top + gap;

                bool moved = AlarmToastNative.SetWindowPos(
                    toastHwnd,
                    IntPtr.Zero,
                    x,
                    y,
                    width,
                    height,
                    AlarmToastNative.SWP_NOACTIVATE | AlarmToastNative.SWP_SHOWWINDOW);

                if (!moved)
                {
                    Logger.Warn("AlarmToastPlacer: SetWindowPos move failed");
                    return false;
                }

                if (anchored && gameHwnd != IntPtr.Zero)
                {
                    if (!SetZOrderAbove(toastHwnd, gameHwnd))
                    {
                        Logger.Warn("AlarmToastPlacer: z-order insert failed; leaving Topmost fallback");
                        window.Topmost = true;
                    }
                }

                Common.LogDebug(
                    $"AlarmToastPlacer: placed - Corner:{corner} Anchored:{anchored} Size:{width}x{height}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AlarmToastPlacer: placement failed");
                return false;
            }
        }

        private static bool SetZOrderAbove(IntPtr toastHwnd, IntPtr gameHwnd)
        {
            IntPtr above = AlarmToastNative.GetWindow(gameHwnd, AlarmToastNative.GW_HWNDPREV);
            if (above == toastHwnd)
            {
                return true;
            }

            IntPtr insertAfter = above != IntPtr.Zero
                ? above
                : (AlarmToastNative.IsTopmost(gameHwnd)
                    ? AlarmToastNative.HWND_TOPMOST
                    : AlarmToastNative.HWND_TOP);

            return AlarmToastNative.SetWindowPos(
                toastHwnd,
                insertAfter,
                0,
                0,
                0,
                0,
                AlarmToastNative.SWP_NOMOVE | AlarmToastNative.SWP_NOSIZE | AlarmToastNative.SWP_NOACTIVATE);
        }

        private static bool TryGetAnchorBounds(IntPtr gameHwnd, out AlarmToastNative.RECT bounds)
        {
            bounds = default(AlarmToastNative.RECT);
            if (gameHwnd == IntPtr.Zero)
            {
                return false;
            }

            AlarmToastNative.RECT windowRect;
            if (!AlarmToastNative.GetWindowRect(gameHwnd, out windowRect) || windowRect.Width <= 0 || windowRect.Height <= 0)
            {
                return false;
            }

            IntPtr monitor = AlarmToastNative.MonitorFromWindow(gameHwnd, AlarmToastNative.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                AlarmToastNative.MONITORINFO info = new AlarmToastNative.MONITORINFO();
                info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(AlarmToastNative.MONITORINFO));
                if (AlarmToastNative.GetMonitorInfo(monitor, ref info) && info.rcWork.Width > 0)
                {
                    // Prefer the intersection of the game window with the monitor work area.
                    bounds = Intersect(windowRect, info.rcWork);
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        return true;
                    }

                    bounds = info.rcWork;
                    return true;
                }
            }

            bounds = windowRect;
            return true;
        }

        private static bool TryGetPrimaryWorkArea(out AlarmToastNative.RECT bounds)
        {
            bounds = default(AlarmToastNative.RECT);
            try
            {
                Rect work = SystemParameters.WorkArea;
                double dpi = GetSystemDpiScale();
                bounds.Left = (int)Math.Round(work.Left * dpi);
                bounds.Top = (int)Math.Round(work.Top * dpi);
                bounds.Right = (int)Math.Round(work.Right * dpi);
                bounds.Bottom = (int)Math.Round(work.Bottom * dpi);
                return bounds.Width > 0 && bounds.Height > 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AlarmToastPlacer: primary work area failed");
                return false;
            }
        }

        private static AlarmToastNative.RECT Intersect(AlarmToastNative.RECT a, AlarmToastNative.RECT b)
        {
            AlarmToastNative.RECT r = new AlarmToastNative.RECT
            {
                Left = Math.Max(a.Left, b.Left),
                Top = Math.Max(a.Top, b.Top),
                Right = Math.Min(a.Right, b.Right),
                Bottom = Math.Min(a.Bottom, b.Bottom)
            };
            return r;
        }

        private static double GetDpiScale(Visual visual)
        {
            try
            {
                PresentationSource source = PresentationSource.FromVisual(visual);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch
            {
                // Fall through.
            }

            return GetSystemDpiScale();
        }

        private static double GetSystemDpiScale()
        {
            try
            {
                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    return g.DpiX / 96d;
                }
            }
            catch
            {
                return 1d;
            }
        }
    }
}
