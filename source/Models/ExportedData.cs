using System;

namespace GameActivity.Models
{
    public class ExportedData
    {
        // Playnite data
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }

        // Plugin data
        public string SourceName { get; set; }
        public DateTime? DateSession { get; set; }
        public ulong ElapsedSeconds { get; set; }

        // Plugin data logs
        public int FPS { get; set; }
        public int CPU { get; set; }
        public int GPU { get; set; }
        public int RAM { get; set; }
        public int CPUT { get; set; }
        public int GPUT { get; set; }
    }
}
