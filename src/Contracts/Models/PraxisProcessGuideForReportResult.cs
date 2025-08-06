using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisProcessGuideForReportResult
    {
        public List<PraxisProcessGuideForReport> PraxisProcessGuideForReport { get; set; }
        public bool isFileSizeExceeded { get; set; }
    }
}
