using Playnite.SDK;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using CommonPluginsShared;
using GameActivity.Services;

namespace GameActivity
{
    public partial class GameActivitySettingsView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static IResourceProvider resources => new ResourceProvider();

        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        private StackPanel SpControl { get; set; }


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
            if (sender != null)
            {
                Slider slider = sender as Slider;

                if (labelIntervalLabel_text != null)
                {
                    labelIntervalLabel_text.Content = "(" + slider.Value + " " + resources.GetString("LOCGameActivityTimeLabel") + ")";
                }
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
                cbUseHWiNFOSharedMemory.IsChecked = false;
                cbUseHWiNFOGadget.IsChecked = false;
            }
            if (cb.Name == "cbUseHWiNFOSharedMemory" && (bool)cb.IsChecked)
            {
                cbUseMsiAfterburner.IsChecked = false;
                cbUseHWiNFOGadget.IsChecked = false;

                PART_TabHWiNFO.SelectedIndex = 0;
            }
            if (cb.Name == "cbUseHWiNFOGadget" && (bool)cb.IsChecked)
            {
                cbUseMsiAfterburner.IsChecked = false;
                cbUseHWiNFOSharedMemory.IsChecked = false;

                PART_TabHWiNFO.SelectedIndex = 1;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement link = (FrameworkElement)sender;
            Process.Start((string)link.Tag);
        }


        #region SetColors
        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SpControl = ((Grid)((FrameworkElement)sender).Parent).Children.OfType<StackPanel>().FirstOrDefault();

                if (SpControl.Background is SolidColorBrush brush)
                {
                    Color color = brush.Color;
                    PART_SelectorColorPicker.SetColors(color);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                PART_ColorListContener.Visibility = Visibility.Collapsed;
                PART_ChartColor.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            if (SpControl != null)
            {
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    Color color = PART_SelectorColorPicker.SimpleColor;
                    SpControl.Background = new SolidColorBrush(color);

                    if (SpControl.Tag != null)
                    {
                        int.TryParse((string)SpControl.Tag, out int index);
                        PluginDatabase.PluginSettings.Settings.StoreColors[index].Fill = new SolidColorBrush(color);
                    }
                    else
                    {
                        PluginDatabase.PluginSettings.Settings.ChartColors = new SolidColorBrush(color);
                    }
                }
            }
            else
            {
                Logger.Warn("One control is undefined");
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_ColorListContener.Visibility = Visibility.Visible;
            PART_ChartColor.Visibility = Visibility.Visible;
        }

        private void PART_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_ColorListContener.Visibility = Visibility.Visible;
            PART_ChartColor.Visibility = Visibility.Visible;
        }



        private void BtPickChartColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SpControl = ((Grid)((FrameworkElement)sender).Parent).Children.OfType<StackPanel>().FirstOrDefault();

                if (SpControl.Background is SolidColorBrush brush)
                {
                    Color color = brush.Color;
                    PART_SelectorColorPicker.SetColors(color);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                PART_ColorListContener.Visibility = Visibility.Collapsed;
                PART_ChartColor.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PART_Color.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");
            PluginDatabase.PluginSettings.Settings.ChartColors = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");
        }
        #endregion
    }
}
