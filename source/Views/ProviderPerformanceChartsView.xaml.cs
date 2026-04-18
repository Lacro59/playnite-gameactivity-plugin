using GameActivity.Services;
using GameActivity.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Live comparison of hardware metrics as reported by each monitoring provider.
    /// </summary>
    public partial class ProviderPerformanceChartsView : UserControl
    {
        public ProviderPerformanceChartsView(GameActivityMonitoring monitoring)
        {
            InitializeComponent();
            DataContext = new ProviderPerformanceChartsViewModel(monitoring);
            Loaded += ProviderPerformanceChartsView_Loaded;
            Unloaded += ProviderPerformanceChartsView_Unloaded;
        }

        private void ProviderPerformanceChartsView_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ProviderPerformanceChartsViewModel;
            vm?.Start();
        }

        private void ProviderPerformanceChartsView_Unloaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ProviderPerformanceChartsViewModel;
            vm?.Dispose();
        }
    }
}
