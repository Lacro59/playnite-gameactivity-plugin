using CommonPluginsShared;
using CommonPluginsShared.Commands;
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace GameActivity.Models
{
    /// <summary>
    /// One day of play time for the Gantt chart.
    /// </summary>
    public class GanttValue : ObservableObject
    {
        public DateTime PlayDate { get; set; }
        public ulong PlayTime { get; set; }
    }

    /// <summary>
    /// Row model for the activity Gantt list (one game).
    /// </summary>
    public class GanttData : ObservableObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public DateTime LastActivity { get; set; }
        public ulong Playtime { get; set; }

        private ulong _playtimeInPerdiod;
        public ulong PlaytimeInPerdiod
        {
            get => _playtimeInPerdiod;
            set => SetValue(ref _playtimeInPerdiod, value);
        }

        public List<GanttValue> DateTimes { get; set; }

        public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

        public bool GameExist => API.Instance.Database.Games.Get(Id) != null;
    }
}
