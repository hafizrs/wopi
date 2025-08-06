using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetClientInformationQuery
    {
        public string Email { get; set; }
        public IEnumerable<string> PersonaNames { get; set; }
    }
}