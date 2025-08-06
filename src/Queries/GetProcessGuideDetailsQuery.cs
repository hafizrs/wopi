using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetProcessGuideDetailsQuery
    {
        public IEnumerable<string> ProcessGuideIds { get; set; }
        public string PraxisClientId { get; set; }
        public int TimezoneOffsetInMinutes { get; set; } = 0;
    }
}