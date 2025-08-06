using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ClientInformationResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public List<ClientInformation> Results { get; set; }
    }

    public class ClientInformation
    {
        public string Title { get; set; }
        public string ItemId { get; set; }
        public string Role { get; set; }
        public PraxisImage Logo { get; set; }
    }
}
