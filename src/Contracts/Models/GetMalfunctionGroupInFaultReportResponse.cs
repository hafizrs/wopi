using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class GetMalfunctionGroupInFaultReportResponse
    {
        public string FaultReportId { get; set; }
        public string EquipmentId { get; set; }
        public List<MalfunctionGroupItem> MalfunctionGroup { get; set; }
    }

    public class MalfunctionGroupItem
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
