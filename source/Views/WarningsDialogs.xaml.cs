using GameActivity.Models;
using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Playnite.SDK;

namespace GameActivity.Views
{
    /// <summary>
    /// Interaction logic for WarningsDialogs.xaml.
    /// Displays hardware sensor warnings recorded during a game session.
    /// Window closure is handled by the shared <see cref="CommonPluginsShared.UI.CommandsWindows.CloseHostWindow"/> command.
    /// </summary>
    public partial class WarningsDialogs : UserControl
    {
        public WarningsDialogs(List<WarningData> messages)
        {
            InitializeComponent();
            icData.ItemsSource = messages;
        }
    }

    // ── Value converters ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the themed <c>WarningBrush</c> when the bound boolean is <c>true</c>
    /// (sensor threshold exceeded), or <c>TextBrush</c> otherwise.
    /// Resolves both brushes from application resources so any Playnite theme is supported.
    /// </summary>
    public class BoolToWarningBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool isWarm && isWarm)
                {
                    return ResourceProvider.GetResource("WarningBrush") as Brush ?? Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "GameActivity");
            }

            return ResourceProvider.GetResource("TextBrush") as Brush ?? Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> when the bound <see cref="Data"/> object
    /// is null or has a zero value (sensor not configured or unavailable).
    /// Returns <see cref="Visibility.Visible"/> otherwise.
    /// </summary>
    public class DataToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Data data && data.Value != 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}