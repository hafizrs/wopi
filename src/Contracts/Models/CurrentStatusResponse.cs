using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class CurrentStatusResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public List<string> DependentModules { get; set; }
        public IDictionary<string, List<ItemIdAndTitle>> Values { get; set; }
    }
}
