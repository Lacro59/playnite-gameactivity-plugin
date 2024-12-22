using System;

namespace GameActivity.Models.ExportData
{
    public class ExportData
    {
        public string Name { get; set; }
        public string SourceName { get; set; }
        public int PlayCount { get; set; }
        public ulong Playtime { get; set; }
        public string PlaytimeFormat { get; set; }
        public DateTime? LastSession { get; set; }
        public int AvgCPU { get; set; }
        public int AvgGPU { get; set; }
        public int AvgRAM { get; set; }
        public int AvgFPS { get; set; }
        public int AvgCPUT { get; set; }
        public int AvgGPUT { get; set; }
        public int AvgCPUP { get; set; }
        public int AvgGPUP { get; set; }
    }
}
