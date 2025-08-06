using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisGenericReportResult
    {
        public List<MetaData> MetaDataList { get; set; }
        public IEnumerable<string> ClientIds { get; set; } = null;
        public bool IsFileSizeExceeded { get; set; } = false;
        public List<PraxisProcessGuideForReport> PraxisProcessGuidesForReport { get; set; } = null;
        public List<PraxisEquipmentForReport> PraxisEquipmentForReport { get; set;} = null;
        public List<PraxisEquipmentMaintenanceForReport> PraxisEquipmentMaintenanceForReport { get; set; } = null;
    }
}