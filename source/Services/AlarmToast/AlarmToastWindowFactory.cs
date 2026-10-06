using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;
using CommonPluginsShared;
using Playnite.SDK;

namespace GameActivity.Services.AlarmToast
{
    /// <summary>
    /// Creates a borderless, transparent, non-activating WPF window for in-game toasts
    /// (slim pattern inspired by PlayniteAchievements).
    /// </summary>
    internal static class AlarmToastWindowFactory
    {
        /// <summary>
        /// Builds a chrome-less overlay window ready for toast content.
        /// </summary>
        /// <param name="title">Window title (accessibility / diagnostics only).</param>
        /// <returns>Configured <see cref="Window"/>.</returns>
        public static Window Create(string title)
        {
            Window window = null;
            try
            {
                if (API.Instance?.Dialogs != null)
                {
                    window = API.Instance.Dialogs.CreateWindow(new WindowCreationOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = false
                    });
                }
            }
            catch
            {
                window = null;
            }

            if (window == null)
            {
                Common.LogDebug("AlarmToastWindowFactory: Dialogs.CreateWindow unavailable; using plain Window");
                window = new Window();
            }

            window.Title = title ?? string.Empty;
            window.ShowInTaskbar = false;
            window.ShowActivated = false;
            window.Focusable = false;
            window.Topmost = true;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.AllowsTransparency = true;
            window.Background = Brushes.Transparent;
            window.UseLayoutRounding = true;
            window.SnapsToDevicePixels = true;

            WindowChrome.SetWindowChrome(window, new WindowChrome
            {
                CaptionHeight = 0,
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });

            window.Template = CreateContentOnlyWindowTemplate();
            return window;
        }

        private static ControlTemplate CreateContentOnlyWindowTemplate()
        {
            FrameworkElementFactory surface = new FrameworkElementFactory(typeof(Border));
            surface.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Window.BackgroundProperty));

            FrameworkElementFactory adorner = new FrameworkElementFactory(typeof(AdornerDecorator));
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            adorner.AppendChild(presenter);
            surface.AppendChild(adorner);

            return new ControlTemplate(typeof(Window))
            {
                VisualTree = surface
            };
        }
    }
}
