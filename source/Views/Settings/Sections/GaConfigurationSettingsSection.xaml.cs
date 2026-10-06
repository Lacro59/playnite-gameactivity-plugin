using CommonPluginsShared;
using GameActivity.Services;
using Playnite.SDK;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivity.Views
{
	/// <summary>Configuration colors settings section.</summary>
	public partial class GaConfigurationSettingsSection : UserControl
	{
		private static ILogger Logger => LogManager.GetLogger();

		private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		private StackPanel SpControl { get; set; }

		/// <summary>Initializes a new instance of the <see cref="GaConfigurationSettingsSection"/> class.</summary>
		public GaConfigurationSettingsSection()
		{
			InitializeComponent();

			PART_SelectorColorPicker.OnlySimpleColor = true;
			PART_SelectorColorPicker.IsSimpleColor = true;
		}

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
						PluginDatabase.PluginSettings.StoreColors[index].Fill = new SolidColorBrush(color);
					}
					else
					{
						PluginDatabase.PluginSettings.ChartColors = new SolidColorBrush(color);
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
			PluginDatabase.PluginSettings.ChartColors = (SolidColorBrush)new BrushConverter().ConvertFrom("#2195f2");
		}
	}
}
