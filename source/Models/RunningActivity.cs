using System;
using System.Collections.Generic;
using System.Timers;

namespace GameActivity.Models
{
    /// <summary>
    /// In-memory state for a game session currently being tracked.
    /// </summary>
    public class RunningActivity
    {
        /// <summary>Playnite game id.</summary>
        public Guid Id { get; set; }

        /// <summary>Hardware logging timer (session details), when logging is enabled.</summary>
        public Timer Timer { get; set; }

        /// <summary>In-game alarm evaluation timer (independent of logging).</summary>
        public Timer TimerAlarm { get; set; }

        /// <summary>Persisted activity log for the running game.</summary>
        public GameActivities GameActivitiesLog { get; set; }

        /// <summary>End-of-session warning snapshots accumulated during logging ticks.</summary>
        public List<WarningData> WarningsMessage { get; set; } = new List<WarningData>();

        /// <summary>Latest threshold breach detected by the alarm tick (for toast / sound).</summary>
        public WarningData LastAlarmWarning { get; set; }

        /// <summary>Crash-recovery backup timer.</summary>
        public Timer TimerBackup { get; set; }

        /// <summary>Crash-recovery payload.</summary>
        public ActivityBackup ActivityBackup { get; set; }

        /// <summary>Playtime value observed when the session started.</summary>
        public ulong PlaytimeOnStarted { get; set; }

        /// <summary>Correlation id for log lines of this session.</summary>
        public string SessionCorrelationId { get; set; }

        /// <summary>Process id reported by Playnite at game start, when available.</summary>
        public int? StartedProcessId { get; set; }
    }
}
