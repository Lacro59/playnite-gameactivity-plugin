using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    public class RunningActivity
    {
        public Guid Id { get; set; }

        public System.Timers.Timer timer { get; set; }
        public GameActivities GameActivitiesLog { get; set; }
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        public System.Timers.Timer timerBackup { get; set; }
        public ActivityBackup activityBackup { get; set; }

        public ulong PlaytimeOnStarted { get; set; }
    }
}
