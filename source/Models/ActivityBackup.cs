using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    public class ActivityBackup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ulong ElapsedSeconds { get; set; }
        public string GameActionName { get; set; }
        public int IdConfiguration { get; set; }
        public DateTime DateSession { get; set; }
        public Guid SourceID { get; set; }
        public List<Guid> PlatformIDs { get; set; }
        public List<ActivityDetailsData> ItemsDetailsDatas { get; set; }
    }
}
