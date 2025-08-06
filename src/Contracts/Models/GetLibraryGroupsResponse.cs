using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class GetLibraryGroupsResponse
    {
        public string OrganizationId { get; set; }
        public List<RiqsLibraryGroupResponse> Groups { get; set; }
        public List<RiqsLibraryGroupResponse> SubGroups { get; set; }
        public List<RiqsLibraryGroupResponse> SubSubGroups { get; set; }
    }
}
