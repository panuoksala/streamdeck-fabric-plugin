using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric.models.Fabric
{
    /// <summary>
    /// Generated class from fabric API response.
    /// Describes one job executiong like notebook run.
    /// </summary>
    public class JobItem
    {
        public string id { get; set; }
        public string itemId { get; set; }
        public string jobType { get; set; }
        public string invokeType { get; set; }
        public string status { get; set; }
        public string rootActivityId { get; set; }
        public string startTimeUtc { get; set; }
        public string endTimeUtc { get; set; }
        public string failureReason { get; set; }
    }
}
