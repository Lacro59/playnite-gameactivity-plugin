using Playnite.SDK;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Documents;
using System.Diagnostics;

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


        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Ga_IntegrationInDescription") && (bool)cb.IsChecked)
            {
                Ga_IntegrationInCustomTheme.IsChecked = false;
                Ga_IntegrationInDescriptionWithToggle.IsChecked = false;
            }
            if ((cb.Name == "Ga_IntegrationInDescriptionWithToggle") && (bool)cb.IsChecked)
            {
                Ga_IntegrationInCustomTheme.IsChecked = false;
                Ga_IntegrationInDescription.IsChecked = false;
                Ga_IntegrationInButton.IsChecked = false;
                Ga_IntegrationInButtonDetails.IsChecked = false;
            }


            if ((cb.Name == "Ga_IntegrationInButton") && (bool)cb.IsChecked)
            {
                Ga_IntegrationInCustomTheme.IsChecked = false;
                Ga_IntegrationInDescriptionWithToggle.IsChecked = false;
                Ga_IntegrationInButtonDetails.IsChecked = false;
            }

            if ((cb.Name == "Ga_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Ga_IntegrationInCustomTheme.IsChecked = false;
                Ga_IntegrationInDescriptionWithToggle.IsChecked = false;
                Ga_IntegrationInButton.IsChecked = false;
            }

            if ((cb.Name == "Ga_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                Ga_IntegrationInDescription.IsChecked = false;
                Ga_IntegrationInDescriptionWithToggle.IsChecked = false;
                Ga_IntegrationInButton.IsChecked = false;
                Ga_IntegrationInButtonDetails.IsChecked = false;
            }
        }

        private void CbLogging_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "cbUseMsiAfterburner") && (bool)cb.IsChecked)
            {
                cbUseHWiNFO.IsChecked = false;
            }
            if ((cb.Name == "cbUseHWiNFO") && (bool)cb.IsChecked)
            {
                cbUseMsiAfterburner.IsChecked = false;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)sender;
            Process.Start((string)link.Tag);
        }
    }
}
