using System.Collections.Generic;
using ObjectsComparer;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class ChangeLogResponse
    {
        public bool IsChanged { get; set; }
        public int TotalRecordCount { get; set; }
        public List<Difference> Differences { get; set; }
    }
}
