using Playnite.SDK;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace GameActivity
{
    public partial class GameActivitySettingsView : UserControl
    {
        private static IResourceProvider resources = new ResourceProvider();

        public GameActivitySettingsView()
        {
            InitializeComponent();

            labelIntervalLabel_text.Content = "(5 " + resources.GetString("LOCGameActivityTimeLabel") + ")";
            Slider_ValueChanged(hwSlider, null);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            try
            {
                labelIntervalLabel_text.Content = "(" + slider.Value + " " + resources.GetString("LOCGameActivityTimeLabel") + ")";
            }
            catch
            {
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
