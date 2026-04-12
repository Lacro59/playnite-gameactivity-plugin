using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using GameActivity;
using GameActivity.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows;
using Playnite.SDK.Plugins;
using CommonPluginsShared.Plugins;

namespace GameActivity.Services
{
	public class GameActivityWindows : PluginWindows
	{
        public GameActivityWindows(string pluginName, IPluginDatabase pluginDatabase) : base(pluginName, pluginDatabase)
        {
        }

        public override void ShowPluginGameDataWindow(GenericPlugin plugin, Game gameContext)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				Height = 740,
				MinWidth = 1280,
				WidthPercent = 90
			};

			var viewExtension = new GameActivityViewSingle((GameActivity)plugin, gameContext);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				PluginName,
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		public override void ShowPluginGameDataWindow(GenericPlugin plugin)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				Height = 740,
				MaxWidth = 1500,
				WidthPercent = 80
			};

			var viewExtension = new GameActivityView((GameActivity)plugin);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCGamesActivitiesTitle"),
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		/// <summary>
		/// Opens the live per-provider hardware metrics chart window.
		/// </summary>
		public void ShowProviderPerformanceCharts(GameActivity plugin)
		{
			if (plugin == null)
			{
				return;
			}

			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				WidthPercent = 88,
				MinWidth = 720,
				Height = 760
			};

			var viewExtension = new ProviderPerformanceChartsView(plugin.GameActivityMonitoring);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCGameActivityProviderPerfChartsTitle"),
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		public override void ShowPluginDataMismatch()
        {
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				WidthPercent = 70,
				MaxWidth = 1500,
				Height = 500
			};

			GamesDataMismatch viewExtension = new GamesDataMismatch();
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCGaGamesDataMismatch"),
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}
	}
}