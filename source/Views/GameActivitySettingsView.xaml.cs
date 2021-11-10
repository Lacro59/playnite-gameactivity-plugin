using Playnite.SDK;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Media;
using CommonPluginsShared;
using GameActivity.Services;

namespace GameActivity
{
    public partial class GameActivitySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        private StackPanel spControl;


        public GameActivitySettingsView()
        {
            InitializeComponent();

            labelIntervalLabel_text.Content = "(5 " + resources.GetString("LOCGameActivityTimeLabel") + ")";
            Slider_ValueChanged(hwSlider, null);


            PART_SelectorColorPicker.OnlySimpleColor = true;
            PART_SelectorColorPicker.IsSimpleColor = true;
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


        #region SetColors
        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spControl = ((Grid)((FrameworkElement)sender).Parent).Children.OfType<StackPanel>().FirstOrDefault();

                if (spControl.Background is SolidColorBrush)
                {
                    Color color = ((SolidColorBrush)spControl.Background).Color;
                    PART_SelectorColorPicker.SetColors(color);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                PART_Listbox.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "GameActivity");
            }
        }

        private void PART_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            Color color = default(Color);

            if (spControl != null)
            {
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = PART_SelectorColorPicker.SimpleColor;
                    spControl.Background = new SolidColorBrush(color);

                    int index;
                    int.TryParse((string)spControl.Tag, out index);

                    PluginDatabase.PluginSettings.Settings.StoreColors[index].Fill = new SolidColorBrush(color);
                }
            }
            else
            {
                logger.Warn("One control is undefined");
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_Listbox.Visibility = Visibility.Visible;
        }

        private void PART_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_Listbox.Visibility = Visibility.Visible;
        }
        #endregion
    }
}
