using CommonPluginsShared;
using GameActivity.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Services
{
    public class LibreHardware
    {
        public static LibreHardwareData GetDataWeb(string ip)
        {
            string url = $"http://{ip}:8085/data.json";
            string webData = Web.DownloadStringData(url).GetAwaiter().GetResult();
            Serialization.TryFromJson<LibreHardwareData>(webData, out LibreHardwareData libreHardwareMonitorData);
            return libreHardwareMonitorData;
        }
    }
}
