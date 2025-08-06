using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces
{
    public class GetDrivesResponse
    {
        public List<DriveInfo> drives { get; set; }
    }

    public class DriveInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public DateTime createdTime { get; set; }
    }
}
