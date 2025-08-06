using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetOrganizationBasicInfoQuery
    {
        public string OrganizationId { get; set; }
        public bool UseImpersonate { get; set; }
        public List<string> OrganizationIds { get; set;}
    }
}
