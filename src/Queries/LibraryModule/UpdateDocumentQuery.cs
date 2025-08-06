using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class UpdateDocumentQuery
    {
        public string OpenItemConfigId { get; set; }
        public IEnumerable<PraxisOpenItemDocument> DocumentInfo { get; set; }
    }
}
