using CommonPluginsControls.Controls;
using GameActivity.Services;
using Playnite.SDK.Data;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameActivity.Models
{
    public class ListActivities
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        public string GameTitle { get; set; }
        public string GameId { get; set; }
        public Guid Id { get; set; }
        public string GameIcon { get; set; }
        public DateTime GameLastActivity { get; set; }
        public ulong GameElapsedSeconds { get; set; }

        public List<string> DateActivity { get; set; }

        public string GameSourceName { get; set; }
        public string GameSourceIcon { get; set; }

        public string AvgCPU { get; set; }
        public string AvgGPU { get; set; }
        public string AvgRAM { get; set; }
        public string AvgFPS { get; set; }
        public string AvgCPUT { get; set; }
        public string AvgGPUT { get; set; }
        public string AvgCPUP { get; set; }
        public string AvgGPUP { get; set; }

        public bool EnableWarm { get; set; }
        public string MaxCPUT { get; set; }
        public string MaxGPUT { get; set; }
        public string MinFPS { get; set; }
        public string MaxCPU { get; set; }
        public string MaxGPU { get; set; }
        public string MaxRAM { get; set; }

        public int PCConfigurationId { get; set; }
        public string PCName { get; set; }
        public string GameActionName { get; set; }

        public TextBlockWithIconMode TypeStoreIcon { get; set; }
        public string SourceIcon { get; set; }
        public string SourceIconText { get; set; }

        [DontSerialize]
        public RelayCommand<Guid> GoToGame => PluginDatabase.GoToGame;

        [DontSerialize]
        public bool GameExist => PluginDatabase.PlayniteApi.Database.Games.Get(Id) != null;
    }
}
