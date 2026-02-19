using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows;
using GameActivity.Views;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using CommonPluginsShared.Models;
using System.Linq;

namespace GameActivity.Services
{
	public class WindowPluginService : IWindowPluginService
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		public string PluginName { get; private set; }

		public IPluginDatabase PluginDatabase { get; private set; }

		public WindowPluginService(string pluginName, IPluginDatabase pluginDatabase)
		{
			PluginName = pluginName;
			PluginDatabase = pluginDatabase;

			if (PluginDatabase == null)
			{
				Logger.Warn("WindowPluginService created with a null PluginDatabase instance.");
			}
		}

		public void ShowPluginGameDataWindow(GenericPlugin plugin, Game gameContext)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				Height = 740,
				MaxWidth = 1280,
				WidthPercent = 80
			};

			var viewExtension = new GameActivityViewSingle((GameActivity)plugin, gameContext);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				PluginName,
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		public void ShowPluginGameDataWindow(GenericPlugin plugin)
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

		public void ShowPluginGameDataWindow(Game gameContext)
		{
			throw new System.NotImplementedException();
		}

		public void ShowPluginGameNoDataWindow()
		{
			throw new System.NotImplementedException();
		}

		public void ShowPluginDataWithoutGame(IEnumerable<DataGame> dataGames)
        {
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				Height = 600,
				Width = 600
			};

			ListDataWithoutGame ViewExtension = new ListDataWithoutGame(dataGames.ToList(), PluginDatabase);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCCommonIsolatedPluginData"), 
				ViewExtension, 
				windowOptions);
			_ = windowExtension.ShowDialog();
		}

		public void ShowPluginTransfertData(IEnumerable<DataGame> dataGames)
        {
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				Height = 200,
				Width = 1000
			};

			TransfertData ViewExtension = new TransfertData(dataGames.ToList(), PluginDatabase);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCCommonSelectTransferData"), 
				ViewExtension, 
				windowOptions);
			_ = windowExtension.ShowDialog();
		}

		public void ShowPluginDataMismatch()
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