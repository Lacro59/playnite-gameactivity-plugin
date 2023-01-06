using GameActivity.Models;
using Playnite.SDK;
using CommonPluginsShared;
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
        public WarningsDialogs(List<WarningData> Messages)
        {
            InitializeComponent();

            icData.ItemsSource = Messages;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }
    }

    public class SetTextColor : IValueConverter
    {
        public static IResourceProvider resources => new ResourceProvider();


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return Brushes.Orange;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "GameActivity");
            }

            return resources.GetResource("TextBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
