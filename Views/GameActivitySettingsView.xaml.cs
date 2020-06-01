using System;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity
{
    public partial class GameActivitySettingsView : UserControl
    {
        public GameActivitySettingsView()
        {
            string icoLabel_text = "Show icon for launchers";
            string HWiNFO_enable_text = "Enable log";
            string HWiNFO_timeLog_text = "time interval";

            InitializeComponent();

            icoLabel.Content = icoLabel_text;
            hwLabel.Content = HWiNFO_enable_text;
            hwIntervalLabel.Content = HWiNFO_timeLog_text;

            hwIntervalLabel_text.Content = "(5 minutes)";
            Slider_ValueChanged(hwSlider, null);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            try
            {
                if (slider.Value > 1)
                {
                    hwIntervalLabel_text.Content = "(" + slider.Value + " minutes)";
                }
                else
                {
                    hwIntervalLabel_text.Content = "(" + slider.Value + " minute)";
                }
            }
            catch
            {

            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}