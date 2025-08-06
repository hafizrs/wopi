using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class DmsArtifactUsageReferenceDto
    {
        public string RelatedEntityName { get; set; }
        public List<DmsArtifactUsageReferenceDtoModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}