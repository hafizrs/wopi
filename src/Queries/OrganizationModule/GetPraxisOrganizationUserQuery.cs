using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetPraxisOrganizationUserQuery
    {
        public string OrganizationId { get; set; }
        public List<string> DepartmentIds { get; set; }
        public string SearchKey { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
