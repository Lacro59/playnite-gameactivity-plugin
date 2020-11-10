using GameActivity.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour WarningsDialogs.xaml
    /// </summary>
    public partial class WarningsDialogs : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        public WarningsDialogs(List<WarningData> Messages)
        {
            InitializeComponent();

#if DEBUG
            logger.Debug($"GameActivity - Messages: {JsonConvert.SerializeObject(Messages)}");
#endif
            icData.ItemsSource = Messages;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }
    }

    public class SetTextColor : IValueConverter
    {
        public static IResourceProvider resources = new ResourceProvider();


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return Brushes.Orange;
            }
            return resources.GetResource("TextBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
