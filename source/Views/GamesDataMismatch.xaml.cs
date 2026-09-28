using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using GameActivity.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Code-behind for GamesDataMismatch.xaml.
    /// All business logic lives in <see cref="GamesDataMismatchViewModel"/>.
    /// This file only wires the DataContext and hosts view-local converters.
    /// </summary>
    public partial class GamesDataMismatch : UserControl
    {
        public GamesDataMismatch()
        {
            InitializeComponent();
            DataContext = new GamesDataMismatchViewModel();
        }
    }

    /// <summary>
    /// Formats a signed playtime delta (seconds) with a leading +/− and Playnite duration style.
    /// </summary>
    public class SignedPlayTimeToStringConverter : IValueConverter
    {
        private static readonly PlayTimeToStringConverter Inner = new PlayTimeToStringConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return string.Empty;
                }

                long delta = System.Convert.ToInt64(value, culture ?? CultureInfo.CurrentCulture);
                string formatted = (string)Inner.Convert(
                    (ulong)Math.Abs(delta), null, null, culture ?? CultureInfo.CurrentCulture);

                if (delta > 0)
                {
                    return "+" + formatted;
                }

                if (delta < 0)
                {
                    return "-" + formatted;
                }

                return formatted;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "GameActivity");
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
