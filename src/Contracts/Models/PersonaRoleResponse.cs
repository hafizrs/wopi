using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PersonaRoleResponse
    {
        public int StatusCode { get; set; }
        public List<string> Messages { get; set; }
        public List<string> PersonaRoles { get; set; }
        public List<string> DeleteFeatureRoles { get; set; }
    }
}
