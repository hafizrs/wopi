using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SortByModel
    {
        public string PropertyName { get; set; }
        public SortDirectionEnum Direction { get; set; }
    }
}