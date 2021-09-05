using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    public class WarningData
    {
        public string At { get; set; }
        public Data FpsData { get; set; }
        public Data CpuTempData { get; set; }
        public Data GpuTempData { get; set; }
        public Data CpuUsageData { get; set; }
        public Data GpuUsageData { get; set; }
        public Data RamUsageData { get; set; }
    }
    public class Data
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool IsWarm { get; set; }
    }
}
