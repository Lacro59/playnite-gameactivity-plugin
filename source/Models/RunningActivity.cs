using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace GameActivity.Models
{
    public class RunningActivity
    {
        public Guid Id { get; set; }

        public Timer Timer { get; set; }
        public GameActivities GameActivitiesLog { get; set; }
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        public Timer TimerBackup { get; set; }
        public ActivityBackup ActivityBackup { get; set; }

        public ulong PlaytimeOnStarted { get; set; }
    }
}