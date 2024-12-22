using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models.ExportData
{
    public class ExportDataAll
    {
        public string Name { get; set; }
        public string SourceName { get; set; }
        public DateTime? Session { get; set; }
        public DateTime? DateTimeValue { get; set; }
        public ulong Playtime { get; set; }
        public string PlaytimeFormat { get; set; }
        public string PC { get; set; }
        public int CPU { get; set; }
        public int GPU { get; set; }
        public int RAM { get; set; }
        public int FPS { get; set; }
        public int CPUT { get; set; }
        public int GPUT { get; set; }
        public int CPUP { get; set; }
        public int GPUP { get; set; }
    }
}
