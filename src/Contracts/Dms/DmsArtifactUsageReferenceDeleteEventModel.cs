using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

public class DmsArtifactUsageReferenceDeleteEventModel
{
    public List<string> ObjectArtifactIds { get; set; }
    public string RelatedEntityId { get; set; }
}