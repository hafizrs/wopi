using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces
{
    public class GetSitesResponse
    {
        public List<SiteInfo> value { get; set; }
    }

    public class SiteInfo
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
    }
}
