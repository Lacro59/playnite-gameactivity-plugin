using Newtonsoft.Json.Linq;
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

            List<WarningData> MessagesData = new List<WarningData>();
            for (int i = 0; i < Messages.Count; i++)
            {
                MessagesData.Add(Messages[i]);
            }
            icData.ItemsSource = MessagesData;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }
    }

    public class SetTextColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return Brushes.Orange;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class WarningData
    {
        public string At { get; set; }
        public Data FpsData { get; set; }
        public Data CpuTempData { get; set; }
        public Data GpuTempData { get; set; }
        public Data CpuUsageData { get; set; }
        public Data GpuUsageData { get; set; }
        public Data RamUsageData { get; set; }
    }
    public class Data
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool isWarm { get; set; }
    }
}
