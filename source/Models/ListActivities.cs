using CommonPluginsControls.Controls;
using Playnite.SDK.Data;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using CommonPluginsShared.Commands;

namespace GameActivity.Models
{
    public class ListActivities
    {
        public string GameTitle { get; set; }
        public string GameId { get; set; }
        public Guid Id { get; set; }
        public string GameIcon { get; set; }
        public DateTime GameLastActivity { get; set; }
        public ulong GameElapsedSeconds { get; set; }
        public ulong TimePlayedInMonth { get; set; }

        public List<string> DateActivity { get; set; }

        public string GameSourceName { get; set; }
        public string GameSourceIcon { get; set; }

        public string AvgCPU { get; set; }
        public string AvgGPU { get; set; }
        public string AvgRAM { get; set; }
        public string AvgFPS { get; set; }

        /// <summary>Minimum FPS from logged samples for this session (display only).</summary>
        public string LoggedFpsMin { get; set; }

        /// <summary>Maximum FPS from logged samples for this session (display only).</summary>
        public string LoggedFpsMax { get; set; }

        /// <summary>Median FPS from logged samples for this session (display only).</summary>
        public string LoggedFpsMedian { get; set; }

        /// <summary>Sample standard deviation of FPS from logged samples (display only).</summary>
        public string LoggedFpsStdDev { get; set; }

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
        public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(Id) != null;
    }
}